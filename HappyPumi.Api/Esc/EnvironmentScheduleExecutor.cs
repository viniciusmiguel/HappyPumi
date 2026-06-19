#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HappyPumi.Api.Esc;

/// <summary>
/// Background loop that periodically fires due environment schedules (scheduled rotation / deletion). It runs
/// <see cref="ScheduleExecutionService.RunDueAsync"/> in a fresh scope each interval. The first run is delayed
/// by one interval so startup (and test runs) aren't disturbed; the interval is configurable via
/// <c>Esc:ScheduleIntervalSeconds</c> (default 30s).
/// </summary>
public sealed class EnvironmentScheduleExecutor(IServiceScopeFactory scopes, ILogger<EnvironmentScheduleExecutor> log, TimeSpan interval)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
                using var scope = scopes.CreateScope();
                var executor = scope.ServiceProvider.GetRequiredService<ScheduleExecutionService>();
                var fired = await executor.RunDueAsync(DateTime.UtcNow, stoppingToken);
                if (fired > 0)
                    log.LogInformation("Fired {Count} due ESC schedule(s).", fired);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // A bad schedule must not kill the loop; log and continue next tick.
                log.LogError(ex, "ESC schedule executor pass failed.");
            }
        }
    }
}
