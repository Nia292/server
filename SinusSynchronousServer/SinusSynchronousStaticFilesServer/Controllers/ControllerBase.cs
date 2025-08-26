using SinusSynchronousShared.Utils;
using Microsoft.AspNetCore.Mvc;

namespace SinusSynchronousStaticFilesServer.Controllers;

public class ControllerBase : Controller
{
    protected ILogger _logger;

    public ControllerBase(ILogger logger)
    {
        _logger = logger;
    }

    protected string SinusUser => HttpContext.User.Claims.First(f => string.Equals(f.Type, SinusClaimTypes.Uid, StringComparison.Ordinal)).Value;
    protected string Continent => HttpContext.User.Claims.FirstOrDefault(f => string.Equals(f.Type, SinusClaimTypes.Continent, StringComparison.Ordinal))?.Value ?? "*";
    protected bool IsPriority => !string.IsNullOrEmpty(HttpContext.User.Claims.FirstOrDefault(f => string.Equals(f.Type, SinusClaimTypes.Alias, StringComparison.Ordinal))?.Value ?? string.Empty);
}
