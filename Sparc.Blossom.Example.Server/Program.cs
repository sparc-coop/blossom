using Sparc.Blossom.Example.Single;
using TodoItems;
using Sparc.Kori;

var builder = BlossomApplication.CreateBuilder(args);

builder.Services.AddScoped<IRandomStringGenerator, RandomStringGenerator>();
builder.Services.AddKoriDb();

var app = builder.Build();

await app.RunAsync<Html>();
