using System.Windows;
using VanillaLauncher.Client;

namespace VanillaLauncher.Client.UI;

/// <summary>
/// Лёгкая, НЕ спрятанная за Admin-паролем настройка для игрока — заполняет только то, что
/// нужно клиентской части (GitHubOwner/GitHubRepo, из которых сама выводится ManifestUrl по
/// стандартной формуле, и опционально ClientFolderName). Существует, чтобы админу не нужно
/// было пересобирать и заново рассылать exe+appsettings.json каждому другу — значения здесь
/// не секретные (публичные координаты репозитория, не токен), их можно просто продиктовать
/// в чате. Полная "Настройки" (пароль, ServerDirectory, ServerExcludeMods, публикация и т.д.)
/// остаются исключительно в Admin-режиме — это окно их не трогает и не заменяет.
/// </summary>
public partial class ClientSetupWindow : Window
{
    private readonly AppConfig _config;

    public ClientSetupWindow(AppConfig config)
    {
        InitializeComponent();
        _config = config;

        GitHubOwnerTextBox.Text = _config.GitHubOwner ?? string.Empty;
        GitHubRepoTextBox.Text = _config.GitHubRepo ?? string.Empty;
        ClientFolderNameTextBox.Text = _config.ClientFolderName ?? string.Empty;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var owner = GitHubRepoNameNormalizer.NormalizeOwner(GitHubOwnerTextBox.Text);
        var repo = GitHubRepoNameNormalizer.Normalize(GitHubRepoTextBox.Text);

        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
        {
            ShowError("GitHubOwner и GitHubRepo обязательны — уточни у администратора сервера, если не знаешь их.");
            return;
        }

        _config.GitHubOwner = owner;
        _config.GitHubRepo = repo;
        _config.ClientFolderName = string.IsNullOrWhiteSpace(ClientFolderNameTextBox.Text)
            ? null
            : ClientFolderNameTextBox.Text.Trim();

        // Формула из ClientGuide/AdminGuide — GitHub сам перенаправляет /latest/ на самый
        // свежий релиз при каждом запросе, отдельно вычислять/обновлять её не нужно.
        _config.ManifestUrl = $"https://github.com/{owner}/{repo}/releases/latest/download/manifest.json";

        _config.Save();
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
