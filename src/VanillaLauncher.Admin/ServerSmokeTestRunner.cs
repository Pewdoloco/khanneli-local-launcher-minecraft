namespace VanillaLauncher.Admin;

public enum ServerSmokeTestOutcome
{
    Success,
    Crashed,
    TimedOut
}

public sealed record ServerSmokeTestResult(ServerSmokeTestOutcome Outcome, IReadOnlyList<string> Output)
{
    /// <summary>
    /// Строки, похожие на ошибку (<see cref="ServerLogLineClassifier.IsErrorLine"/>) — для
    /// короткого сообщения администратору вместо всего вывода сервера.
    /// </summary>
    public IReadOnlyList<string> ErrorLines => Output.Where(ServerLogLineClassifier.IsErrorLine).ToList();
}

/// <summary>
/// Кратковременный тестовый запуск сервера — шаг <see cref="PublishPipeline"/> после
/// синхронизации серверных файлов и перед публикацией манифеста. Ловит класс багов
/// "клиентский мод просочился на сервер" (метаданные соврали про environment, или мод забыли
/// добавить в ServerExcludeMods) ДО того, как обновление уйдёт на GitHub и до него доберутся
/// игроки — не постфактум, когда админ вручную нажмёт "Запустить сервер" уже после публикации.
/// </summary>
public sealed class ServerSmokeTestRunner
{
    public async Task<ServerSmokeTestResult> RunAsync(
        ServerProcessController controller,
        TimeSpan timeout,
        TimeSpan stopTimeout,
        CancellationToken ct = default)
    {
        var output = new List<string>();
        var tcs = new TaskCompletionSource<ServerSmokeTestOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnOutput(string line)
        {
            lock (output) output.Add(line);
            if (ServerLogLineClassifier.IsSuccessLine(line))
                tcs.TrySetResult(ServerSmokeTestOutcome.Success);
        }

        void OnExited() => tcs.TrySetResult(ServerSmokeTestOutcome.Crashed);

        controller.OutputReceived += OnOutput;
        controller.Exited += OnExited;

        try
        {
            controller.Start();

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeout, ct));
            var outcome = completed == tcs.Task ? tcs.Task.Result : ServerSmokeTestOutcome.TimedOut;

            if (outcome == ServerSmokeTestOutcome.Success)
                await controller.StopAsync(stopTimeout, ct);
            else if (controller.IsRunning)
                controller.Kill();

            List<string> outputSnapshot;
            lock (output) outputSnapshot = output.ToList();

            return new ServerSmokeTestResult(outcome, outputSnapshot);
        }
        finally
        {
            controller.OutputReceived -= OnOutput;
            controller.Exited -= OnExited;
        }
    }
}
