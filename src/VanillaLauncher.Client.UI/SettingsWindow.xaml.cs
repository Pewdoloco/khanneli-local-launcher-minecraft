using System.Windows;
using System.Windows.Controls;
using VanillaLauncher.Client;

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
        ManifestUrlTextBox.Text = _config.ManifestUrl;
        GitHubOwnerTextBox.Text = _config.GitHubOwner ?? string.Empty;
        GitHubRepoTextBox.Text = _config.GitHubRepo ?? string.Empty;
        EngineGitHubOwnerTextBox.Text = _config.EngineGitHubOwner ?? string.Empty;
        EngineGitHubRepoTextBox.Text = _config.EngineGitHubRepo ?? string.Empty;
        IncludeFoldersTextBox.Text = JoinLines(_config.IncludeFolders);
        ServerExcludeModsTextBox.Text = JoinLines(_config.ServerExcludeMods);
        MaxBackupsToKeepTextBox.Text = _config.MaxBackupsToKeep.ToString();

        ClientGuideShortTextBox.Text = _config.ClientGuideShort;
        ClientGuideFullTextBox.Text = _config.ClientGuideFull;
        AdminGuideShortTextBox.Text = _config.AdminGuideShort;
        AdminGuideFullTextBox.Text = _config.AdminGuideFull;
    }

    private static string JoinLines(IEnumerable<string> values) => string.Join(Environment.NewLine, values);

    private static List<string> SplitLines(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    private void BrowseProfileRoot_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Выбери папку клиентской сборки" };
        if (dialog.ShowDialog(this) == true)
            ProfileRootTextBox.Text = dialog.FolderName;
    }

    private void BrowseServerDirectory_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Выбери (или создай) папку сервера" };
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
            ErrorText.Text = "Автопоиск клиентской папки не нашёл совпадений — укажи путь вручную (Обзор...).";
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
            ErrorText.Text = "Автопоиск серверной папки не нашёл совпадений — укажи путь вручную (Обзор...).";
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
            ErrorText.Text = "MaxBackupsToKeep должен быть целым числом ≥ 1 (нужно хранить хотя бы один бэкап).";
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
        _config.ManifestUrl = ManifestUrlTextBox.Text.Trim();
        _config.GitHubOwner = NullIfEmpty(GitHubOwnerTextBox.Text);
        _config.GitHubRepo = NullIfEmpty(GitHubRepoTextBox.Text);
        _config.EngineGitHubOwner = NullIfEmpty(EngineGitHubOwnerTextBox.Text);
        _config.EngineGitHubRepo = NullIfEmpty(EngineGitHubRepoTextBox.Text);
        _config.IncludeFolders = SplitLines(IncludeFoldersTextBox.Text);
        _config.ServerExcludeMods = SplitLines(ServerExcludeModsTextBox.Text);
        _config.MaxBackupsToKeep = maxBackups;

        _config.ClientGuideShort = ClientGuideShortTextBox.Text;
        _config.ClientGuideFull = ClientGuideFullTextBox.Text;
        _config.AdminGuideShort = AdminGuideShortTextBox.Text;
        _config.AdminGuideFull = AdminGuideFullTextBox.Text;

        _config.Save();
        DialogResult = true;
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
