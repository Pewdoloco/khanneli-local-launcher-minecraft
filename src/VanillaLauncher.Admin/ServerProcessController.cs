using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace VanillaLauncher.Admin;

/// <summary>
/// Обёртка над серверным .bat: старт через cmd.exe, построчный захват вывода,
/// штатная остановка через команду "stop" на stdin сервера, произвольные команды через
/// <see cref="SendCommandAsync"/> пока сервер работает.
/// </summary>
public sealed class ServerProcessController : IDisposable
{
    private readonly string _serverDirectory;
    private readonly string _batFileName;
    private Process? _process;

    public event Action<string>? OutputReceived;
    public event Action? Exited;

    /// <summary>
    /// Срабатывает сразу после успешного <see cref="Start"/> — не после готовности сервера
    /// (см. <see cref="OutputReceived"/>/строка "Done (...)!"), а именно после самого факта
    /// запуска процесса. Общий сигнал для индикатора "идёт запуск..." в UI — работает
    /// одинаково что для ручного нажатия "Запустить сервер" в AdminWindow, что для
    /// автоматического старта внутри <see cref="ServerSmokeTestRunner"/> при публикации:
    /// оба пути идут через один и тот же <see cref="Start"/>.
    /// </summary>
    public event Action? Started;

    public bool IsRunning => _process is { HasExited: false };
    public int? ProcessId => _process?.Id;

    public ServerProcessController(string serverDirectory, string batFileName = "start.bat")
    {
        _serverDirectory = serverDirectory;
        _batFileName = batFileName;
    }

    /// <summary>
    /// cmd.exe пишет свои собственные сообщения (в т.ч. приглашение "pause" при закрытии
    /// stdin) в OEM-кодпейдже консоли (866 для ru-RU), а не в ANSI/UTF-8 — если не задать
    /// кодировку явно, .NET декодирует их как попало и получается нечитаемая абракадабра
    /// вместо "Для продолжения нажмите любую клавишу . . .". Сам Java-процесс сервера обычно
    /// пишет в UTF-8 и не подчиняется этой кодировке — смешанность неизбежна при захвате
    /// вывода через cmd.exe, но подавляющее большинство строк (лог сервера) от этого не
    /// страдает, а служебные сообщения самого cmd.exe читаются корректно.
    /// CodePagesEncodingProvider нужен once — .NET Core/5+ не регистрирует OEM/ANSI кодпейджи
    /// по умолчанию, только Unicode-семейство.
    ///
    /// НЕ бери кодпейдж из CultureInfo.CurrentCulture.TextInfo.OEMCodePage — на практике он
    /// может не совпадать с реальной консольной OEM-кодировкой Windows (найдено вживую: на
    /// машине с системной локалью ru-RU/866, .NET-поток отдавал CurrentCulture = en-AI/850,
    /// декодирование под этим кодпейджем давало другую, но всё равно нечитаемую абракадабру).
    /// GetOEMCP() — тот же самый WinAPI-вызов, что использует сам cmd.exe/chcp, поэтому
    /// единственный надёжный источник здесь.
    /// </summary>
    static ServerProcessController()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [DllImport("kernel32.dll")]
    private static extern uint GetOEMCP();

    private static Encoding ConsoleOemEncoding => Encoding.GetEncoding((int)GetOEMCP());

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
            StandardOutputEncoding = ConsoleOemEncoding,
            StandardErrorEncoding = ConsoleOemEncoding,
            StandardInputEncoding = ConsoleOemEncoding,
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
        Started?.Invoke();
    }

    /// <summary>
    /// Отправляет произвольную команду в stdin сервера (say, list, whitelist add, op и т.д.) —
    /// для интерактивной консоли Admin-режима. В отличие от <see cref="StopAsync"/>, stdin
    /// НЕ закрывается — сервер продолжает работать и принимать дальнейшие команды.
    /// </summary>
    public async Task SendCommandAsync(string command, CancellationToken ct = default)
    {
        if (!IsRunning)
            throw new InvalidOperationException("Сервер не запущен.");

        await _process!.StandardInput.WriteLineAsync(command);
        await _process.StandardInput.FlushAsync(ct);
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

    /// <summary>
    /// Принудительно завершает процесс сервера (весь дочерний cmd.exe/java, не только cmd.exe) —
    /// используется, когда штатная остановка невозможна или не нужна (например,
    /// <see cref="ServerSmokeTestRunner"/> после неудачного тестового старта при публикации:
    /// сервер уже в краше или завис, слать "stop" и ждать штатного завершения бессмысленно).
    /// Не путать со <see cref="StopAsync"/> — там штатный "stop" в stdin и ожидание exit.
    /// </summary>
    public void Kill()
    {
        if (_process is { HasExited: false } process)
            process.Kill(entireProcessTree: true);
    }

    public void Dispose()
    {
        _process?.Dispose();
    }
}
