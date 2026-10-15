using WebHealth.Server.Models;

namespace WebHealth.Server.Services
{
    public interface IHostCheckerEngine
    {
        Task<HostHealthSnapshot> ExecuteCheckCycleAsync(HealthTarget targer, CancellationToken ct = default);
    }
}
