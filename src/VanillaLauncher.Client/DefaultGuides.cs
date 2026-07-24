namespace VanillaLauncher.Client;

/// <summary>
/// Дефолтный текст встроенного окна "Инструкция" (см. AppConfig.ClientGuide*/AdminGuide*).
/// Универсален для любого модпака на этом движке — не упоминает конкретный репозиторий,
/// сервер или адрес. Админ переписывает под свой модпак через экран "Настройки" — с этого
/// момента текст становится авторским контентом конкретного деплоя и не переводится
/// автоматически (см. <see cref="Resolve"/> и <c>GuideWindow.UpdateText</c>): пока в
/// AppConfig лежит НЕТРОНУТЫЙ дефолт (RU или EN), окно "Инструкция" показывает его на
/// текущем языке интерфейса; как только админ его изменил — показывается как есть, на
/// языке, на котором админ его написал.
/// Текст рендерится в обычном read-only TextBox (не Markdown-viewer) — здесь сознательно
/// нет markdown-синтаксиса (#, **), только перенос строк и «кавычки»/"quotes" для названий
/// кнопок.
/// </summary>
public static class DefaultGuides
{
    /// <summary>
    /// Возвращает локализованный дефолт, если <paramref name="current"/> — нетронутый
    /// дефолт движка (RU или EN, вне зависимости от того, на каком языке он был сохранён в
    /// последний раз), иначе возвращает <paramref name="current"/> как есть — админ его
    /// переписал под свой модпак, и это не наш текст, чтобы переводить.
    /// </summary>
    public static string Resolve(string current, string ru, string en, string language) =>
        current == ru || current == en ? (language == "en" ? en : ru) : current;

    public const string ClientShort = """
        Лаунчер держит твою сборку модов в актуальном состоянии.

        1. Запусти exe в отдельной, пустой папке.
        2. Если лаунчер ещё не настроен под модпак — появится кнопка «Настроить лаунчер»:
           впиши GitHubOwner/GitHubRepo, которые сообщит администратор. Если кнопки нет —
           уже настроен, переходи к шагу 3.
        3. Если попросит — укажи папку сборки Minecraft (профиль CurseForge/твоего лаунчера
           с подпапками mods и config). Если найдёт сам по имени — спрашивать не будет.
        4. «Проверить обновления» — сверяет твои файлы с эталоном.
        5. «Обновить» — докачивает то, что изменилось (активна, только если есть расхождения).
        6. «Режим администратора» — не для игрока, просто игнорируй.

        Если что-то не работает — спроси администратора сервера.
        """;

    public const string ClientShortEn = """
        The launcher keeps your mod build up to date.

        1. Run the exe in a separate, empty folder.
        2. If the launcher isn't set up for a modpack yet — a "Set Up Launcher" button will
           appear: enter the GitHubOwner/GitHubRepo your admin gives you. If the button isn't
           there — it's already set up, skip to step 3.
        3. If asked — point it to your Minecraft build folder (CurseForge/your launcher's
           profile with mods and config subfolders). If it finds it by name itself, it won't
           ask.
        4. "Check for Updates" — compares your files against the reference build.
        5. "Update" — downloads whatever changed (active only if there are differences).
        6. "Admin Mode" — not for players, just ignore it.

        If something isn't working — ask your server admin.
        """;

