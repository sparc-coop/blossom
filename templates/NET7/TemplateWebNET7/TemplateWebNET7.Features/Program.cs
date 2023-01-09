var builder = WebApplication.CreateBuilder(args);

builder.AddBlossom(builder.Configuration["WebClientUrl"]);

#if (AddAzureStorage)
builder.Services.AddAzureStorage(builder.Configuration.GetConnectionString("Storage")!);
#endif

#if (AddSQL)
builder.Services.AddSqlServer<TemplateWebNET7Context>(builder.Configuration.GetConnectionString("Database")!, ServiceLifetime.Transient);
#endif

#if (AddCosmos)
builder.Services.AddCosmos<TemplateWebNET7Context>(builder.Configuration.GetConnectionString("Database")!, "TemplateWebNET7db", ServiceLifetime.Transient);
#endif

#if (AddAzureADAuth)
var auth = builder.Services.AddAzureADB2CAuthentication<User>(builder.Configuration);
builder.AddPasswordlessAuthentication<User>(auth);
#endif

#if (AddTwilio)
builder.Services.AddTwilio(builder.Configuration);
#endif

#if (AddAzureNotifications)
builder.Services.AddAzurePushNotifications(builder.Configuration.GetSection("AzureNotifications"));
#endif

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

app.UsePasswordlessAuthentication<User>();

app.Run();
