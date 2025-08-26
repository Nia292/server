using SinusSynchronousShared.Utils.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SinusSynchronousShared.Services;

[Route("configuration/[controller]")]
[Authorize(Policy = "Internal")]
public class SinusConfigurationController<T> : Controller where T : class, ISinusConfiguration
{
    private readonly ILogger<SinusConfigurationController<T>> _logger;
    private IOptionsMonitor<T> _config;

    public SinusConfigurationController(IOptionsMonitor<T> config, ILogger<SinusConfigurationController<T>> logger)
    {
        _config = config;
        _logger = logger;
    }

    [HttpGet("GetConfigurationEntry")]
    [Authorize(Policy = "Internal")]
    public IActionResult GetConfigurationEntry(string key, string defaultValue)
    {
        var result = _config.CurrentValue.SerializeValue(key, defaultValue);
        _logger.LogInformation("Requested " + key + ", returning:" + result);
        return Ok(result);
    }
}

#pragma warning disable MA0048 // File name must match type name
public class SinusStaticFilesServerConfigurationController : SinusConfigurationController<StaticFilesServerConfiguration>
{
    public SinusStaticFilesServerConfigurationController(IOptionsMonitor<StaticFilesServerConfiguration> config, ILogger<SinusStaticFilesServerConfigurationController> logger) : base(config, logger)
    {
    }
}

public class SinusBaseConfigurationController : SinusConfigurationController<SinusConfigurationBase>
{
    public SinusBaseConfigurationController(IOptionsMonitor<SinusConfigurationBase> config, ILogger<SinusBaseConfigurationController> logger) : base(config, logger)
    {
    }
}

public class SinusServerConfigurationController : SinusConfigurationController<ServerConfiguration>
{
    public SinusServerConfigurationController(IOptionsMonitor<ServerConfiguration> config, ILogger<SinusServerConfigurationController> logger) : base(config, logger)
    {
    }
}

public class SinusServicesConfigurationController : SinusConfigurationController<ServicesConfiguration>
{
    public SinusServicesConfigurationController(IOptionsMonitor<ServicesConfiguration> config, ILogger<SinusServicesConfigurationController> logger) : base(config, logger)
    {
    }
}
#pragma warning restore MA0048 // File name must match type name
