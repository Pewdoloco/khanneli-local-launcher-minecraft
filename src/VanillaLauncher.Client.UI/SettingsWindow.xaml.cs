using System.Windows;
using System.Windows.Controls;
using VanillaLauncher.Client;
using VanillaLauncher.Client.UI.Localization;

namespace VanillaLauncher.Client.UI;

/// <summary>
/// Экран "Настройки" — отдельное окно, не часть AdminWindow (см. docs/ARCHITECTURE.md,
/// раздел про Этап "Настройка лаунчера под модпак"). Открывается только изнутри AdminWindow,
/// то есть только после того же гейта по паролю, что и весь Admin-режим — обычный
/// пользователь никогда не создаёт это окно.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly AppConfig _config;

    public SettingsWindow(AppConfig config)
    {
        InitializeComponent();
        _config = config;

        ProfileRootTextBox.Text = _config.ProfileRoot;
        ClientFolderNameTextBox.Text = _config.ClientFolderName ?? string.Empty;
        ClientSearchRootsTextBox.Text = JoinLines(_config.ClientSearchRoots);

        ServerDirectoryTextBox.Text = _config.ServerDirectory ?? string.Empty;
        ServerFolderNameTextBox.Text = _config.ServerFolderName ?? string.Empty;
        ServerSearchRootsTextBox.Text = JoinLines(_config.ServerSearchRoots);

        ServerBatFileNameTextBox.Text = _config.ServerBatFileName;
        ServerHostTextBox.Text = _config.ServerHost ?? string.Empty;
        ServerPortTextBox.Text = _config.ServerPort.ToString();
        ManifestUrlTextBox.Text = _config.ManifestUrl;
        GitHubOwnerTextBox.Text = _config.GitHubOwner ?? string.Empty;
        GitHubRepoTextBox.Text = _config.GitHubRepo ?? string.Empty;
        EngineGitHubOwnerTextBox.Text = _config.EngineGitHubOwner ?? string.Empty;
        EngineGitHubRepoTextBox.Text = _config.EngineGitHubRepo ?? string.Empty;
        IncludeFoldersTextBox.Text = JoinLines(_config.IncludeFolders);
        ServerExcludeModsTextBox.Text = JoinLines(_config.ServerExcludeMods);
        MaxBackupsToKeepTextBox.Text = _config.MaxBackupsToKeep.ToString();

        // Resolve — та же логика, что и в GuideWindow: пока поле не тронуто админом, показываем
        // дефолт движка на ТЕКУЩЕМ языке интерфейса (иначе EN-администратору показывался бы
        // русский дефолт без всякой возможности прочитать/отредактировать английскую версию).
        // Модальность SettingsWindow (ShowDialog) исключает переключение языка, пока это окно
        // открыто — родительское окно с тумблером заблокировано, поэтому достаточно разрешить
        // один раз при открытии, live-обновление не нужно (в отличие от GuideWindow).
        var language = Loc.Instance.Language;
        ClientGuideShortTextBox.Text = DefaultGuides.Resolve(_config.ClientGuideShort, DefaultGuides.ClientShort, DefaultGuides.ClientShortEn, language);
        ClientGuideFullTextBox.Text = DefaultGuides.Resolve(_config.ClientGuideFull, DefaultGuides.ClientFull, DefaultGuides.ClientFullEn, language);
        AdminGuideShortTextBox.Text = DefaultGuides.Resolve(_config.AdminGuideShort, DefaultGuides.AdminShort, DefaultGuides.AdminShortEn, language);
        AdminGuideFullTextBox.Text = DefaultGuides.Resolve(_config.AdminGuideFull, DefaultGuides.AdminFull, DefaultGuides.AdminFullEn, language);
        ClientGuideManualTextBox.Text = DefaultGuides.Resolve(_config.ClientGuideManual, DefaultGuides.ClientManual, DefaultGuides.ClientManualEn, language);
        AdminGuideManualTextBox.Text = DefaultGuides.Resolve(_config.AdminGuideManual, DefaultGuides.AdminManual, DefaultGuides.AdminManualEn, language);
    }

    private static string JoinLines(IEnumerable<string> values) => string.Join(Environment.NewLine, values);

    private static List<string> SplitLines(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    private void BrowseProfileRoot_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = Loc.Instance["Settings.BrowseProfileRootDialog.Title"] };
        if (dialog.ShowDialog(this) == true)
            ProfileRootTextBox.Text = dialog.FolderName;
    }

    private void BrowseServerDirectory_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = Loc.Instance["Settings.BrowseServerDirDialog.Title"] };
        if (dialog.ShowDialog(this) == true)
            ServerDirectoryTextBox.Text = dialog.FolderName;
    }

    private void AutoDetectClient_Click(object sender, RoutedEventArgs e)
    {
        var roots = SplitLines(ClientSearchRootsTextBox.Text);
        var detected = PathAutoDetectService.TryFind(ClientFolderNameTextBox.Text, roots);

        if (detected is not null)
        {
            ProfileRootTextBox.Text = detected;
            ErrorText.Visibility = Visibility.Collapsed;
        }
        else
        {
            ErrorText.Text = Loc.Instance["Settings.AutoDetectClientError"];
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void AutoDetectServer_Click(object sender, RoutedEventArgs e)
    {
        var roots = new List<string> { AppContext.BaseDirectory };
        roots.AddRange(SplitLines(ServerSearchRootsTextBox.Text));
        var detected = PathAutoDetectService.TryFind(ServerFolderNameTextBox.Text, roots);

        if (detected is not null)
        {
            ServerDirectoryTextBox.Text = detected;
            ErrorText.Visibility = Visibility.Collapsed;
        }
        else
        {
            ErrorText.Text = Loc.Instance["Settings.AutoDetectServerError"];
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void PickExcludedMods_Click(object sender, RoutedEventArgs e)
    {
        var defaultFolder = string.IsNullOrWhiteSpace(ProfileRootTextBox.Text)
            ? null
            : System.IO.Path.Combine(ProfileRootTextBox.Text, "mods");

        var currentlyExcluded = SplitLines(ServerExcludeModsTextBox.Text);
        var picker = new SelectExcludedModsWindow(defaultFolder, currentlyExcluded) { Owner = this };

        if (picker.ShowDialog() != true)
            return;

        // Пикер управляет только теми записями, что реально нашёл в просканированной папке —
        // введённые вручную имена для модов из другой папки (например, старый список из
        // appsettings.json до первого использования пикера) не трогаем и не теряем.
        var scanned = new HashSet<string>(picker.ScannedFileNames, StringComparer.OrdinalIgnoreCase);
        var preserved = currentlyExcluded.Where(name => !scanned.Contains(name));
        var merged = preserved.Concat(picker.SelectedExcludedMods).Distinct(StringComparer.OrdinalIgnoreCase);

        ServerExcludeModsTextBox.Text = JoinLines(merged);
    }

    // Ключ Tag кнопки -> (textbox, компактная высота, увеличенная высота "для удобства
    // печати"). WorldBackupService требует MaxBackupsToKeep >= 1 (см. AdminWindow) — тут
    // высоты касаются только полей руководств, но заодно рядом лежит и валидация того же
    // рода для MaxBackupsToKeep ниже в SaveButton_Click.
    private (System.Windows.Controls.TextBox TextBox, double Compact, double Expanded) GetGuideField(string key) => key switch
    {
        "ClientShort" => (ClientGuideShortTextBox, 80, 320),
        "ClientFull" => (ClientGuideFullTextBox, 140, 380),
        "AdminShort" => (AdminGuideShortTextBox, 80, 320),
        "AdminFull" => (AdminGuideFullTextBox, 140, 380),
        "ClientManual" => (ClientGuideManualTextBox, 140, 380),
        "AdminManual" => (AdminGuideManualTextBox, 140, 380),
        _ => throw new InvalidOperationException($"Неизвестное поле руководства: {key}")
    };

    /// <summary>
    /// Поля руководств read-only по умолчанию — случайный клик/ввод текста в это поле,
    /// пока правишь соседние настройки, иначе легко бы испортил инструкцию незаметно.
    /// "Редактировать" снимает read-only и увеличивает поле для удобной печати; "Готово"
    /// возвращает оба свойства обратно, не сохраняя (сохранение — только общей кнопкой
    /// "Сохранить" внизу окна, как и у всех остальных полей).
    /// </summary>
    private void ToggleGuideEdit_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var key = (string)button.Tag;
        var (textBox, compact, expanded) = GetGuideField(key);

        var startEditing = textBox.IsReadOnly;
        textBox.IsReadOnly = !startEditing;
        textBox.Height = startEditing ? expanded : compact;
        button.Content = startEditing ? "Готово" : "Редактировать";

        if (startEditing)
            textBox.Focus();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(MaxBackupsToKeepTextBox.Text, out var maxBackups) || maxBackups < 1)
        {
            // WorldBackupService.ctor бросает ArgumentOutOfRangeException при < 1 — сверяем
            // здесь ту же границу, чтобы не пропустить в конфиг значение, которое сломает
            // "Пересоздать мир" при первом же клике в AdminWindow.
            ErrorText.Text = Loc.Instance["Settings.MaxBackupsError"];
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        if (!int.TryParse(ServerPortTextBox.Text, out var serverPort) || serverPort is < 1 or > 65535)
        {
            ErrorText.Text = Loc.Instance["Settings.ServerPortError"];
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        _config.ProfileRoot = ProfileRootTextBox.Text.Trim();
        _config.ClientFolderName = NullIfEmpty(ClientFolderNameTextBox.Text);
        _config.ClientSearchRoots = SplitLines(ClientSearchRootsTextBox.Text);

        _config.ServerDirectory = NullIfEmpty(ServerDirectoryTextBox.Text);
        _config.ServerFolderName = NullIfEmpty(ServerFolderNameTextBox.Text);
        _config.ServerSearchRoots = SplitLines(ServerSearchRootsTextBox.Text);

        _config.ServerBatFileName = ServerBatFileNameTextBox.Text.Trim();
        _config.ServerHost = NullIfEmpty(ServerHostTextBox.Text);
        _config.ServerPort = serverPort;
        _config.ManifestUrl = ManifestUrlTextBox.Text.Trim();
        _config.GitHubOwner = GitHubRepoNameNormalizer.NormalizeOwner(GitHubOwnerTextBox.Text);
        _config.GitHubRepo = GitHubRepoNameNormalizer.Normalize(GitHubRepoTextBox.Text);
        _config.EngineGitHubOwner = GitHubRepoNameNormalizer.NormalizeOwner(EngineGitHubOwnerTextBox.Text);
        _config.EngineGitHubRepo = GitHubRepoNameNormalizer.Normalize(EngineGitHubRepoTextBox.Text);
        _config.IncludeFolders = SplitLines(IncludeFoldersTextBox.Text);
        _config.ServerExcludeMods = SplitLines(ServerExcludeModsTextBox.Text);
        _config.MaxBackupsToKeep = maxBackups;

        _config.ClientGuideShort = ClientGuideShortTextBox.Text;
        _config.ClientGuideFull = ClientGuideFullTextBox.Text;
        _config.AdminGuideShort = AdminGuideShortTextBox.Text;
        _config.AdminGuideFull = AdminGuideFullTextBox.Text;
        _config.ClientGuideManual = ClientGuideManualTextBox.Text;
        _config.AdminGuideManual = AdminGuideManualTextBox.Text;

        _config.Save();
        DialogResult = true;
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