    public const string ClientFull = """
        Как пользоваться лаунчером — клиент

        ЧТО ЭТО
        Программа сверяет твою локальную сборку модов с эталоном (манифестом) и докачивает
        только то, что изменилось. Отдельно ставить/удалять моды вручную не нужно.

        УСТАНОВКА
        1. Возьми у администратора ссылку на exe — подойдёт даже ссылка на Releases
           репозитория движка лаунчера (там лежит универсальный exe без привязки к
           конкретному модпаку). Помести exe в отдельную, пустую папку — не в System32 и не
           в папку с игрой.
        2. Запусти exe. Если лаунчер ещё не настроен под нужный модпак, появится кнопка
           «Настроить лаунчер» — нажми её и впиши GitHubOwner/GitHubRepo, которые сообщит
           администратор (ссылку на репозиторий можно вставить целиком в любое из двух
           полей — лишнее обрежется само). Дальше лаунчер сам вычислит адрес манифеста и
           сохранит настройки.
           Если администратор предпочитает раздавать готовый appsettings.json вместе с exe
           одним архивом — это тоже сработает, шаг «Настроить лаунчер» тогда не понадобится.
        3. Если кнопки «Настроить лаунчер» нет — лаунчер уже настроен, можно сразу переходить
           к первому запуску.

        Дальнейшие обновления самого exe (не модпака) лаунчер умеет накатывать сам, кнопками
        «Проверить обновление лаунчера»/«Обновить лаунчер» — appsettings.json при этом не
        трогается, обновляется только сам exe.

        ПЕРВЫЙ ЗАПУСК
        Лаунчер попросит указать папку сборки — это твой Minecraft-профиль с модами (обычно
        внутри лежат подпапки mods и config; это папка инстанса в CurseForge/твоём лаунчере
        Minecraft). Если лаунчер сам нашёл подходящую папку по имени — путь подставится
        автоматически. Если нет — нажми «Сменить папку сборки» и укажи её вручную. Выбор
        запомнится, при следующем запуске спрашивать не будет; эта же кнопка доступна в любой
        момент позже, если понадобится указать другую папку.

        КНОПКИ
        - «Проверить обновления» — сверяет твои файлы с последней версией сборки, показывает
          расхождения.
        - «Обновить» — активна, если есть расхождения; докачивает недостающее/изменённое,
          прогресс виден в журнале.
        - «Настроить лаунчер» — видна, только пока лаунчер не настроен под модпак; см.
          «Установка» выше.
        - «Инструкция» — открывает это окно.
        - «Режим администратора» — не для тебя, если ты не администратор сервера.
        - «Сменить папку сборки» — доступна всегда, открывает выбор папки заново.
        - «Проверить обновление лаунчера» / «Обновить лаунчер» — про сам exe, не про моды;
          отдельная пара от «Проверить обновления»/«Обновить» выше, легко перепутать.

        ПОДКЛЮЧЕНИЕ К СЕРВЕРУ
        Адрес, порт и правила подключения (whitelist и т.д.) тебе сообщит администратор — это
        не настраивается в лаунчере.

        ЕСЛИ ЧТО-ТО НЕ ТАК
        - «Лаунчер не настроен под модпак» сразу при запуске — нажми «Настроить лаунчер» и
          впиши GitHubOwner/GitHubRepo, которые сообщит администратор (см. «Установка» выше) —
          это самый быстрый способ, без Admin-режима. Если кнопки нет, но лаунчер всё равно не
          работает — возможно, appsettings.json рядом с exe случайно удалён или повреждён,
          попроси у администратора exe заново.
        - Ошибка проверки обновлений (404 и т.п.) — вероятно, обновление ещё не опубликовано,
          попробуй позже.
        - Указал не ту папку — нажми «Сменить папку сборки» ещё раз, либо удали
          appsettings.json рядом с exe и перезапусти лаунчер — он снова спросит папку.
        - Лаунчер не запускается / антивирус ругается — self-contained exe без цифровой
          подписи иногда триггерит Windows Defender SmartScreen; частое ложное срабатывание —
          «Подробнее» → «Выполнить в любом случае». Если сомневаешься — спроси администратора.
        """;

    public const string ClientFullEn = """
        How to use the launcher — Client

        WHAT THIS IS
        The program compares your local mod build against the reference (manifest) and
        downloads only what changed. You don't need to install/remove mods manually.

        INSTALLATION
        1. Get a link to the exe from your admin — even a link to the engine repository's
           Releases works (it hosts a universal exe not tied to any specific modpack). Put
           the exe in a separate, empty folder — not in System32 and not in your game folder.
        2. Run the exe. If the launcher isn't set up for the modpack you need yet, a "Set Up
           Launcher" button will appear — click it and enter the GitHubOwner/GitHubRepo your
           admin gives you (you can paste the whole repository link into either field — the
           extra part is stripped automatically). The launcher will then compute the manifest
           address itself and save the settings.
           If your admin prefers to hand out a ready-made appsettings.json alongside the exe —
           that works too, and you won't need the "Set Up Launcher" step.
        3. If there's no "Set Up Launcher" button — the launcher is already configured, you
           can go straight to the first run.

        The launcher can update itself later (not the modpack) with the "Check Launcher
        Update"/"Update Launcher" buttons — appsettings.json isn't touched, only the exe
        itself.

        FIRST RUN
        The launcher will ask you to point it to your build folder — this is your Minecraft
        profile with mods (usually it has mods and config subfolders inside; it's the
        instance folder in CurseForge/your Minecraft launcher). If the launcher found a
        matching folder by name itself, the path fills in automatically. If not — click
        "Change Build Folder" and pick it manually. Your choice is remembered, it won't ask
        again next time; this same button is available at any later point too, if you need
        to point it somewhere else.

        BUTTONS
        - "Check for Updates" — compares your files against the latest build version, shows
          differences.
        - "Update" — active if there are differences; downloads what's missing/changed,
          progress is shown in the log.
        - "Set Up Launcher" — visible only while the launcher isn't set up for a modpack; see
          "Installation" above.
        - "Guide" — opens this window.
        - "Admin Mode" — not for you unless you're the server admin.
        - "Change Build Folder" — always available, opens the folder picker again.
        - "Check Launcher Update" / "Update Launcher" — about the exe itself, not the mods; a
          separate pair from "Check for Updates"/"Update" above, easy to mix up.

        CONNECTING TO THE SERVER
        Address, port, and connection rules (whitelist, etc.) are told to you by your admin —
        this isn't configured in the launcher.

        IF SOMETHING'S WRONG
        - "The launcher isn't set up for a modpack" right at startup — click "Set Up
          Launcher" and enter the GitHubOwner/GitHubRepo your admin gives you (see
          "Installation" above) — this is the fastest way, no Admin Mode needed. If the
          button isn't there but the launcher still doesn't work — appsettings.json next to
          the exe may have been accidentally deleted or corrupted, ask your admin for the exe
          again.
        - Update check error (404, etc.) — an update probably hasn't been published yet, try
          again later.
        - Picked the wrong folder — click "Change Build Folder" again, or delete
          appsettings.json next to the exe and restart the launcher — it will ask for the
          folder again.
        - Launcher won't start / antivirus complains — a self-contained exe without a digital
          signature sometimes triggers Windows Defender SmartScreen; a common false positive
          — click "More info" → "Run anyway". If unsure, ask your admin.
        """;

