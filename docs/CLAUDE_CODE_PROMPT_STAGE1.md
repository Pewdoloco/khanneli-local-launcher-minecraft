# Промпт для Claude Code — Этап 1

Скопируй это сообщение в Claude Code, когда откроешь репозиторий локально:

---

Прочитай `docs/SPEC.md`, `docs/ARCHITECTURE.md` и `docs/TASKS.md`.

В `src/VanillaLauncher.Client/` уже есть стартовый скелет (.NET 8, консоль):
ManifestModel.cs, ManifestService.cs, HashService.cs, UpdateService.cs, Downloader.cs, Program.cs, csproj.

Задачи для завершения Этапа 1:
1. Проверь, что проект собирается (`dotnet build`) и поправь возможные ошибки компиляции.
2. Вынеси захардкоженные значения из `Program.cs` (manifestUrl, profileRoot) в `AppConfig.cs`,
   читаемый из `appsettings.json` рядом с exe.
3. Напиши небольшой скрипт/утилиту `tools/GenerateManifest` (консольная), которая проходит по
   папке сборки и генерирует `manifest.json` (path, sha256, size, url) — понадобится для публикации
   релизов на Этапе 5, но полезно уже сейчас, чтобы сгенерировать тестовый manifest.json.
4. Прогони на реальной папке `D:\Games\curseforge\Instances\VanillaScary`: сгенерируй manifest,
   положи рядом пару изменённых файлов, убедись что UpdateService правильно находит расхождения.
5. Добавь базовые unit-тесты на HashService и UpdateService (xUnit).
6. Обнови `docs/TASKS.md`, отметь Этап 1 выполненным, зафиксируй в CHANGELOG.md (создай, если нет)
   что именно сделано.
7. Закоммить с понятным сообщением, не трогая файлы других этапов.

Не начинай UI (WPF) и Admin-модуль — это отдельные этапы.
