namespace VanillaLauncher.Admin;

/// <summary>
/// Классифицирует строки консольного вывода сервера — успешный старт и вероятная ошибка.
/// Общий код между AdminWindow (индикатор успеха старта, панель "Ошибки") и
/// ServerSmokeTestRunner (тестовый запуск перед публикацией) — раньше жил только в AdminWindow
/// как приватная логика code-behind, не был переиспользуем и не тестировался напрямую.
/// </summary>
public static class ServerLogLineClassifier
{
    /// <summary>
    /// "Done (12.345s)! For help, type "help"" — стандартная строка готовности сервера,
    /// неизменна много лет во всех форках (vanilla/Forge/Fabric/Paper).
    /// </summary>
    public static bool IsSuccessLine(string line) => line.Contains("Done (") && line.Contains(")!");

    /// <summary>
    /// Java-стектрейсы ("Exception ...", "\tat ...", "Caused by: ...") и строки уровня
    /// ERROR/FATAL от Log4j.
    /// </summary>
    public static bool IsErrorLine(string line)
    {
        var trimmed = line.TrimStart();
        return line.Contains("ERROR") || line.Contains("FATAL") || line.Contains("Exception")
            || trimmed.StartsWith("at ") || trimmed.StartsWith("Caused by:");
    }
}