    public const string ClientManual = """
        Пошаговое руководство — Пользователь (Клиент)

        1. Нажать «Проверить обновление лаунчера» — это про сам .exe, не про моды.
           1.1. Если нашлось обновление — нажать «Обновить лаунчер», дождаться перезапуска.
        2. Нажать «Настроить лаунчер» (кнопка видна только если лаунчер ещё не настроен под
           модпак — если её нет, значит всё уже заполнено, переходи к шагу 3).
           2.1. Запросить у администратора ссылку вида
                https://github.com/GitHubOwner/GitHubRepo на репозиторий, куда он загрузил
                модпак.
           2.2. Заполнить поля GitHubOwner и GitHubRepo — ссылку можно вставить как есть в
                поле GitHubRepo, лишнее обрежется само при сохранении. Нажать «Сохранить».
        3. В лаунчере CurseForge создать модпак.
           3.1. Уточнить у администратора версию игры и загрузчик модов (Forge/Fabric/…) —
                должны совпадать.
           3.2. Создать сборку.
           3.3. Перейти на страницу сборки в CurseForge.
           3.4. Нажать на троеточие слева от кнопки «Воспроизвести».
           3.5. Нажать «Открыть папку».
           3.6. Скопировать путь.
           3.7. В VanillaLauncher нажать «Сменить папку сборки».
           3.8. Вставить скопированный путь.
        4. Нажать «Проверить обновления» — это уже про моды/конфиги, не про сам лаунчер
           (легко перепутать с шагом 1).
        5. Запустить игру через CurseForge.
        """;

    public const string ClientManualEn = """
        Step-by-Step Guide — Player (Client)

        1. Click "Check Launcher Update" — this is about the .exe itself, not the mods.
           1.1. If an update was found — click "Update Launcher", wait for the restart.
        2. Click "Set Up Launcher" (the button is only visible if the launcher isn't set up
           for a modpack yet — if it's not there, everything is already filled in, skip to
           step 3).
           2.1. Ask your admin for a link like https://github.com/GitHubOwner/GitHubRepo to
                the repository where they uploaded the modpack.
           2.2. Fill in the GitHubOwner and GitHubRepo fields — you can paste the link as-is
                into the GitHubRepo field, the extra part is stripped automatically on save.
                Click "Save".
        3. In CurseForge, create the modpack.
           3.1. Confirm the game version and mod loader (Forge/Fabric/…) with your admin —
                they must match.
           3.2. Create the instance.
           3.3. Go to the instance page in CurseForge.
           3.4. Click the "···" menu next to the "Play" button.
           3.5. Click "Open Folder".
           3.6. Copy the path.
           3.7. In VanillaLauncher, click "Change Build Folder".
           3.8. Paste the copied path.
        4. Click "Check for Updates" — this is about mods/configs now, not the launcher
           itself (easy to confuse with step 1).
        5. Launch the game through CurseForge.
        """;

