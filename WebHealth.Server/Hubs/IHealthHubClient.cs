namespace WebHealth.Server.Hubs
{
    public interface IHealthHubClient
    {
        Task ReceiveHealthUpdate(object update);
    }
}
