using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace SmsService.Services;

public class TwilioSmsService : ISmsService
{
    private readonly string _fromNumber;
    private readonly ILogger<TwilioSmsService> _logger;

    public TwilioSmsService(IConfiguration config, ILogger<TwilioSmsService> logger)
    {
        _logger = logger;

        var accountSid = config["Twilio:AccountSid"]
            ?? throw new InvalidOperationException("Twilio:AccountSid not configured");
        var authToken = config["Twilio:AuthToken"]
            ?? throw new InvalidOperationException("Twilio:AuthToken not configured");
        _fromNumber = config["Twilio:FromPhoneNumber"]
            ?? throw new InvalidOperationException("Twilio:FromPhoneNumber not configured");

        TwilioClient.Init(accountSid, authToken);

        _logger.LogInformation("Twilio SMS Service initialized with number: {FromNumber}", _fromNumber);
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
                "Sending SMS via Twilio to {Phone} (company: {CompanyId})",
                normalizedPhone, companyId ?? "N/A");

            var messageResource = await MessageResource.CreateAsync(
                to: new PhoneNumber(normalizedPhone),
                from: new PhoneNumber(_fromNumber),
                body: message);

            if (messageResource.Status != MessageResource.StatusEnum.Failed &&
                messageResource.Status != MessageResource.StatusEnum.Undelivered)
            {
                _logger.LogInformation(
                    "SMS sent via Twilio to {Phone}, SID: {Sid}, Status: {Status}",
                    normalizedPhone, messageResource.Sid, messageResource.Status);

                return (true, messageResource.Sid, null);
            }
            else
            {
                _logger.LogWarning(
                    "Twilio SMS failed to {Phone}, SID: {Sid}, Error: {ErrorCode} {ErrorMessage}",
                    normalizedPhone, messageResource.Sid,
                    messageResource.ErrorCode, messageResource.ErrorMessage);

                return (false, messageResource.Sid,
                    $"Failed: {messageResource.ErrorCode} - {messageResource.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS via Twilio to {Phone}", toPhoneNumber);
            return (false, null, ex.Message);
        }
    }

    private static string NormalizePhoneNumber(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (!phone.StartsWith('+'))
        {
            if (digits.Length == 10)
                return $"+1{digits}";

            return $"+{digits}";
        }

        return $"+{digits}";
    }
}
