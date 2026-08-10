namespace VanillaLauncher.Client;

/// <summary>
/// Пишет весь журнал приложения (то же самое, что показывается в LogList/ErrorLogList
/// MainWindow и AdminWindow, плюс необработанные исключения) в файл на диске — раньше лог был
/// только в памяти, пока окно открыто, и пропадал бесследно при закрытии/креше приложения,
/// оставляя только скриншот как единственный способ разобраться в проблеме постфактум.
///
/// Один файл на запуск процесса (<c>logs/launcher-{timestamp}.log</c>, рядом с exe) — не нужна
/// отдельная логика ротации по размеру, только чистка старых файлов при старте
/// (<see cref="MaxFilesToKeep"/>), тот же принцип, что у <c>WorldBackupService.MaxBackupsToKeep</c>.
/// Пишет построчно с немедленным флашем — если приложение крашнется, последняя строка перед
/// крашем не потеряется в буфере.
///
/// Best-effort по конструкции: если папку логов не удалось создать/писать (например, лаунчер
/// лежит в защищённой от записи папке), <see cref="FilePath"/> остаётся <c>null</c>, а
/// <see cref="Log"/> просто ничего не делает — логирование никогда не должно ронять или
/// мешать основной работе приложения.
/// </summary>
public sealed class FileLogger : IDisposable
{
    private const int MaxFilesToKeep = 20;
    private readonly object _lock = new();
    private readonly StreamWriter? _writer;

    public string? FilePath { get; }

    public FileLogger(string? baseDirectory = null)
    {
        var dir = baseDirectory ?? AppContext.BaseDirectory;
        var logsDir = Path.Combine(dir, "logs");

        try
        {
            Directory.CreateDirectory(logsDir);

            FilePath = Path.Combine(logsDir, $"launcher-{DateTime.Now:yyyyMMdd-HHmmss}.log");
            _writer = new StreamWriter(FilePath, append: false, System.Text.Encoding.UTF8) { AutoFlush = true };
            _writer.WriteLine($"=== VanillaLauncher запущен {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");

            // Чистка — ПОСЛЕ создания текущего файла (тот же порядок, что у
            // WorldBackupService.RotateBackups): скан видит и только что созданный файл, так
            // Skip(MaxFilesToKeep) держит на диске ровно MaxFilesToKeep файлов включая текущий,
            // а не MaxFilesToKeep+1.
            CleanupOldLogs(logsDir);
        }
        catch (IOException)
        {
            FilePath = null;
        }
        catch (UnauthorizedAccessException)
        {
            FilePath = null;
        }
    }

    public void Log(string message)
    {
        if (_writer is null)
            return;

        lock (_lock)
        {
            try
            {
                _writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}  {message}");
            }
            catch (IOException)
            {
                // диск/файл недоступен — молча продолжаем работу без логирования на диск
            }
        }
    }

    private static void CleanupOldLogs(string logsDir)
    {
        var files = new DirectoryInfo(logsDir).GetFiles("launcher-*.log")
            .OrderByDescending(f => f.CreationTimeUtc)
            .Skip(MaxFilesToKeep);

        foreach (var stale in files)
        {
            try { stale.Delete(); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer?.Dispose();
        }
    }
}
