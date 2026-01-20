using SmsService.Diagnostic;

// SMS Diagnostic Test Program
// Использование: dotnet run TestSms.cs

Console.WriteLine("=== SMS Diagnostic Test ===");

// Настройки из appsettings.json
var connectionString = "endpoint=https://aegis-sms.unitedstates.communication.azure.com/;accesskey=52emR4ubCYo9NTrpc5VF8XTjdGFaQVQ9MSr2zFYmz8v7U2jBsEXqJQQJ99BLACULyCpAChBNAAAAAZCSVz7m";
var fromNumber = "+18332702587";  // ваш номер 1-833-270-2587

Console.WriteLine($"Testing FROM number: {fromNumber} (1-833-270-2587)");

// Запросить тестовый номер
Console.Write("Enter test phone number (your number to receive SMS): ");
var testNumber = Console.ReadLine();

if (string.IsNullOrWhiteSpace(testNumber))
{
    Console.WriteLine("❌ Test number required!");
    return;
}

Console.WriteLine();

// Запустить диагностику
var diagnostic = new SmssDiagnosticTool(connectionString, fromNumber);
var result = await diagnostic.RunFullDiagnosticAsync(testNumber);

Console.WriteLine("Press any key to exit...");
Console.ReadKey();