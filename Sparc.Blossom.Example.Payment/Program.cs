using Sparc.Blossom.Example.Payment.Components;
using Sparc.Blossom.Payment.Stripe;
using Sparc.Blossom.Payment.StripeIntegration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddStripePayments(options =>
{
    options.ApiKey = builder.Configuration["Stripe:ApiKey"] ?? "sk_test_123";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapStripeWebhookEndpoint("/webhook/stripe", context =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Sparc.Blossom.Payment.StripeIntegration.StripeWebhookHandler>>();
    var secret = builder.Configuration["Stripe:WebhookSecret"] ?? "whsec_test_123";
    return new Sparc.Blossom.Payment.StripeIntegration.StripeWebhookHandler(secret, logger);
});

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
