using System.Text.RegularExpressions;

namespace VanillaLauncher.Admin;

/// <summary>
/// Разбирает строки консольного вывода сервера на события входа/выхода игрока — "Steve
/// joined the game" / "Steve left the game". Формат этих двух строк не менялся много лет ни
/// в одном форке (vanilla/Forge/Fabric/Paper), тот же принцип, что у "Done (...)!" в
/// AdminWindow. Ложные срабатывания возможны, если игрок пишет в чат текст, совпадающий с
/// концом этих фраз — тот же класс ограничения, что у IsErrorLine в AdminWindow.
/// </summary>
public static class PlayerActivityParser
{
    private static readonly Regex JoinRegex = new(@"(?<name>[A-Za-z0-9_]{1,16}) joined the game$", RegexOptions.Compiled);
    private static readonly Regex LeaveRegex = new(@"(?<name>[A-Za-z0-9_]{1,16}) left the game$", RegexOptions.Compiled);

    public static string? TryParseJoin(string line)
    {
        var match = JoinRegex.Match(line.TrimEnd());
        return match.Success ? match.Groups["name"].Value : null;
    }

    public static string? TryParseLeave(string line)
    {
        var match = LeaveRegex.Match(line.TrimEnd());
        return match.Success ? match.Groups["name"].Value : null;
    }
}