    public const string AdminShort = """
        Тот же exe, что у игроков — Admin-режим скрыт за паролем.

        1. Первый клик по «Режим администратора» — задать пароль.
        2. «Настройки» — все параметры (пути, ManifestUrl, GitHubOwner/Repo,
           ServerExcludeMods и т.д.), включая автопоиск папок.
        3. «Запустить/Остановить сервер» — обёртка над .bat-файлом сервера.
        4. «Пересоздать мир» — с обязательным бэкапом.
        5. «Опубликовать обновление» — бэкап → стоп → синхронизация модов → генерация
           manifest.json → публикация в GitHub Release. Сервер после этого НЕ запускается
           сам — запусти его вручную кнопкой «Запустить сервер», когда будешь готов.

        Токен для публикации — только через переменную окружения
        VANILLALAUNCHER_GITHUB_TOKEN, никогда не в файле.
        """;

    public const string AdminShortEn = """
        Same exe as players get — Admin Mode is just hidden behind a password.

        1. First click on "Admin Mode" — set a password.
        2. "Settings" — all parameters (paths, ManifestUrl, GitHubOwner/Repo,
           ServerExcludeMods, etc.), including path auto-detection.
        3. "Start/Stop Server" — a wrapper over the server's .bat file.
        4. "Recreate World" — with a mandatory backup.
        5. "Publish Update" — backup → stop → sync mods → generate manifest.json → publish
           to a GitHub Release. The server does NOT start back up automatically afterward —
           start it yourself with the "Start Server" button when you're ready.

        The token for publishing — only via the VANILLALAUNCHER_GITHUB_TOKEN environment
        variable, never in a file.
        """;

