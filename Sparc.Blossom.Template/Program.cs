using Sparc.Blossom;
using Sparc.Blossom.Template;

var builder = BlossomApplication.CreateBuilder(args);

var app = builder.Build();
await app.RunAsync<Html>();
