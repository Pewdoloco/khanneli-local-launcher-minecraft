using System.IO.Compression;
using System.Text.Json;

namespace VanillaLauncher.Client;

/// <summary>
/// Читает поле "environment" из fabric.mod.json внутри мод-жара — это же поле
/// AdminGuide и так предлагает проверять вручную перед публикацией нового мода.
/// "client" ловится этой проверкой надёжно; "server"/"*"/отсутствие поля/не-fabric
/// jar — нет (см. TryGetEnvironment) — часть модов декларирует "*", но на деле
/// трогает клиентские классы и падает на сервере только при реальном запуске
/// (пример — better_tab). Такие случаи эта проверка сознательно не ловит, только
/// честно объявленные "client".
/// </summary>
public static class FabricModEnvironmentReader
{
    public static string? TryGetEnvironment(string jarPath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(jarPath);
            var entry = archive.GetEntry("fabric.mod.json");
            if (entry is null)
                return null;

            using var stream = entry.Open();
            using var doc = JsonDocument.Parse(stream);

            return doc.RootElement.TryGetProperty("environment", out var env) && env.ValueKind == JsonValueKind.String
                ? env.GetString()
                : null;
        }
        catch
        {
            // Повреждённый/не-zip файл, не-fabric jar (forge и т.п.), некорректный JSON —
            // во всех случаях считаем environment неизвестным, а не ошибкой: это не должно
            // останавливать сканирование остальных модов в папке.
            return null;
        }
    }

    public static bool IsClientOnly(string jarPath) =>
        string.Equals(TryGetEnvironment(jarPath), "client", StringComparison.OrdinalIgnoreCase);
}