    public const string AdminFull = """
        Как пользоваться лаунчером — администратор

        ВХОД
        Кнопка «Режим администратора» в клиентском окне. Первый вход на этой машине
        предложит задать пароль (хранится как соль+хеш, не в открытом виде); последующие —
        просто просят пароль.

        РАЗОВАЯ НАСТРОЙКА НОВОГО ДЕПЛОЯ
        1. Открой «Настройки» и заполни: ManifestUrl, GitHubOwner/GitHubRepo (репозиторий,
           куда публикуются релизы модпака), пути клиента/сервера (или их ожидаемые имена +
           корни поиска для автопоиска — можно с переменными окружения вида %USERPROFILE%,
           чтобы работало у всех друзей независимо от имени пользователя/диска),
           ServerBatFileName, IncludeFolders, ServerExcludeMods, MaxBackupsToKeep.
           EngineGitHubOwner/EngineGitHubRepo можно оставить пустыми — по умолчанию
           используется репозиторий движка, самообновление exe работает и без явной
           настройки этих двух полей.
        2. Задай переменную окружения VANILLALAUNCHER_GITHUB_TOKEN (Personal Access Token с
           правом Contents: Read and write на репозиторий модпака) — нужна для публикации
           обновлений. Токен нигде не хранится в файлах/репозитории.

        КАК РАЗДАТЬ EXE ИГРОКАМ (частый вопрос)
        Игрок — не администратор, у него нет пароля от Admin-режима, и заходить в
        «Настройки» ему незачем. Раздать exe можно двумя способами:
        - Минимальный (рекомендуется): дай игроку ссылку на exe — подойдёт и ссылка на
          Releases репозитория движка (там лежит «чистый» exe без привязки к конкретному
          модпаку) — и продиктуй текстом GitHubOwner/GitHubRepo своего модпак-репозитория.
          Игрок сам нажимает «Настроить лаунчер» при первом запуске и вписывает эти два
          значения — дальше всё работает само, включая самообновление exe кнопкой
          «Проверить обновление лаунчера».
        - Альтернативный (по желанию): собери архив exe + свой appsettings.json (без
          admin-auth.json — это твой личный пароль) и отправь целиком — тогда шаг «Настроить
          лаунчер» игроку не понадобится. Не обязателен, просто вариант для тех, кто
          предпочитает готовый файл.
        В обоих случаях игрока спросят только его личную папку сборки (см. ClientGuide,
        «Первый запуск»).

        АВТОПОИСК ПУТЕЙ
        При каждом запуске, если сохранённый путь клиента/сервера не существует на этой
        машине, лаунчер сначала пробует найти его сам (по имени папки среди заданных корней
        поиска), и только если не нашёл — предлагает выбрать вручную. Найденный путь
        сохраняется автоматически.

        КНОПКИ ADMIN-РЕЖИМА
        - «Запустить сервер» / «Остановить сервер» — обёртка над .bat-файлом сервера;
          остановка — штатная команда, дожидается корректного завершения.
        - «Пересоздать мир (с бэкапом)» — отказывает, если сервер запущен. Бэкапит текущий
          мир в zip, затем удаляет — новый мир сгенерируется на следующем запуске сервера.
          Хранится последние N бэкапов (MaxBackupsToKeep), старые ротируются автоматически.
        - «Опубликовать обновление» — весь пайплайн одной кнопкой: бэкап мира → стоп сервера
          → синхронизация модов/конфигов сервера с эталонной клиентской папкой → генерация
          manifest.json и заливка в новый GitHub Release. Сервер после этого остаётся
          остановленным — не запускается сам, нажми «Запустить сервер» вручную, когда будешь
          готов (например, после проверки, что новые моды не валят его при старте).
        - «Настройки» — редактирование всех параметров конфига, включая пути (с автопоиском
          и выбором вручную) и список исключаемых из сервера модов (можно выбрать из папки со
          сборкой, а не вписывать имена файлов руками).
        - «Инструкция» — открывает это окно.

        SERVEREXCLUDEMODS — КАКИЕ МОДЫ НЕ ДОЛЖНЫ ПОПАДАТЬ НА СЕРВЕР
        Часть модов (визуальные/UI/клиентские улучшения) ломают дедик-сервер при старте, даже
        если их метаданные утверждают обратное. Список таких модов — в поле ServerExcludeMods,
        редактируется в «Настройках» (вручную или выбором из папки сборки). Синхронизация
        сервера исключает эти файлы, но раздача клиентам их не трогает — игрокам эти моды
        всё равно нужны.

        Проверка нового мода: сначала посмотри поле "environment" в fabric.mod.json внутри
        jar (это обычный zip) — если "client", сразу добавляй в исключения. Если метаданные
        не проясняют — запусти сервер после публикации и проверь лог на
        NoClassDefFoundError/Failed to start.

        ПУБЛИКАЦИЯ ОБНОВЛЕНИЯ МОДПАКА
        1. Обнови/добавь моды в эталонной клиентской папке.
        2. Если добавил новый мод — проверь, не клиентский ли он (см. выше), до публикации.
        3. Впиши версию (например, 1.2.3) и нажми «Опубликовать обновление».
        4. Дождись «Публикация завершена» в логе — с этого момента игроки увидят новую
           версию по кнопке «Проверить обновления».
        5. Сервер после публикации остановлен — запусти его вручную кнопкой «Запустить
           сервер» и проверь консоль на NoClassDefFoundError/Failed to start (см. выше про
           client-only моды), прежде чем звать игроков заходить.

        Каждая публикация создаёт новый релиз и перезаливает все файлы заново — ассеты
        старых релизов не переиспользуются, поэтому старые релизы можно удалять, не боясь
        сломать текущую версию у игроков.

        ЕСЛИ ПУБЛИКАЦИЯ ПАДАЕТ С ОШИБКОЙ
        Ошибки от GitHub теперь по возможности сопровождаются строкой "Подсказка: ..." прямо
        под сырым сообщением — сначала смотри её. Частые случаи:
        - 404 Not Found — GitHubOwner/GitHubRepo в «Настройках» указаны неверно. Самая частая
          причина: в поле GitHubRepo вставили ссылку целиком (https://github.com/Owner/Repo)
          вместо простого имени репозитория (Repo). Начиная с этой версии поле само вырезает имя
          из вставленной ссылки при сохранении — но если конфиг правился не через «Настройки»
          (например, вручную в appsettings.json), пересохрани его через «Настройки» ещё раз.
        - 403 Forbidden ("Resource not accessible...") — у токена VANILLALAUNCHER_GITHUB_TOKEN
          нет права Contents: Read and write на этот репозиторий. Проверь права токена на
          github.com/settings/tokens (или /settings/personal-access-tokens для fine-grained) —
          для fine-grained токенов также проверь, что репозиторий явно выбран в списке доступа.
        - 401 Unauthorized — токен VANILLALAUNCHER_GITHUB_TOKEN недействителен или просрочен
          (не путать с 403 выше — там токен рабочий, но без нужных прав). Выпусти новый
          Personal Access Token и пропиши его в ту же переменную окружения.
        - 422 "Repository is empty" — в репозитории модпака нет ни одного коммита, а релиз
          GitHub всегда привязан к ветке/тегу — создавать не на чем. Зайди на страницу
          репозитория на github.com и создай любой файл (например README) через встроенную
          кнопку — после этого достаточно одного коммита, и публикация заработает.
        - 422 "already exists" — релиз с таким номером версии уже есть в репозитории. Впиши
          другой номер версии в поле «Версия релиза».

        АУДИТ МОДОВ ПЕРЕД ПУБЛИКАЦИЕЙ (проверка client/server)
        В окне «Выбрать моды из папки...» (Настройки → ServerExcludeMods) лаунчер теперь сам
        открывает каждый .jar и читает "environment" в fabric.mod.json — моды с честным
        "environment": "client" отмечаются автоматически (подпись "[авто: environment=client]").
        Это ловит не все случаи: часть модов (например, better_tab) заявляет "environment": "*"
        (якобы универсальный), но на деле клиентский и валит дедик-сервер при старте — такие
        распознаются только реальным запуском сервера и разбором лога на
        NoClassDefFoundError/Failed to start, автоматика тут не поможет. Если после публикации
        сервер не поднимается — смотри лог на этот класс ошибок и добавляй виновника в
        ServerExcludeMods вручную.
        """;

