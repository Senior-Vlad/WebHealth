namespace WebHealth.Server.Services;
using System;
using Models;
public class HealthMetricsCache : IHealthMetricsCache
{
    private static readonly  Lazy<HealthMetricsCache> _instance = new Lazy<HealthMetricsCache>(() => new HealthMetricsCache(), true);
    private readonly object _lock = new object();
	private readonly Dictionary<Guid, HostHealthSnapshot> _cache = new Dictionary<Guid, HostHealthSnapshot>(); // or ConcurrentDictionary
    private readonly Dictionary<Guid, List<HostHealthSnapshot>> _history = new();
    public HealthMetricsCache(){ }

    #region old singleton implementation
    //private readonly static object _lock = new object();
    //private static HealthMetricsCache _instance;
    //public static HealthMetricsCache Instance
    //{
    //	get
    //	{
    //		if (_instance == null)
    //		{
    //			lock (_lock)
    //			{
    //				if (_instance == null)
    //				{
    //					_instance = new HealthMetricsCache();
    //				}
    //			}
    //		}
    //		return _instance;
    //	}
    //}
    #endregion

    public static HealthMetricsCache Instance => _instance.Value;
    public void SaveSnapshot(HostHealthSnapshot snapshot)
	{
        lock (_lock)
        {
            if (!_history.TryGetValue(snapshot.HostId, out var historyList))
            {
                historyList = new List<HostHealthSnapshot>();
                _history[snapshot.HostId] = historyList;
            }
            historyList.Add(snapshot);
            if (historyList.Count > 50)
            {
                historyList.RemoveAt(0);
            }
			this._cache[snapshot.HostId] = snapshot;
		}
	}

	public HostHealthSnapshot? GetLatest(Guid hostId)
	{
		lock (_lock)
		{
			if (this._cache.TryGetValue(hostId, out var snapshot))
			{
                return snapshot;
			}
		}
		return null;

	}

    public double CalculateUptime(Guid hostId)
    {
        lock (_lock)
        {
            if (!_history.TryGetValue(hostId, out var historyList) || historyList.Count == 0)
            {
                return 100.0;
            }
            return historyList.Count(el => el.Status == HealthStatus.Healthy) * 100.0 / historyList.Count;
        }
    }

    public double CalculateAverageLatency(Guid hostId)
    {
        lock (_lock)
        {
            if (!_history.TryGetValue(hostId, out var historyList) || historyList.Count == 0)
            {
                return 0.0;
            }
            return _history[hostId].Average(el => el.TotalRoundTripMs);
        }
    }
}
