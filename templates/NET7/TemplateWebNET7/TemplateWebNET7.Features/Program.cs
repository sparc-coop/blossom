var builder = WebApplication.CreateBuilder(args);

builder.AddSparcKernel(builder.Configuration["WebClientUrl"]);

var app = builder.Build();

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseSparcKernel();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
