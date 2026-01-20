# Azure Communication Services - Phone Number Checker
# Проверка номеров телефонов в Azure Communication Services

Write-Host "=== Azure Communication Services Phone Number Checker ===" -ForegroundColor Green

# Проверяем установку Azure CLI
Write-Host "Проверяем Azure CLI..." -ForegroundColor Yellow
try {
    $azVersion = az --version
    Write-Host "✅ Azure CLI установлен" -ForegroundColor Green
} catch {
    Write-Host "❌ Azure CLI не установлен. Установите: https://docs.microsoft.com/cli/azure/install-azure-cli" -ForegroundColor Red
    exit 1
}

# Логин в Azure (если нужно)
Write-Host "Проверяем авторизацию в Azure..." -ForegroundColor Yellow
$account = az account show 2>$null
if (-not $account) {
    Write-Host "⚠️  Не авторизован в Azure. Выполняем вход..." -ForegroundColor Yellow
    az login
}

# Параметры из конфигурации
$resourceGroup = Read-Host "Введите имя Resource Group где находится Communication Service"
$communicationServiceName = "aegis-sms"  # По connection string

Write-Host "🔍 Проверяем Communication Service: $communicationServiceName" -ForegroundColor Cyan

# Проверяем существование Communication Service
Write-Host "Проверяем ресурс Communication Service..." -ForegroundColor Yellow
$commService = az communication list --resource-group $resourceGroup --query "[?name=='$communicationServiceName']" | ConvertFrom-Json

if (-not $commService) {
    Write-Host "❌ Communication Service '$communicationServiceName' не найден в группе '$resourceGroup'" -ForegroundColor Red
    Write-Host "Доступные Communication Services:" -ForegroundColor Yellow
    az communication list --resource-group $resourceGroup --query "[].name" -o table
    exit 1
}

Write-Host "✅ Communication Service найден" -ForegroundColor Green

# Проверяем номера телефонов
Write-Host "🔍 Проверяем приобретенные номера телефонов..." -ForegroundColor Cyan

try {
    $phoneNumbers = az communication phonenumber list --connection-string "endpoint=https://aegis-sms.unitedstates.communication.azure.com/;accesskey=52emR4ubCYo9NTrpc5VF8XTjdGFaQVQ9MSr2zFYmz8v7U2jBsEXqJQQJ99BLACULyCpAChBNAAAAAZCSVz7m" | ConvertFrom-Json

    if ($phoneNumbers.Count -eq 0) {
        Write-Host "❌ Номера телефонов не найдены!" -ForegroundColor Red
        Write-Host "💡 Купите номер телефона в Azure Portal:" -ForegroundColor Yellow
        Write-Host "   1. Azure Portal > Communication Services > Phone numbers" -ForegroundColor Gray
        Write-Host "   2. Get phone number > United States > Toll-free" -ForegroundColor Gray
        Write-Host "   3. Убедитесь что выбрана SMS capability" -ForegroundColor Gray
    } else {
        Write-Host "✅ Найдено номеров: $($phoneNumbers.Count)" -ForegroundColor Green

        foreach ($phone in $phoneNumbers) {
            Write-Host "" -ForegroundColor White
            Write-Host "📱 Номер: $($phone.phoneNumber)" -ForegroundColor Cyan
            Write-Host "   Тип: $($phone.phoneNumberType)" -ForegroundColor Gray
            Write-Host "   Возможности: $($phone.capabilities -join ', ')" -ForegroundColor Gray
            Write-Host "   Назначен: $($phone.assignmentType)" -ForegroundColor Gray

            # Проверяем SMS capability
            if ($phone.capabilities -contains 'sms') {
                Write-Host "   ✅ SMS поддерживается" -ForegroundColor Green
            } else {
                Write-Host "   ❌ SMS НЕ поддерживается" -ForegroundColor Red
            }

            # Проверяем соответствие номеру в конфиге
            if ($phone.phoneNumber -eq "+18332702587") {
                Write-Host "   ✅ Это номер из конфигурации!" -ForegroundColor Green
            }
        }
    }
} catch {
    Write-Host "❌ Ошибка при получении номеров: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "💡 Проверьте connection string и права доступа" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🔧 Рекомендации:" -ForegroundColor Green
Write-Host "1. Если номеров нет - купите toll-free номер с SMS capability" -ForegroundColor Gray
Write-Host "2. Если номер есть но SMS не поддерживается - обновите capabilities" -ForegroundColor Gray
Write-Host "3. Обновите appsettings.json с правильным номером" -ForegroundColor Gray
Write-Host "4. Проверьте billing - убедитесь что есть средства на аккаунте" -ForegroundColor Gray

Write-Host ""
Write-Host "=== Проверка завершена ===" -ForegroundColor Green