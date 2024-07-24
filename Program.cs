using appman.DataModels;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json.Serialization;

namespace appman;

public class Program {
    public static IConfiguration Setting { get; private set; }

    public static void Main(string[] args) {
        Setting = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var builder = WebApplication.CreateBuilder(args);
        builder.Services.Configure<JsonOptions>(opt => {
            opt.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
		builder.Services.AddCors(options => {
			options.AddPolicy(name: "a", policy => {
				policy.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            });
		});
        builder.Services.AddDbContext<ApplicationContext>();
        var app = builder.Build();
		app.UseDefaultFiles(new DefaultFilesOptions {
			RequestPath = new PathString("/appman")
		});
		app.UseStaticFiles(new StaticFileOptions {
			RequestPath = new PathString("/appman")
		});
		app.UseCors("a");
		
        app.UseExceptionHandler(c => c.Run(async context => {
            var exception = context.Features.Get<IExceptionHandlerPathFeature>().Error;
            await context.Response.WriteAsJsonAsync(APIResponse<object>.GetError(10, exception.Message.Trim()));
        }));

        // Appman API
        app.Map("/appman/api/auth.getAccessToken", Auth.GetAccessTokenAsync);
        app.Map("/appman/api/users.get", Users.GetAsync);
        app.Map("/appman/api/users.createInvite", Users.CreateInviteAsync);
        app.Map("/appman/api/users.getInvites", Users.GetInvitesAsync);

        //app.Map("/appman/api/getApps", AppMan.GetAppsAsync);
        //app.Map("/appman/api/createApp", AppMan.CreateAppAsync);
        //app.Map("/appman/api/deleteApp", AppMan.DeleteAppAsync);
        //app.Map("/appman/api/getAppBranches", AppMan.GetAppBranchesAsync);

        // app.Map("/appman/api/{method}", (string method) => Results.Json(APIResponse<object>.GetError(11, $"method {method} not found.")));
        app.Map("/appman/api/{method}", () => Results.Json(APIResponse<object>.GetError(11)));
        app.Map("/appman/api", () => Results.Json(APIResponse<object>.GetError(11)));

        // Appman registration
        app.Map("/appman/register", Users.RegisterNewAsync);

        // Other
        app.Map("/", () => Results.NotFound());
        app.Run();
    }
}