using Anthropic.SDK;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.Json;
using OpenAI;
using Scalar.AspNetCore;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Billing;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;
using Sparc.Blossom.Engine;
using Sparc.Blossom.Plugins.Slack;
using Sparc.Blossom.Realtime;
using Sparc.Blossom.Spaces;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddScoped<FriendlyId>();

builder.Services.AddCosmos<SparcEngineContext>(builder.Configuration.GetConnectionString("Cosmos")!, builder.Environment.IsDevelopment() ? "sparc-dev" : "sparc", ServiceLifetime.Scoped);
builder.Services.AddAzureStorage(builder.Configuration.GetConnectionString("Storage")!);

builder.AddSparcAuthentication<BlossomUser>();
builder.AddSparcBilling();
builder.AddSparcSpaces();
builder.Services.AddScoped(_ => new OpenAIClient(builder.Configuration.GetConnectionString("OpenAI")!));

builder.Services.AddSlackIntegration();

Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", builder.Configuration.GetConnectionString("Anthropic"));
builder.Services.AddHttpClient<AnthropicClient>().AddStandardResilienceHandler();

builder.AddSparcContent();
builder.AddBlossomRealtime();

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
app.UseSparcSpaces();

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

using var scope = app.Services.CreateScope();
scope.ServiceProvider.GetRequiredService<Contents>().Map(app);

foreach (var translator in scope.ServiceProvider.GetServices<ITranslator>())
    await translator.GetLanguagesAsync();

app.Run();
