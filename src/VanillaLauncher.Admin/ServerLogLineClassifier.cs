using System.Text.RegularExpressions;

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
    /// Заголовок необёрнутого Java-исключения без уровня Log4j — например
    /// <c>java.util.concurrent.ExecutionException: java.lang.AbstractMethodError: ...</c>.
    /// Реальный случай: краш от библиотеки apoli внутри Origins (Fabric) на живом сервере не
    /// начинался ни с "Exception in thread", ни содержал [ERROR]/[FATAL] — первая строка
    /// обёртки ExecutionException проходила мимо всех прежних маркеров. Якорь на начало строки
    /// (после Trim) — не ловим упоминание исключения внутри чужого сообщения, только строку,
    /// которая САМА является именем класса исключения.
    /// </summary>
    private static readonly Regex WrappedExceptionHeader =
        new(@"^[\w.$]*(Exception|Error)(:|$)", RegexOptions.Compiled);

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

        var trimmed = line.Trim();
        return trimmed.StartsWith("Exception in thread")
            || trimmed.StartsWith("at ")
            || trimmed.StartsWith("Caused by:")
            || trimmed.StartsWith("Suppressed:")
            || WrappedExceptionHeader.IsMatch(trimmed);
    }
}
