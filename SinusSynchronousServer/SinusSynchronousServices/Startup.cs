using SinusSynchronousServices.Discord;
using SinusSynchronousShared.Data;
using SinusSynchronousShared.Metrics;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using SinusSynchronousShared.Utils;
using SinusSynchronousShared.Services;
using StackExchange.Redis;
using SinusSynchronousShared.Utils.Configuration;

namespace SinusSynchronousServices;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var config = app.ApplicationServices.GetRequiredService<IConfigurationService<SinusConfigurationBase>>();

        var metricServer = new KestrelMetricServer(config.GetValueOrDefault<int>(nameof(SinusConfigurationBase.MetricsPort), 4982));
        metricServer.Start();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var sinusConfig = Configuration.GetSection("SinusSynchronous");

        services.AddDbContextPool<SinusDbContext>(options =>
        {
            options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"), builder =>
            {
                builder.MigrationsHistoryTable("_efmigrationshistory", "public");
            }).UseSnakeCaseNamingConvention();
            options.EnableThreadSafetyChecks(false);
        }, Configuration.GetValue(nameof(SinusConfigurationBase.DbContextPoolSize), 1024));
        services.AddDbContextFactory<SinusDbContext>(options =>
        {
            options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"), builder =>
            {
                builder.MigrationsHistoryTable("_efmigrationshistory", "public");
                builder.MigrationsAssembly("SinusSynchronousShared");
            }).UseSnakeCaseNamingConvention();
            options.EnableThreadSafetyChecks(false);
        });

        services.AddSingleton(m => new SinusMetrics(m.GetService<ILogger<SinusMetrics>>(), new List<string> { },
        new List<string> { }));

        var redis = sinusConfig.GetValue(nameof(ServerConfiguration.RedisConnectionString), string.Empty);
        var options = ConfigurationOptions.Parse(redis);
        options.ClientName = "Sinus";
        options.ChannelPrefix = "UserData";
        ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(options);
        services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);

        services.Configure<ServicesConfiguration>(Configuration.GetRequiredSection("SinusSynchronous"));
        services.Configure<ServerConfiguration>(Configuration.GetRequiredSection("SinusSynchronous"));
        services.Configure<SinusConfigurationBase>(Configuration.GetRequiredSection("SinusSynchronous"));
        services.AddSingleton(Configuration);
        services.AddSingleton<ServerTokenGenerator>();
        services.AddSingleton<DiscordBotServices>();
        services.AddHostedService<DiscordBot>();
        services.AddSingleton<IConfigurationService<ServicesConfiguration>, SinusConfigurationServiceServer<ServicesConfiguration>>();
        services.AddSingleton<IConfigurationService<ServerConfiguration>, SinusConfigurationServiceClient<ServerConfiguration>>();
        services.AddSingleton<IConfigurationService<SinusConfigurationBase>, SinusConfigurationServiceClient<SinusConfigurationBase>>();

        services.AddHostedService(p => (SinusConfigurationServiceClient<SinusConfigurationBase>)p.GetService<IConfigurationService<SinusConfigurationBase>>());
        services.AddHostedService(p => (SinusConfigurationServiceClient<ServerConfiguration>)p.GetService<IConfigurationService<ServerConfiguration>>());
    }
}