using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace VanillaLauncher.Client.UI;

/// <summary>
/// Модальный picker для ServerExcludeMods: сканирует *.jar в выбранной папке (обычно
/// ProfileRoot\mods) и даёт отметить чекбоксами, какие не должны попадать на сервер, вместо
/// того чтобы вписывать имена файлов вручную. Модальный намеренно (в отличие от GuideWindow) —
/// это быстрый разовый выбор внутри уже открытого SettingsWindow, а не справочный материал,
/// который нужно держать открытым параллельно с остальной работой.
/// </summary>
public partial class SelectExcludedModsWindow : Window
{
    private readonly HashSet<string> _initiallyExcluded;

    public List<string> SelectedExcludedMods { get; private set; } = new();

    /// <summary>Все .jar, найденные при последнем сканировании — не только отмеченные.
    /// Вызывающая сторона использует это, чтобы не потерять записи ServerExcludeMods,
    /// не относящиеся к просканированной папке (например, введённые вручную для другого
    /// набора модов).</summary>
    public List<string> ScannedFileNames { get; private set; } = new();

    public SelectExcludedModsWindow(string? defaultFolder, IEnumerable<string> currentlyExcluded)
    {
        InitializeComponent();
        _initiallyExcluded = new HashSet<string>(currentlyExcluded, StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(defaultFolder))
        {
            FolderTextBox.Text = defaultFolder;
            if (Directory.Exists(defaultFolder))
                ScanFolder(defaultFolder);
        }
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Выбери папку с модами" };
        if (dialog.ShowDialog(this) != true)
            return;

        FolderTextBox.Text = dialog.FolderName;
        ScanFolder(dialog.FolderName);
    }

    private void ScanFolder(string folder)
    {
        ModsListBox.Items.Clear();
        ScannedFileNames = new List<string>();
        ErrorText.Visibility = Visibility.Collapsed;

        if (!Directory.Exists(folder))
        {
            ErrorText.Text = "Папка не найдена.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        var jarNames = Directory.EnumerateFiles(folder, "*.jar", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OfType<string>()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        ScannedFileNames = jarNames;

        foreach (var name in jarNames)
        {
            ModsListBox.Items.Add(new CheckBox
            {
                Content = name,
                IsChecked = _initiallyExcluded.Contains(name),
                Margin = new Thickness(2)
            });
        }

        if (jarNames.Count == 0)
        {
            ErrorText.Text = "В папке нет .jar файлов.";
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedExcludedMods = ModsListBox.Items
            .OfType<CheckBox>()
            .Where(cb => cb.IsChecked == true)
            .Select(cb => (string)cb.Content)
            .ToList();
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
