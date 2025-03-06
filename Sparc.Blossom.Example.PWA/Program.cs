using Sparc.Blossom.Example.PWA;
using TodoItems;
using Sparc.Kori;
using Sparc.Blossom.Example.PWA.Shared;

var builder = BlossomApplication.CreateBuilder<App>(args);

builder.Services.AddScoped<IRandomStringGenerator, RandomStringGenerator>();
builder.Services.AddKoriDb();

var app = builder.Build();

await app.RunAsync();

