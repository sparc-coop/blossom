using Sparc.Blossom;
using Sparc.Blossom.Server.Example;

var builder = WebApplication.CreateBuilder(args);
builder.AddBlossom();

var app = builder.UseBlossom<App>();
app.Run();
