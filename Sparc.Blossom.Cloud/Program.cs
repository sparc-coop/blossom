using MediatR.NotificationPublishers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Scalar.AspNetCore;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Cloud.Tools;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddScoped<FriendlyId>();
builder.Services.AddScoped<FriendlyUsername>();

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

var app = builder.Build();
app.UseBlossomCloudAuthentication<BlossomUser>();
app.UseBlossomCloudTranslation();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapGet("/tools/friendlyid", (FriendlyId friendlyId) => friendlyId.Create());
app.MapGet("/tools/friendlyusername", (FriendlyUsername friendlyUsername) => friendlyUsername.GetRandomName());
app.Run();
