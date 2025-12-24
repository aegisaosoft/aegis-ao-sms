using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmsService.Models.Dto;
using System.Text;
using System.Text.Json;

namespace SmsService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IHttpClientFactory httpClientFactory,
        IConfiguration config, 
        ILogger<AuthController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Login via Auth Server (AegisUser - userId/password)
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthServer");
            
            // Use user auth (AegisUser) - userId/password
            var authRequest = new { userId = request.Username, password = request.Password };
            var content = new StringContent(
                JsonSerializer.Serialize(authRequest),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("/api/auth/user/login", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("AegisUser {UserId} logged in via Auth Server", request.Username);
            }
            else
            {
                _logger.LogWarning("Login failed for AegisUser {UserId}", request.Username);
            }

            return Content(responseContent, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auth Server unavailable");
            return StatusCode(503, ApiResponse.Error("Authentication service unavailable"));
        }
    }

    /// <summary>
    /// Check Auth Server connectivity
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<IActionResult> HealthCheck()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthServer");
            var response = await client.GetAsync("/api/health");
            
            return Ok(new
            {
                smsService = "healthy",
                authServer = response.IsSuccessStatusCode ? "healthy" : "unhealthy",
                authServerUrl = _config["AuthServer:BaseUrl"]
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                smsService = "healthy",
                authServer = "unreachable",
                error = ex.Message
            });
        }
    }
}
