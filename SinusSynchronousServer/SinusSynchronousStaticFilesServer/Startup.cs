using SinusSynchronousShared.Data;
using SinusSynchronousShared.Metrics;
using SinusSynchronousShared.Services;
using SinusSynchronousShared.Utils;
using SinusSynchronousStaticFilesServer.Controllers;
using SinusSynchronousStaticFilesServer.Services;
using SinusSynchronousStaticFilesServer.Utils;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.System.Text.Json;
using StackExchange.Redis;
using System.Net;
using System.Text;
using SinusSynchronousShared.Utils.Configuration;

namespace SinusSynchronousStaticFilesServer;

public class Startup
{
    private bool _isMain;
    private bool _isDistributionNode;
    private readonly ILogger<Startup> _logger;

    public Startup(IConfiguration configuration, ILogger<Startup> logger)
    {
        Configuration = configuration;
        _logger = logger;
        var sinusSettings = Configuration.GetRequiredSection("SinusSynchronous");
        _isDistributionNode = sinusSettings.GetValue(nameof(StaticFilesServerConfiguration.IsDistributionNode), false);
        _isMain = string.IsNullOrEmpty(sinusSettings.GetValue(nameof(StaticFilesServerConfiguration.MainFileServerAddress), string.Empty)) && _isDistributionNode;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddLogging();

        services.Configure<StaticFilesServerConfiguration>(Configuration.GetRequiredSection("SinusSynchronous"));
        services.Configure<SinusConfigurationBase>(Configuration.GetRequiredSection("SinusSynchronous"));
        services.Configure<KestrelServerOptions>(Configuration.GetSection("Kestrel"));
        services.AddSingleton(Configuration);

        var sinusConfig = Configuration.GetRequiredSection("SinusSynchronous");

        // metrics configuration
        services.AddSingleton(m => new SinusMetrics(m.GetService<ILogger<SinusMetrics>>(), new List<string>
        {
            MetricsAPI.CounterFileRequests,
            MetricsAPI.CounterFileRequestSize
        }, new List<string>
        {
            MetricsAPI.GaugeFilesTotalColdStorage,
            MetricsAPI.GaugeFilesTotalSizeColdStorage,
            MetricsAPI.GaugeFilesTotalSize,
            MetricsAPI.GaugeFilesTotal,
            MetricsAPI.GaugeFilesUniquePastDay,
            MetricsAPI.GaugeFilesUniquePastDaySize,
            MetricsAPI.GaugeFilesUniquePastHour,
            MetricsAPI.GaugeFilesUniquePastHourSize,
            MetricsAPI.GaugeCurrentDownloads,
            MetricsAPI.GaugeDownloadQueue,
            MetricsAPI.GaugeDownloadQueueCancelled,
            MetricsAPI.GaugeDownloadPriorityQueue,
            MetricsAPI.GaugeDownloadPriorityQueueCancelled,
            MetricsAPI.GaugeQueueFree,
            MetricsAPI.GaugeQueueInactive,
            MetricsAPI.GaugeQueueActive,
            MetricsAPI.GaugeFilesDownloadingFromCache,
            MetricsAPI.GaugeFilesTasksWaitingForDownloadFromCache
        }));

        // generic services
        services.AddSingleton<CachedFileProvider>();
        services.AddSingleton<FileStatisticsService>();
        services.AddSingleton<RequestFileStreamResultFactory>();
        services.AddSingleton<ServerTokenGenerator>();
        services.AddSingleton<RequestQueueService>();
        services.AddHostedService(p => p.GetService<RequestQueueService>());
        services.AddHostedService(m => m.GetService<FileStatisticsService>());
        services.AddSingleton<IConfigurationService<SinusConfigurationBase>, SinusConfigurationServiceClient<SinusConfigurationBase>>();
        services.AddHostedService(p => (SinusConfigurationServiceClient<SinusConfigurationBase>)p.GetService<IConfigurationService<SinusConfigurationBase>>());

        // specific services
        if (_isMain)
        {
            services.AddSingleton<IClientReadyMessageService, MainClientReadyMessageService>();
            services.AddHostedService<MainFileCleanupService>();
            services.AddSingleton<IConfigurationService<StaticFilesServerConfiguration>, SinusConfigurationServiceServer<StaticFilesServerConfiguration>>();
            services.AddSingleton<MainServerShardRegistrationService>();
            services.AddHostedService(s => s.GetRequiredService<MainServerShardRegistrationService>());
            services.AddDbContextPool<SinusDbContext>(options =>
            {
                options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"), builder =>
                {
                    builder.MigrationsHistoryTable("_efmigrationshistory", "public");
                }).UseSnakeCaseNamingConvention();
                options.EnableThreadSafetyChecks(false);
            }, sinusConfig.GetValue(nameof(SinusConfigurationBase.DbContextPoolSize), 1024));
            services.AddDbContextFactory<SinusDbContext>(options =>
            {
                options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"), builder =>
                {
                    builder.MigrationsHistoryTable("_efmigrationshistory", "public");
                    builder.MigrationsAssembly("SinusSynchronousShared");
                }).UseSnakeCaseNamingConvention();
                options.EnableThreadSafetyChecks(false);
            });

