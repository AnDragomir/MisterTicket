using MisterTicketApi.Services.ServicesInterfaces;

namespace MisterTicketApi.BackgroundJobs;

/// <summary>
/// Releases expired holds even when nobody is looking at the event.
///
/// Without this, seats are only freed when someone requests that event's map,
/// so a client watching a quiet event would never see the seats come back.
/// </summary>
public class ExpiredHoldSweeper : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    private readonly IServiceProvider _services;
    private readonly ILogger<ExpiredHoldSweeper> _logger;

    public ExpiredHoldSweeper(IServiceProvider services, ILogger<ExpiredHoldSweeper> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // The service is scoped, so it needs its own scope per sweep.
                using var scope = _services.CreateScope();
                var reservations = scope.ServiceProvider.GetRequiredService<IReservationService>();

                // Releasing broadcasts on its own.
                var released = await reservations.ReleaseExpiredAsync();

                if (released > 0)
                    _logger.LogInformation("Released {Count} expired reservation(s).", released);
            }
            catch (Exception ex)
            {
                // A failed sweep must not kill the loop.
                _logger.LogError(ex, "The expired-hold sweep failed.");
            }
        }
    }
}