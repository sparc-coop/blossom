using Sparc.Blossom;
using Sparc.Blossom.Example.Single;

var builder = WebApplication.CreateBuilder(args);
builder.AddBlossom();

var app = builder.UseBlossom<App>();
app.Run();
