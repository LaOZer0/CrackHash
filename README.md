# CrackHash

Распределенная система для подбора строк по MD5 хешу.

## Описание

Проект представляет собой клиент-серверное приложение для брутфорс-атаки на MD5 хеши. Система состоит из:

- **Manager** — центральный сервер, который принимает задачи от клиентов, распределяет их между воркерами и отслеживает статус выполнения
- **Worker** — рабочие узлы, которые выполняют подбор строк и отправляют результаты менеджеру

## Архитектура

```
┌─────────────┐      ┌─────────────┐
│   Client    │─────▶│   Manager   │
└─────────────┘      └─────────────┘
                          │
          ┌───────────────┼───────────────┐
          ▼               ▼               ▼
     ┌─────────┐     ┌─────────┐     ┌─────────┐
     │ Worker1 │     │ Worker2 │     │ Worker3 │
     └─────────┘     └─────────┘     └─────────┘
```

## Технологии

- .NET 8
- ASP.NET Core Web API
- Docker & Docker Compose
- XML (для обмена данными между Manager и Worker)

## API Менеджера

### POST /api/hash/crack

Отправка задачи на подбор хеша.

**Request Body:**
```json
{
  "hash": "d41d8cd98f00b204e9800998ecf8427e",
  "maxLength": 5
}
```

**Response:**
```json
{
  "requestId": "guid-строка"
}
```

### GET /api/hash/status?requestId={id}

Получение статуса задачи.

**Response:**
```json
{
  "status": "READY|IN_PROGRESS|QUEUED|NOT_FOUND",
  "progress": 0-100,
  "estimatedTimeRemaining": "00:01:30",
  "data": ["найденные строки"]
}
```

### GET /health

Проверка здоровья сервиса.

## Запуск

### Через Docker Compose (рекомендуется)

```bash
docker-compose up --build
```

Система запустит:
- 1 экземпляр Manager на порту 8080
- 3 экземпляра Worker

### Локальная разработка

**Manager:**
```bash
cd Manager
dotnet run
```

**Worker:**
```bash
cd Worker
dotnet run
```

## Пример использования

```bash
# Отправка задачи
curl -X POST http://localhost:8080/api/hash/crack \
  -H "Content-Type: application/json" \
  -d '{"hash": "d41d8cd98f00b204e9800998ecf8427e", "maxLength": 5}'

# Проверка статуса
curl http://localhost:8080/api/hash/status?requestId=<ваш-requestId>
```

## Структура проекта

```
/workspace
├── Manager/           # Сервис менеджера
│   ├── Controllers/   # API контроллеры
│   ├── Models/        # Модели данных
│   ├── Services/      # Бизнес-логика
│   └── data/          # Хранилище состояния
├── Worker/            # Сервис воркера
│   ├── Controllers/   # API контроллеры
│   ├── Models/        # Модели данных
│   ├── Services/      # Логика подбора хешей
│   └── Utils/         # Вспомогательные классы
├── compose.yaml       # Docker Compose конфигурация
└── Dockerfile         # Docker файлы для сборки
```

## Особенности

- **State Persistence** — сохранение состояния задач для восстановления после перезапуска
- **Распределение нагрузки** — задача делится на части между воркерами
- **Health Monitoring** — мониторинг доступности воркеров
- **Progress Tracking** — отслеживание прогресса выполнения задачи

## Лицензия

MIT
