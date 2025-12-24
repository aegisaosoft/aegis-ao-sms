using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmsService.Models.Dto;
using SmsService.Services;

namespace SmsService.Controllers;

[ApiController]
[Route("api/sms")]
[Authorize]
public class SmsController : ControllerBase
{
    private readonly ISmsService _smsService;
    private readonly IUrlShortenerService _urlShortener;
    private readonly ILogger<SmsController> _logger;

    public SmsController(
        ISmsService smsService,
        IUrlShortenerService urlShortener,
        ILogger<SmsController> logger)
    {
        _smsService = smsService;
        _urlShortener = urlShortener;
        _logger = logger;
    }

    /// <summary>
    /// Send a custom SMS message
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> SendSms([FromBody] SendSmsRequest request)
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
        var companyId = User.FindFirst("companyId")?.Value;
        
        _logger.LogInformation("SMS request from user {UserId}, company {CompanyId} to {Phone}", 
            userId, companyId, request.PhoneNumber);
        
        var (success, messageId, error) = await _smsService.SendSmsAsync(
            request.PhoneNumber,
            request.Message,
            companyId);
        
        if (!success)
        {
            _logger.LogError("SMS failed to {Phone}: {Error}", request.PhoneNumber, error);
            return StatusCode(500, ApiResponse.Error($"Failed to send SMS: {error}"));
        }
        
        _logger.LogInformation("SMS sent successfully to {Phone}, messageId: {MessageId}", 
            request.PhoneNumber, messageId);
        
        return Ok(ApiResponse<object>.Ok(new { MessageId = messageId }, "SMS sent successfully"));
    }

    /// <summary>
    /// Send SMS with auto-shortened link
    /// </summary>
    [HttpPost("send-with-link")]
    public async Task<IActionResult> SendSmsWithLink([FromBody] SendSmsWithLinkRequest request)
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
        var companyId = User.FindFirst("companyId")?.Value;

        // Shorten the URL
        string finalUrl = request.Url;
        if (request.ShortenUrl)
        {
            var (shortened, shortUrl, shortError) = await _urlShortener.ShortenUrlAsync(request.Url);
            if (shortened && !string.IsNullOrEmpty(shortUrl))
            {
                finalUrl = shortUrl;
                _logger.LogInformation("URL shortened: {Original} -> {Short}", request.Url, shortUrl);
            }
            else
            {
                _logger.LogWarning("Failed to shorten URL, using original: {Error}", shortError);
            }
        }

        // Build message
        var message = string.IsNullOrEmpty(request.Message) 
            ? finalUrl 
            : $"{request.Message} {finalUrl}";

        _logger.LogInformation("SMS with link from user {UserId} to {Phone}", userId, request.PhoneNumber);
        
        var (success, messageId, error) = await _smsService.SendSmsAsync(
            request.PhoneNumber,
            message,
            companyId);
        
        if (!success)
        {
            return StatusCode(500, ApiResponse.Error($"Failed to send SMS: {error}"));
        }
        
        return Ok(ApiResponse<object>.Ok(new 
        { 
            MessageId = messageId,
            OriginalUrl = request.Url,
            SentUrl = finalUrl,
            MessageSent = message
        }, "SMS sent successfully"));
    }

    /// <summary>
    /// Send SMS to multiple recipients
    /// </summary>
    [HttpPost("send-bulk")]
    public async Task<IActionResult> SendBulkSms([FromBody] SendBulkSmsRequest request)
    {
        var companyId = User.FindFirst("companyId")?.Value;
        var results = new List<object>();
        
        foreach (var phone in request.PhoneNumbers)
        {
            var (success, messageId, error) = await _smsService.SendSmsAsync(
                phone,
                request.Message,
                companyId);
            
            results.Add(new 
            { 
                PhoneNumber = phone, 
                Success = success, 
                MessageId = messageId, 
                Error = error 
            });
        }
        
        var successCount = results.Count(r => ((dynamic)r).Success);
        
        return Ok(ApiResponse<object>.Ok(new 
        { 
            Total = request.PhoneNumbers.Count,
            Sent = successCount,
            Failed = request.PhoneNumbers.Count - successCount,
            Results = results 
        }));
    }
}
