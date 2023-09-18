using Sparc.Blossom;
using Sparc.Blossom.Example.Single;

var builder = WebApplication.CreateBuilder(args);
builder.AddBlossom<App>();

var app = builder.UseBlossom<App>();
app.Run();
