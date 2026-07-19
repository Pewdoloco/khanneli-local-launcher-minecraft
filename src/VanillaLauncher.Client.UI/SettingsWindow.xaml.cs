using System.Windows;
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
        IncludeFoldersTextBox.Text = JoinLines(_config.IncludeFolders);
        ServerExcludeModsTextBox.Text = JoinLines(_config.ServerExcludeMods);
        MaxBackupsToKeepTextBox.Text = _config.MaxBackupsToKeep.ToString();
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

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(MaxBackupsToKeepTextBox.Text, out var maxBackups) || maxBackups < 0)
        {
            ErrorText.Text = "MaxBackupsToKeep должен быть целым числом ≥ 0.";
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
        _config.IncludeFolders = SplitLines(IncludeFoldersTextBox.Text);
        _config.ServerExcludeMods = SplitLines(ServerExcludeModsTextBox.Text);
        _config.MaxBackupsToKeep = maxBackups;

        _config.Save();
        DialogResult = true;
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
