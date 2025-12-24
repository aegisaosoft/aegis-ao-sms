using System.Text.Json;

namespace SmsService.Services;

public interface IUrlShortenerService
{
    Task<(bool Success, string? ShortUrl, string? Error)> ShortenUrlAsync(string longUrl);
}

public class UrlShortenerService : IUrlShortenerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UrlShortenerService> _logger;

    public UrlShortenerService(HttpClient httpClient, ILogger<UrlShortenerService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(bool Success, string? ShortUrl, string? Error)> ShortenUrlAsync(string longUrl)
    {
        try
        {
            // Try TinyURL first (no API key needed)
            var result = await TryTinyUrl(longUrl);
            if (result.Success)
                return result;

            // Fallback to is.gd
            result = await TryIsGd(longUrl);
            if (result.Success)
                return result;

            return (false, null, "All URL shortening services failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shortening URL: {Url}", longUrl);
            return (false, null, ex.Message);
        }
    }

    private async Task<(bool Success, string? ShortUrl, string? Error)> TryTinyUrl(string longUrl)
    {
        try
        {
            var encodedUrl = Uri.EscapeDataString(longUrl);
            var response = await _httpClient.GetStringAsync(
                $"https://tinyurl.com/api-create.php?url={encodedUrl}");

            if (!string.IsNullOrEmpty(response) && response.StartsWith("http"))
            {
                _logger.LogInformation("URL shortened via TinyURL: {Long} -> {Short}", longUrl, response);
                return (true, response.Trim(), null);
            }

            return (false, null, "TinyURL returned invalid response");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TinyURL failed");
            return (false, null, ex.Message);
        }
    }

    private async Task<(bool Success, string? ShortUrl, string? Error)> TryIsGd(string longUrl)
    {
        try
        {
            var encodedUrl = Uri.EscapeDataString(longUrl);
            var response = await _httpClient.GetStringAsync(
                $"https://is.gd/create.php?format=simple&url={encodedUrl}");

            if (!string.IsNullOrEmpty(response) && response.StartsWith("http"))
            {
                _logger.LogInformation("URL shortened via is.gd: {Long} -> {Short}", longUrl, response);
                return (true, response.Trim(), null);
            }

            return (false, null, "is.gd returned invalid response");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "is.gd failed");
            return (false, null, ex.Message);
        }
    }
}
