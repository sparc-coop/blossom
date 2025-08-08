using Sparc.Blossom;
using Sparc.Blossom.Chat.Example;
using Sparc.Blossom.Engine;

var builder = BlossomApplication.CreateBuilder<Html>(args);
builder.Services.AddBlossomEngine("https://localhost:7185");
var app = builder.Build();

await app.RunAsync<Html>();
