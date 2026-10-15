using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WebHealth.Server.Data;
using WebHealth.Server.Hubs;

namespace WebHealth.Server.Services
{
    public class HealthCheckBackgroundWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<HealthHub, IHealthHubClient> _hubContext;
        private readonly IHealthMetricsCache _cache;
        private readonly ILogger<HealthCheckBackgroundWorker> _logger;

        public HealthCheckBackgroundWorker(IServiceScopeFactory scopeFactory, IHubContext<HealthHub, IHealthHubClient> hubContext, IHealthMetricsCache cache, ILogger<HealthCheckBackgroundWorker> logger)
        {
            this._scopeFactory = scopeFactory;
            this._hubContext = hubContext;
            this._cache = cache;
            this._logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            while(!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var scope = this._scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var engine = scope.ServiceProvider.GetRequiredService<IHostCheckerEngine>();

                var targets = await db.Targets.Where(target => target.IsActive).ToListAsync();

                foreach (var target in targets)
                {
                    var snapshot = await engine.ExecuteCheckCycleAsync(target, stoppingToken);
                    var update = new
                    {
                        HostId = target.Id,
                        Name = target.Name,
                        Status = snapshot.Status.ToString(),
                        LatencyMs = snapshot.TotalRoundTripMs,
                        AverageLatencyMs = this._cache.CalculateAverageLatency(target.Id),
                        Uptime = this._cache.CalculateUptime(target.Id),
                        Phases = snapshot.PhaseResults,
                        Timestamp = snapshot.TimestampUtc,
                    };

                    await this._hubContext.Clients.All.ReceiveHealthUpdate(update);
                }
            }
        }
    }
}
