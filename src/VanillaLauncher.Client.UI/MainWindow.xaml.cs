using System.Net.Http;
using System.Windows;
using VanillaLauncher.Admin;
using VanillaLauncher.Client;

namespace VanillaLauncher.Client.UI;

public partial class MainWindow : Window
{
    private readonly HttpClient _http = new();
    private AppConfig? _config;
    private List<FilePlanItem>? _plan;
    private EngineUpdateInfo? _engineUpdateInfo;

    public MainWindow()
    {
        InitializeComponent();
        Title = $"VanillaLauncher {EngineVersion.Current}";
        Loaded += async (_, _) => await InitializeAsync();
    }

    /// <summary>
    /// ProfileRoot в appsettings.json — это папка сборки конкретного человека, у каждого
    /// своя. Значение, зашитое при сборке exe, подходит только автору сборки; у всех
    /// остальных, кто скачал тот же exe, этой папки на диске просто не будет — тогда
    /// просим выбрать её явно и сохраняем выбор локально (только на этой машине).
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            _config = AppConfig.Load();
        }
        catch (Exception ex)
        {
            StatusText.Text = "Ошибка конфигурации лаунчера.";
            Log($"Не удалось загрузить appsettings.json: {ex.Message}");
            CheckButton.IsEnabled = false;
            return;
        }

        if (!_config.IsConfigured)
        {
            // Отличаем "движок ещё не адаптирован под модпак" (ManifestUrl/GitHubOwner/
            // GitHubRepo пустые — свежий, ненастроенный exe) от "у этого игрока просто
            // другой путь" ниже. См. AppConfig.IsConfigured и docs/TASK_PATH_AUTODETECT.md.
            StatusText.Text = "Лаунчер не настроен под модпак. Обратись к администратору.";
            Log("ManifestUrl/GitHubOwner/GitHubRepo не заданы — это неадаптированная сборка движка.");
            CheckButton.IsEnabled = false;
            return;
        }

        if (!System.IO.Directory.Exists(_config.ProfileRoot))
        {
            var detected = PathAutoDetectService.TryFind(_config.ClientFolderName, _config.ClientSearchRoots);
            if (detected is not null)
            {
                _config.ProfileRoot = detected;
                _config.Save();
                Log($"Папка сборки определена автоматически: {detected}");
            }
            else
            {
                StatusText.Text = "Не найдена папка сборки Minecraft — укажи её вручную.";
                Log($"Папка «{_config.ProfileRoot}» не существует на этой машине, автоопределение не нашло совпадений.");
                CheckButton.IsEnabled = false;
                SelectProfileRootButton.Visibility = Visibility.Visible;
                return;
            }
        }

        await CheckForUpdatesAsync();
    }

    private void SelectProfileRootButton_Click(object sender, RoutedEventArgs e)
    {
        if (!PromptForProfileRoot())
            return;

        SelectProfileRootButton.Visibility = Visibility.Collapsed;
        CheckButton.IsEnabled = true;
        _ = CheckForUpdatesAsync();
    }

    private bool PromptForProfileRoot()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Выбери папку сборки Minecraft (профиль CurseForge/лаунчера с модами)"
        };

        if (dialog.ShowDialog(this) != true)
            return false;

        _config!.ProfileRoot = dialog.FolderName;
        _config.Save();
        Log($"Папка сборки установлена: {dialog.FolderName}");
        return true;
    }

    private async void CheckButton_Click(object sender, RoutedEventArgs e) => await CheckForUpdatesAsync();

    private async Task CheckForUpdatesAsync()
    {
        CheckButton.IsEnabled = false;
        UpdateButton.IsEnabled = false;
        StatusText.Text = "Проверка манифеста...";
        Log("Проверка манифеста...");

        try
        {
            _config ??= AppConfig.Load();

            var manifestService = new ManifestService(_http);
            var manifest = await manifestService.FetchAsync(_config.ManifestUrl);
            Log($"Версия сборки: {manifest.Version}, файлов в манифесте: {manifest.Files.Count}");

            var updateService = new UpdateService(_config.ProfileRoot);
            _plan = await updateService.BuildPlanAsync(manifest);

            var toDownload = _plan.Count(p => p.Action == FileAction.NeedsDownload);

            if (toDownload == 0)
            {
                StatusText.Text = $"Сборка актуальна (версия {manifest.Version}).";
                Log("Расхождений не найдено.");
            }
            else
            {
                StatusText.Text = $"Требуется обновление: {toDownload} из {_plan.Count} файлов.";
                Log($"Требуют обновления: {toDownload} из {_plan.Count}");
                UpdateButton.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "Ошибка проверки обновлений.";
            Log($"Ошибка: {ex.Message}");
        }
        finally
        {
            CheckButton.IsEnabled = true;
        }
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_plan is null || _config is null)
            return;

        CheckButton.IsEnabled = false;
        UpdateButton.IsEnabled = false;
        ProgressBar.Visibility = Visibility.Visible;
        ProgressBar.Value = 0;

        try
        {
            var downloader = new Downloader(_http, _config.ProfileRoot);
            var progress = new Progress<DownloadProgress>(p =>
            {
                var label = p.Stage == DownloadStage.Started ? "Скачивание" : "Готово";
                Log($"{label}: {p.FilePath} ({p.CompletedCount}/{p.TotalCount})");
                if (p.TotalCount > 0)
                    ProgressBar.Value = 100.0 * p.CompletedCount / p.TotalCount;
            });

            await downloader.ApplyAsync(_plan, progress);

            Log("Докачка завершена.");
            ProgressBar.Value = 100;
        }
        catch (Exception ex)
        {
            StatusText.Text = "Ошибка обновления.";
            Log($"Ошибка: {ex.Message}");
        }
        finally
        {
            CheckButton.IsEnabled = true;
            ProgressBar.Visibility = Visibility.Collapsed;
        }

        // Пересчитать состояние после докачки
        await CheckForUpdatesAsync();
    }

    private void GuideButton_Click(object sender, RoutedEventArgs e)
    {
        _config ??= AppConfig.Load();

        // Show(), не ShowDialog() — окно инструкции не должно мешать проверять/качать
        // обновления, пока открыто. Owner проставлен только ради z-order/сворачивания вместе
        // с главным окном, к модальности отношения не имеет.
        new GuideWindow("Инструкция — Клиент", _config.ClientGuideShort, _config.ClientGuideFull)
        {
            Owner = this
        }.Show();
    }

    private async void CheckEngineUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        _config ??= AppConfig.Load();

        if (string.IsNullOrWhiteSpace(_config.EngineGitHubOwner) || string.IsNullOrWhiteSpace(_config.EngineGitHubRepo))
        {
            EngineStatusText.Text = "EngineGitHubOwner/EngineGitHubRepo не заданы в Настройках — проверка обновлений лаунчера недоступна.";
            return;
        }

        CheckEngineUpdateButton.IsEnabled = false;
        UpdateEngineButton.IsEnabled = false;
        EngineStatusText.Text = "Проверка обновлений лаунчера...";
        Log("Проверка обновлений лаунчера...");

        try
        {
            _engineUpdateInfo = await EngineSelfUpdater.CheckForUpdateAsync(
                _http, _config.EngineGitHubOwner, _config.EngineGitHubRepo, EngineVersion.Current);

            if (_engineUpdateInfo.IsUpdateAvailable)
            {
                EngineStatusText.Text = $"Доступно обновление лаунчера: {_engineUpdateInfo.LatestVersion} (у вас {EngineVersion.Current}).";
                Log($"Найдено обновление лаунчера: {_engineUpdateInfo.LatestVersion} (сейчас {EngineVersion.Current}).");
                UpdateEngineButton.IsEnabled = true;
            }
            else
            {
                EngineStatusText.Text = $"У вас последняя версия лаунчера ({EngineVersion.Current}).";
                Log($"Обновлений лаунчера не найдено (у вас {EngineVersion.Current}).");
            }
        }
        catch (Exception ex)
        {
            EngineStatusText.Text = "Ошибка проверки обновлений лаунчера.";
            Log($"Ошибка проверки обновлений лаунчера: {ex.Message}");
        }
        finally
        {
            CheckEngineUpdateButton.IsEnabled = true;
        }
    }

    private async void UpdateEngineButton_Click(object sender, RoutedEventArgs e)
    {
        if (_engineUpdateInfo is not { IsUpdateAvailable: true, DownloadUrl: not null })
            return;

        var confirm = MessageBox.Show(
            $"Доступна новая версия лаунчера: {_engineUpdateInfo.LatestVersion} (у вас {EngineVersion.Current}).\n" +
            "Лаунчер скачает обновление, закроется и перезапустится с новой версией. Продолжить?",
            "Обновление лаунчера",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (confirm != MessageBoxResult.Yes)
            return;

        CheckEngineUpdateButton.IsEnabled = false;
        UpdateEngineButton.IsEnabled = false;
        EngineStatusText.Text = "Скачивание обновления лаунчера...";
        Log("Скачивание обновления лаунчера...");

        try
        {
            await EngineSelfUpdater.PrepareAndLaunchUpdateAsync(_http, _engineUpdateInfo.DownloadUrl);
            Log("Обновление скачано — лаунчер перезапускается.");
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            EngineStatusText.Text = "Ошибка обновления лаунчера.";
            Log($"Ошибка обновления лаунчера: {ex.Message}");
            CheckEngineUpdateButton.IsEnabled = true;
            UpdateEngineButton.IsEnabled = true;
        }
    }

    private void AdminButton_Click(object sender, RoutedEventArgs e)
    {
        _config ??= AppConfig.Load();

        var authService = new AdminAuthService(System.IO.Path.Combine(AppContext.BaseDirectory, "admin-auth.json"));

        if (!authService.HasPassword())
        {
            var setup = new SetAdminPasswordWindow(authService) { Owner = this };
            if (setup.ShowDialog() != true)
                return;
        }
        else
        {
            var login = new AdminLoginWindow(authService) { Owner = this };
            if (login.ShowDialog() != true)
                return;
        }

        new AdminWindow(_config).Show();
    }

    private void Log(string message)
    {
        LogList.Items.Add($"{DateTime.Now:HH:mm:ss}  {message}");
        if (LogList.Items.Count > 0)
            LogList.ScrollIntoView(LogList.Items[^1]);
    }
}
