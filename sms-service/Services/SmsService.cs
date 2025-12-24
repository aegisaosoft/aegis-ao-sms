using Azure.Communication.Sms;

namespace SmsService.Services;

public interface ISmsService
{
    Task<(bool Success, string? MessageId, string? Error)> SendSmsAsync(
        string toPhoneNumber, 
        string message, 
        string? companyId = null);
}

public class AzureSmsService : ISmsService
{
    private readonly SmsClient _smsClient;
    private readonly string _fromNumber;
    private readonly ILogger<AzureSmsService> _logger;

    public AzureSmsService(IConfiguration config, ILogger<AzureSmsService> logger)
    {
        _logger = logger;
        
        var connectionString = config["AzureCommunication:ConnectionString"] 
            ?? throw new InvalidOperationException("AzureCommunication:ConnectionString not configured");
        _fromNumber = config["AzureCommunication:FromPhoneNumber"] 
            ?? throw new InvalidOperationException("AzureCommunication:FromPhoneNumber not configured");
        
        _smsClient = new SmsClient(connectionString);
        
        _logger.LogInformation("Azure SMS Service initialized with number: {FromNumber}", _fromNumber);
    }

    public async Task<(bool Success, string? MessageId, string? Error)> SendSmsAsync(
        string toPhoneNumber, 
        string message, 
        string? companyId = null)
    {
        try
        {
            var normalizedPhone = NormalizePhoneNumber(toPhoneNumber);
            
            _logger.LogInformation(
                "Sending SMS to {Phone} (company: {CompanyId})", 
                normalizedPhone, companyId ?? "N/A");
            
            var response = await _smsClient.SendAsync(
                from: _fromNumber,
                to: normalizedPhone,
                message: message);

            var result = response.Value;
            
            if (result.Successful)
            {
                _logger.LogInformation(
                    "SMS sent successfully to {Phone}, MessageId: {MessageId}", 
                    normalizedPhone, result.MessageId);
                    
                return (true, result.MessageId, null);
            }
            else
            {
                _logger.LogWarning(
                    "SMS failed to {Phone}, MessageId: {MessageId}", 
                    normalizedPhone, result.MessageId);
                    
                return (false, result.MessageId, "Failed to send SMS");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {Phone}", toPhoneNumber);
            return (false, null, ex.Message);
        }
    }

    private static string NormalizePhoneNumber(string phone)
    {
        // Remove all non-digit characters
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        
        // Add + prefix if not present
        if (!phone.StartsWith('+'))
        {
            // Assume US number if 10 digits
            if (digits.Length == 10)
                return $"+1{digits}";
            
            return $"+{digits}";
        }
        
        return $"+{digits}";
    }
}
