using VanillaLauncher.Client;

namespace VanillaLauncher.Admin;

/// <summary>
/// Зеркалирует mods/config (и что укажут) из папки сборки в серверную папку:
/// копирует новые/изменённые файлы (по хешу — не по имени/дате) и удаляет
/// в серверной папке то, чего больше нет в источнике. Без этого сервер бы
/// копил моды прошлых версий, несовместимые с текущим клиентским манифестом.
///
/// excludeFileNames — клиентские моды, которых не должно быть на дедик-сервере
/// (по имени файла, без учёта регистра). Часть мод-паков собирает один общий
/// список модов на клиента и сервер, но часть модов клиентские (declared
/// "environment": "client" в fabric.mod.json — или того хуже, задекларированы
/// "*" (универсальные), но их код всё равно трогает клиентские классы вроде
/// net.minecraft.client.gui.screens.Screen и падает на сервере при старте —
/// такое environment-поле не ловит, это выясняется только реальным запуском).
/// </summary>
public static class ServerFileSync
{
    public static async Task MirrorAsync(
        string sourceRoot,
        string serverRoot,
        IReadOnlyList<string> includeFolders,
        IReadOnlyList<string>? excludeFileNames = null,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var excludeSet = (excludeFileNames ?? Array.Empty<string>())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var folder in includeFolders)
        {
            var sourceFolder = Path.Combine(sourceRoot, folder);
            var targetFolder = Path.Combine(serverRoot, folder);

            if (!Directory.Exists(sourceFolder))
            {
                progress?.Report($"(пропущено, нет в источнике: {folder})");
                continue;
            }

            Directory.CreateDirectory(targetFolder);

            var sourceRelativePaths = Directory.EnumerateFiles(sourceFolder, "*", SearchOption.AllDirectories)
                .Select(p => Path.GetRelativePath(sourceFolder, p))
                .Where(rel => !excludeSet.Contains(Path.GetFileName(rel)))
                .ToHashSet();

            foreach (var rel in sourceRelativePaths)
            {
                ct.ThrowIfCancellationRequested();

                var srcPath = Path.Combine(sourceFolder, rel);
                var dstPath = Path.Combine(targetFolder, rel);

                if (File.Exists(dstPath))
                {
                    var srcHash = await HashService.ComputeSha256Async(srcPath, ct);
                    var dstHash = await HashService.ComputeSha256Async(dstPath, ct);
                    if (HashService.Matches(srcHash, dstHash))
                        continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(dstPath)!);
                File.Copy(srcPath, dstPath, overwrite: true);
                progress?.Report($"Обновлено: {folder}/{rel}");
            }

            foreach (var existingFile in Directory.EnumerateFiles(targetFolder, "*", SearchOption.AllDirectories).ToList())
            {
                var rel = Path.GetRelativePath(targetFolder, existingFile);
                if (!sourceRelativePaths.Contains(rel))
                {
                    File.Delete(existingFile);
                    progress?.Report($"Удалено (устарело): {folder}/{rel}");
                }
            }
        }
    }
}
