using Anthropic.SDK;
using MediatR.NotificationPublishers;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.Json;
using OpenAI;
using Scalar.AspNetCore;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Billing;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;
using Sparc.Blossom.Engine;
using Sparc.Blossom.Realtime;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddScoped<FriendlyId>();

builder.Services.AddCosmos<SparcEngineContext>(builder.Configuration.GetConnectionString("Cosmos")!, builder.Environment.IsDevelopment() ? "sparc-dev" : "sparc", ServiceLifetime.Scoped);
builder.Services.AddAzureStorage(builder.Configuration.GetConnectionString("Storage")!);

builder.AddSparcAuthentication<BlossomUser>();
builder.AddSparcBilling();
builder.AddSparcChat();
builder.Services.AddScoped(_ => new OpenAIClient(builder.Configuration.GetConnectionString("OpenAI")!));

Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", builder.Configuration.GetConnectionString("Anthropic"));
builder.Services.AddHttpClient<AnthropicClient>().AddStandardResilienceHandler();

builder.AddTovikTranslator();
builder.Services.AddBlossomService<BillToTovik>();

builder.Services.AddMediatR(options =>
{
    options.RegisterServicesFromAssemblyContaining<Program>();
    options.RegisterServicesFromAssemblyContaining<BlossomEvent>();
    options.NotificationPublisher = new TaskWhenAllPublisher();
    options.NotificationPublisherType = typeof(TaskWhenAllPublisher);
});

builder.Services.AddTwilio(builder.Configuration);

builder.Services.AddScoped<ICorsPolicyProvider, SparcEngineDomainPolicyProvider>();
builder.Services.AddCors();

builder.Services.AddHybridCache();
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
});

var app = builder.Build();
app.MapStaticAssets();
app.UseSparcAuthentication<BlossomUser>();
app.UseSparcBilling();
app.UseSparcChat();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors();

app.MapGet("/aura/friendlyid", (FriendlyId friendlyId) => friendlyId.Create());
app.MapGet("/hi", () => "Hi from Sparc!");
app.MapGet("/upgrade-2", async (IRepository<Page> pages, IRepository<SparcDomain> domains, IRepository<UserCharge> charges) =>
{
    var domainsWithTovik = await domains.Query.ToListAsync();
    foreach (var domain in domainsWithTovik)
    {
        var lastTranslated = await charges.Query
            .Where(charges => charges.Domain == domain.Domain)
            .OrderByDescending(charges => charges.Timestamp)
            .Select(charges => charges.Timestamp)
            .FirstOrDefaultAsync();

        if (lastTranslated == default)
            domain.LastTranslatedDate = null;
        else
            domain.LastTranslatedDate = lastTranslated;
        await domains.UpdateAsync(domain);
    }
});

using var scope = app.Services.CreateScope();
scope.ServiceProvider.GetRequiredService<TovikTranslator>().Map(app);

foreach (var translator in scope.ServiceProvider.GetServices<ITranslator>())
    await translator.GetLanguagesAsync();

if (!string.IsNullOrWhiteSpace(app.Configuration.GetConnectionString("Cognitive")))
{
    var translator = scope.ServiceProvider.GetRequiredService<TovikTranslator>();
    await translator.GetLanguagesAsync();
}
app.Run();
