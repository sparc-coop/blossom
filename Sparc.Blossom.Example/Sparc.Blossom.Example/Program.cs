using Sparc.Blossom;
using Sparc.Blossom.Example.Server.Components;

var builder = WebApplication.CreateBuilder(args);
builder.AddBlossom();

var app = builder.UseBlossom<App>(typeof(Sparc.Blossom.Example.Client._Imports).Assembly);
app.Run();
