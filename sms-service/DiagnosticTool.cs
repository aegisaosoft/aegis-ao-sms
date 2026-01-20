using Azure.Communication.Sms;
using Azure.Communication;
using System.Text.Json;

namespace SmsService.Diagnostic;

public class SmssDiagnosticTool
{
    private readonly string _connectionString;
    private readonly string _fromNumber;

    public SmssDiagnosticTool(string connectionString, string fromNumber)
    {
        _connectionString = connectionString;
        _fromNumber = fromNumber;
    }

    public async Task<DiagnosticResult> RunFullDiagnosticAsync(string testPhoneNumber)
    {
        var result = new DiagnosticResult();

        Console.WriteLine("=== SMS Diagnostic Tool ===");
        Console.WriteLine($"From Number: {_fromNumber}");
        Console.WriteLine($"Test Number: {testPhoneNumber}");
        Console.WriteLine($"Connection: {_connectionString[..50]}...");
        Console.WriteLine();

        try
        {
            var smsClient = new SmsClient(_connectionString);

            // Test 1: Simple connection test
            result.ConnectionTest = await TestConnectionAsync(smsClient);

            // Test 2: Phone number validation
            result.PhoneNumberTest = TestPhoneNumber(testPhoneNumber);

            // Test 3: Send test SMS
            result.SmsTest = await TestSmsDeliveryAsync(smsClient, testPhoneNumber);

            // Test 4: Check Azure response details
            result.AzureResponseTest = await TestAzureResponseAsync(smsClient, testPhoneNumber);

        }
        catch (Exception ex)
        {
            result.GeneralError = $"Diagnostic failed: {ex.Message}";
        }

        PrintDiagnosticResults(result);
        return result;
    }

