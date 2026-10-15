using System.Diagnostics;
using System.Net.Sockets;
using WebHealth.Server.Models;

namespace WebHealth.Server.Services
{
    public class RealHostCheckService : IHostCheckService
    {
        private static readonly SemaphoreSlim _throttle = new(initialCount: 4, maxCount: 4);
        private readonly IHttpClientFactory _httpClientFactory;
        public RealHostCheckService(IHttpClientFactory httpClientFactory)
        {
            this._httpClientFactory = httpClientFactory;
        }
        public async Task<PhaseResult> CheckHttpStatusAsync(HealthTarget target, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            await _throttle.WaitAsync(ct);
            try
            {
                using var client = this._httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                using var response = await client.GetAsync(target.Url, HttpCompletionOption.ResponseHeadersRead, ct);
                sw.Stop();
                if (response.IsSuccessStatusCode)
                {
                    return new PhaseResult(response.StatusCode.ToString(), response.IsSuccessStatusCode, sw.ElapsedMilliseconds, null);
                }
                return new PhaseResult(response.StatusCode.ToString(), false, sw.ElapsedMilliseconds, response?.ReasonPhrase);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new PhaseResult("HTTP Status", false, sw.ElapsedMilliseconds, ex.Message);
            }
            finally
            {
                _throttle.Release();
            }
        }

        public async Task<PhaseResult> CheckPayloadAsync(HealthTarget target, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            await _throttle.WaitAsync(ct);
            try
            {
                using var client = this._httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                using var response = await client.GetAsync(target.Url, ct);
                sw.Stop();
                bool hasContent = response.Content.Headers.ContentLength is null or > 0;
                return new PhaseResult(response.StatusCode.ToString(), response.IsSuccessStatusCode && hasContent, sw.ElapsedMilliseconds, hasContent ? null : "No content");
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new PhaseResult("Payload / TLS", false, sw.ElapsedMilliseconds, ex.Message);
            }
            finally
            {
                _throttle.Release();
            }
        }

        public async Task<PhaseResult> CheckTcpAsync(HealthTarget target, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            await _throttle.WaitAsync(ct);
            try
            {
                var uri = new Uri(target.Url);
                int port = uri.Port > 0 ? uri.Port : (uri.Scheme == "https" ? 443 : 80);
                using var tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(uri.Host, port, ct);
                sw.Stop();
                return new PhaseResult("TCP Handshake", true, sw.ElapsedMilliseconds, null);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new PhaseResult("TCP Handshake", false, sw.ElapsedMilliseconds, ex.Message);
            }
            finally
            {
                _throttle.Release();
            }
        }
    }
}