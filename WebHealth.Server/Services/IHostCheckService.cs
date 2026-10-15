namespace WebHealth.Server.Services;
using Models;
public interface IHostCheckService
{
    Task<PhaseResult> CheckTcpAsync(HealthTarget target, CancellationToken ct = default);
    Task<PhaseResult> CheckHttpStatusAsync(HealthTarget target, CancellationToken ct = default);
    Task<PhaseResult> CheckPayloadAsync(HealthTarget target, CancellationToken ct = default);
}