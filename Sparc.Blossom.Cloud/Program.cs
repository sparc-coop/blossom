using MediatR.NotificationPublishers;
using Scalar.AspNetCore;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Cloud.Tools;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;
using Sparc.Blossom.Data.Pouch;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddScoped<FriendlyId>();

builder.Services.AddCosmos<BlossomCloudContext>(builder.Configuration.GetConnectionString("Cosmos")!, "BlossomCloud", ServiceLifetime.Scoped);

builder.AddBlossomCloudAuthentication<BlossomUser>();
builder.AddBlossomCloudTranslation();

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
app.UseBlossomCloudAuthentication<BlossomUser>();

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
    var dataRepo = scope.ServiceProvider.GetRequiredService<CosmosDbSimpleRepository<Datum>>();
    var repRepo = scope.ServiceProvider.GetRequiredService<CosmosDbSimpleRepository<ReplicationLog>>();
    var adapter = new CosmosPouchAdapter(dataRepo, repRepo);
    adapter.Map(app);
}


app.Run();
