using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KNOTS.Services;

public class FriendRequestCleanupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public FriendRequestCleanupBackgroundService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CleanupExpiredRequests(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));
        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CleanupExpiredRequests(stoppingToken);
        }
    }

    private async Task CleanupExpiredRequests(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var friendService = scope.ServiceProvider.GetRequiredService<InterfaceFriendService>();
            var deletedCount = await friendService.DeleteExpiredPendingRequestsAsync(cancellationToken);

            if (deletedCount > 0)
            {
                Console.WriteLine($"Deleted {deletedCount} expired friend request(s).");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Friend request cleanup failed: {ex.Message}");
        }
    }
}
