using Scalar.AspNetCore;
using Sparc.Blossom.Cloud.Tools;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddScoped<FriendlyId>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapGet("/tools/friendlyid", (FriendlyId friendlyId) => friendlyId.Create());

app.Run();
