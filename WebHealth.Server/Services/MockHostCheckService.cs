using System.Diagnostics;
using WebHealth.Server.Models;

namespace WebHealth.Server.Services
{
    public class MockHostCheckService : IHostCheckService
    {
        public async Task<PhaseResult> CheckHttpStatusAsync(HealthTarget target, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();

            await Task.Delay(Random.Shared.Next(30, 100), ct);
            sw.Stop();
            if (target.Url.Contains("500", StringComparison.OrdinalIgnoreCase))
            {
                return new PhaseResult("Payload Check", false, sw.ElapsedMilliseconds, "HTTP 500 Internal Server Error (Mock)");
            }
            return new PhaseResult("Payload Check", true, sw.ElapsedMilliseconds, null);
        }

        public async Task<PhaseResult> CheckPayloadAsync(HealthTarget target, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            await Task.Delay(Random.Shared.Next(50, 150), ct);
            sw.Stop();
            if (target.Url.Contains("500", StringComparison.OrdinalIgnoreCase))
            {
                return new PhaseResult("Payload Check", false, sw.ElapsedMilliseconds, "HTTP 500 Internal Server Error (Mock)");
            }
            return new PhaseResult("Payload Check", true, sw.ElapsedMilliseconds, null);
        }

        public async Task<PhaseResult> CheckTcpAsync(HealthTarget target, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            await Task.Delay(Random.Shared.Next(20,80), ct);
            sw.Stop();
            if (target.Url.Contains("down", StringComparison.OrdinalIgnoreCase))
            {
                return new PhaseResult("TCP Handshake", false, sw.ElapsedMilliseconds, "Connection refused (Mock)");
            }
            return new PhaseResult("TCP Handshake", true, sw.ElapsedMilliseconds, null);
        }
    }
}
