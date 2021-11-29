var builder = WebApplication.CreateBuilder(args);

builder.Services.Sparcify<Program>().AddAuthentication().AddJwtBearer();

builder.Services.AddRazorPages();

var app = builder.Build();

app.Sparcify<Program>(app.Environment);

app.Run();
