using PasswordlessExample;
using PasswordlessExample.Client.Pages;
using PasswordlessExample.Components;
using Sparc.Blossom.Authenticator;
using Sparc.Blossom.Data;
using System.Drawing;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddHttpContextAccessor();

builder.Services.AddSqlServer<ShapeContext>(builder.Configuration.GetConnectionString("Shape")!, ServiceLifetime.Scoped);

builder.Services.AddPasswordlessAuthentication<User>(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(PasswordlessExample.Client._Imports).Assembly);

app.Run();
