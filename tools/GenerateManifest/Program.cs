using System.Text.Json;
using VanillaLauncher.Client;

// tools/GenerateManifest — обходит папку сборки и генерирует manifest.json
// (path, sha256, size, url), см. docs/ARCHITECTURE.md. Логика хеширования/обхода
// живёт в VanillaLauncher.Client.ManifestGenerator — этот файл только строит URL и
// пишет JSON. Тот же генератор использует Admin-пайплайн публикации (Этап 5),
// но с плоскими GitHub-совместимыми именами ассетов вместо вложенных путей.
//
// Использование:
//   GenerateManifest <sourceRoot> <outputManifestPath> <baseDownloadUrl> [version] [--include mods,config] [--flat]
//
// По умолчанию в манифест попадают только папки "mods" и "config" —
// сборка (моды/конфиги), а не мир, скриншоты, логи и прочие личные данные.
// Датапаки распространяются отдельным механизмом (см. docs заметку из GDD), сюда не входят.
//
// --flat: URL строится из схлопнутого имени файла (mods/a.jar -> baseUrl/mods_a.jar) —
// нужно для реального GitHub Release (assets не поддерживают "/" в имени). Без флага
// URL сохраняет вложенный путь как есть — подходит для обычного HTTP-хостинга с папками.

if (args.Length < 3)
{
    Console.WriteLine("Использование: GenerateManifest <sourceRoot> <outputManifestPath> <baseDownloadUrl> [version] [--include mods,config] [--flat]");
    return 1;
}

var sourceRoot = Path.GetFullPath(args[0]);
var outputPath = Path.GetFullPath(args[1]);
var baseUrl = args[2].TrimEnd('/');
var version = args.Length > 3 && !args[3].StartsWith("--") ? args[3] : "dev";
var flat = args.Contains("--flat");

var includeFolders = new List<string> { "mods", "config" };
var includeIndex = Array.IndexOf(args, "--include");
if (includeIndex >= 0 && includeIndex + 1 < args.Length)
{
    includeFolders = args[includeIndex + 1]
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList();
}

if (!Directory.Exists(sourceRoot))
{
    Console.WriteLine($"Папка не найдена: {sourceRoot}");
    return 1;
}

Console.WriteLine($"Источник: {sourceRoot}");
Console.WriteLine($"Включаемые папки: {string.Join(", ", includeFolders)}");

string UrlForPath(string relativePath)
{
    var name = flat ? ManifestGenerator.FlattenPathForAssetName(relativePath) : relativePath;
    return $"{baseUrl}/{Uri.EscapeDataString(name).Replace("%2F", "/")}";
}

var progress = new Progress<string>(line => Console.WriteLine($"  {line}"));
var manifest = await ManifestGenerator.GenerateAsync(sourceRoot, includeFolders, version, UrlForPath, progress);

var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
await File.WriteAllTextAsync(outputPath, json);

Console.WriteLine($"Готово: {manifest.Files.Count} файлов -> {outputPath}");
return 0;
