# Hyperquant - Синтетический арбитраж фьючерсов

## Архитектура

### Микросервисы
- **Hyperquant.Gateway** - API Gateway для внешнего взаимодействия
- **Hyperquant.Bybit** - Сервис для работы с биржей Bybit
- **Hyperquant.Jobs** - Планировщик задач на Quartz.NET
- **Hyperquant.Repository** - Сервис для работы с базой данных
- **Hyperquant.Migrator** - Сервис для миграций PostgreSQL
- **Hyperquant.RabbitMigrator** - Сервис для настройки RabbitMQ

### Библиотеки
- **Hyperquant.Data** - Слой доступа к данным
- **Hyperquant.Models** - Доменные модели
- **Hyperquant.Dto** - DTO модели
- **Hyperquant.Abstraction** - Интерфейсы и абстракции
- **Hyperquant.Aspire.ServiceDefaults** - Общие настройки сервисов

## Установка и запуск

### Конфигурация
1. Настройте переменные окружения для Bybit API в `appsettings.json` или через переменные окружения:
```json
{
  "BYBIT_API_KEY": "your_api_key",
  "BYBIT_API_SECRET": "your_api_secret"
}
```

2. Настройте параметры задачи в `Hyperquant.Jobs/appsettings.json`:
```json
{
  "JobData": {
    "FuturesFirst": "BTCUSDT-18APR25",
    "FuturesSecond": "BTCUSDT-30MAY25",
    "Interval": "OneHour",
    "From": "",
    "To": ""
  }
}
```

### Запуск
1. Запустите Aspire:
```bash
cd Hyperquant.Aspire/Hyperquant.Aspire.AppHost
dotnet run
```

## API Endpoints
У Api Gateway есть swagger, там описание эндпоинта

### POST /start-update
Запускает процесс обновления данных по фьючерсам.

Пример запроса:
```json
{
  "from": "2024-03-01T00:00:00Z",
  "to": "2024-03-31T00:00:00Z",
  "futuresFirst": "BTCUSDT-18APR25",
  "futuresSecond": "BTCUSDT-30MAY25",
  "interval": "OneHour"
}
```

## Мониторинг
- Логи через Serilog
- Метрики и трейсинг через OpenTelemetry
- Состояние сервисов в Aspire Dashboard


## Особенности реализации
- Использование паттерна Repository
- CQRS для разделения операций чтения и записи
- Чистая архитектура с разделением на слои
- Обработка отсутствующих данных с использованием последней доступной цены

## Разработка

### Добавление новой биржи
1. Создайте новый сервис по аналогии с Hyperquant.Bybit
2. Реализуйте интерфейс `IExchangeFuturesUpdate`
3. Добавьте сервис в оркестратор Aspire
