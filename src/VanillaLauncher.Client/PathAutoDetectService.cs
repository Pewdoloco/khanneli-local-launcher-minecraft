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
    /// </summary>
    public static string? TryFind(string? folderName, IEnumerable<string>? searchRoots)
    {
        if (string.IsNullOrWhiteSpace(folderName) || searchRoots is null)
            return null;

        foreach (var root in searchRoots)
        {
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
}
