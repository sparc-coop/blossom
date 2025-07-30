using Sparc.Blossom;
using Sparc.Blossom.Chat.Example;
using Sparc.Engine;

var builder = BlossomApplication.CreateBuilder<Html>(args);
builder.Services.AddSparcEngine("https://localhost:7185");
var app = builder.Build();

await app.RunAsync<Html>();
