using MediatR.NotificationPublishers;
using Microsoft.AspNetCore.Http.Json;
using Scalar.AspNetCore;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using Sparc.Engine;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddScoped<FriendlyId>();

builder.Services.AddCosmos<SparcEngineContext>(builder.Configuration.GetConnectionString("Cosmos")!, "sparc", ServiceLifetime.Scoped);

builder.AddSparcEngineAuthentication<BlossomUser>();
builder.AddSparcEngineTranslation();

builder.Services.AddMediatR(options =>
{
    options.RegisterServicesFromAssemblyContaining<Program>();
    options.RegisterServicesFromAssemblyContaining<BlossomEvent>();
    options.NotificationPublisher = new TaskWhenAllPublisher();
    options.NotificationPublisherType = typeof(TaskWhenAllPublisher);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:7047")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .SetIsOriginAllowed((string x) => true)
              .AllowCredentials();
    });
});

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
});

var app = builder.Build();
app.MapStaticAssets();
app.UseSparcEngineAuthentication<BlossomUser>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors();

app.MapGet("/tools/friendlyid", (FriendlyId friendlyId) => friendlyId.Create());
app.MapGet("/hi", () => "Hi from Sparc!");

using var scope = app.Services.CreateScope();
scope.ServiceProvider.GetRequiredService<Contents>().Map(app);

if (!string.IsNullOrWhiteSpace(app.Configuration.GetConnectionString("Cognitive")))
{
    var translator = scope.ServiceProvider.GetRequiredService<KoriTranslator>();
    await translator.GetLanguagesAsync();
}
app.Run();
