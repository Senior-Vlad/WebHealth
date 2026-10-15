namespace WebHealth.Server.Services;

using Models;

public class HostCheckerEngine : IHostCheckerEngine
{
    private readonly IHealthMetricsCache _metricsCache;
    private readonly RealHostCheckService _realChecker;
    private readonly MockHostCheckService _mockChecker;
    public HostCheckerEngine(IHealthMetricsCache metricsCache, RealHostCheckService realChecker, MockHostCheckService mockChecker)
    {
        this._metricsCache = metricsCache;
        this._realChecker = realChecker;
        this._mockChecker = mockChecker;
    }

    public async Task<HostHealthSnapshot> ExecuteCheckCycleAsync(HealthTarget target, CancellationToken ct = default)
    {
        IHostCheckService checker = target.IsMock ? _mockChecker : _realChecker;
        var results = new PhaseResult[3];
        HostHealthSnapshot? snapshot = null;
        using var barrier = new Barrier(3, b =>
        {
            snapshot = AggregatePhaseResults(target.Id, results);
            _metricsCache.SaveSnapshot(snapshot);
        });

        var tcpTask = Task.Run(async () =>
        {
            try
            {
                results[0] = await checker.CheckTcpAsync(target, ct);
            }
            catch (Exception ex)
            {
                results[0] = new PhaseResult("TCP", false, 0, ex.Message);
            }
            finally
            {
                barrier.SignalAndWait(ct);
            }
        });

        var httpTask = Task.Run(async () =>
        {
            try
            {
                results[1] = await checker.CheckHttpStatusAsync(target, ct);
            }
            catch (Exception ex)
            {
                results[1] = new PhaseResult("HTTP", false, 0, ex.Message);
            }
            finally
            {
                barrier.SignalAndWait(ct);
            }
        });

        var payloadTask = Task.Run(async () =>
        {
            try
            {
                results[2] = await checker.CheckPayloadAsync(target, ct);
            }
            catch (Exception ex)
            {
                results[2] = new PhaseResult("Payload", false, 0, ex.Message);
            }
            finally
            {
                barrier.SignalAndWait(ct);
            }
        });

        await Task.WhenAll(tcpTask, httpTask, payloadTask);
        return snapshot!;
    }

    private HostHealthSnapshot AggregatePhaseResults(Guid hoitId, PhaseResult[] results)
    {
        long maxDuration = 0;
        HealthStatus statusResult = HealthStatus.Healthy;
        foreach (var result in results)
        {
            if (maxDuration < result.DurationMs)
            {
                maxDuration = result.DurationMs;
            }
            if (maxDuration > TimeSpan.FromSeconds(5).TotalMilliseconds)
            {
                statusResult = HealthStatus.Degraded;
                break;
            }
            if (!result.IsSuccess)
            {
                statusResult = HealthStatus.Down;
                break;
            }
        }
        
        return new HostHealthSnapshot { HostId = hoitId, TimestampUtc = DateTime.UtcNow, PhaseResults = results, Status = statusResult, TotalRoundTripMs = maxDuration};
    }
}

