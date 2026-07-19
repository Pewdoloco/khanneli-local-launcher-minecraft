using System.Diagnostics;

namespace VanillaLauncher.Admin;

/// <summary>
/// Обёртка над серверным .bat: старт через cmd.exe, построчный захват вывода,
/// штатная остановка через команду "stop" на stdin сервера.
/// </summary>
public sealed class ServerProcessController : IDisposable
{
    private readonly string _serverDirectory;
    private readonly string _batFileName;
    private Process? _process;

    public event Action<string>? OutputReceived;
    public event Action? Exited;

    public bool IsRunning => _process is { HasExited: false };
    public int? ProcessId => _process?.Id;

    public ServerProcessController(string serverDirectory, string batFileName = "start.bat")
    {
        _serverDirectory = serverDirectory;
        _batFileName = batFileName;
    }

    public void Start()
    {
        if (IsRunning)
            throw new InvalidOperationException("Сервер уже запущен.");

        var batPath = Path.GetFullPath(Path.Combine(_serverDirectory, _batFileName));
        if (!File.Exists(batPath))
            throw new FileNotFoundException($"Не найден файл запуска сервера: {batPath}", batPath);

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            WorkingDirectory = _serverDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("/c");
        // Полный путь обязателен: относительное имя файла (даже с корректной
        // рабочей директорией) в некоторых окружениях не резолвится через cmd /c
        // ("не является внутренней или внешней командой"), хотя dir/cd подтверждают
        // правильный cwd. Абсолютный путь работает стабильно.
        psi.ArgumentList.Add(batPath);

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) OutputReceived?.Invoke(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) OutputReceived?.Invoke(e.Data); };
        process.Exited += (_, _) => Exited?.Invoke();

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        _process = process;
    }

    /// <summary>
    /// Отправляет "stop" в stdin сервера и ждёт штатного завершения процесса.
    /// Stdin закрывается сразу после команды: серверу она уже не нужна, а закрытый
    /// поток даёт завершиться "pause" в конце .bat (он иначе ждёт ввод бесконечно,
    /// раз консоли у процесса нет — читает из перенаправленного, уже закрытого stdin).
    /// </summary>
    /// <returns>true, если процесс завершился в течение timeout; false — если завис.</returns>
    public async Task<bool> StopAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        if (!IsRunning)
            return true;

        var process = _process!;

        await process.StandardInput.WriteLineAsync("stop");
        await process.StandardInput.FlushAsync(ct);
        process.StandardInput.Close();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
            return true;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return false;
        }
    }

    public void Dispose()
    {
        _process?.Dispose();
    }
}