    public const string AdminFullEn = """
        How to use the launcher — Admin

        LOGIN
        The "Admin Mode" button in the client window. First login on this machine will ask
        you to set a password (stored as a salt+hash, not in plain text); subsequent logins
        just ask for the password.

        ONE-TIME SETUP FOR A NEW DEPLOYMENT
        1. Open "Settings" and fill in: ManifestUrl, GitHubOwner/GitHubRepo (the repository
           where modpack releases are published), client/server paths (or their expected
           names + search roots for auto-detection — environment variables like
           %USERPROFILE% work too, so it works for every friend regardless of
           username/drive), ServerBatFileName, IncludeFolders, ServerExcludeMods,
           MaxBackupsToKeep. EngineGitHubOwner/EngineGitHubRepo can be left empty — the
           engine repository is used by default, self-update works without explicitly
           setting these two fields.
        2. Set the VANILLALAUNCHER_GITHUB_TOKEN environment variable (a Personal Access
           Token with Contents: Read and write on the modpack repository) — needed to
           publish updates. The token is never stored in files/the repository.

        HOW TO GIVE THE EXE TO PLAYERS (frequently asked)
        A player isn't an admin, they don't have the Admin Mode password, and there's no
        reason for them to go into "Settings". You can hand out the exe two ways:
        - Minimal (recommended): give the player a link to the exe — even a link to the
          engine repository's Releases works (that's a "clean" exe not tied to any specific
          modpack) — and tell them your modpack repository's GitHubOwner/GitHubRepo as plain
          text. The player clicks "Set Up Launcher" themselves on first run and enters those
          two values — everything else works on its own from there, including self-updating
          the exe via "Check Launcher Update".
        - Alternative (optional): put together an archive with the exe + your own
          appsettings.json (without admin-auth.json — that's your personal password) and
          send the whole thing — then the player won't need the "Set Up Launcher" step. Not
          required, just an option for those who prefer a ready-made file.
        Either way, the player will only be asked for their own build folder (see the Client
        Guide, "First Run").

        PATH AUTO-DETECTION
        On every launch, if the saved client/server path doesn't exist on this machine, the
        launcher first tries to find it itself (by folder name among the configured search
        roots), and only offers a manual picker if it doesn't find anything. A found path is
        saved automatically.

        ADMIN MODE BUTTONS
        - "Start Server" / "Stop Server" — a wrapper over the server's .bat file; stopping
          sends the proper command and waits for a clean shutdown.
        - "Recreate World (with Backup)" — refuses if the server is running. Backs up the
          current world to a zip, then deletes it — a new world will be generated on the
          next server start. The last N backups are kept (MaxBackupsToKeep), older ones are
          rotated out automatically.
        - "Publish Update" — the whole pipeline in one click: back up the world → stop the
          server → sync server mods/configs with the reference client folder → generate
          manifest.json and upload it to a new GitHub Release. The server stays stopped
          afterward — it doesn't start itself, click "Start Server" manually when you're
          ready (e.g. after confirming the new mods don't crash it on startup).
        - "Settings" — edit every config parameter, including paths (auto-detect and manual
          picker) and the list of mods excluded from the server (you can pick from the build
          folder instead of typing file names by hand).
        - "Guide" — opens this window.

        SERVEREXCLUDEMODS — WHICH MODS MUST NOT END UP ON THE SERVER
        Some mods (visual/UI/client-side enhancements) crash a dedicated server on startup,
        even when their metadata claims otherwise. The list of such mods lives in the
        ServerExcludeMods field, edited in "Settings" (by hand or by picking from the build
        folder). Server sync excludes these files, but distribution to players is
        untouched — players still need these mods.

        Checking a new mod: first check the "environment" field in fabric.mod.json inside
        the jar (it's a plain zip) — if it says "client", add it to the exclusions right
        away. If the metadata doesn't make it clear — start the server after publishing and
        check the log for NoClassDefFoundError/Failed to start.

        PUBLISHING A MODPACK UPDATE
        1. Update/add mods in the reference client folder.
        2. If you added a new mod — check whether it's client-only (see above) before
           publishing.
        3. Enter a version (e.g. 1.2.3) and click "Publish Update".
        4. Wait for "Publication complete" in the log — from this point players will see the
           new version when they click "Check for Updates".
        5. The server is stopped after publishing — start it yourself with the "Start
           Server" button and check the console for NoClassDefFoundError/Failed to start
           (see above about client-only mods) before inviting players in.

        Every publish creates a new release and re-uploads all files from scratch — assets
        from older releases aren't reused, so you can delete old releases on GitHub without
        worrying about breaking the current version for players.

        IF PUBLISHING FAILS WITH AN ERROR
        Errors from GitHub are now accompanied, where possible, by a "Hint: ..." line right
        under the raw message — check that first. Common cases:
        - 404 Not Found — GitHubOwner/GitHubRepo in "Settings" are wrong. The most common
          cause: the full link (https://github.com/Owner/Repo) was pasted into the
          GitHubRepo field instead of just the repository name (Repo). As of this version
          the field strips the name from a pasted link automatically on save — but if the
          config was edited outside of "Settings" (e.g. by hand in appsettings.json), save
          it through "Settings" again.
        - 401 Unauthorized — the VANILLALAUNCHER_GITHUB_TOKEN token is invalid or expired
          (not to be confused with 403 below — there the token works fine, it just lacks the
          right permissions). Issue a new Personal Access Token and set it in the same
          environment variable.
        - 403 Forbidden ("Resource not accessible...") — the VANILLALAUNCHER_GITHUB_TOKEN
          token doesn't have write access to this repository. Check at
          github.com/settings/tokens (or /settings/personal-access-tokens for fine-grained
          tokens) that the token has been granted Contents: Read and write — for
          fine-grained tokens also check that the repository is explicitly selected in its
          access list.
        - 422 "Repository is empty" — the modpack repository has no commits at all, and a
          GitHub release always needs a branch/tag to attach to — there's nothing to create
          it on. Go to the repository page on github.com and create any file (e.g. a README)
          through the built-in button — one commit is enough, then publishing will work.
        - 422 "already exists" — a release with that version number already exists in the
          repository. Enter a different version number in the "Release version" field.

        MOD AUDIT BEFORE PUBLISHING (client/server check)
        In the "Pick Mods from Folder..." window (Settings → ServerExcludeMods), the
        launcher now opens every .jar itself and reads "environment" in fabric.mod.json —
        mods that honestly declare "environment": "client" are checked off automatically
        (labeled "[auto: environment=client]"). This doesn't catch every case: some mods
        (e.g. better_tab) claim to be universal ("environment": "*") but are actually
        client-only and crash a dedicated server on startup — those can only be caught by
        actually starting the server and checking the log, the automation can't help there.
        If the server won't come up after publishing, check the log for this class of error
        and add the culprit to ServerExcludeMods by hand.
        """;

