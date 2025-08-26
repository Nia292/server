using SinusSynchronous.API.Routes;
using SinusSynchronousShared.Utils.Configuration;
using SinusSynchronousStaticFilesServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SinusSynchronousStaticFilesServer.Controllers;

[Route(SinusFiles.Main)]
[Authorize(Policy = "Internal")]
public class MainController : ControllerBase
{
    private readonly IClientReadyMessageService _messageService;
    private readonly MainServerShardRegistrationService _shardRegistrationService;

    public MainController(ILogger<MainController> logger, IClientReadyMessageService sinusHub,
        MainServerShardRegistrationService shardRegistrationService) : base(logger)
    {
        _messageService = sinusHub;
        _shardRegistrationService = shardRegistrationService;
    }

    [HttpGet(SinusFiles.Main_SendReady)]
    public async Task<IActionResult> SendReadyToClients(string uid, Guid requestId)
    {
        await _messageService.SendDownloadReady(uid, requestId).ConfigureAwait(false);
        return Ok();
    }

    [HttpPost("shardRegister")]
    public IActionResult RegisterShard([FromBody] ShardConfiguration shardConfiguration)
    {
        try
        {
            _shardRegistrationService.RegisterShard(SinusUser, shardConfiguration);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Shard could not be registered {shard}", SinusUser);
            return BadRequest();
        }
    }

    [HttpPost("shardUnregister")]
    public IActionResult UnregisterShard()
    {
        _shardRegistrationService.UnregisterShard(SinusUser);
        return Ok();
    }

    [HttpPost("shardHeartbeat")]
    public IActionResult ShardHeartbeat()
    {
        try
        {
            _shardRegistrationService.ShardHeartbeat(SinusUser);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Shard not registered: {shard}", SinusUser);
            return BadRequest();
        }
    }
}