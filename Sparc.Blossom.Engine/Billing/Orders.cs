using Sparc.Blossom.Authentication;
using Sparc.Blossom.Billing.Stripe;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Billing;

public class Orders(
    BlossomAggregateOptions<SparcOrder> options, 
    StripePaymentService stripe, 
    SparcAuthenticator<BlossomUser> auth,
    IRepository<BlossomUser> users,
    IConfiguration config) 
    : BlossomAggregate<SparcOrder>(options), IBlossomEndpoints
{
    readonly IConfiguration Config = config;

    public async Task<SparcOrder> GetAsync(string id)
    {
        var order = await Repository.FindAsync(id)
            ?? throw new InvalidOperationException($"Order with ID {id} not found.");

        if (order.UserId != User.Id())
            throw new UnauthorizedAccessException("You do not have permission to access this order.");

        return order;
    }

    public async Task<SparcPaymentIntent> StartCheckoutAsync(SparcOrder order)
    {
        order.Currency ??= User.Get("currency") ?? "USD";
        order.UserId = User.Id();

        var intent = await stripe.CreateOrUpdatePaymentIntentAsync(order);
        await Repository.UpdateAsync(order);

        return new SparcPaymentIntent
        {
            ClientSecret = intent.ClientSecret,
            PublishableKey = Config["Stripe:PublishableKey"]!,
            PaymentIntentId = intent.Id,
            Amount = stripe.FromStripePrice(intent.Amount, order.Currency),
            Currency = order.Currency,
            FormattedAmount = SparcCurrency.From(order.Currency).ToString(stripe.FromStripePrice(intent.Amount, order.Currency))
        };
    }

    public async Task<IResult> Fulfill(HttpRequest request)
    {
        var json = await new StreamReader(request.Body).ReadToEndAsync();
        var intent = stripe.GetPaymentIntentFromJson(json, request.Headers["Stripe-Signature"]!, Config["Stripe:WebhookSecret"]!);
        if (intent?.Status == "succeeded")
        {
            var order = await Repository.FindAsync(intent.Metadata["OrderId"]);
            if (order != null && order.FulfilledDate == null)
                await Fulfill(order, order.UserId);
        }

        return Results.Ok();
    }

    public async Task<SparcOrder> Fulfill(string id)
    {
        var order = await Repository.Query.Where(x => x.Id == id && x.FulfilledDate == null).FirstOrDefaultAsync() 
            ?? throw new InvalidOperationException($"Order with ID {id} not found.");
        
        return await Fulfill(order, User.Id());
    }

    private async Task<SparcOrder> Fulfill(SparcOrder order, string userId)
    {
        var product = order.Fulfill();
        await Repository.UpdateAsync(order);
        
        var user = await users.FindAsync(userId);
        user?.Fulfill(product);
        await users.UpdateAsync(user!);
        
        return order;
    }

    public async Task<GetProductResponse> GetProduct(HttpRequest request, string productId, string? currency = null)
    {
        var sparcCurrency = SparcCurrency.From(currency ?? User.Get("currency") ?? request.Headers.AcceptLanguage);

        var product = await stripe.GetProductAsync(productId);
        var price = await stripe.GetPriceAsync(productId, sparcCurrency.Id);

        return new GetProductResponse(productId,
            product.Name,
            price ?? 0,
            sparcCurrency.Id,
            sparcCurrency.ToString(price ?? 0));
    }

    public async Task<SparcCurrency> SetCurrencyAsync(SparcCurrency currency)
    {
        var user = await auth.GetAsync(User);
        user.Avatar.Currency = currency;
        await auth.UpdateAsync(User, user.Avatar);
        return currency;
    }

    public SparcCurrency GetCurrency(HttpRequest request)
        => SparcCurrency.From(User.Get("currency") ?? request.Headers.AcceptLanguage);

    public IEnumerable<SparcCurrency> AllCurrencies() => SparcCurrency.All().Where(x => StripePaymentService.Currencies.Contains(x.Id.ToLower()));

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var billingGroup = endpoints.MapGroup("/billing");

        billingGroup.MapGet("/orders/{id}", async (Orders svc, string id) => await svc.GetAsync(id));
        billingGroup.MapPost("/payments", async (Orders svc, SparcOrder order) => await svc.StartCheckoutAsync(order));
        billingGroup.MapPost("/fulfill", async (Orders svc, HttpRequest request) => await svc.Fulfill(request));
        billingGroup.MapGet("/fulfill/{key}", async (Orders svc, string key) => await svc.Fulfill(key));

        billingGroup.MapGet("/products/{productId}", async (Orders svc, HttpRequest req, string productId, string? currency = null) 
            => await svc.GetProduct(req, productId, currency));

        billingGroup.MapGet("/currency", (Orders svc, HttpRequest req) => svc.GetCurrency(req));
        billingGroup.MapPost("/currency", async (Orders svc, SparcCurrency currency)
            => await svc.SetCurrencyAsync(currency));

        billingGroup.MapGet("/currencies", () => AllCurrencies()).CacheOutput();
    }
}