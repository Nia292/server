using SinusSynchronousShared.Services;
using SinusSynchronousShared.Utils.Configuration;

namespace SinusSynchronousAuthService;

public class Program
{
    public static void Main(string[] args)
    {
        var hostBuilder = CreateHostBuilder(args);
        using var host = hostBuilder.Build();
        using (var scope = host.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var options = services.GetRequiredService<IConfigurationService<AuthServiceConfiguration>>();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            if (options.IsMain)
            {
                logger.LogInformation(options.ToString());
            } else
            {
                logger.LogInformation(options.ToString());
            }
        }
            try
            {
                host.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
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
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseContentRoot(AppContext.BaseDirectory);
                webBuilder.ConfigureLogging((ctx, builder) =>
                {
                    builder.AddConfiguration(ctx.Configuration.GetSection("Logging"));
                    builder.AddFile(o => o.RootPath = AppContext.BaseDirectory);
                });
                webBuilder.UseStartup(ctx => new Startup(ctx.Configuration, logger));
            });
    }
}
