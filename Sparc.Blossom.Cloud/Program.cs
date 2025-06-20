using MediatR.NotificationPublishers;
using Scalar.AspNetCore;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Cloud.Billing;
using Sparc.Blossom.Cloud.Tools;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;
using Sparc.Blossom.Payment.Stripe;
using Sparc.Notifications.Twilio;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddScoped<FriendlyId>();
builder.Services.AddScoped<FriendlyUsername>();

builder.Services.AddCosmos<BlossomCloudContext>(builder.Configuration.GetConnectionString("Cosmos")!, "BlossomCloud", ServiceLifetime.Scoped);

builder.AddBlossomCloudAuthentication<BlossomUser>();
builder.AddBlossomCloudTranslation();
builder.AddBlossomCloudBilling();

builder.Services.AddMediatR(options =>
{
    options.RegisterServicesFromAssemblyContaining<Program>();
    options.RegisterServicesFromAssemblyContaining<BlossomEvent>();
    options.NotificationPublisher = new TaskWhenAllPublisher();
    options.NotificationPublisherType = typeof(TaskWhenAllPublisher);
});

builder.Services.AddTwilio(builder.Configuration);

var app = builder.Build();
app.UseBlossomCloudAuthentication<BlossomUser>();
app.UseBlossomCloudTranslation();
app.UseBlossomCloudBilling();

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
