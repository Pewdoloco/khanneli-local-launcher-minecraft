# Дорожная карта

Каждый этап — отдельная сессия/PR, чтобы не размазывать работу и не упираться в лимиты.

- [x] Этап 0 — спецификация и архитектура (этот пакет)
- [x] **Этап 1 — Client core (без UI)**: manifest, хеш-сверка, докачка (см. src/VanillaLauncher.Client). Детали в CHANGELOG.md.
- [x] **Этап 2 — Client UI (WPF)**: статус, кнопка "Обновить", прогресс докачки (см. src/VanillaLauncher.Client.UI). Детали в CHANGELOG.md.
- [x] **Этап 3 — Admin**: обёртка над `.bat`, старт/стоп с корректным `stop`, лог консоли (см. src/VanillaLauncher.Admin, AdminWindow в src/VanillaLauncher.Client.UI). Без авторизации — она отдельно на Этапе 6. Детали в CHANGELOG.md.
- [x] **Этап 4 — Admin**: пересоздание мира с обязательным бэкапом, ротация бэкапов (см. WorldBackupService в src/VanillaLauncher.Admin). Детали в CHANGELOG.md.
- [x] **Этап 5 — Admin**: публикация обновления одной кнопкой (бэкап→стоп→обновление→старт), генерация manifest.json и загрузка в GitHub Release (см. PublishPipeline, ReleasePublisher, GitHubReleaseClient в src/VanillaLauncher.Admin). Покрыто автотестами; **живой прогон на реальный GitHub/Server VS ещё не делался** — осознанно отложено до первого настоящего релиза. Детали в CHANGELOG.md.
- [x] **Этап 6 — Авторизация Admin-режима**: пароль как соль+PBKDF2-хеш, гейт перед AdminWindow (см. AdminAuthService в src/VanillaLauncher.Admin, SetAdminPasswordWindow/AdminLoginWindow в src/VanillaLauncher.Client.UI). Привязка к ПК не реализована — см. CHANGELOG.md, почему. Детали в CHANGELOG.md.
- [x] **Подготовка к релизу** (пост-Этап 6): онбординг выбора папки сборки на первом запуске (каждый друг — своя папка), глобальная обработка необработанных исключений (не крашить приложение), publish-сборка (self-contained single-file exe), инструкции docs/CLIENT_GUIDE.md и docs/ADMIN_GUIDE.md. Детали в CHANGELOG.md.
- [x] **Первый настоящий релиз — `26.1.2-b1`**: живая публикация через Admin, нашла и починила реальную проблему (ServerExcludeMods — клиентские моды ломали дедик-сервер). Сервер запущен на исправленном наборе, клиент независимо сверился с реальным манифестом на GitHub — совпадение 1:1. Детали в CHANGELOG.md.
- [x] **`VanillaLauncher.exe` опубликован** как ассет релиза первого настоящего модпак-деплоя — лаунчер готов к раздаче игрокам.
- [x] **Настройка лаунчера под модпак** (пути клиент/сервер + экран "Настройки" + модель "движок/модпак-репозитории"): см. docs/TASK_PATH_AUTODETECT.md. Автоопределение путей (`PathAutoDetectService`), экран `SettingsWindow` (Admin-only), пустой embedded-дефолт движка (`appsettings.default.json`), CI-публикация `engine-vX.Y.Z`. Фактическое разделение на отдельные GitHub-репозитории "движок"/"модпак" сделано.
- [x] **`engine-v1.7.0`** — вотермарки во всех полях `SettingsWindow`/`ClientSetupWindow`, обязательные поля отмечены `*`, явное окно об итоге публикации (успех/ошибка). Детали в CHANGELOG.md.
- [x] **`engine-v1.8.0`/`engine-v1.8.1`** — полировка встроенной "Инструкции": markdown убран из текста, третий вариант длины "Пошаговое руководство" (`GuideLength.Manual`), фикс недостающего случая 401 Unauthorized в списке ошибок публикации. Детали в CHANGELOG.md, docs/ARCHITECTURE.md.
- [x] **`engine-v1.8.2`** — актуализация ARCHITECTURE.md/TASKS.md, репозиторий очищен от упоминаний конкретных модпак-деплоев (генерик-плейсхолдеры в watermark/докax). Детали в CHANGELOG.md.
- [x] **`engine-v1.9.0`** — тумблер языка интерфейса (RU/EN), минималистичный визуальный стиль (Windows 11/macOS-подобный). Текст встроенной "Инструкции" сознательно не переведён (admin-авторский контент). Детали в CHANGELOG.md, docs/ARCHITECTURE.md.
- [ ] Этап 7 (опционально) — разделение лог/команды консоли, статус игроков, доступ к серверу через Tailscale-проверку

## Как передавать этапы в Claude Code
Открой `docs/SPEC.md` и `docs/ARCHITECTURE.md` в контексте, затем:
> "Реализуй этап N из docs/TASKS.md. Ориентируйся на docs/SPEC.md и docs/ARCHITECTURE.md. Не трогай остальные этапы."

Это экономит его лимит — не нужно пересказывать весь проект заново каждый раз.
