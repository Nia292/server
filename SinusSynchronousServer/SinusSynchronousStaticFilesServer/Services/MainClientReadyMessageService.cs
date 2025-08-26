using Microsoft.AspNetCore.SignalR;
using SinusSynchronous.API.SignalR;
using SinusSynchronousServer.Hubs;

namespace SinusSynchronousStaticFilesServer.Services;

public class MainClientReadyMessageService : IClientReadyMessageService
{
    private readonly ILogger<MainClientReadyMessageService> _logger;
    private readonly IHubContext<SinusHub> _sinusHub;

    public MainClientReadyMessageService(ILogger<MainClientReadyMessageService> logger, IHubContext<SinusHub> sinusHub)
    {
        _logger = logger;
        _sinusHub = sinusHub;
    }

    public async Task SendDownloadReady(string uid, Guid requestId)
    {
        _logger.LogInformation("Sending Client Ready for {uid}:{requestId} to SignalR", uid, requestId);
        await _sinusHub.Clients.User(uid).SendAsync(nameof(ISinusHub.Client_DownloadReady), requestId).ConfigureAwait(false);
    }
}
