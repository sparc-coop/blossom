using Sparc.Blossom.Example.Single;
using TodoItems;
using Sparc.Blossom.Data;

var builder = BlossomApplication.CreateBuilder<Html>(args);

builder.Services.AddScoped<IRandomStringGenerator, RandomStringGenerator>();
builder.Services.AddPouch();

var app = builder.Build();

await app.RunAsync<Html>();
