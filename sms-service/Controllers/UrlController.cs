using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmsService.Models.Dto;
using SmsService.Services;

namespace SmsService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UrlController : ControllerBase
{
    private readonly IUrlShortenerService _urlShortener;
    private readonly ILogger<UrlController> _logger;

    public UrlController(IUrlShortenerService urlShortener, ILogger<UrlController> logger)
    {
        _urlShortener = urlShortener;
        _logger = logger;
    }

    /// <summary>
    /// Shorten a long URL for SMS
    /// </summary>
    [HttpGet("shorten")]
    public async Task<IActionResult> ShortenUrl([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return BadRequest(ApiResponse.Error("URL is required"));
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return BadRequest(ApiResponse.Error("Invalid URL format"));
        }

        var (success, shortUrl, error) = await _urlShortener.ShortenUrlAsync(url);

        if (!success)
        {
            _logger.LogError("Failed to shorten URL: {Url}, Error: {Error}", url, error);
            return StatusCode(500, ApiResponse.Error($"Failed to shorten URL: {error}"));
        }

        return Ok(ApiResponse<ShortenUrlResponse>.Ok(new ShortenUrlResponse
        {
            OriginalUrl = url,
            ShortUrl = shortUrl!,
            CharactersSaved = url.Length - shortUrl!.Length
        }));
    }

    /// <summary>
    /// Shorten URL (POST version)
    /// </summary>
    [HttpPost("shorten")]
    public async Task<IActionResult> ShortenUrlPost([FromBody] ShortenUrlRequest request)
    {
        return await ShortenUrl(request.Url);
    }
}
