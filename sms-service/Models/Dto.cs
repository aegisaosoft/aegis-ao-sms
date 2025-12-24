using System.ComponentModel.DataAnnotations;

namespace SmsService.Models.Dto;

// === SMS DTOs ===

public record SendSmsRequest
{
    [Required, Phone]
    public string PhoneNumber { get; init; } = string.Empty;
    
    [Required]
    public string Message { get; init; } = string.Empty;
}

public record SendBulkSmsRequest
{
    [Required]
    public List<string> PhoneNumbers { get; init; } = new();
    
    [Required]
    public string Message { get; init; } = string.Empty;
}

public record SendSmsWithLinkRequest
{
    [Required, Phone]
    public string PhoneNumber { get; init; } = string.Empty;
    
    [Required]
    public string Url { get; init; } = string.Empty;
    
    public string? Message { get; init; }
    
    public bool ShortenUrl { get; init; } = true;
}

// === URL Shortener DTOs ===

public record ShortenUrlRequest
{
    [Required]
    public string Url { get; init; } = string.Empty;
}

public record ShortenUrlResponse
{
    public string OriginalUrl { get; init; } = string.Empty;
    public string ShortUrl { get; init; } = string.Empty;
    public int CharactersSaved { get; init; }
}

// === Auth DTOs ===

public record LoginRequest
{
    [Required]
    public string Username { get; init; } = string.Empty;
    
    [Required]
    public string Password { get; init; } = string.Empty;
}

// === Common DTOs ===

public record ApiResponse<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    
    public static ApiResponse<T> Ok(T data, string? message = null) => 
        new() { Success = true, Data = data, Message = message };
    
    public static ApiResponse<T> Error(string message) => 
        new() { Success = false, Message = message };
}

public record ApiResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    
    public static ApiResponse Ok(string? message = null) => 
        new() { Success = true, Message = message };
    
    public static ApiResponse Error(string message) => 
        new() { Success = false, Message = message };
}
