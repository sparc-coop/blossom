using Sparc.Blossom;

var builder = WebApplication.CreateBuilder(args);
builder.AddBlossom();

var app = builder.BuildBlossom();
app.Run();
