using System.Text.Json;

namespace VanillaLauncher.Client;

public sealed class AppConfig
{
    public string ManifestUrl { get; set; } = string.Empty;
    public string ProfileRoot { get; set; } = string.Empty;

    // Только для Admin-режима (Этап 3+). В чисто клиентской сборке можно не задавать.
    public string? ServerDirectory { get; set; }
    public string ServerBatFileName { get; set; } = "start.bat";
    public int MaxBackupsToKeep { get; set; } = 5;

    // Только для публикации обновлений (Этап 5). ProfileRoot одновременно служит
    // "эталонной сборкой", откуда генерируется манифест и синхронизируются файлы сервера.
    public string? GitHubOwner { get; set; }
    public string? GitHubRepo { get; set; }
    public List<string> IncludeFolders { get; set; } = new() { "mods", "config" };

    // Клиентские моды, которых не должно быть на дедик-сервере (по имени файла).
    // См. docs/ARCHITECTURE.md — почему это отдельный список, а не автоопределение.
    public List<string> ServerExcludeMods { get; set; } = new();

    // Автоопределение путей (docs/TASK_PATH_AUTODETECT.md). Ожидаемые имена папок и корни
    // поиска — настройки, а не захардкоженные строки, чтобы один и тот же движок подходил
    // под любой модпак без правки кода. Пусто по умолчанию — только конфигурация конкретного
    // модпака заполняет реальные имена/пути.
    public string? ClientFolderName { get; set; }
    public List<string> ClientSearchRoots { get; set; } = new();
    public string? ServerFolderName { get; set; }
    public List<string> ServerSearchRoots { get; set; } = new();

    /// <summary>
    /// Отличает "движок ещё не адаптирован под модпак" (свежий/пустой конфиг — ни один
    /// друг ещё ничего не выбирал) от "у конкретного пользователя просто другой путь".
    /// ProfileRoot сюда сознательно не входит: это per-machine значение, которое у
    /// свежепришедшего игрока всегда пусто/не существует даже на полностью настроенном
    /// под модпак движке — см. docs/TASK_PATH_AUTODETECT.md, "Открытые технические вопросы".
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ManifestUrl) &&
        !string.IsNullOrWhiteSpace(GitHubOwner) &&
        !string.IsNullOrWhiteSpace(GitHubRepo);

    private const string FileName = "appsettings.json";

    // Не сериализуется (не публичное поле) — папка, откуда конфиг был загружен,
    // нужна только чтобы Save() знал, куда писать обратно.
    private string? _loadedFromDirectory;

    private const string EmbeddedDefaultResourceName = "VanillaLauncher.Client.appsettings.default.json";

    public static AppConfig Load(string? baseDirectory = null)
    {
        var dir = baseDirectory ?? AppContext.BaseDirectory;
        var path = Path.Combine(dir, FileName);

        // Внешний appsettings.json может отсутствовать — например, к релизу приложили
        // только exe и забыли файл конфига рядом (было ровно так с 26.1.2-b1). В этом
        // случае используем встроенный в сборку дефолт, чтобы программа хотя бы
        // запустилась, а не падала с "файл не найден" у человека, который просто скачал exe.
        var json = File.Exists(path) ? File.ReadAllText(path) : ReadEmbeddedDefault();

        var config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (config is null)
            throw new InvalidOperationException($"Не удалось разобрать {FileName}");

        // ManifestUrl/ProfileRoot больше не обязательны на уровне Load(): у "движка" без
        // адаптации под модпак (см. docs/TASK_PATH_AUTODETECT.md) конфиг по-настоящему
        // пустой — это валидное состояние, а не ошибка чтения файла. Различать "не
        // настроено" и "настроено, но путь не найден на этой машине" — дело вызывающей
        // стороны через IsConfigured, а не повод бросать исключение здесь.
        config._loadedFromDirectory = dir;
        return config;
    }

    private static string ReadEmbeddedDefault()
    {
        var assembly = typeof(AppConfig).Assembly;
        using var stream = assembly.GetManifestResourceStream(EmbeddedDefaultResourceName)
            ?? throw new InvalidOperationException(
                $"Внешний {FileName} не найден, и встроенный дефолт ({EmbeddedDefaultResourceName}) " +
                "тоже не прочитать — сборка exe повреждена или собрана неправильно.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Перезаписывает appsettings.json, из которого конфиг был загружен — например,
    /// после того как пользователь на первом запуске выбрал свою папку сборки
    /// (у каждого она своя, дефолтное значение в файле подходит только автору сборки).
    /// </summary>
    public void Save()
    {
        if (_loadedFromDirectory is null)
            throw new InvalidOperationException("Конфиг не был загружен из файла через Load() — некуда сохранять.");

        var path = Path.Combine(_loadedFromDirectory, FileName);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
