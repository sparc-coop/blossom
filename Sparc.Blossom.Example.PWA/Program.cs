using Sparc.Blossom.Example.PWA;
using TodoItems;
using Sparc.Blossom.Data;

var builder = BlossomApplication.CreateBuilder<App>(args);

builder.Services.AddScoped<IRandomStringGenerator, RandomStringGenerator>();
builder.Services.AddPouch();

var app = builder.Build();

await app.RunAsync();