            var signalRServiceBuilder = services.AddSignalR(hubOptions =>
            {
                hubOptions.MaximumReceiveMessageSize = long.MaxValue;
                hubOptions.EnableDetailedErrors = true;
                hubOptions.MaximumParallelInvocationsPerClient = 10;
                hubOptions.StreamBufferCapacity = 200;
            }).AddMessagePackProtocol(opt =>
            {
                var resolver = CompositeResolver.Create(StandardResolverAllowPrivate.Instance,
                    BuiltinResolver.Instance,
                    AttributeFormatterResolver.Instance,
                    // replace enum resolver
                    DynamicEnumAsStringResolver.Instance,
                    DynamicGenericResolver.Instance,
                    DynamicUnionResolver.Instance,
                    DynamicObjectResolver.Instance,
                    PrimitiveObjectResolver.Instance,
                    // final fallback(last priority)
                    StandardResolver.Instance);

                opt.SerializerOptions = MessagePackSerializerOptions.Standard
                    .WithCompression(MessagePackCompression.Lz4Block)
                    .WithResolver(resolver);
            });

            // configure redis for SignalR
            var redisConnection = sinusConfig.GetValue(nameof(ServerConfiguration.RedisConnectionString), string.Empty);
            signalRServiceBuilder.AddStackExchangeRedis(redisConnection, options => { });

            var options = ConfigurationOptions.Parse(redisConnection);

            var endpoint = options.EndPoints[0];
            string address = "";
            int port = 0;
            if (endpoint is DnsEndPoint dnsEndPoint) { address = dnsEndPoint.Host; port = dnsEndPoint.Port; }
            if (endpoint is IPEndPoint ipEndPoint) { address = ipEndPoint.Address.ToString(); port = ipEndPoint.Port; }
            var redisConfiguration = new RedisConfiguration()
            {
                AbortOnConnectFail = true,
                KeyPrefix = "",
                Hosts = new RedisHost[]
                {
                new RedisHost(){ Host = address, Port = port },
                },
                AllowAdmin = true,
                ConnectTimeout = options.ConnectTimeout,
                Database = 0,
                Ssl = false,
                Password = options.Password,
                ServerEnumerationStrategy = new ServerEnumerationStrategy()
                {
                    Mode = ServerEnumerationStrategy.ModeOptions.All,
                    TargetRole = ServerEnumerationStrategy.TargetRoleOptions.Any,
                    UnreachableServerAction = ServerEnumerationStrategy.UnreachableServerActionOptions.Throw,
                },
                MaxValueLength = 1024,
                PoolSize = sinusConfig.GetValue(nameof(ServerConfiguration.RedisPool), 50),
                SyncTimeout = options.SyncTimeout,
            };

            services.AddStackExchangeRedisExtensions<SystemTextJsonSerializer>(redisConfiguration);
        }
        else
        {
            services.AddSingleton<ShardRegistrationService>();
            services.AddHostedService(s => s.GetRequiredService<ShardRegistrationService>());
            services.AddSingleton<IClientReadyMessageService, ShardClientReadyMessageService>();
            services.AddHostedService<ShardFileCleanupService>();
            services.AddSingleton<IConfigurationService<StaticFilesServerConfiguration>, SinusConfigurationServiceClient<StaticFilesServerConfiguration>>();
            services.AddHostedService(p => (SinusConfigurationServiceClient<StaticFilesServerConfiguration>)p.GetService<IConfigurationService<StaticFilesServerConfiguration>>());
        }

        services.AddMemoryCache();

        // controller setup
        services.AddControllers().ConfigureApplicationPartManager(a =>
        {
            a.FeatureProviders.Remove(a.FeatureProviders.OfType<ControllerFeatureProvider>().First());
            if (_isMain)
            {
                a.FeatureProviders.Add(new AllowedControllersFeatureProvider(typeof(SinusStaticFilesServerConfigurationController),
                    typeof(CacheController), typeof(RequestController), typeof(ServerFilesController),
                    typeof(DistributionController), typeof(MainController), typeof(SpeedTestController)));
            }
            else if (_isDistributionNode)
            {
                a.FeatureProviders.Add(new AllowedControllersFeatureProvider(typeof(CacheController), typeof(RequestController), typeof(DistributionController), typeof(SpeedTestController)));
            }
            else
            {
                a.FeatureProviders.Add(new AllowedControllersFeatureProvider(typeof(CacheController), typeof(RequestController), typeof(SpeedTestController)));
            }
        });

        // authentication and authorization 
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IConfigurationService<SinusConfigurationBase>>((o, s) =>
            {
                o.TokenValidationParameters = new()
                {
                    ValidateIssuer = false,
                    ValidateLifetime = true,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(s.GetValue<string>(nameof(SinusConfigurationBase.Jwt))))
                    {
                        KeyId = sinusConfig.GetValue<string>(nameof(SinusConfigurationBase.JwtKeyId)),
                    },
                };
            });
        services.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer();
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            options.AddPolicy("Internal", new AuthorizationPolicyBuilder().RequireClaim(SinusClaimTypes.Internal, "true").Build());
        });
        services.AddSingleton<IUserIdProvider, IdBasedUserIdProvider>();

        services.AddHealthChecks();
        services.AddHttpLogging(e => e = new Microsoft.AspNetCore.HttpLogging.HttpLoggingOptions());
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseHttpLogging();

        app.UseRouting();

        var config = app.ApplicationServices.GetRequiredService<IConfigurationService<SinusConfigurationBase>>();

#pragma warning disable IDISP001 // Dispose created
#pragma warning disable IDISP004 // Don't ignore created IDisposable
        var metricServer = new KestrelMetricServer(config.GetValueOrDefault<int>(nameof(SinusConfigurationBase.MetricsPort), 4981))
            .Start();
#pragma warning restore IDISP004 // Don't ignore created IDisposable
#pragma warning restore IDISP001 // Dispose created

        app.UseHttpMetrics(); 

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(e =>
        {
            if (_isMain)
            {
                e.MapHub<SinusSynchronousServer.Hubs.SinusHub>("/dummyhub");
            }

            e.MapControllers();
            e.MapHealthChecks("/health").WithMetadata(new AllowAnonymousAttribute());
        });
    }
}