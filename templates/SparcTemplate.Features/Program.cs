var builder = WebApplication.CreateBuilder(args);

builder.Services.Sparcify<Program>(builder.Configuration["ClientUrl"]).AddAzureADB2CAuthentication(builder.Configuration);

builder.Services.AddRazorPages();

var app = builder.Build();

app.Sparcify<Program>(app.Environment);

app.Run();
