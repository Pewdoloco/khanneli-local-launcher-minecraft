namespace VanillaLauncher.Client;

/// <summary>
/// Автоопределение путей клиентской/серверной директории по имени папки — до фоллбека на
/// ручной выбор пользователем. См. docs/TASK_PATH_AUTODETECT.md.
/// </summary>
public static class PathAutoDetectService
{
    /// <summary>
    /// Ищет прямую поддиректорию с именем <paramref name="folderName"/> (регистронезависимо)
    /// в каждом из <paramref name="searchRoots"/> по порядку, возвращает первое совпадение.
    /// Не рекурсивный обход — ожидаемая директория сборки/сервера всегда прямой потомок
    /// одного из корней поиска, а не вложена произвольно глубоко.
    ///
    /// Каждый корень перед проверкой проходит через <see cref="ExpandRoot"/> — админ не может
    /// знать заранее, под каким именем пользователя (диском, локалью) друзья установят
    /// CurseForge на своей машине, поэтому корни поиска задаются в конфиге с переменными
    /// окружения (например, "%USERPROFILE%\curseforge\Instances"), а не абсолютным путём
    /// одного конкретного человека.
    /// </summary>
    public static string? TryFind(string? folderName, IEnumerable<string>? searchRoots)
    {
        if (string.IsNullOrWhiteSpace(folderName) || searchRoots is null)
            return null;

        foreach (var rawRoot in searchRoots)
        {
            var root = ExpandRoot(rawRoot);
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                continue;

            foreach (var candidate in Directory.EnumerateDirectories(root))
            {
                if (string.Equals(Path.GetFileName(candidate), folderName, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Раскрывает переменные окружения вида %USERPROFILE%/%LOCALAPPDATA%/%APPDATA% в корне
    /// поиска. <see cref="Environment.ExpandEnvironmentVariables"/> тихо оставляет нераспознанные
    /// "%имя%" как есть (не бросает исключение) — если админ опечатался в имени переменной,
    /// результат просто не найдётся на диске и автопоиск перейдёт к следующему корню/фоллбеку,
    /// а не упадёт с ошибкой.
    /// </summary>
    private static string ExpandRoot(string root) =>
        string.IsNullOrWhiteSpace(root) ? root : Environment.ExpandEnvironmentVariables(root);
}
