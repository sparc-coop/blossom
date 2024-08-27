using PasswordlessProject;
using Sparc.Blossom;
using Sparc.Blossom.Data;
using PasswordlessProject.Components;
using Sparc.Blossom.Authentication.Passwordless;

BlossomApplication.Run<App, User>(args,
    builder =>
    {
        builder.Services.AddCosmos<KodekitContext>(builder.Configuration["ConnectionStrings:CosmosDb"]!, "kodekit", ServiceLifetime.Scoped);

        //builder.AddBlossomPasswordlessAuthentication<User>();
        // Change default cookie to expire in 30 days
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
        });

        //builder.Services.AddPasswordless<User>(builder.Configuration);
        //builder.Services.AddScoped<UserRepository>();
    },
    app =>
    {

    });