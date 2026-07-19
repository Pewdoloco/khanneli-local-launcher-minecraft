namespace VanillaLauncher.Admin;

public static class ServerPropertiesReader
{
    /// <summary>Читает level-name из server.properties. По умолчанию "world" (ванильное значение).</summary>
    public static string GetLevelName(string serverDirectory)
    {
        var path = Path.Combine(serverDirectory, "server.properties");
        if (!File.Exists(path))
            return "world";

        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("level-name=", StringComparison.OrdinalIgnoreCase))
                continue;

            var value = trimmed["level-name=".Length..].Trim();
            return string.IsNullOrEmpty(value) ? "world" : value;
        }

        return "world";
    }
}
