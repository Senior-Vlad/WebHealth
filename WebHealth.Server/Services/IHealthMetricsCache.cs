namespace WebHealth.Server.Services;
using Models;

public interface IHealthMetricsCache
{
    void SaveSnapshot(HostHealthSnapshot snapshot);
    HostHealthSnapshot? GetLatest(Guid hostId);
    double CalculateUptime(Guid hostId);
    double CalculateAverageLatency(Guid hostId);
}
