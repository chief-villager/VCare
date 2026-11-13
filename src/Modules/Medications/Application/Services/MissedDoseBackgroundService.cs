using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Medications.Application.Services
{
    /// <summary>
    /// Runs <see cref="MissedDoseRecorder"/> every few minutes. A dose is recorded
    /// missed at most <see cref="Interval"/> after its grace hour runs out; until
    /// then the MAR chart already shows it missed, from the same rule.
    /// </summary>
    internal sealed class MissedDoseBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<MissedDoseBackgroundService> logger) : BackgroundService
    {
        internal static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval);
            do
            {
                try
                {
                    // A scope per pass: its DbContext must not outlive the pass.
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var recorder = scope.ServiceProvider.GetRequiredService<MissedDoseRecorder>();
                    await recorder.RecordMissedDosesAsync(DateTime.Now, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;                      // ordinary shutdown
                }
                catch (Exception ex)
                {
                    // Swallowed so a bad pass (database down) does not stop the host;
                    // the timer gives the back-off.
                    logger.LogError(ex, "Recording missed doses failed; retrying in {Interval}.", Interval);
                }
            }
            while (await WaitForNextTickAsync(timer, stoppingToken));
        }

        private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken stoppingToken)
        {
            try { return await timer.WaitForNextTickAsync(stoppingToken); }
            catch (OperationCanceledException) { return false; }
        }
    }
}