    public const string AdminManual = """
        Пошаговое руководство — Администратор

        1. Создать GitHub-репозиторий для своей сборки (название любое).
           1.1. При создании сделать его публичным — обязательно: приватный репозиторий не
                даёт игрокам скачивать моды вообще (Releases приватного репозитория
                недоступны без токена, а раздавать токен игрокам нельзя).
           1.2. Включить тумблер «Add a README file» (обязательно — иначе первая публикация
                упадёт с ошибкой «репозиторий пустой»).
        2. Перейти на https://github.com/settings/personal-access-tokens/new.
           2.1. Заполнить:
                - New fine-grained personal access token → Token name, Expiration (любое
                  время).
                - Repository access → Only select repositories → выбрать только что
                  созданный репозиторий (можно выбрать сразу несколько, если репозиториев
                  для публикации будет больше одного).
                - Permissions → «+ Add permissions» → «Contents» → Access: Read and write.
           2.2. Нажать «Generate token», скопировать токен (показывается только один раз).
        3. Windows: «Изменение системных переменных среды» → «Переменные среды» →
           «Переменные среды пользователя для …» → Создать.
           - Имя переменной: VANILLALAUNCHER_GITHUB_TOKEN (именно так, без вариантов).
           - Значение переменной: вставить скопированный токен.
           - Везде «ОК», выйти.
        4. Запустить VanillaLauncher.exe, нажать «Режим администратора».
           4.1. При первом запуске — задать пароль администратора (запомнить/записать его).
        5. Нажать «Настройки», заполнить (поля со звёздочкой обязательны):
           - ProfileRoot* — путь к эталонной сборке (папка модпака в CurseForge).
           - ServerDirectory* — путь к папке сервера.
           - GitHubOwner* / GitHubRepo* — владелец и имя репозитория из шага 1 (ссылку можно
             вставить целиком в любое из двух полей — лишнее обрежется само).
           - ManifestUrl* — https://github.com/{Owner}/{Repo}/releases/latest/download/manifest.json.
           - Сохранить.
        6. Нажать «Запустить сервер», проверить консоль на ошибки — особенно
           NoClassDefFoundError/Failed to start (признак клиентского мода в серверной сборке —
           такой мод добавить в ServerExcludeMods в «Настройках»).
        7. Вписать версию в поле «Версия релиза».
        8. Нажать «Опубликовать обновление».
           8.1. Подтвердить в диалоге («это создаст ПУБЛИЧНЫЙ релиз»).
           8.2. Дождаться окна об успешной публикации (или ошибке — оба варианта показываются
                явным всплывающим окном, не только текстом в логе).
        9. Скопировать ссылку на репозиторий (https://github.com/{Owner}/{Repo}) и
           продиктовать игрокам GitHubOwner/GitHubRepo текстом вместе со ссылкой на exe
           (подойдёт и ссылка на Releases репозитория движка) — дальше игрок сам нажимает
           «Настроить лаунчер» и вписывает эти два значения. Пересобирать и слать
           appsettings.json одним архивом не обязательно — это по-прежнему рабочая
           альтернатива для тех, кто предпочитает готовый файл (см. «Как раздать exe
           игрокам» в полном руководстве администратора).
        """;

