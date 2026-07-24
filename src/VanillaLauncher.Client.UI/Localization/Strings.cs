namespace VanillaLauncher.Client.UI.Localization;

/// <summary>
/// Единый словарь всех строк интерфейса лаунчера (RU/EN) — заголовки окон, кнопки, статичные
/// подписи, watermark-подсказки, сообщения в журнале/статусе, диалоги и тексты ошибок.
/// Не включает контент встроенной "Инструкции" (<see cref="Client.DefaultGuides"/>) — это
/// admin-редактируемый текст конкретного модпака, а не часть chrome-интерфейса движка, и
/// авто-перевод авторского текста админа не входит в задачу (см. CHANGELOG.md, engine-v1.9.0).
/// Ключ, отсутствующий в словаре, возвращается как есть (см. <see cref="Loc"/>) — опечатка не
/// роняет приложение, а показывает сырой ключ, что легко заметить и поправить.
/// </summary>
internal static class Strings
{
    private static readonly Dictionary<string, (string Ru, string En)> Map = new()
    {
        // --- Общее ---
        ["Common.AppTitle"] = ("VanillaLauncher", "VanillaLauncher"),
        ["Common.AdminWindowTitle"] = ("VanillaLauncher — Admin", "VanillaLauncher — Admin"),
        ["Common.Browse"] = ("Обзор...", "Browse..."),
        ["Common.Cancel"] = ("Отмена", "Cancel"),
        ["Common.Save"] = ("Сохранить", "Save"),
        ["Common.Guide"] = ("Инструкция", "Guide"),
        ["Common.Error"] = ("Ошибка: {0}", "Error: {0}"),
        ["Common.LangRu"] = ("РУС", "RUS"),
        ["Common.LangEn"] = ("ENG", "ENG"),

        // --- App.xaml.cs ---
        ["App.UnhandledError.Message"] = (
            "Произошла непредвиденная ошибка:\n\n{0}\n\nЛаунчер попробует продолжить работу.",
            "An unexpected error occurred:\n\n{0}\n\nThe launcher will try to keep running."),
        ["App.UnhandledError.Title"] = ("VanillaLauncher — ошибка", "VanillaLauncher — Error"),

        // --- MainWindow ---
        ["Main.StatusInitial"] = ("Проверка манифеста...", "Checking manifest..."),
        ["Main.CheckButton"] = ("Проверить обновления", "Check for Updates"),
        ["Main.UpdateButton"] = ("Обновить", "Update"),
        ["Main.SetupButton"] = ("Настроить лаунчер", "Set Up Launcher"),
        ["Main.GuideButton"] = ("Инструкция", "Guide"),
        ["Main.AdminButton"] = ("Режим администратора", "Admin Mode"),
        ["Main.SelectProfileRootButton"] = ("Сменить папку сборки", "Change Build Folder"),
        ["Main.CheckEngineUpdateButton"] = ("Проверить обновление лаунчера", "Check Launcher Update"),
        ["Main.UpdateEngineButton"] = ("Обновить лаунчер", "Update Launcher"),
        ["Main.LogHeader"] = ("Журнал:", "Log:"),
        ["Main.CopySelected"] = ("Копировать выделенное", "Copy Selected"),
        ["Main.CopyAll"] = ("Копировать всё", "Copy All"),

        ["Main.ConfigError.Status"] = ("Ошибка конфигурации лаунчера.", "Launcher configuration error."),
        ["Main.ConfigError.Log"] = ("Не удалось загрузить appsettings.json: {0}", "Failed to load appsettings.json: {0}"),
        ["Main.NotConfigured.Status"] = (
            "Лаунчер не настроен под модпак. Нажми «Настроить лаунчер» и впиши данные от администратора, либо обратись к нему за готовым appsettings.json.",
            "The launcher isn't set up for a modpack yet. Click \"Set Up Launcher\" and enter the details from your admin, or ask them for a ready-made appsettings.json."),
        ["Main.NotConfigured.Log"] = (
            "ManifestUrl/GitHubOwner/GitHubRepo не заданы — это неадаптированная сборка движка.",
            "ManifestUrl/GitHubOwner/GitHubRepo aren't set — this is an unconfigured engine build."),
        ["Main.ProfileRootAutoDetected.Log"] = ("Папка сборки определена автоматически: {0}", "Build folder auto-detected: {0}"),
        ["Main.ProfileRootNotFound.Status"] = ("Не найдена папка сборки Minecraft — укажи её вручную.", "Minecraft build folder not found — please select it manually."),
        ["Main.ProfileRootNotFound.Log"] = (
            "Папка «{0}» не существует на этой машине, автоопределение не нашло совпадений.",
            "Folder \"{0}\" doesn't exist on this machine, auto-detection found no match."),
        ["Main.SetupDone.Log"] = ("Настроено вручную: GitHubOwner={0}, GitHubRepo={1}.", "Configured manually: GitHubOwner={0}, GitHubRepo={1}."),
        ["Main.SelectProfileRootDialog.Title"] = (
            "Выбери папку сборки Minecraft (профиль CurseForge/лаунчера с модами)",
            "Select your Minecraft build folder (CurseForge/launcher profile with mods)"),
        ["Main.SelectProfileRoot.Log"] = ("Папка сборки установлена: {0}", "Build folder set: {0}"),
        ["Main.CheckingManifest.Log"] = ("Проверка манифеста...", "Checking manifest..."),
        ["Main.ManifestInfo.Log"] = ("Версия сборки: {0}, файлов в манифесте: {1}", "Build version: {0}, files in manifest: {1}"),
        ["Main.UpToDate.Status"] = ("Сборка актуальна (версия {0}).", "Build is up to date (version {0})."),
        ["Main.UpToDate.Log"] = ("Расхождений не найдено.", "No differences found."),
        ["Main.UpdateNeeded.Status"] = ("Требуется обновление: {0} из {1} файлов.", "Update needed: {0} of {1} files."),
        ["Main.UpdateNeeded.Log"] = ("Требуют обновления: {0} из {1}", "Need updating: {0} of {1}"),
        ["Main.CheckUpdatesError.Status"] = ("Ошибка проверки обновлений.", "Error checking for updates."),
        ["Main.Downloading.Log"] = ("Скачивание", "Downloading"),
        ["Main.DownloadDone.Log"] = ("Готово", "Done"),
        ["Main.UpdateComplete.Log"] = ("Докачка завершена.", "Download complete."),
        ["Main.UpdateError.Status"] = ("Ошибка обновления.", "Update error."),
        ["Main.EngineCheck.Status"] = ("Проверка обновлений лаунчера...", "Checking launcher updates..."),
        ["Main.EngineUpdateAvailable.Status"] = ("Доступно обновление лаунчера: {0} (у вас {1}).", "Launcher update available: {0} (you have {1})."),
        ["Main.EngineUpdateAvailable.Log"] = ("Найдено обновление лаунчера: {0} (сейчас {1}).", "Found launcher update: {0} (currently {1})."),
        ["Main.EngineUpToDate.Status"] = ("У вас последняя версия лаунчера ({0}).", "You have the latest launcher version ({0})."),
        ["Main.EngineUpToDate.Log"] = ("Обновлений лаунчера не найдено (у вас {0}).", "No launcher updates found (you have {0})."),
        ["Main.EngineCheckError.Status"] = ("Ошибка проверки обновлений лаунчера.", "Error checking launcher updates."),
        ["Main.EngineCheckError.Log"] = ("Ошибка проверки обновлений лаунчера: {0}", "Error checking launcher updates: {0}"),
        ["Main.EngineUpdateConfirm.Text"] = (
            "Доступна новая версия лаунчера: {0} (у вас {1}).\nЛаунчер скачает обновление, закроется и перезапустится с новой версией. Продолжить?",
            "A new launcher version is available: {0} (you have {1}).\nThe launcher will download the update, close, and restart on the new version. Continue?"),
        ["Main.EngineUpdateConfirm.Title"] = ("Обновление лаунчера", "Launcher Update"),
        ["Main.EngineDownloading.Status"] = ("Скачивание обновления лаунчера...", "Downloading launcher update..."),
        ["Main.EngineUpdateDone.Log"] = ("Обновление скачано — лаунчер перезапускается.", "Update downloaded — launcher is restarting."),
        ["Main.EngineUpdateError.Status"] = ("Ошибка обновления лаунчера.", "Launcher update error."),
        ["Main.EngineUpdateError.Log"] = ("Ошибка обновления лаунчера: {0}", "Launcher update error: {0}"),

        // --- AdminWindow ---
        ["Admin.StatusStopped"] = ("Сервер остановлен.", "Server stopped."),
        ["Admin.StartButton"] = ("Запустить сервер", "Start Server"),
        ["Admin.StopButton"] = ("Остановить сервер", "Stop Server"),
        ["Admin.SelectServerDirectoryButton"] = ("Выбрать папку сервера", "Select Server Folder"),
        ["Admin.SettingsButton"] = ("Настройки", "Settings"),
        ["Admin.GuideButton"] = ("Инструкция", "Guide"),
        ["Admin.RecreateWorldButton"] = ("Пересоздать мир (с бэкапом)", "Recreate World (with Backup)"),
        ["Admin.VersionLabel"] = ("Версия релиза:", "Release version:"),
        ["Admin.VersionWatermark"] = ("например: 1.2.3", "e.g. 1.2.3"),
        ["Admin.VersionWatermarkPrevious"] = ("было: {0}", "previous: {0}"),
        ["Admin.PublishButton"] = ("Опубликовать обновление", "Publish Update"),
        ["Admin.ConsoleHeader"] = ("Консоль сервера:", "Server console:"),

        ["Admin.SelectServerDirDialog.Title"] = ("Выбери (или создай) папку сервера", "Select (or create) the server folder"),
        ["Admin.ServerDirAutoDetected.Status"] = ("Серверная папка определена автоматически: {0}", "Server folder auto-detected: {0}"),
        ["Admin.ServerDirAutoDetected.Log"] = ("ServerDirectory автоопределён: {0}", "ServerDirectory auto-detected: {0}"),
        ["Admin.ServerDirMissing.Status"] = ("ServerDirectory не задан — укажи папку сервера вручную.", "ServerDirectory isn't set — please select the server folder manually."),
        ["Admin.ServerDirSet.Log"] = ("Папка сервера установлена: {0}", "Server folder set: {0}"),

        ["Admin.ServerStarting.Status"] = ("Сервер запускается...", "Server starting..."),
        ["Admin.ServerStartError.Status"] = ("Ошибка запуска.", "Startup error."),
        ["Admin.ServerStopping.Status"] = ("Останавливаем сервер (ждём штатного завершения)...", "Stopping server (waiting for graceful shutdown)..."),
        ["Admin.ServerStopTimeout.Status"] = ("Сервер не завершился штатно за 60 секунд.", "Server didn't shut down gracefully within 60 seconds."),
        ["Admin.ServerStopTimeout.Log"] = ("Сервер не ответил на stop за отведённое время — возможно, завис.", "Server didn't respond to stop within the time limit — it may be hung."),
        ["Admin.ServerStopped.MessageBox"] = ("Сервер остановлен.", "Server stopped."),

        ["Admin.RecreateWorldRunning.MessageBox"] = (
            "Сначала останови сервер — пересоздавать мир на работающем сервере нельзя.",
            "Stop the server first — you can't recreate the world while it's running."),
        ["Admin.RecreateWorldConfirm.Text"] = (
            "Мир «{0}» будет забэкаплен в backups/, затем удалён. Новый мир сгенерируется при следующем запуске сервера. Продолжить?",
            "World \"{0}\" will be backed up to backups/, then deleted. A new world will be generated on the next server start. Continue?"),
        ["Admin.RecreateWorldConfirm.Title"] = ("Пересоздать мир", "Recreate World"),
        ["Admin.RecreateWorldBackingUp.Status"] = ("Бэкап мира «{0}»...", "Backing up world \"{0}\"..."),
        ["Admin.RecreateWorldBackingUp.Log"] = ("Пересоздание мира «{0}»: бэкап...", "Recreating world \"{0}\": backing up..."),
        ["Admin.RecreateWorldMissing.Status"] = ("Мир «{0}» не найден — нечего было пересоздавать.", "World \"{0}\" not found — nothing to recreate."),
        ["Admin.RecreateWorldMissing.Log"] = ("Папка мира отсутствовала, бэкап не создавался.", "World folder was missing, no backup was created."),
        ["Admin.RecreateWorldDone.Status"] = ("Мир «{0}» пересоздан.", "World \"{0}\" recreated."),
        ["Admin.RecreateWorldDone.Log"] = ("Бэкап сохранён: {0}", "Backup saved: {0}"),
        ["Admin.RecreateWorldDone.Log2"] = ("Папка мира удалена. Новый мир сгенерируется при следующем запуске сервера.", "World folder deleted. A new world will be generated on the next server start."),
        ["Admin.RecreateWorldError.Status"] = ("Ошибка пересоздания мира.", "Error recreating world."),

        ["Admin.PublishNoVersion.MessageBox"] = ("Укажи версию релиза (например, 26.1.2-b2).", "Enter a release version (e.g. 26.1.2-b2)."),
        ["Admin.PublishNoRepo.MessageBox"] = ("GitHubOwner/GitHubRepo не заданы в appsettings.json.", "GitHubOwner/GitHubRepo aren't set in appsettings.json."),
        ["Admin.PublishConfirm.Text"] = (
            "Будет опубликован релиз «{0}» из {1}:\n1. Бэкап мира\n2. Остановка сервера (если запущен)\n3. Обновление серверных модов/конфигов\n4. Генерация и загрузка manifest.json + файлов в новый GitHub Release.\n\nСервер после публикации НЕ запускается обратно автоматически — запусти его сам кнопкой «Запустить сервер», когда будешь готов.\n\nЭто создаст ПУБЛИЧНЫЙ релиз в репозитории. Продолжить?",
            "Release \"{0}\" from {1} will be published:\n1. World backup\n2. Stop server (if running)\n3. Update server mods/configs\n4. Generate and upload manifest.json + files to a new GitHub Release.\n\nThe server will NOT restart automatically after publishing — start it yourself with the \"Start Server\" button when you're ready.\n\nThis will create a PUBLIC release in the repository. Continue?"),
        ["Admin.PublishConfirm.Title"] = ("Опубликовать обновление", "Publish Update"),
        ["Admin.Publishing.Status"] = ("Публикация «{0}»...", "Publishing \"{0}\"..."),
        ["Admin.PublishDone.Status"] = ("Опубликовано: {0}.", "Published: {0}."),
        ["Admin.PublishDone.MessageBox"] = (
            "Релиз «{0}» опубликован. Сервер остановлен — запусти его сам, когда будешь готов.",
            "Release \"{0}\" published. The server is stopped — start it yourself when you're ready."),
        ["Admin.PublishError.Status"] = ("Ошибка публикации.", "Publish error."),
        ["Admin.PublishError.Log"] = ("Ошибка: {0}", "Error: {0}"),
        ["Admin.PublishError.MessageBox"] = ("Публикация не удалась: {0}", "Publish failed: {0}"),

        ["Admin.CloseConfirm.Text"] = ("Сервер всё ещё запущен. Остановить его и закрыть лаунчер?", "The server is still running. Stop it and close the launcher?"),
        ["Admin.ClosingStoppingServer.Status"] = ("Останавливаем сервер перед закрытием лаунчера...", "Stopping server before closing the launcher..."),
        ["Admin.ClosingStoppingServer.Log"] = ("Останавливаем сервер перед закрытием лаунчера...", "Stopping server before closing the launcher..."),
        ["Admin.ClosingStopTimeout.MessageBox"] = (
            "Сервер не остановился штатно за 60 секунд — закрытие отменено, лаунчер остаётся открытым. Проверь консоль сервера и попробуй ещё раз.",
            "The server didn't stop gracefully within 60 seconds — closing was cancelled, the launcher stays open. Check the server console and try again."),
        ["Admin.ClosingStopped.MessageBox"] = ("Сервер остановлен. Лаунчер закрывается.", "Server stopped. The launcher is closing."),

        // --- SettingsWindow ---
        ["Settings.Title"] = ("VanillaLauncher — Настройки", "VanillaLauncher — Settings"),
        ["Settings.Intro"] = (
            "Настройки видны и редактируются только в Admin-режиме — обычный пользователь сюда не попадает. Поля со звёздочкой (",
            "Settings are only visible and editable in Admin Mode — regular players never see this screen. Fields marked with an asterisk ("),
        ["Settings.IntroAfterStar"] = (") обязательны для базовой работы лаунчера.", ") are required for the launcher to work."),
        ["Settings.ProfileRootLabel"] = ("Путь клиентской сборки (ProfileRoot) *", "Client build path (ProfileRoot) *"),
        ["Settings.ClientFolderNameLabel"] = ("Ожидаемое имя папки сборки (для автопоиска)", "Expected build folder name (for auto-detection)"),
        ["Settings.ClientFolderNameHint"] = (
            "Только ИМЯ папки (не путь), например MyModpack — так называется папка инстанса в CurseForge/твоём лаунчере у КАЖДОГО игрока, независимо от диска и имени пользователя. Лаунчер ищет прямую подпапку с таким именем (без учёта регистра) в каждом из корней ниже по порядку — не рекурсивно, вложенные глубже подпапки не проверяются. Пусто — автопоиск клиента отключён, каждый игрок будет указывать путь вручную (кнопка «Обзор» в его окне).",
            "Just the folder NAME (not a path), e.g. MyModpack — this is the instance folder name in CurseForge/your launcher for EVERY player, regardless of drive or username. The launcher looks for a direct subfolder with this name (case-insensitive) in each root below, in order — not recursive, deeper nested subfolders aren't checked. Empty — client auto-detection is disabled, every player will pick the folder manually (\"Browse\" button in their window)."),
        ["Settings.ClientSearchRootsLabel"] = (
            "Корни поиска клиента (по одному пути на строку; можно использовать переменные окружения, например %USERPROFILE%\\curseforge\\Instances — у каждого друга свой пользователь/диск)",
            "Client search roots (one path per line; environment variables are supported, e.g. %USERPROFILE%\\curseforge\\Instances — every friend has their own username/drive)"),
        ["Settings.AutoDetectButton"] = ("Найти автоматически", "Auto-Detect"),
        ["Settings.ServerDirectoryLabel"] = ("Путь серверной директории (ServerDirectory) *", "Server directory path (ServerDirectory) *"),
        ["Settings.ServerFolderNameLabel"] = ("Ожидаемое имя папки сервера (для автопоиска, рядом с .exe)", "Expected server folder name (for auto-detection, next to the .exe)"),
        ["Settings.ServerFolderNameHint"] = (
            "Только ИМЯ папки сервера (не путь) — актуально, если лаунчер лежит не прямо в папке сервера, а рядом с ней (например, обе — подпапки одной общей папки). Работает так же, как имя папки клиента выше: прямая подпапка, без учёта регистра, не рекурсивно. Папка рядом с самим exe проверяется всегда, даже если это поле пустое.",
            "Just the server folder NAME (not a path) — relevant when the launcher isn't directly inside the server folder but next to it (e.g. both are subfolders of one parent folder). Works the same as the client folder name above: a direct subfolder, case-insensitive, not recursive. The folder next to the .exe itself is always checked, even if this field is empty."),
        ["Settings.ServerSearchRootsLabel"] = (
            "Доп. корни поиска сервера (по одному пути на строку, тоже с поддержкой %VAR%; папка рядом с .exe проверяется всегда)",
            "Additional server search roots (one path per line, %VAR% supported too; the folder next to the .exe is always checked)"),
        ["Settings.ServerBatFileNameLabel"] = ("Имя .bat-файла сервера (ServerBatFileName)", "Server .bat file name (ServerBatFileName)"),
        ["Settings.ManifestUrlLabel"] = ("ManifestUrl *", "ManifestUrl *"),
        ["Settings.ManifestUrlHint"] = (
            "Ссылка на manifest.json — список файлов сборки для клиента. Обычно не нужно вычислять вручную: если ниже задан репозиторий модпака (GitHubOwner/GitHubRepo) и хотя бы раз опубликовано обновление, подходит https://github.com/{Owner}/{Repo}/releases/latest/download/manifest.json — этот адрес всегда указывает на файл из САМОГО СВЕЖЕГО релиза.",
            "Link to manifest.json — the client's build file list. Usually no need to compute manually: if the modpack repository (GitHubOwner/GitHubRepo) is set below and at least one update has been published, https://github.com/{Owner}/{Repo}/releases/latest/download/manifest.json works — this address always points to the file from the LATEST release."),
        ["Settings.ModpackRepoHeader"] = (
            "Репозиторий МОДПАКА — куда публикуются моды/manifest.json (GitHubOwner / GitHubRepo)",
            "MODPACK repository — where mods/manifest.json get published (GitHubOwner / GitHubRepo)"),
        ["Settings.ModpackRepoHint"] = (
            "GitHubOwner — имя пользователя или организации на GitHub (например, MyName). GitHubRepo — имя конкретного репозитория внутри этого аккаунта (например, MyModpack-server). Вместе они указывают на один репозиторий: github.com/{GitHubOwner}/{GitHubRepo}. Можно вставить ссылку целиком в любое из двух полей — лишнее уберётся само при сохранении.",
            "GitHubOwner — a GitHub username or organization (e.g. MyName). GitHubRepo — the specific repository name within that account (e.g. MyModpack-server). Together they point to one repository: github.com/{GitHubOwner}/{GitHubRepo}. You can paste the full link into either field — the extra part is stripped automatically on save."),
        ["Settings.GitHubOwnerLabel"] = ("GitHubOwner *", "GitHubOwner *"),
        ["Settings.GitHubRepoLabel"] = ("GitHubRepo *", "GitHubRepo *"),
        ["Settings.EngineRepoHeader"] = (
            "Репозиторий ДВИЖКА — откуда качать новую версию самого exe (EngineGitHubOwner / EngineGitHubRepo)",
            "ENGINE repository — where to download new exe versions from (EngineGitHubOwner / EngineGitHubRepo)"),
        ["Settings.EngineRepoHint"] = (
            "Та же пара полей (Owner/Repo), но про ДРУГОЙ репозиторий: не тот, что публикует контент модпака выше, а репозиторий с исходным кодом самого лаунчера. Используется кнопкой «Проверить обновление лаунчера» в главном окне. Пусто = используется значение по умолчанию (видно водяным знаком в полях ниже) — самообновление работает и без явной настройки этих двух полей, если только это не форк движка со своим репозиторием.",
            "The same pair of fields (Owner/Repo), but for a DIFFERENT repository: not the one publishing modpack content above, but the one with the launcher's own source code. Used by the \"Check Launcher Update\" button in the main window. Empty = the default value is used (shown as a watermark in the fields below) — self-update works without explicitly setting these two fields, unless this is a fork of the engine with its own repository."),
        ["Settings.EngineOwnerLabel"] = ("EngineGitHubOwner", "EngineGitHubOwner"),
        ["Settings.EngineOwnerHint"] = ("Pewdoloco (по умолчанию)", "Pewdoloco (default)"),
        ["Settings.EngineRepoLabel"] = ("EngineGitHubRepo", "EngineGitHubRepo"),
        ["Settings.EngineRepoWatermark"] = ("khanneli-local-launcher-minecraft (по умолчанию)", "khanneli-local-launcher-minecraft (default)"),
        ["Settings.IncludeFoldersLabel"] = ("IncludeFolders (по одной папке на строку)", "IncludeFolders (one folder per line)"),
        ["Settings.ExcludeModsLabel"] = ("ServerExcludeMods (по одному имени файла на строку)", "ServerExcludeMods (one file name per line)"),
        ["Settings.ExcludeModsHint"] = (
            "Здесь только исключения — не полный список модов сборки, а лишь те, что НЕ должны попасть на сервер. Это и есть итоговый список исключений: его можно править прямо здесь текстом (дописать/удалить строку) в любой момент. Кнопка ниже — просто более удобный способ его заполнить: она сканирует папку с модами и позволяет отмечать файлы чекбоксами вместо ручного набора имён; результат сразу подставляется в это поле, ничего отдельно нажимать не нужно.",
            "This is exclusions only — not the full mod list, just the ones that must NOT end up on the server. This is the actual exclusion list: you can edit it as plain text right here (add/remove a line) at any time. The button below is just a more convenient way to fill it: it scans the mods folder and lets you check files instead of typing names by hand; the result is placed into this field immediately, nothing else to click."),
        ["Settings.PickExcludedModsButton"] = ("Выбрать моды из папки...", "Pick Mods from Folder..."),
        ["Settings.MaxBackupsLabel"] = ("MaxBackupsToKeep", "MaxBackupsToKeep"),
        ["Settings.GuidesHeader"] = ("Руководства (показываются по кнопке «Инструкция» в окнах лаунчера)", "Guides (shown by the \"Guide\" button in the launcher windows)"),
        ["Settings.GuidesHint"] = (
            "Поля только для чтения, пока не нажата «Редактировать» — это защита от случайного клика/ввода в текст руководства во время правки других настроек рядом. Во время редактирования поле увеличивается для удобства печати, после «Готово» уменьшается обратно.",
            "Fields are read-only until \"Edit\" is clicked — this protects against accidentally clicking/typing into the guide text while editing other nearby settings. While editing, the field grows for easier typing, then shrinks back after \"Done\"."),
        ["Settings.ClientGuideShortHeader"] = ("Клиент — краткое", "Client — Short"),
        ["Settings.ClientGuideFullHeader"] = ("Клиент — полное", "Client — Full"),
        ["Settings.ClientGuideManualHeader"] = ("Клиент — пошаговое руководство", "Client — Step-by-Step Guide"),
        ["Settings.AdminGuideShortHeader"] = ("Админ — краткое", "Admin — Short"),
        ["Settings.AdminGuideFullHeader"] = ("Админ — полное", "Admin — Full"),
        ["Settings.AdminGuideManualHeader"] = ("Админ — пошаговое руководство", "Admin — Step-by-Step Guide"),
        ["Settings.EditButton"] = ("Редактировать", "Edit"),
        ["Settings.DoneButton"] = ("Готово", "Done"),
        ["Settings.MaxBackupsError"] = (
            "MaxBackupsToKeep должен быть целым числом ≥ 1 (нужно хранить хотя бы один бэкап).",
            "MaxBackupsToKeep must be an integer ≥ 1 (at least one backup must be kept)."),
        ["Settings.AutoDetectClientError"] = ("Автопоиск клиентской папки не нашёл совпадений — укажи путь вручную (Обзор...).", "Client folder auto-detection found no match — select the path manually (Browse...)."),
        ["Settings.AutoDetectServerError"] = ("Автопоиск серверной папки не нашёл совпадений — укажи путь вручную (Обзор...).", "Server folder auto-detection found no match — select the path manually (Browse...)."),
        ["Settings.BrowseProfileRootDialog.Title"] = ("Выбери папку клиентской сборки", "Select the client build folder"),
        ["Settings.BrowseServerDirDialog.Title"] = ("Выбери (или создай) папку сервера", "Select (or create) the server folder"),
        ["Settings.LanguageHeader"] = ("Язык интерфейса", "Interface Language"),

        // --- GuideWindow ---
        ["Guide.RoleUser"] = ("Пользователь", "Player"),
        ["Guide.RoleAdmin"] = ("Администратор", "Admin"),
        ["Guide.LengthShort"] = ("Краткое", "Short"),
        ["Guide.LengthFull"] = ("Полное", "Full"),
        ["Guide.LengthManual"] = ("Пошаговое руководство", "Step-by-Step Guide"),
        ["Guide.TitleUser"] = ("Инструкция — Пользователь", "Guide — Player"),
        ["Guide.TitleAdmin"] = ("Инструкция — Администратор", "Guide — Admin"),

        // --- ClientSetupWindow ---
        ["ClientSetup.Title"] = ("Настройка лаунчера", "Launcher Setup"),
        ["ClientSetup.Intro"] = (
            "Эти значения тебе должен сообщить администратор сервера — не придумывай их сам. Это не пароль и не секрет, обычно просто пара слов текстом в чате. Поля со звёздочкой (*) обязательны.",
            "Your server admin should give you these values — don't guess them yourself. This isn't a password or a secret, usually just a couple of words in chat. Fields marked with an asterisk (*) are required."),
        ["ClientSetup.OwnerLabel"] = ("GitHubOwner (владелец репозитория) *", "GitHubOwner (repository owner) *"),
        ["ClientSetup.OwnerHint"] = ("Имя пользователя или организации на GitHub, например MyName.", "A GitHub username or organization, e.g. MyName."),
        ["ClientSetup.RepoLabel"] = ("GitHubRepo (репозиторий модпака) *", "GitHubRepo (modpack repository) *"),
        ["ClientSetup.RepoHint"] = (
            "Имя репозитория, например MyModpack-server. Если прислали ссылку целиком (https://github.com/Owner/Repo) — можно вставить как есть, лишнее уберётся само при сохранении.",
            "The repository name, e.g. MyModpack-server. If you were sent the full link (https://github.com/Owner/Repo) — you can paste it as-is, the extra part is stripped automatically on save."),
        ["ClientSetup.FolderNameLabel"] = ("Имя папки сборки (необязательно)", "Build folder name (optional)"),
        ["ClientSetup.FolderNameHint"] = (
            "Имя ТОЛЬКО папки твоей сборки Minecraft (не весь путь) — например, MyModpack. Лаунчер сам поищет папку с таким именем внутри стандартного пути CurseForge и не будет спрашивать её вручную, если найдёт. Можно оставить пустым — тогда лаунчер один раз спросит папку сборки отдельным диалогом (кнопка «Обзор»), дальше запомнит выбор.",
            "Just the NAME of your Minecraft build folder (not the full path) — e.g. MyModpack. The launcher will look for a folder with this name inside the standard CurseForge path and won't ask manually if found. You can leave it empty — the launcher will then ask once via a separate dialog (\"Browse\" button) and remember your choice."),
        ["ClientSetup.MissingFieldsError"] = (
            "GitHubOwner и GitHubRepo обязательны — уточни у администратора сервера, если не знаешь их.",
            "GitHubOwner and GitHubRepo are required — ask your server admin if you don't know them."),

        // --- SelectExcludedModsWindow ---
        ["SelectMods.Title"] = ("Выбрать моды для ServerExcludeMods", "Select Mods for ServerExcludeMods"),
        ["SelectMods.FolderLabel"] = ("Папка с модами (обычно ProfileRoot\\mods):", "Mods folder (usually ProfileRoot\\mods):"),
        ["SelectMods.SearchButton"] = ("Найти", "Search"),
        ["SelectMods.Instruction"] = ("Отметь моды, которые НЕ должны попадать на сервер:", "Check the mods that must NOT end up on the server:"),
        ["SelectMods.PrevPage"] = ("◀ Пред.", "◀ Prev"),
        ["SelectMods.NextPage"] = ("След. ▶", "Next ▶"),
        ["SelectMods.ApplyButton"] = ("Применить", "Apply"),
        ["SelectMods.BrowseDialog.Title"] = ("Выбери папку с модами", "Select the mods folder"),
        ["SelectMods.FolderNotFound"] = ("Папка не найдена.", "Folder not found."),
        ["SelectMods.NoJars"] = ("В папке нет .jar файлов.", "No .jar files in this folder."),
        ["SelectMods.AutoDetectNone"] = (
            "Моды с честным \"environment\": \"client\" в метаданных не найдены — не значит, что клиентских модов нет, часть из них не декларирует это поле вообще.",
            "No mods found with an honest \"environment\": \"client\" in their metadata — this doesn't mean there are no client-only mods, some just don't declare this field at all."),
        ["SelectMods.AutoDetectSome"] = (
            "Автоматически отмечено как client-only по метаданным (\"environment\": \"client\"): {0}. Часть модов заявляет универсальность (\"*\"), но на деле клиентская и падает на сервере — это НЕ определяется автоматически, проверяй запуском сервера.",
            "Automatically flagged as client-only by metadata (\"environment\": \"client\"): {0}. Some mods claim to be universal (\"*\") but are actually client-only and crash the server — this is NOT detected automatically, verify by starting the server."),
        ["SelectMods.AutoDetectSuffix"] = ("  [авто: environment=client]", "  [auto: environment=client]"),
        ["SelectMods.NoResults"] = ("Ничего не найдено", "Nothing found"),
        ["SelectMods.PageInfo"] = ("Страница {0} из {1} ({2} файлов)", "Page {0} of {1} ({2} files)"),

        // --- AdminLoginWindow ---
        ["AdminLogin.Title"] = ("Вход в режим администратора", "Admin Mode Login"),
        ["AdminLogin.PasswordLabel"] = ("Пароль:", "Password:"),
        ["AdminLogin.LoginButton"] = ("Войти", "Log In"),
        ["AdminLogin.WrongPassword"] = ("Неверный пароль.", "Incorrect password."),

        // --- SetAdminPasswordWindow ---
        ["SetPassword.Title"] = ("Задать пароль администратора", "Set Admin Password"),
        ["SetPassword.Intro"] = ("Это первый запуск Admin-режима на этой машине — задай пароль.", "This is the first time Admin Mode runs on this machine — set a password."),
        ["SetPassword.PasswordLabel"] = ("Пароль:", "Password:"),
        ["SetPassword.ConfirmLabel"] = ("Повтори пароль:", "Confirm password:"),
        ["SetPassword.SetButton"] = ("Задать пароль", "Set Password"),
        ["SetPassword.EmptyError"] = ("Пароль не может быть пустым.", "Password can't be empty."),
        ["SetPassword.MismatchError"] = ("Пароли не совпадают.", "Passwords don't match."),
    };

    public static string Get(string key, string language)
    {
        if (!Map.TryGetValue(key, out var pair))
            return key;

        return language == "en" ? pair.En : pair.Ru;
    }
}
