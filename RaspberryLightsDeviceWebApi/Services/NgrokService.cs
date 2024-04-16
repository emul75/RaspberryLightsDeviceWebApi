using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RaspberryLightsDeviceWebApi.Interfaces;

namespace RaspberryLightsDeviceWebApi.Services;

public class NgrokService : INgrokService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient = new();

    private static string? PublicUrl { get; set; }
    private const string NgrokDownloadUrl = "https://bin.equinox.io/c/bNyj1mQVY4c/ngrok-v3-stable-linux-arm.zip";
    private const string NgrokPath = "./ngrok";
    private const string NgrokApiUrl = "http://127.0.0.1:4040/api/tunnels";
    private const string UpdateIpUrl = "https://raspberrylights.azurewebsites.net/Device/UpdateIp";
    private const string LocalApiUrl = "https://localhost:5252";

    public NgrokService(IConfiguration configuration)
    {
        _configuration = configuration;
    }


    public async Task StartupSetup()
    {
        await StartNgrokTunnel();

        if (!await WaitForNgrokAsync(30))
            throw new Exception("Ngrok did not become ready in the specified time.");

        await UpdatePublicUrl();
    }

    public async Task StartNgrokTunnel()
    {
        await EnsureNgrokDownloaded();

        if (await IsNgrokTunnelRunning())
            return;

        var ngrokAuthToken = _configuration.GetValue<string>("NgrokAuthToken");

        var startInfo = new ProcessStartInfo
        {
            FileName = NgrokPath,
            Arguments = $"http {LocalApiUrl} --authtoken={ngrokAuthToken}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var process = new Process { StartInfo = startInfo };
        process.Start();
    }

    public async Task UpdatePublicUrl()
    {
        var tunnels = await GetNgrokActiveTunnels();

        if (tunnels?.Tunnels != null && tunnels.Tunnels.Any())
        {
            await SendNewIpToWebApp(tunnels);
        }
        else
        {
            throw new Exception("No tunnels found in the response.");
        }
    }

    private async Task EnsureNgrokDownloaded()
    {
        if (!File.Exists(NgrokPath))
        {
            var response = await _httpClient.GetAsync(NgrokDownloadUrl);
            var content = await response.Content.ReadAsByteArrayAsync();

            using (var ms = new MemoryStream(content))
            {
                using (var archive = new ZipArchive(ms))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.FullName.Equals("ngrok", StringComparison.InvariantCultureIgnoreCase)) continue;
                        entry.ExtractToFile(NgrokPath);
                        break;
                    }
                }
            }

            Console.WriteLine("ngrok has been downloaded");

            if (!File.Exists(NgrokPath))
            {
                throw new Exception("Failed to download ngrok.");
            }
        }
        else
        {
            Console.WriteLine("ngrok was already downloaded");
        }
    }

    private async Task SendNewIpToWebApp(NgrokTunnelsResponse tunnels)
    {
        PublicUrl = tunnels.Tunnels.First().PublicUrl;

        var deviceId = _configuration.GetValue<string>("DeviceGuid");

        var updateDeviceIpCommand = new UpdateDeviceIpCommand
        {
            DeviceId = deviceId,
            NewIp = PublicUrl
        };

        var updateIpContent =
            new StringContent(JsonSerializer.Serialize(updateDeviceIpCommand), Encoding.UTF8, "application/json");

        await _httpClient.PostAsync(UpdateIpUrl, updateIpContent);
    }

    private async Task<bool> WaitForNgrokAsync(int timeoutSeconds)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed.TotalSeconds < timeoutSeconds)
        {
            try
            {
                var response = await _httpClient.GetAsync(NgrokApiUrl);
                if (response.IsSuccessStatusCode)
                    return true;
            }
            catch
            {
            }

            await Task.Delay(1000);
        }

        return false;
    }

    private async Task<NgrokTunnelsResponse?> GetNgrokActiveTunnels()
    {
        NgrokTunnelsResponse? tunnels;
        var response = await _httpClient.GetAsync(NgrokApiUrl);
        var content = await response.Content.ReadAsStringAsync();

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            };

            tunnels = JsonSerializer.Deserialize<NgrokTunnelsResponse>(content, options);
            if (tunnels?.Tunnels == null || !tunnels.Tunnels.Any())
            {
                throw new Exception("No tunnels found in the response.");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Deserialization error: {e}");
            throw;
        }

        return tunnels;
    }

    private async Task<bool> IsNgrokTunnelRunning()
    {
        try
        {
            var response = await _httpClient.GetAsync(NgrokApiUrl);
            if (!response.IsSuccessStatusCode)
                return false;

            var content = await response.Content.ReadAsStringAsync();
            var tunnels = JsonSerializer.Deserialize<NgrokTunnelsResponse>(content);

            return tunnels.Tunnels.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private class UpdateDeviceIpCommand
    {
        public string? NewIp { get; set; }
        public string DeviceId { get; set; }
    }

    public class NgrokTunnel
    {
        public string Name { get; set; }
        public string Uri { get; set; }
        [JsonPropertyName("public_url")] public string PublicUrl { get; set; }
    }

    private class NgrokTunnelsResponse
    {
        public List<NgrokTunnel> Tunnels { get; set; }
    }
}