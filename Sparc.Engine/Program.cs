using MediatR.NotificationPublishers;
using Scalar.AspNetCore;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using Sparc.Blossom.Data.Pouch;
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
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .SetIsOriginAllowed((string x) => true)
              .AllowCredentials();
    });
});

var app = builder.Build();
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

using (var scope = app.Services.CreateScope())
{
    var dataRepo = scope.ServiceProvider.GetRequiredService<CosmosDbDynamicRepository<Datum>>();
    var repRepo = scope.ServiceProvider.GetRequiredService<CosmosDbSimpleRepository<ReplicationLog>>();
    var adapter = new CosmosPouchAdapter(dataRepo, repRepo);
    adapter.Map(app);
}


app.Run();
