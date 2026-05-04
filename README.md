# VideoHostingByWhoami

Видеохостинговая платформа с клиентом на Avalonia и сервером на ASP.NET Core.

## Архитектура

### Клиент (Avalonia UI)
- **MainWindow** - Главное окно с каталогом видео
- **PlayerWindow** - Видеоплеер с комментариями и лайками
- **UploadWindow** - Загрузка новых видео
- **PlaylistWindow** - Управление плейлистами
- **ModerationWindow** - Модерация видео (для модераторов)
- **NotificationsWindow** - Уведомления

### Сервер (ASP.NET Core)
- REST API с JWT авторизацией
- PostgreSQL база данных
- HLS стриминг видео
- Обработка видео (FFmpeg)
- Система модерации

## Запуск

### 1. База данных

```bash
# С помощью Docker
docker-compose up -d postgres

# Или установите PostgreSQL и создайте БД:
# CREATE DATABASE videohosting;
```

### 2. Сервер

```bash
cd Server
dotnet restore
dotnet run --urls "http://localhost:5000"
```

### 3. Клиент

```bash
dotnet restore
dotnet run
```

## Функции

### Для пользователей
- Регистрация и авторизация (анонимная, без email)
- Просмотр каталога видео
- Поиск и фильтрация по категориям
- Потоковое воспроизведение видео (HLS)
- Комментарии и лайки/дизлайки
- Создание плейлистов

### Для модераторов
- Модерация загруженных видео
- Одобрение/отклонение с указанием причины

### Для авторов
- Загрузка видео с метаданными
- Автоматическая конвертация в HLS
- Получение уведомлений о модерации

## API Endpoints

### Auth
- `POST /api/auth/register` - Регистрация
- `POST /api/auth/login` - Авторизация

### Videos
- `GET /api/videos` - Список видео
- `GET /api/videos/{id}` - Информация о видео
- `GET /api/videos/{id}/stream` - HLS стрим
- `GET /api/videos/{id}/preview` - Превью изображение
- `POST /api/videos` - Загрузка видео (требуется авторизация)
- `PUT /api/videos/{id}` - Обновление видео
- `DELETE /api/videos/{id}` - Удаление видео

### Comments
- `GET /api/comments/video/{videoId}` - Комментарии к видео
- `POST /api/comments` - Добавить комментарий
- `PUT /api/comments/{id}` - Редактировать комментарий
- `DELETE /api/comments/{id}` - Удалить комментарий

### Likes
- `GET /api/likes/status/{videoId}` - Статус лайка пользователя
- `POST /api/likes` - Поставить лайк/дизлайк
- `DELETE /api/likes/{videoId}` - Убрать лайк

### Playlists
- `GET /api/playlists` - Список плейлистов пользователя
- `POST /api/playlists` - Создать плейлист
- `POST /api/playlists/{playlistId}/videos` - Добавить видео в плейлист
- `DELETE /api/playlists/{playlistId}/videos/{videoId}` - Удалить видео из плейлиста
- `DELETE /api/playlists/{playlistId}` - Удалить плейлист

### Moderation
- `GET /api/moderation/pending` - Видео на модерации (для модераторов)
- `POST /api/moderation` - Модерировать видео
- `GET /api/moderation/notifications` - Уведомления пользователя
- `POST /api/moderation/notifications/{id}/read` - Пометить как прочитанное

## Технологии

- **Frontend**: Avalonia UI, .NET 10
- **Backend**: ASP.NET Core, Entity Framework Core
- **Database**: PostgreSQL
- **Video**: FFmpeg, HLS
- **Auth**: JWT Bearer

## Цветовая схема

Фон: `#030712` (темно-синий)
Карточки: `#1F2937`
Акцент: `#3B82F6`