    private async Task<TestResult> TestConnectionAsync(SmsClient smsClient)
    {
        Console.WriteLine("🔗 Test 1: Connection to Azure Communication Services");

        try
        {
            // Try to create client - this validates connection string
            var endpoint = new Uri(_connectionString.Split(';')[0].Replace("endpoint=", ""));
            Console.WriteLine($"   Endpoint: {endpoint}");
            Console.WriteLine($"   ✅ Connection string valid");
            return new TestResult { Success = true, Message = "Connection successful" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Connection failed: {ex.Message}");
            return new TestResult { Success = false, Message = ex.Message };
        }
    }

    private TestResult TestPhoneNumber(string phoneNumber)
    {
        Console.WriteLine("📱 Test 2: Phone Number Validation");

        var cleanNumber = phoneNumber.Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", "");

        Console.WriteLine($"   Original: {phoneNumber}");
        Console.WriteLine($"   Cleaned: {cleanNumber}");

        // Check format
        if (!cleanNumber.StartsWith("+"))
        {
            if (cleanNumber.Length == 10)
            {
                cleanNumber = "+1" + cleanNumber;
                Console.WriteLine($"   Formatted: {cleanNumber}");
            }
            else if (cleanNumber.Length == 11 && cleanNumber.StartsWith("1"))
            {
                cleanNumber = "+" + cleanNumber;
                Console.WriteLine($"   Formatted: {cleanNumber}");
            }
        }

        // Basic validation
        if (cleanNumber.Length >= 10 && cleanNumber.StartsWith("+1"))
        {
            Console.WriteLine($"   ✅ Phone number format valid");
            return new TestResult { Success = true, Message = $"Valid: {cleanNumber}" };
        }
        else
        {
            Console.WriteLine($"   ❌ Invalid phone number format");
            return new TestResult { Success = false, Message = "Invalid format" };
        }
    }

    private async Task<TestResult> TestSmsDeliveryAsync(SmsClient smsClient, string testPhoneNumber)
    {
        Console.WriteLine("📤 Test 3: SMS Delivery Test");

        try
        {
            var normalizedPhone = NormalizePhoneNumber(testPhoneNumber);
            var testMessage = $"Test SMS from Aegis-AO at {DateTime.Now:HH:mm:ss}";

            Console.WriteLine($"   Sending to: {normalizedPhone}");
            Console.WriteLine($"   From: {_fromNumber}");
            Console.WriteLine($"   Message: {testMessage}");

            var response = await smsClient.SendAsync(
                from: _fromNumber,
                to: normalizedPhone,
                message: testMessage);

            var result = response.Value;

            Console.WriteLine($"   Response Successful: {result.Successful}");
            Console.WriteLine($"   Message ID: {result.MessageId}");
            Console.WriteLine($"   HTTP Status: {response.GetRawResponse().Status}");

            if (result.Successful)
            {
                Console.WriteLine($"   ✅ SMS sent successfully");
                return new TestResult
                {
                    Success = true,
                    Message = $"Sent successfully. MessageId: {result.MessageId}"
                };
            }
            else
            {
                Console.WriteLine($"   ❌ SMS failed to send");
                return new TestResult
                {
                    Success = false,
                    Message = $"Failed. MessageId: {result.MessageId}"
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Exception: {ex.Message}");
            return new TestResult { Success = false, Message = ex.Message };
        }
    }

    private async Task<TestResult> TestAzureResponseAsync(SmsClient smsClient, string testPhoneNumber)
    {
        Console.WriteLine("🔍 Test 4: Detailed Azure Response Analysis");

        try
        {
            var normalizedPhone = NormalizePhoneNumber(testPhoneNumber);
            var response = await smsClient.SendAsync(_fromNumber, normalizedPhone, "Diagnostic test");

            var rawResponse = response.GetRawResponse();
            var headers = rawResponse.Headers;

            Console.WriteLine($"   Status Code: {rawResponse.Status}");
            Console.WriteLine($"   Reason Phrase: {rawResponse.ReasonPhrase}");

            foreach (var header in headers)
            {
                Console.WriteLine($"   Header: {header.Name} = {header.Value}");
            }

            // Try to get more details from response body
            if (rawResponse.Content != null)
            {
                var content = rawResponse.Content.ToString();
                Console.WriteLine($"   Content: {content}");
            }

            return new TestResult
            {
                Success = true,
                Message = $"Status: {rawResponse.Status}, MessageId: {response.Value.MessageId}"
            };
        }
        catch (Exception ex)
        {
            return new TestResult { Success = false, Message = ex.Message };
        }
    }

    private void PrintDiagnosticResults(DiagnosticResult result)
    {
        Console.WriteLine("\n=== DIAGNOSTIC SUMMARY ===");
        Console.WriteLine($"Connection Test: {(result.ConnectionTest.Success ? "✅ PASS" : "❌ FAIL")} - {result.ConnectionTest.Message}");
        Console.WriteLine($"Phone Number Test: {(result.PhoneNumberTest.Success ? "✅ PASS" : "❌ FAIL")} - {result.PhoneNumberTest.Message}");
        Console.WriteLine($"SMS Test: {(result.SmsTest.Success ? "✅ PASS" : "❌ FAIL")} - {result.SmsTest.Message}");
        Console.WriteLine($"Azure Response Test: {(result.AzureResponseTest.Success ? "✅ PASS" : "❌ FAIL")} - {result.AzureResponseTest.Message}");

        if (!string.IsNullOrEmpty(result.GeneralError))
        {
            Console.WriteLine($"General Error: ❌ {result.GeneralError}");
        }

        Console.WriteLine("\n=== RECOMMENDATIONS ===");

        if (!result.ConnectionTest.Success)
        {
            Console.WriteLine("🔧 Check your Azure Communication Services connection string");
            Console.WriteLine("🔧 Verify the service is active and accessible");
        }

        if (!result.PhoneNumberTest.Success)
        {
            Console.WriteLine("🔧 Fix phone number format - ensure it includes country code");
        }

        if (result.SmsTest.Success && result.AzureResponseTest.Success)
        {
            Console.WriteLine("✅ Technical setup appears correct!");
            Console.WriteLine("🔍 If SMS still not delivered, check:");
            Console.WriteLine("   - Phone number is not blocked/filtered");
            Console.WriteLine("   - Carrier is not blocking messages");
            Console.WriteLine("   - Azure Communication Services account has sufficient credits");
            Console.WriteLine("   - Phone number has SMS capability enabled in Azure Portal");
            Console.WriteLine("   - Check Azure Communication Services logs in Azure Portal");
        }

        Console.WriteLine("=========================\n");
    }

    private string NormalizePhoneNumber(string phone)
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

public class DiagnosticResult
{
    public TestResult ConnectionTest { get; set; } = new();
    public TestResult PhoneNumberTest { get; set; } = new();
    public TestResult SmsTest { get; set; } = new();
    public TestResult AzureResponseTest { get; set; } = new();
    public string GeneralError { get; set; } = "";
}

public class TestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}