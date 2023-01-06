var builder = WebApplication.CreateBuilder(args);

builder.AddBlossom(builder.Configuration["WebClientUrl"]);

@*#if (AddAzureStorage)
builder.Services.AddAzureStorage(builder.Configuration.GetConnectionString("Storage")!);
#endif*@

builder.Services.AddServerSideBlazor();
builder.Services.AddOutputCache();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseBlossom();
app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToFile("index.html");

if (builder.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

app.Run();