    public const string AdminManualEn = """
        Step-by-Step Guide — Admin

        1. Create a GitHub repository for your build (any name).
           1.1. Make it public when creating it — required: a private repository won't let
                players download mods at all (Releases of a private repository aren't
                accessible without a token, and you can't hand out a token to players).
           1.2. Turn on "Add a README file" (required — otherwise the first publish will
                fail with a "repository is empty" error).
        2. Go to https://github.com/settings/personal-access-tokens/new.
           2.1. Fill in:
                - New fine-grained personal access token → Token name, Expiration (any
                  duration).
                - Repository access → Only select repositories → pick the repository you
                  just created (you can select several if you'll be publishing to more than
                  one).
                - Permissions → "+ Add permissions" → "Contents" → Access: Read and write.
           2.2. Click "Generate token", copy the token (it's shown only once).
        3. Windows: "Edit system environment variables" → "Environment Variables" → "User
           variables for …" → New.
           - Variable name: VANILLALAUNCHER_GITHUB_TOKEN (exactly this, no variations).
           - Variable value: paste the copied token.
           - "OK" everywhere, exit.
        4. Run VanillaLauncher.exe, click "Admin Mode".
           4.1. On first run — set an admin password (remember/write it down).
        5. Click "Settings", fill in (fields marked with an asterisk are required):
           - ProfileRoot* — path to the reference build (the modpack folder in CurseForge).
           - ServerDirectory* — path to the server folder.
           - GitHubOwner* / GitHubRepo* — the owner and repository name from step 1 (you
             can paste the full link into either field — the extra part is stripped
             automatically).
           - ManifestUrl* — https://github.com/{Owner}/{Repo}/releases/latest/download/manifest.json.
           - Save.
        6. Click "Start Server", check the console for errors — especially
           NoClassDefFoundError/Failed to start (a sign of a client-only mod in the server
           build — add that mod to ServerExcludeMods in "Settings").
        7. Enter a version in the "Release version" field.
        8. Click "Publish Update".
           8.1. Confirm in the dialog ("this will create a PUBLIC release").
           8.2. Wait for the success (or error) dialog — both cases are now shown as an
                explicit pop-up window, not just text in the log.
        9. Copy the repository link (https://github.com/{Owner}/{Repo}) and tell players the
           GitHubOwner/GitHubRepo as plain text along with a link to the exe (a link to the
           engine repository's Releases works too) — the player then clicks "Set Up
           Launcher" themselves and enters those two values. Rebuilding and sending
           appsettings.json isn't required — it's still a working alternative for those who
           prefer a ready-made file (see "How to Give the Exe to Players" in the full admin
           guide).
        """;
}
