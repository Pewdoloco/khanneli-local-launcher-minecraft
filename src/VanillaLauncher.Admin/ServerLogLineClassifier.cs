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
    /// Строки уровня ERROR/FATAL (по факту, не по наличию слова "Exception"/"Error" где-то в
    /// тексте) плюс сами Java-стектрейсы. Раньше проверка была на голое
    /// <c>line.Contains("Exception")</c>, из-за чего в панель "Ошибки" попадали обычные WARN-
    /// строки Log4j, если их сообщение просто УПОМИНАЛО исключение — например
    /// <c>[main/WARN] [mixin/]: Error loading class: ... (java.lang.RuntimeException: ...)</c>
    /// (реальный случай из живого прогона) — предупреждение, не ошибка, но проходило старый
    /// фильтр из-за "RuntimeException" внутри сообщения. Теперь требуется реальный маркер
    /// уровня: Log4j-формат <c>[поток/ERROR]</c>/<c>[поток/FATAL]</c> (в квадратных или
    /// круглых скобках), таблица Forge/NeoForge со статусом <c>ERROR</c>
    /// (см. <see cref="ForgeModStateTableParser"/> — тот же класс строк, тут просто
    /// более дешёвая проверка без парсинга колонок), либо raw-строки крашдампа JVM без
    /// префикса уровня (<c>Exception in thread ...</c>, <c>\tat ...</c>, <c>Caused by: ...</c>).
    /// </summary>
    public static bool IsErrorLine(string line)
    {
        if (line.Contains("/ERROR]") || line.Contains("[ERROR]") || line.Contains("/FATAL]") || line.Contains("[FATAL]"))
            return true;

        if (line.Contains('|') && line.Split('|').Any(column => column.Trim().Equals("ERROR", StringComparison.OrdinalIgnoreCase)))
            return true;

        var trimmed = line.TrimStart();
        return trimmed.StartsWith("Exception in thread")
            || trimmed.StartsWith("at ")
            || trimmed.StartsWith("Caused by:")
            || trimmed.StartsWith("Suppressed:");
    }
}
