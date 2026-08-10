using System.IO;
using System.Net.Http;
using System.Windows;
using VanillaLauncher.Admin;
using VanillaLauncher.Client;
using VanillaLauncher.Client.UI.Localization;

namespace VanillaLauncher.Client.UI;

public partial class MainWindow : Window
{
    private readonly HttpClient _http = new();
    private readonly ServerReachabilityChecker _reachabilityChecker = new();
    private AppConfig? _config;
    private List<FilePlanItem>? _plan;
    private EngineUpdateInfo? _engineUpdateInfo;

    // Прямое присваивание StatusText.Text (не биндинг) молча снимает XAML-биндинг с этого
    // DependencyProperty — так что переключение языка тумблером само по себе не обновляет уже
    // показанный статус. Запоминаем последний ключ/аргументы и перерисовываем текст явно из
    // SetLanguage(), иначе после переключения на лету статус так и останется на старом языке
    // до следующего события, которое его перезапишет.
    private string? _statusKey;
    private object[] _statusArgs = Array.Empty<object>();
    private string? _engineStatusKey;
    private object[] _engineStatusArgs = Array.Empty<object>();
    private string? _serverStatusKey;
    private object[] _serverStatusArgs = Array.Empty<object>();

    private void SetStatus(string key, params object[] args)
    {
        _statusKey = key;
        _statusArgs = args;
        StatusText.Text = args.Length == 0 ? Loc.Instance[key] : string.Format(Loc.Instance[key], args);
    }

    private void SetEngineStatus(string key, params object[] args)
    {
        _engineStatusKey = key;
        _engineStatusArgs = args;
        EngineStatusText.Text = args.Length == 0 ? Loc.Instance[key] : string.Format(Loc.Instance[key], args);
    }

    private void SetServerStatus(string key, params object[] args)
    {
        _serverStatusKey = key;
        _serverStatusArgs = args;
        ServerReachabilityText.Text = args.Length == 0 ? Loc.Instance[key] : string.Format(Loc.Instance[key], args);
    }

    public MainWindow()
    {
        InitializeComponent();
        Title = $"VanillaLauncher {EngineVersion.Current}";
        RefreshLanguageButtons();
        Loaded += async (_, _) => await InitializeAsync();
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
        if (_engineStatusKey is not null)
            SetEngineStatus(_engineStatusKey, _engineStatusArgs);
        if (_serverStatusKey is not null)
            SetServerStatus(_serverStatusKey, _serverStatusArgs);

        if (_config is null)
            return;

        _config.Language = language;
        try { _config.Save(); } catch (IOException) { } catch (UnauthorizedAccessException) { }
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
            Loc.Instance.Language = _config.Language;
            RefreshLanguageButtons();
        }
        catch (Exception ex)
        {
            SetStatus("Main.ConfigError.Status");
            Log(string.Format(Loc.Instance["Main.ConfigError.Log"], ex.Message));
            CheckButton.IsEnabled = false;
            return;
        }

        if (!_config.IsConfigured)
        {
            // Отличаем "движок ещё не адаптирован под модпак" (ManifestUrl/GitHubOwner/
            // GitHubRepo пустые — свежий, ненастроенный exe) от "у этого игрока просто
            // другой путь" ниже. См. AppConfig.IsConfigured и docs/TASK_PATH_AUTODETECT.md.
            // "Настроить лаунчер" даёт игроку самому вписать GitHubOwner/GitHubRepo (не
            // секретные, просто координаты репозитория) — не через Admin-режим, админу не
            // нужно пересобирать и рассылать exe+appsettings.json каждому другу заново.
            SetStatus("Main.NotConfigured.Status");
            Log(Loc.Instance["Main.NotConfigured.Log"]);
            CheckButton.IsEnabled = false;
            SetupButton.Visibility = Visibility.Visible;
            return;
        }

