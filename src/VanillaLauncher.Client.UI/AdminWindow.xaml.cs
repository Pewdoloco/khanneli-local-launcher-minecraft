using System.IO;
using System.Windows;
using System.Windows.Media;
using VanillaLauncher.Admin;
using VanillaLauncher.Client;
using VanillaLauncher.Client.UI.Localization;

namespace VanillaLauncher.Client.UI;

public partial class AdminWindow : Window
{
    private readonly AppConfig _config;
    private ServerProcessController? _controller;

    // См. аналогичный комментарий в MainWindow.xaml.cs — прямое присваивание StatusText.Text
    // снимает XAML-биндинг, поэтому переключение языка тумблером само не обновляет уже
    // показанный статус без явного перерисовывания последнего ключа.
    private string? _statusKey;
    private object[] _statusArgs = Array.Empty<object>();

    // Тот же класс проблемы, что и со StatusText: Watermark.Hint выставляется императивно
    // (нужно из-за асинхронного запроса к GitHub, см. LoadLastPublishedVersionAsync), поэтому
    // тумблер языка сам его не обновляет — запоминаем последний известный тег и перерисовываем
    // при переключении.
    private string? _lastPublishedTag;

    // true между Start() и первым из двух исходов: строка "Done (...)!" в выводе (успех) или
    // Exited раньше, чем она появилась (провал старта — например, NoClassDefFoundError от
    // клиентского мода в серверной сборке). После явной остановки (StopButton) сбрасывается
    // в неизвестное состояние — это не провал, а штатное действие администратора.
    private bool _awaitingStartupOutcome;
    private bool? _startOutcomeSuccess;

    // Игроки, вошедшие в игру с последнего Start() — сбрасывается при каждом старте/остановке
    // (сервер не помнит своих игроков между процессами, и мы этого не эмулируем через "list").
    // Имена берутся построчным парсингом консоли (PlayerActivityParser) — тот же принцип, что
    // у IsErrorLine/StartOutcome ниже, отдельного сетевого запроса к серверу для этого нет.
    private readonly HashSet<string> _onlinePlayers = new(StringComparer.OrdinalIgnoreCase);

