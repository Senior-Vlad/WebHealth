using Microsoft.AspNetCore.SignalR;

namespace WebHealth.Server.Hubs
{
    public class HealthHub : Hub<IHealthHubClient>
    {
    }
}
