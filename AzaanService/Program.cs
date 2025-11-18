using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace AzaanService
{
    using Core;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            
            // Add systemd support
            builder.Host.UseSystemd();
            // Configure logging
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            
            // Configure INI file
            builder.Configuration.AddIniFile("azaan.conf", optional: false, reloadOnChange: true);
            
            // Configure services
            builder.Services.AddHostedService<Worker>();
            builder.Services.AddSingleton<ICaster, CasterSet>();
            builder.Services.AddSingleton<IFileManager, FileManager>();
            builder.Services.AddCors();

            WebApplication app = builder.Build();
            
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Starting Azaan Service");
            
            // Configure static files
            var resourcePath = builder.Configuration["azaan:source"];
            if (!Path.Exists(resourcePath))
            {
                resourcePath = builder.Configuration["azaan:backupsource"] ?? Path.Combine(Directory.GetCurrentDirectory(), "resources");
            }

            logger.LogInformation("Serving static files from: {ResourcePath}", resourcePath);
            
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(resourcePath),
                RequestPath = PathString.Empty,
                OnPrepareResponse = context => logger.LogInformation("Something relevant {context}", context),
                ServeUnknownFileTypes = true
            });

            app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

            logger.LogInformation("Listening on http://*:13337");
            app.Run("http://0.0.0.0:13337");
        }
    }
}