    private void UpdatePlayersOnlineText()
    {
        PlayersOnlineText.Text = _onlinePlayers.Count == 0
            ? Loc.Instance["Admin.PlayersOnlineEmpty"]
            : string.Format(
                Loc.Instance["Admin.PlayersOnlineCount"],
                _onlinePlayers.Count,
                string.Join(", ", _onlinePlayers.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)));
    }

    private void ClearOnlinePlayers()
    {
        _onlinePlayers.Clear();
        UpdatePlayersOnlineText();
    }

    private void SetStartOutcome(bool success)
    {
        _startOutcomeSuccess = success;
        StartOutcomeText.Visibility = Visibility.Visible;
        StartOutcomeText.Text = Loc.Instance[success ? "Admin.StartOutcomeSuccess" : "Admin.StartOutcomeFailure"];
        StartOutcomeText.Foreground = (System.Windows.Media.Brush)FindResource(success ? "SuccessBrush" : "DangerBrush");
    }

    private void ClearStartOutcome()
    {
        _startOutcomeSuccess = null;
        _awaitingStartupOutcome = false;
        StartOutcomeText.Visibility = Visibility.Collapsed;
    }

    private void SetStatus(string key, params object[] args)
    {
        _statusKey = key;
        _statusArgs = args;
        StatusText.Text = args.Length == 0 ? Loc.Instance[key] : string.Format(Loc.Instance[key], args);
    }

    private void RefreshVersionWatermark()
    {
        Watermark.SetHint(VersionTextBox, _lastPublishedTag is null
            ? Loc.Instance["Admin.VersionWatermark"]
            : string.Format(Loc.Instance["Admin.VersionWatermarkPrevious"], _lastPublishedTag));
    }

    // Ввод команд имеет смысл ровно тогда же, когда доступна "Остановить сервер" — сервер
    // запущен и стабилен (не в процессе старта/остановки/публикации). Выставляется везде,
    // где меняется StopButton.IsEnabled, а не биндится напрямую на него — самих мест мало
    // и логика включения нетривиальная (например, во время публикации сервер может быть
    // временно недоступен, даже если StopButton на вид активна).
    private void SetCommandInputEnabled(bool enabled)
    {
        CommandInputTextBox.IsEnabled = enabled;
        SendCommandButton.IsEnabled = enabled;
    }

    public AdminWindow(AppConfig config)
    {
        InitializeComponent();
        _config = config;
        Loc.Instance.Language = _config.Language;
        RefreshLanguageButtons();
        RefreshServerDirectoryState();

        RefreshVersionWatermark();
        UpdatePlayersOnlineText();
        _ = LoadLastPublishedVersionAsync();
    }

    private void RefreshLanguageButtons()
    {
        LangRuButton.Tag = Loc.Instance.Language == "ru" ? "Active" : null;
        LangEnButton.Tag = Loc.Instance.Language == "en" ? "Active" : null;
    }

    private void LangRuButton_Click(object sender, RoutedEventArgs e) => SetLanguage("ru");

    private void LangEnButton_Click(object sender, RoutedEventArgs e) => SetLanguage("en");

    private void SetLanguage(string language)
    {
        Loc.Instance.Language = language;
        RefreshLanguageButtons();

        if (_statusKey is not null)
            SetStatus(_statusKey, _statusArgs);
        if (_startOutcomeSuccess is { } success)
            SetStartOutcome(success);
        RefreshVersionWatermark();
        UpdatePlayersOnlineText();

        _config.Language = language;
        try { _config.Save(); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    /// <summary>
    /// Показывает тег последнего опубликованного релиза водяным знаком в поле "Версия
    /// релиза" — чтобы не лезть на github.com/{owner}/{repo}/releases каждый раз, когда
    /// нужно вспомнить, что публиковалось прошлый раз. Best-effort: публичный GET без
    /// токена, тихо оставляет generic-подсказку при любой ошибке/отсутствии релизов —
    /// показ версии не настолько важен, чтобы падать или мешать остальному окну.
    /// </summary>
    private async Task LoadLastPublishedVersionAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.GitHubOwner) || string.IsNullOrWhiteSpace(_config.GitHubRepo))
            return;

        try
        {
            using var http = new System.Net.Http.HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("VanillaLauncher-Admin");
            http.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            var url = $"https://api.github.com/repos/{_config.GitHubOwner}/{_config.GitHubRepo}/releases/latest";
            using var response = await http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var tag = doc.RootElement.GetProperty("tag_name").GetString();

            if (!string.IsNullOrWhiteSpace(tag))
            {
                _lastPublishedTag = tag;
                RefreshVersionWatermark();
            }
        }
        catch
        {
            // сеть недоступна/репозиторий ещё не создан и т.п. — остаётся generic-подсказка
        }
    }

    /// <summary>
    /// Определяет ServerDirectory, если он ещё не задан/не существует на этой машине:
    /// сперва автоопределение по ServerFolderName (папка рядом с .exe — сценарий "создана
    /// рядом с лаунчером" — плюс любые ServerSearchRoots из конфига), затем фоллбек на
    /// ручной выбор кнопкой. См. docs/TASK_PATH_AUTODETECT.md.
    /// </summary>
    private void RefreshServerDirectoryState()
    {
        if (!string.IsNullOrWhiteSpace(_config.ServerDirectory) && System.IO.Directory.Exists(_config.ServerDirectory))
        {
            SelectServerDirectoryButton.Visibility = Visibility.Collapsed;
            StartButton.IsEnabled = _controller is null || !_controller.IsRunning;
            return;
        }

        var roots = new List<string> { AppContext.BaseDirectory };
        roots.AddRange(_config.ServerSearchRoots);
        var detected = PathAutoDetectService.TryFind(_config.ServerFolderName, roots);

        if (detected is not null)
        {
            _config.ServerDirectory = detected;
            _config.Save();
            SetStatus("Admin.ServerDirAutoDetected.Status", detected);
            Log(string.Format(Loc.Instance["Admin.ServerDirAutoDetected.Log"], detected));
            SelectServerDirectoryButton.Visibility = Visibility.Collapsed;
            StartButton.IsEnabled = true;
            return;
        }

        SetStatus("Admin.ServerDirMissing.Status");
        StartButton.IsEnabled = false;
        SelectServerDirectoryButton.Visibility = Visibility.Visible;
    }

    private void SelectServerDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = Loc.Instance["Admin.SelectServerDirDialog.Title"]
        };

        if (dialog.ShowDialog(this) != true)
            return;

        _config.ServerDirectory = dialog.FolderName;
        _config.Save();
        Log(string.Format(Loc.Instance["Admin.ServerDirSet.Log"], dialog.FolderName));
        RefreshServerDirectoryState();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = new SettingsWindow(_config) { Owner = this };
        if (settings.ShowDialog() == true)
        {
            // Поля вроде ServerDirectory/ServerBatFileName могли поменяться — контроллер,
            // если уже создан (но не запущен), нужно пересоздать под новые значения.
            if (_controller?.IsRunning != true)
                _controller = null;

            RefreshLanguageButtons();
            RefreshServerDirectoryState();
        }
    }

    private void GuideButton_Click(object sender, RoutedEventArgs e)
    {
        // Show(), не ShowDialog() — можно продолжать управлять сервером, пока инструкция
        // открыта (например, сверяться с ней во время публикации обновления).
        new GuideWindow(_config, GuideRole.Admin)
        {
            Owner = this
        }.Show();
    }

    private ServerProcessController EnsureController()
    {
        if (_controller is null)
        {
            _controller = new ServerProcessController(_config.ServerDirectory!, _config.ServerBatFileName);
            // Индикатор "идёт запуск..." — одинаково работает и для ручного нажатия "Запустить
            // сервер", и для автоматического старта внутри смоук-теста при публикации
            // (ServerSmokeTestRunner), так как оба пути идут через один и тот же Start().
            _controller.Started += () => Dispatcher.Invoke(() =>
            {
                StartupProgressBar.Visibility = Visibility.Visible;
            });
            _controller.OutputReceived += line => Dispatcher.Invoke(() =>
            {
                Log(line);
                if (ServerLogLineClassifier.IsErrorLine(line))
                    LogError(line);

                var joined = PlayerActivityParser.TryParseJoin(line);
                if (joined is not null)
                {
                    _onlinePlayers.Add(joined);
                    UpdatePlayersOnlineText();
                }
                else
                {
                    var left = PlayerActivityParser.TryParseLeave(line);
                    if (left is not null)
                    {
                        _onlinePlayers.Remove(left);
                        UpdatePlayersOnlineText();
                    }
                }
                if (ServerLogLineClassifier.IsSuccessLine(line))
                    StartupProgressBar.Visibility = Visibility.Collapsed;
                if (_awaitingStartupOutcome && ServerLogLineClassifier.IsSuccessLine(line))
                {
                    _awaitingStartupOutcome = false;
                    SetStartOutcome(success: true);
                }
            });
            _controller.Exited += () => Dispatcher.Invoke(() =>
            {
                StartupProgressBar.Visibility = Visibility.Collapsed;
                if (_awaitingStartupOutcome)
                    SetStartOutcome(success: false);
                _awaitingStartupOutcome = false;
                ClearOnlinePlayers();

                // Во время публикации сервер может сам стартовать и останавливаться (смоук-тест
                // в PublishPipeline, см. ServerSmokeTestRunner) — не перетираем статус публикации
                // и не включаем кнопки раньше времени. PublishButton.IsEnabled — надёжный признак
                // "публикация ещё идёт": это единственное место, которое его выключает/включает
                // (см. PublishButton_Click).
                if (!PublishButton.IsEnabled)
                    return;

                SetStatus("Admin.StatusStopped");
                StartButton.IsEnabled = true;
                StopButton.IsEnabled = false;
                SetCommandInputEnabled(false);
            });
        }

        return _controller;
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_config.ServerDirectory))
            return;

        var controller = EnsureController();

        try
        {
            ClearStartOutcome();
            ClearOnlinePlayers();
            controller.Start();
            _awaitingStartupOutcome = true;
            SetStatus("Admin.ServerStarting.Status");
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            SetCommandInputEnabled(true);
        }
        catch (Exception ex)
        {
            SetStatus("Admin.ServerStartError.Status");
            Log(string.Format(Loc.Instance["Common.Error"], ex.Message));
        }
    }

    private async void StopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_controller is null || !_controller.IsRunning)
            return;

        StopButton.IsEnabled = false;
        SetCommandInputEnabled(false);
        ClearStartOutcome();
        SetStatus("Admin.ServerStopping.Status");

        var stoppedGracefully = await _controller.StopAsync(TimeSpan.FromSeconds(60));

        if (!stoppedGracefully)
        {
            SetStatus("Admin.ServerStopTimeout.Status");
            Log(Loc.Instance["Admin.ServerStopTimeout.Log"]);
            StopButton.IsEnabled = true;
        }
        // Явного окна с подтверждением больше нет — цветной StatusText/StartOutcomeText уже
        // достаточно заметен, а лишний модальный клик мешал, когда админ переключается между
        // окнами. Кнопки/StatusText в любом случае обновит обработчик Exited.
    }

    private async void RecreateWorldButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_config.ServerDirectory))
            return;

        if (_controller?.IsRunning == true)
        {
            MessageBox.Show(
                Loc.Instance["Admin.RecreateWorldRunning.MessageBox"],
                Loc.Instance["Common.AdminWindowTitle"],
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var levelName = ServerPropertiesReader.GetLevelName(_config.ServerDirectory);
        var confirm = MessageBox.Show(
            string.Format(Loc.Instance["Admin.RecreateWorldConfirm.Text"], levelName),
            Loc.Instance["Admin.RecreateWorldConfirm.Title"],
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirm != MessageBoxResult.Yes)
            return;

        RecreateWorldButton.IsEnabled = false;
        SetStatus("Admin.RecreateWorldBackingUp.Status", levelName);
        Log(string.Format(Loc.Instance["Admin.RecreateWorldBackingUp.Log"], levelName));

        try
        {
            var backupsDir = System.IO.Path.Combine(_config.ServerDirectory, "backups");
            var service = new WorldBackupService(_config.ServerDirectory, backupsDir, _config.MaxBackupsToKeep);

            var backupPath = await Task.Run(() => service.RecreateWorld(levelName));

            if (backupPath is null)
            {
                SetStatus("Admin.RecreateWorldMissing.Status", levelName);
                Log(Loc.Instance["Admin.RecreateWorldMissing.Log"]);
            }
            else
            {
                SetStatus("Admin.RecreateWorldDone.Status", levelName);
                Log(string.Format(Loc.Instance["Admin.RecreateWorldDone.Log"], backupPath));
                Log(Loc.Instance["Admin.RecreateWorldDone.Log2"]);
            }
        }
        catch (Exception ex)
        {
            SetStatus("Admin.RecreateWorldError.Status");
            Log(string.Format(Loc.Instance["Common.Error"], ex.Message));
        }
        finally
        {
            RecreateWorldButton.IsEnabled = true;
        }
    }

    private async void PublishButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_config.ServerDirectory))
            return;

        var version = VersionTextBox.Text.Trim();
        if (string.IsNullOrEmpty(version))
        {
            MessageBox.Show(Loc.Instance["Admin.PublishNoVersion.MessageBox"],
                Loc.Instance["Common.AdminWindowTitle"], MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_config.GitHubOwner) || string.IsNullOrWhiteSpace(_config.GitHubRepo))
        {
            MessageBox.Show(Loc.Instance["Admin.PublishNoRepo.MessageBox"],
                Loc.Instance["Common.AdminWindowTitle"], MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string token;
        try
        {
            token = GitHubTokenProvider.GetTokenOrThrow();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Loc.Instance["Common.AdminWindowTitle"], MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var confirm = MessageBox.Show(
            string.Format(Loc.Instance["Admin.PublishConfirm.Text"], version, _config.ProfileRoot),
            Loc.Instance["Admin.PublishConfirm.Title"],
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirm != MessageBoxResult.Yes)
            return;

        var controller = EnsureController();

        PublishButton.IsEnabled = false;
        StartButton.IsEnabled = false;
        StopButton.IsEnabled = false;
        RecreateWorldButton.IsEnabled = false;
        SetCommandInputEnabled(false);
        SetStatus("Admin.Publishing.Status", version);

        try
        {
            var backupsDir = System.IO.Path.Combine(_config.ServerDirectory, "backups");
            var worldBackup = new WorldBackupService(_config.ServerDirectory, backupsDir, _config.MaxBackupsToKeep);
            var levelName = ServerPropertiesReader.GetLevelName(_config.ServerDirectory);

            using var http = new System.Net.Http.HttpClient();
            var githubClient = new GitHubReleaseClient(http, _config.GitHubOwner!, _config.GitHubRepo!, token);
            var publisher = new ReleasePublisher(githubClient);
            var progress = new Progress<string>(Log);

            await new PublishPipeline().RunAsync(
                controller,
                worldBackup,
                levelName,
                _config.ServerDirectory,
                _config.ProfileRoot,
                _config.IncludeFolders,
                version,
                publisher,
                progress,
                serverExcludeFileNames: _config.ServerExcludeMods);

            SetStatus("Admin.PublishDone.Status", version);

            // Пассивного обновления StatusText/лога недостаточно — та же причина, что и у
            // явного окна после "Остановить сервер" (см. StopButton_Click): легко пропустить,
            // если не смотреть на окно именно в момент завершения долгой публикации.
            MessageBox.Show(
                string.Format(Loc.Instance["Admin.PublishDone.MessageBox"], version),
                Loc.Instance["Common.AdminWindowTitle"], MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            SetStatus("Admin.PublishError.Status");
            Log(string.Format(Loc.Instance["Admin.PublishError.Log"], ex.Message));
            MessageBox.Show(string.Format(Loc.Instance["Admin.PublishError.MessageBox"], ex.Message), Loc.Instance["Common.AdminWindowTitle"],
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            PublishButton.IsEnabled = true;
            StartButton.IsEnabled = !controller.IsRunning;
            StopButton.IsEnabled = controller.IsRunning;
            SetCommandInputEnabled(controller.IsRunning);
            RecreateWorldButton.IsEnabled = true;
        }
    }

    private void SendCommandButton_Click(object sender, RoutedEventArgs e) => _ = SendCommandAsync();

    private void CommandInputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
            _ = SendCommandAsync();
    }

    /// <summary>
    /// Отправляет введённую команду в stdin сервера (say, list, whitelist add, op и т.д.) —
    /// не путать со "Стоп" (см. StopButton_Click, у неё отдельная фиксированная команда и
    /// логика ожидания завершения процесса). Эхо команды в лог — сервер сам её не печатает
    /// (в консоли виден только вывод сервера, не то, что было введено на его stdin).
    /// </summary>
    private async Task SendCommandAsync()
    {
        var command = CommandInputTextBox.Text.Trim();
        if (string.IsNullOrEmpty(command) || _controller is not { IsRunning: true } controller)
            return;

        Log($"> {command}");
        CommandInputTextBox.Clear();

        try
        {
            await controller.SendCommandAsync(command);
        }
        catch (Exception ex)
        {
            Log(string.Format(Loc.Instance["Admin.CommandSendError"], ex.Message));
        }
    }

    // true только когда мы сами инициируем повторное закрытие после того, как сервер уже
    // остановлен (см. StopServerThenCloseAsync) — иначе Window_Closing поймал бы и это
    // закрытие тоже и снова ушёл в диалог подтверждения по кругу.
    private bool _closingAfterServerStopped;

    /// <summary>
    /// Раньше закрытие окна просто предупреждало, что сервер продолжит работать в фоне,
    /// но не мешало закрыться. Теперь — если сервер запущен, закрытие ОТМЕНЯЕТСЯ и вместо
    /// этого предлагается сначала штатно остановить сервер; лаунчер закрывается только
    /// после успешной остановки (или сразу, если сервер и так не запущен).
    /// </summary>
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_closingAfterServerStopped || _controller?.IsRunning != true)
            return;

        e.Cancel = true;

        var confirm = MessageBox.Show(
            Loc.Instance["Admin.CloseConfirm.Text"],
            Loc.Instance["Common.AdminWindowTitle"],
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirm == MessageBoxResult.Yes)
            _ = StopServerThenCloseAsync();
    }

    private async Task StopServerThenCloseAsync()
    {
        StopButton.IsEnabled = false;
        SetCommandInputEnabled(false);
        SetStatus("Admin.ClosingStoppingServer.Status");
        Log(Loc.Instance["Admin.ClosingStoppingServer.Log"]);

        var stopped = await _controller!.StopAsync(TimeSpan.FromSeconds(60));

        if (!stopped)
        {
            MessageBox.Show(
                Loc.Instance["Admin.ClosingStopTimeout.MessageBox"],
                Loc.Instance["Common.AdminWindowTitle"], MessageBoxButton.OK, MessageBoxImage.Error);
            StopButton.IsEnabled = true;
            return;
        }

        MessageBox.Show(Loc.Instance["Admin.ClosingStopped.MessageBox"], Loc.Instance["Common.AdminWindowTitle"],
            MessageBoxButton.OK, MessageBoxImage.Information);

        _closingAfterServerStopped = true;
        Close();
    }

    // Ошибки от GitHub API (см. GitHubApiErrorTranslator) могут быть длинными и содержать
    // JSON — проще скопировать и переслать администратору/разработчику, чем перепечатывать
    // руками. Общие для LogList и ErrorLogList — оба ListBox устроены одинаково.
    private void CopySelectedLog_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e) =>
        CopySelectedLines(LogList);

    private void CopySelectedLog_Click(object sender, RoutedEventArgs e) => CopySelectedLines(LogList);

    private void CopyAllLog_Click(object sender, RoutedEventArgs e) => CopyAllLines(LogList);

    private void CopySelectedErrorLog_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e) =>
        CopySelectedLines(ErrorLogList);

    private void CopySelectedErrorLog_Click(object sender, RoutedEventArgs e) => CopySelectedLines(ErrorLogList);

    private void CopyAllErrorLog_Click(object sender, RoutedEventArgs e) => CopyAllLines(ErrorLogList);

    private static void CopySelectedLines(System.Windows.Controls.ListBox listBox)
    {
        var items = listBox.SelectedItems.Count > 0 ? listBox.SelectedItems : listBox.Items;
        var text = string.Join(Environment.NewLine, items.Cast<string>());
        if (!string.IsNullOrEmpty(text))
            Clipboard.SetText(text);
    }

    private static void CopyAllLines(System.Windows.Controls.ListBox listBox)
    {
        var text = string.Join(Environment.NewLine, listBox.Items.Cast<string>());
        if (!string.IsNullOrEmpty(text))
            Clipboard.SetText(text);
    }

    private void Log(string message)
    {
        LogList.Items.Add($"{DateTime.Now:HH:mm:ss}  {message}");
        if (LogList.Items.Count > 0)
            LogList.ScrollIntoView(LogList.Items[^1]);
        App.Logger.Log($"[Admin] {message}");
    }

    private void LogError(string message)
    {
        ErrorLogList.Items.Add($"{DateTime.Now:HH:mm:ss}  {message}");
        if (ErrorLogList.Items.Count > 0)
            ErrorLogList.ScrollIntoView(ErrorLogList.Items[^1]);
    }
}