        await ProceedWithConfiguredClientAsync();
    }

    /// <summary>
    /// Общее продолжение после того, как GitHubOwner/GitHubRepo/ManifestUrl уже на месте —
    /// неважно, были ли они в appsettings.json с самого начала или их только что вписал
    /// игрок через SetupButton_Click.
    /// </summary>
    private async Task ProceedWithConfiguredClientAsync()
    {
        if (!System.IO.Directory.Exists(_config!.ProfileRoot))
        {
            var detected = PathAutoDetectService.TryFind(_config.ClientFolderName, _config.ClientSearchRoots);
            if (detected is not null)
            {
                _config.ProfileRoot = detected;
                _config.Save();
                Log(string.Format(Loc.Instance["Main.ProfileRootAutoDetected.Log"], detected));
            }
            else
            {
                SetStatus("Main.ProfileRootNotFound.Status");
                Log(string.Format(Loc.Instance["Main.ProfileRootNotFound.Log"], _config.ProfileRoot));
                CheckButton.IsEnabled = false;
                return;
            }
        }

        CheckButton.IsEnabled = true;
        await CheckForUpdatesAsync();

        // ServerHost пуст, если админ ещё не настроил Tailscale-адрес в "Настройках" —
        // тогда строка проверки остаётся скрытой (RefreshServerReachabilityVisibility),
        // не занимает место непонятным "не настроено" у модпаков, где этим не пользуются.
        RefreshServerReachabilityVisibility();
        if (!string.IsNullOrWhiteSpace(_config.ServerHost))
            _ = CheckServerReachabilityAsync();
    }

    private void RefreshServerReachabilityVisibility()
    {
        ServerReachabilityRow.Visibility = string.IsNullOrWhiteSpace(_config?.ServerHost)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private async void CheckServerButton_Click(object sender, RoutedEventArgs e) => await CheckServerReachabilityAsync();

    /// <summary>
    /// TCP-проверка адреса:порта сервера (обычно Tailscale IP/MagicDNS-имя) перед игрой — см.
    /// ServerReachabilityChecker. Не блокирует обновление мода/докачку, вызывается отдельно и
    /// не мешает остальному флоу проверки обновлений при ошибке/недоступности.
    /// </summary>
    private async Task CheckServerReachabilityAsync()
    {
        if (_config is null || string.IsNullOrWhiteSpace(_config.ServerHost))
            return;

        CheckServerButton.IsEnabled = false;
        ServerReachabilityText.Foreground = (System.Windows.Media.Brush)FindResource("MutedTextBrush");
        SetServerStatus("Main.ServerChecking.Status");

        var reachable = await _reachabilityChecker.IsReachableAsync(
            _config.ServerHost, _config.ServerPort, TimeSpan.FromSeconds(3));

        ServerReachabilityText.Foreground = (System.Windows.Media.Brush)FindResource(reachable ? "SuccessBrush" : "DangerBrush");
        SetServerStatus(
            reachable ? "Main.ServerReachable.Status" : "Main.ServerUnreachable.Status",
            _config.ServerHost, _config.ServerPort);

        CheckServerButton.IsEnabled = true;
    }

    private async void SetupButton_Click(object sender, RoutedEventArgs e)
    {
        _config ??= AppConfig.Load();

        var setup = new ClientSetupWindow(_config) { Owner = this };
        if (setup.ShowDialog() != true)
            return;

        SetupButton.Visibility = Visibility.Collapsed;
        Log(string.Format(Loc.Instance["Main.SetupDone.Log"], _config.GitHubOwner, _config.GitHubRepo));
        await ProceedWithConfiguredClientAsync();
    }

    /// <summary>
    /// Кнопка "Сменить папку сборки" видна и активна всегда, не только при первом
    /// запуске/ошибке автопоиска — игрок может пересобрать инстанс в другом месте или
    /// просто ошибиться при выборе и должен иметь возможность поправить путь сам, не прося
    /// администратора отредактировать appsettings.json за него.
    /// </summary>
    private void SelectProfileRootButton_Click(object sender, RoutedEventArgs e)
    {
        _config ??= AppConfig.Load();

        if (!PromptForProfileRoot())
            return;

        if (_config.IsConfigured)
        {
            CheckButton.IsEnabled = true;
            _ = CheckForUpdatesAsync();
        }
    }

    private bool PromptForProfileRoot()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = Loc.Instance["Main.SelectProfileRootDialog.Title"]
        };

        if (dialog.ShowDialog(this) != true)
            return false;

        _config!.ProfileRoot = dialog.FolderName;
        _config.Save();
        Log(string.Format(Loc.Instance["Main.SelectProfileRoot.Log"], dialog.FolderName));
        return true;
    }

    private async void CheckButton_Click(object sender, RoutedEventArgs e) => await CheckForUpdatesAsync();

    private async Task CheckForUpdatesAsync()
    {
        CheckButton.IsEnabled = false;
        UpdateButton.IsEnabled = false;
        SetStatus("Main.CheckingManifest.Log");
        Log(Loc.Instance["Main.CheckingManifest.Log"]);

        try
        {
            _config ??= AppConfig.Load();

            var manifestService = new ManifestService(_http);
            var manifest = await manifestService.FetchAsync(_config.ManifestUrl);
            Log(string.Format(Loc.Instance["Main.ManifestInfo.Log"], manifest.Version, manifest.Files.Count));

            var updateService = new UpdateService(_config.ProfileRoot);
            _plan = await updateService.BuildPlanAsync(manifest);

            var toDownload = _plan.Count(p => p.Action == FileAction.NeedsDownload);

            if (toDownload == 0)
            {
                SetStatus("Main.UpToDate.Status", manifest.Version);
                Log(Loc.Instance["Main.UpToDate.Log"]);
            }
            else
            {
                SetStatus("Main.UpdateNeeded.Status", toDownload, _plan.Count);
                Log(string.Format(Loc.Instance["Main.UpdateNeeded.Log"], toDownload, _plan.Count));
                UpdateButton.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            SetStatus("Main.CheckUpdatesError.Status");
            Log(string.Format(Loc.Instance["Common.Error"], ex.Message));
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
                var label = p.Stage == DownloadStage.Started ? Loc.Instance["Main.Downloading.Log"] : Loc.Instance["Main.DownloadDone.Log"];
                Log($"{label}: {p.FilePath} ({p.CompletedCount}/{p.TotalCount})");
                if (p.TotalCount > 0)
                    ProgressBar.Value = 100.0 * p.CompletedCount / p.TotalCount;
            });

            await downloader.ApplyAsync(_plan, progress);

            Log(Loc.Instance["Main.UpdateComplete.Log"]);
            ProgressBar.Value = 100;
        }
        catch (Exception ex)
        {
            SetStatus("Main.UpdateError.Status");
            Log(string.Format(Loc.Instance["Common.Error"], ex.Message));
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
        // с главным окном; от блокировки чужими ShowDialog() защищает WM_ENABLE hook внутри
        // самого GuideWindow (см. его комментарий) — Owner тут ни при чём.
        new GuideWindow(_config, GuideRole.Client)
        {
            Owner = this
        }.Show();
    }

    private async void CheckEngineUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        _config ??= AppConfig.Load();

        // Фоллбек на канонический репозиторий движка, если поля не заданы явно — чтобы
        // самообновление работало без отдельной настройки EngineGitHubOwner/EngineGitHubRepo
        // для любого модпака на этом движке. Явно заданное значение в конфиге (например, у
        // форка со своим движком) фоллбек не перезаписывает и не игнорирует.
        var owner = string.IsNullOrWhiteSpace(_config.EngineGitHubOwner) ? EngineRepositoryDefaults.Owner : _config.EngineGitHubOwner;
        var repo = string.IsNullOrWhiteSpace(_config.EngineGitHubRepo) ? EngineRepositoryDefaults.Repo : _config.EngineGitHubRepo;

        CheckEngineUpdateButton.IsEnabled = false;
        UpdateEngineButton.IsEnabled = false;
        SetEngineStatus("Main.EngineCheck.Status");
        Log(Loc.Instance["Main.EngineCheck.Status"]);

        try
        {
            _engineUpdateInfo = await EngineSelfUpdater.CheckForUpdateAsync(
                _http, owner, repo, EngineVersion.Current);

            if (_engineUpdateInfo.IsUpdateAvailable)
            {
                SetEngineStatus("Main.EngineUpdateAvailable.Status", _engineUpdateInfo.LatestVersion, EngineVersion.Current);
                Log(string.Format(Loc.Instance["Main.EngineUpdateAvailable.Log"], _engineUpdateInfo.LatestVersion, EngineVersion.Current));
                UpdateEngineButton.IsEnabled = true;
            }
            else
            {
                SetEngineStatus("Main.EngineUpToDate.Status", EngineVersion.Current);
                Log(string.Format(Loc.Instance["Main.EngineUpToDate.Log"], EngineVersion.Current));
            }
        }
        catch (Exception ex)
        {
            SetEngineStatus("Main.EngineCheckError.Status");
            Log(string.Format(Loc.Instance["Main.EngineCheckError.Log"], ex.Message));
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
            string.Format(Loc.Instance["Main.EngineUpdateConfirm.Text"], _engineUpdateInfo.LatestVersion, EngineVersion.Current),
            Loc.Instance["Main.EngineUpdateConfirm.Title"],
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (confirm != MessageBoxResult.Yes)
            return;

        CheckEngineUpdateButton.IsEnabled = false;
        UpdateEngineButton.IsEnabled = false;
        SetEngineStatus("Main.EngineDownloading.Status");
        Log(Loc.Instance["Main.EngineDownloading.Status"]);

        try
        {
            await EngineSelfUpdater.PrepareAndLaunchUpdateAsync(_http, _engineUpdateInfo.DownloadUrl);
            Log(Loc.Instance["Main.EngineUpdateDone.Log"]);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            SetEngineStatus("Main.EngineUpdateError.Status");
            Log(string.Format(Loc.Instance["Main.EngineUpdateError.Log"], ex.Message));
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

    // Ошибки от GitHub API (см. GitHubApiErrorTranslator) могут быть длинными и содержать
    // JSON — проще скопировать и переслать администратору/разработчику, чем перепечатывать
    // руками. ListBox с ItemTemplate из TextBlock не даёт штатного выделения текста мышью,
    // поэтому копирование — через контекстное меню/Ctrl+C, а не через выделение по символам.
    private void CopySelectedLog_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e) =>
        CopySelectedLogLines();

    private void CopySelectedLog_Click(object sender, RoutedEventArgs e) => CopySelectedLogLines();

    private void CopyAllLog_Click(object sender, RoutedEventArgs e)
    {
        var text = string.Join(Environment.NewLine, LogList.Items.Cast<string>());
        if (!string.IsNullOrEmpty(text))
            Clipboard.SetText(text);
    }

    private void CopySelectedLogLines()
    {
        var items = LogList.SelectedItems.Count > 0 ? LogList.SelectedItems : LogList.Items;
        var text = string.Join(Environment.NewLine, items.Cast<string>());
        if (!string.IsNullOrEmpty(text))
            Clipboard.SetText(text);
    }
}
