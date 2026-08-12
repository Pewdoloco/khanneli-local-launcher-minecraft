namespace VanillaLauncher.Admin;

public static class ServerPropertiesReader
{
    private const string FileName = "server.properties";

    /// <summary>Читает level-name из server.properties. По умолчанию "world" (ванильное значение).</summary>
    public static string GetLevelName(string serverDirectory) =>
        GetValue(serverDirectory, "level-name") is { Length: > 0 } value ? value : "world";

    /// <summary>
    /// Читает server-ip из server.properties — null, если ключа нет или он пуст (ванильный
    /// дефолт: сервер слушает на всех интерфейсах). У части деплоев сюда осознанно вписывают
    /// Tailscale IP/hostname — тогда сервер слушает ТОЛЬКО на этом адресе (см. AdminWindow —
    /// синхронизация с AppConfig.ServerHost).
    /// </summary>
    public static string? GetServerIp(string serverDirectory) =>
        GetValue(serverDirectory, "server-ip") is { Length: > 0 } value ? value : null;

    /// <summary>Читает server-port из server.properties — null, если ключа нет или не число.</summary>
    public static int? GetServerPort(string serverDirectory)
    {
        var raw = GetValue(serverDirectory, "server-port");
        return int.TryParse(raw, out var port) ? port : null;
    }

    private static string? GetValue(string serverDirectory, string key)
    {
        var path = Path.Combine(serverDirectory, FileName);
        if (!File.Exists(path))
            return null;

        var prefix = key + "=";
        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            return trimmed[prefix.Length..].Trim();
        }

        return null;
    }

    /// <summary>
    /// Точечно правит server-ip/server-port в server.properties — остальные строки (порядок,
    /// комментарии, прочие ключи) остаются как есть, файл не перегенерируется с нуля. Если
    /// ключа ещё нет в файле (мало вероятно — ванильный сервер сам пишет оба при первом
    /// запуске, но на случай пустого/нового файла), строка дописывается в конец.
    /// </summary>
    public static void SetServerAddress(string serverDirectory, string? ip, int port)
    {
        var path = Path.Combine(serverDirectory, FileName);
        var lines = File.Exists(path) ? File.ReadAllLines(path).ToList() : new List<string>();

        SetOrAppend(lines, "server-ip", ip ?? string.Empty);
        SetOrAppend(lines, "server-port", port.ToString());

        File.WriteAllLines(path, lines);
    }

    private static void SetOrAppend(List<string> lines, string key, string value)
    {
        var prefix = key + "=";
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = prefix + value;
                return;
            }
        }

        lines.Add(prefix + value);
    }
}
