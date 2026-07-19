using System.Windows;
using VanillaLauncher.Admin;
using VanillaLauncher.Client;

namespace VanillaLauncher.Client.UI;

public partial class AdminWindow : Window
{
    private readonly AppConfig _config;
    private ServerProcessController? _controller;

    public AdminWindow(AppConfig config)
    {
        InitializeComponent();
        _config = config;
        RefreshServerDirectoryState();
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
            StatusText.Text = $"Серверная папка определена автоматически: {detected}";
            Log($"ServerDirectory автоопределён: {detected}");
            SelectServerDirectoryButton.Visibility = Visibility.Collapsed;
            StartButton.IsEnabled = true;
            return;
        }

        StatusText.Text = "ServerDirectory не задан — укажи папку сервера вручную.";
        StartButton.IsEnabled = false;
        SelectServerDirectoryButton.Visibility = Visibility.Visible;
    }

    private void SelectServerDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Выбери (или создай) папку сервера"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        _config.ServerDirectory = dialog.FolderName;
        _config.Save();
        Log($"Папка сервера установлена: {dialog.FolderName}");
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

            RefreshServerDirectoryState();
        }
    }

    private ServerProcessController EnsureController()
    {
        if (_controller is null)
        {
            _controller = new ServerProcessController(_config.ServerDirectory!, _config.ServerBatFileName);
            _controller.OutputReceived += line => Dispatcher.Invoke(() => Log(line));
            _controller.Exited += () => Dispatcher.Invoke(() =>
            {
                StatusText.Text = "Сервер остановлен.";
                StartButton.IsEnabled = true;
                StopButton.IsEnabled = false;
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
            controller.Start();
            StatusText.Text = "Сервер запускается...";
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            StatusText.Text = "Ошибка запуска.";
            Log($"Ошибка: {ex.Message}");
        }
    }

    private async void StopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_controller is null || !_controller.IsRunning)
            return;

        StopButton.IsEnabled = false;
        StatusText.Text = "Останавливаем сервер (ждём штатного завершения)...";

        var stoppedGracefully = await _controller.StopAsync(TimeSpan.FromSeconds(60));

        if (!stoppedGracefully)
        {
            StatusText.Text = "Сервер не завершился штатно за 60 секунд.";
            Log("Сервер не ответил на stop за отведённое время — возможно, завис.");
            StopButton.IsEnabled = true;
        }
        // При успешной остановке StatusText/кнопки обновит обработчик Exited.
    }

    private async void RecreateWorldButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_config.ServerDirectory))
            return;

        if (_controller?.IsRunning == true)
        {
            MessageBox.Show(
                "Сначала останови сервер — пересоздавать мир на работающем сервере нельзя.",
                "VanillaLauncher — Admin",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var levelName = ServerPropertiesReader.GetLevelName(_config.ServerDirectory);
        var confirm = MessageBox.Show(
            $"Мир «{levelName}» будет забэкаплен в backups/, затем удалён. " +
            "Новый мир сгенерируется при следующем запуске сервера. Продолжить?",
            "Пересоздать мир",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirm != MessageBoxResult.Yes)
            return;

        RecreateWorldButton.IsEnabled = false;
        StatusText.Text = $"Бэкап мира «{levelName}»...";
        Log($"Пересоздание мира «{levelName}»: бэкап...");

        try
        {
            var backupsDir = System.IO.Path.Combine(_config.ServerDirectory, "backups");
            var service = new WorldBackupService(_config.ServerDirectory, backupsDir, _config.MaxBackupsToKeep);

            var backupPath = await Task.Run(() => service.RecreateWorld(levelName));

            if (backupPath is null)
            {
                StatusText.Text = $"Мир «{levelName}» не найден — нечего было пересоздавать.";
                Log("Папка мира отсутствовала, бэкап не создавался.");
            }
            else
            {
                StatusText.Text = $"Мир «{levelName}» пересоздан.";
                Log($"Бэкап сохранён: {backupPath}");
                Log("Папка мира удалена. Новый мир сгенерируется при следующем запуске сервера.");
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "Ошибка пересоздания мира.";
            Log($"Ошибка: {ex.Message}");
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
            MessageBox.Show("Укажи версию релиза (например, 26.1.2-b2).",
                "VanillaLauncher — Admin", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_config.GitHubOwner) || string.IsNullOrWhiteSpace(_config.GitHubRepo))
        {
            MessageBox.Show("GitHubOwner/GitHubRepo не заданы в appsettings.json.",
                "VanillaLauncher — Admin", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string token;
        try
        {
            token = GitHubTokenProvider.GetTokenOrThrow();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "VanillaLauncher — Admin", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var confirm = MessageBox.Show(
            $"Будет опубликован релиз «{version}» из {_config.ProfileRoot}:\n" +
            "1. Бэкап мира\n2. Остановка сервера (если запущен)\n3. Обновление серверных модов/конфигов\n" +
            "4. Генерация и загрузка manifest.json + файлов в новый GitHub Release\n5. Запуск сервера обратно.\n\n" +
            "Это создаст ПУБЛИЧНЫЙ релиз в репозитории. Продолжить?",
            "Опубликовать обновление",
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
        StatusText.Text = $"Публикация «{version}»...";

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

            StatusText.Text = $"Опубликовано: {version}.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Ошибка публикации.";
            Log($"Ошибка: {ex.Message}");
        }
        finally
        {
            PublishButton.IsEnabled = true;
            StartButton.IsEnabled = !controller.IsRunning;
            StopButton.IsEnabled = controller.IsRunning;
            RecreateWorldButton.IsEnabled = true;
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_controller?.IsRunning == true)
        {
            MessageBox.Show(
                "Сервер продолжит работать в фоне после закрытия этого окна. " +
                "Останавливай его кнопкой «Остановить сервер» перед закрытием, если это не нужно.",
                "VanillaLauncher — Admin",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void Log(string message)
    {
        LogList.Items.Add(message);
        if (LogList.Items.Count > 0)
            LogList.ScrollIntoView(LogList.Items[^1]);
    }
}
