# SMS Diagnostic Test Script
# Тестирует SMS сервис через diagnostic endpoint

param(
    [Parameter(Mandatory=$true)]
    [string]$TestPhoneNumber,

    [string]$ApiUrl = "https://localhost:5001/azure/sms/diagnostic",

    [string]$AuthToken = $null
)

Write-Host "=== SMS Diagnostic Test для номера 1-833-270-2587 ===" -ForegroundColor Green
Write-Host "Testing phone: $TestPhoneNumber" -ForegroundColor Cyan
Write-Host "API URL: $ApiUrl" -ForegroundColor Gray
Write-Host ""

# Проверяем формат номера
if ($TestPhoneNumber -notmatch '^\+?1?[0-9]{10,11}$') {
    Write-Host "⚠️  Внимание: Номер может быть в неправильном формате" -ForegroundColor Yellow
    Write-Host "   Убедитесь что номер содержит 10-11 цифр (например: +1234567890 или 234567890)" -ForegroundColor Gray
}

# Подготавливаем запрос
$body = @{
    testPhoneNumber = $TestPhoneNumber
} | ConvertTo-Json

$headers = @{
    'Content-Type' = 'application/json'
}

# Добавляем Bearer token если предоставлен
if ($AuthToken) {
    $headers['Authorization'] = "Bearer $AuthToken"
    Write-Host "Using auth token: $($AuthToken.Substring(0, [Math]::Min(20, $AuthToken.Length)))..." -ForegroundColor Gray
}

Write-Host "🚀 Отправляем запрос на диагностику..." -ForegroundColor Yellow

try {
    # Игнорируем SSL ошибки для localhost testing
    if ($ApiUrl.StartsWith("https://localhost")) {
        [System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
    }

    $response = Invoke-RestMethod -Uri $ApiUrl -Method POST -Body $body -Headers $headers -ErrorAction Stop

    Write-Host "✅ Диагностика завершена!" -ForegroundColor Green
    Write-Host ""

    # Анализируем результаты
    $data = $response.data
    $tests = $data.tests

    Write-Host "=== РЕЗУЛЬТАТЫ ТЕСТОВ ===" -ForegroundColor Cyan
    Write-Host "From Number: $($data.fromNumber) (1-833-270-2587)" -ForegroundColor White
    Write-Host "Test Number: $($data.testNumber)" -ForegroundColor White
    Write-Host ""

    # Connection Test
    $icon = if ($tests.connection.success) { "✅" } else { "❌" }
    Write-Host "$icon Connection Test: $($tests.connection.message)" -ForegroundColor $(if ($tests.connection.success) { "Green" } else { "Red" })

    # Phone Validation Test
    $icon = if ($tests.phoneValidation.success) { "✅" } else { "❌" }
    Write-Host "$icon Phone Validation: $($tests.phoneValidation.message)" -ForegroundColor $(if ($tests.phoneValidation.success) { "Green" } else { "Red" })

    # SMS Send Test
    $icon = if ($tests.smsSend.success) { "✅" } else { "❌" }
    Write-Host "$icon SMS Send Test: $($tests.smsSend.message)" -ForegroundColor $(if ($tests.smsSend.success) { "Green" } else { "Red" })

    # Azure Response Test
    $icon = if ($tests.azureResponse.success) { "✅" } else { "❌" }
    Write-Host "$icon Azure Response: $($tests.azureResponse.message)" -ForegroundColor $(if ($tests.azureResponse.success) { "Green" } else { "Red" })

    if ($data.generalError) {
        Write-Host "❌ General Error: $($data.generalError)" -ForegroundColor Red
    }

    Write-Host ""
    Write-Host "=== РЕКОМЕНДАЦИИ ===" -ForegroundColor Yellow
    foreach ($rec in $data.recommendations) {
        Write-Host "💡 $rec" -ForegroundColor Gray
    }

    # Общий вывод
    Write-Host ""
    $allPassed = $tests.connection.success -and $tests.phoneValidation.success -and $tests.smsSend.success -and $tests.azureResponse.success

    if ($allPassed) {
        Write-Host "🎉 Все тесты прошли успешно!" -ForegroundColor Green
        Write-Host "📱 Проверьте телефон $TestPhoneNumber - должно прийти тестовое SMS" -ForegroundColor Green
        Write-Host ""
        Write-Host "❓ Если SMS не пришло, возможные причины:" -ForegroundColor Yellow
        Write-Host "   - Оператор блокирует сообщения" -ForegroundColor Gray
        Write-Host "   - Недостаточно средств на Azure аккаунте" -ForegroundColor Gray
        Write-Host "   - Номер 1-833-270-2587 не активен для SMS в Azure Portal" -ForegroundColor Gray
        Write-Host "   - Проверьте Azure Communication Services delivery logs" -ForegroundColor Gray
    } else {
        Write-Host "⚠️  Обнаружены проблемы в тестах" -ForegroundColor Red
        Write-Host "📋 Следуйте рекомендациям выше для их решения" -ForegroundColor Yellow
    }

} catch {
    $errorMessage = $_.Exception.Message
    $statusCode = $_.Exception.Response.StatusCode.value__

    Write-Host "❌ Ошибка при вызове API" -ForegroundColor Red
    Write-Host "Status Code: $statusCode" -ForegroundColor Red
    Write-Host "Error: $errorMessage" -ForegroundColor Red

    if ($statusCode -eq 401) {
        Write-Host "💡 Возможно требуется авторизация. Попробуйте:" -ForegroundColor Yellow
        Write-Host "   .\test-sms-diagnostic.ps1 -TestPhoneNumber '$TestPhoneNumber' -AuthToken 'YOUR_JWT_TOKEN'" -ForegroundColor Gray
    }

    if ($statusCode -eq 404) {
        Write-Host "💡 Проверьте что SMS сервис запущен и доступен по адресу:" -ForegroundColor Yellow
        Write-Host "   $ApiUrl" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "=== КОНЕЦ ДИАГНОСТИКИ ===" -ForegroundColor Green