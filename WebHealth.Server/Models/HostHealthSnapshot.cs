namespace WebHealth.Server.Models;
using System;
public record HostHealthSnapshot
{
    public  Guid HostId { get; init; } = Guid.NewGuid();
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public HealthStatus Status { get; init; } = HealthStatus.Unknown;
    public long TotalRoundTripMs { get; init; } = 0;
    public IReadOnlyList<PhaseResult> PhaseResults { get; init; } = new List<PhaseResult>();
}
