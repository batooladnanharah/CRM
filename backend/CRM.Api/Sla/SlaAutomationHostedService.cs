using Microsoft.Extensions.Options;

namespace CRM.Api.Sla;

// Only registered (see Program.cs) when Sla:Enabled is true — tests disable
// it via CustomWebApplicationFactory so timing never leaks into other suites.
public sealed class SlaAutomationHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<SlaAutomationOptions> options,
    ILogger<SlaAutomationHostedService> logger) : BackgroundService
{
    // Guards against a tick starting while the previous one is still running
    // (e.g. a slow database) — the overlapping tick is skipped, not queued.
    private readonly SemaphoreSlim _reentrancyGuard = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, options.Value.IntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (!await _reentrancyGuard.WaitAsync(0, stoppingToken))
            {
                logger.LogWarning("sla_automation_cycle_skipped reason=previous_cycle_still_running");
                continue;
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var evaluator = scope.ServiceProvider.GetRequiredService<ISlaEvaluator>();
                var evaluatedCount = await evaluator.EvaluateAllOpenAsync(stoppingToken);
                logger.LogInformation("SLA automation cycle evaluated {Count} open ticket(s)", evaluatedCount);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "sla_automation_cycle_failed");
            }
            finally
            {
                _reentrancyGuard.Release();
            }
        }
    }
}
