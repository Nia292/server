using SinusSynchronousShared.Services;
using SinusSynchronousShared.Utils.Configuration;

namespace SinusSynchronousStaticFilesServer;

public class Program
{
    public static void Main(string[] args)
    {
        var hostBuilder = CreateHostBuilder(args);
        var host = hostBuilder.Build();

        using (var scope = host.Services.CreateScope())
        {
            var options = host.Services.GetService<IConfigurationService<StaticFilesServerConfiguration>>();
            var optionsServer = host.Services.GetService<IConfigurationService<SinusConfigurationBase>>();
            var logger = host.Services.GetService<ILogger<Program>>();
            logger.LogInformation("Loaded SinusSynchronous Static Files Server Configuration (IsMain: {isMain})", options.IsMain);
            logger.LogInformation(options.ToString());
            logger.LogInformation("Loaded SinusSynchronous Server Auth Configuration (IsMain: {isMain})", optionsServer.IsMain);
            logger.LogInformation(optionsServer.ToString());
        }

        host.Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
        });
        var logger = loggerFactory.CreateLogger<Startup>();
        return Host.CreateDefaultBuilder(args)
            .UseSystemd()
            .UseConsoleLifetime()
            .ConfigureAppConfiguration((ctx, config) =>
            {
                config.AddJsonFile("base.appsettings.json", optional: true, reloadOnChange: true);
                var appSettingsPath = Environment.GetEnvironmentVariable("APPSETTINGS_PATH");
                if (!string.IsNullOrEmpty(appSettingsPath))
                {
                    config.AddJsonFile(appSettingsPath, optional: true, reloadOnChange: true);
                }
                else
                {
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                }

                config.AddEnvironmentVariables();
            })
            .ConfigureLogging((ctx, builder) =>
            {
                builder.AddConfiguration(ctx.Configuration.GetSection("Logging"));
                builder.AddFile(o => o.RootPath = AppContext.BaseDirectory);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseContentRoot(AppContext.BaseDirectory);
                webBuilder.UseStartup(ctx => new Startup(ctx.Configuration, logger));
            });
    }
}