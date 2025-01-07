using Sparc.Blossom.Example.Single;
using TodoItems;

var builder = BlossomApplication.CreateBuilder(args);

builder.Services.AddScoped<IRandomStringGenerator, RandomStringGenerator>();

var app = builder.Build();

await app.RunAsync<Html>();
