namespace VanillaLauncher.Client;

public static class ManifestGenerator
{
    /// <summary>
    /// Обходит sourceRoot по списку includeFolders (по умолчанию mods/config — не мир,
    /// не скриншоты, не логи, не датапаки — те распространяются отдельным механизмом),
    /// считает SHA-256 каждого файла и строит Manifest. URL для каждого файла решает
    /// вызывающая сторона через urlForPath — единой схемы URL не может быть, потому что
    /// это зависит от места публикации (GitHub Release, локальный тестовый сервер и т.д.).
    /// </summary>
    public static async Task<Manifest> GenerateAsync(
        string sourceRoot,
        IReadOnlyList<string> includeFolders,
        string version,
        Func<string, string> urlForPath,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var entries = new List<ManifestFileEntry>();

        foreach (var folder in includeFolders)
        {
            var folderPath = Path.Combine(sourceRoot, folder);
            if (!Directory.Exists(folderPath))
            {
                progress?.Report($"(пропущено, не существует: {folder})");
                continue;
            }

            foreach (var filePath in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();

                var relativePath = Path.GetRelativePath(sourceRoot, filePath).Replace('\\', '/');
                var sha256 = await HashService.ComputeSha256Async(filePath, ct);
                var size = new FileInfo(filePath).Length;

                entries.Add(new ManifestFileEntry
                {
                    Path = relativePath,
                    Sha256 = sha256,
                    Size = size,
                    Url = urlForPath(relativePath)
                });

                progress?.Report($"{relativePath} ({size} байт)");
            }
        }

        return new Manifest
        {
            Version = version,
            GeneratedAt = DateTimeOffset.UtcNow,
            Files = entries
        };
    }

    /// <summary>
    /// Плоское имя ассета для GitHub Release: assets не поддерживают "/" в имени,
    /// поэтому относительный путь схлопывается в одно имя файла ("mods/a.jar" -> "mods__a.jar").
    /// Схлопывание, а не просто взятие имени файла, — чтобы не столкнуть лбами
    /// одноимённые файлы из разных папок (mods/foo.jar и config/foo.jar).
    /// </summary>
    public static string FlattenPathForAssetName(string relativePath) => relativePath.Replace('/', '_');
}
