using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VanillaLauncher.Client.UI;

/// <summary>
/// Модальный picker для ServerExcludeMods: сканирует *.jar в выбранной папке (обычно
/// ProfileRoot\mods) и даёт отметить чекбоксами, какие не должны попадать на сервер, вместо
/// того чтобы вписывать имена файлов вручную. Модальный намеренно (в отличие от GuideWindow) —
/// это быстрый разовый выбор внутри уже открытого SettingsWindow, а не справочный материал,
/// который нужно держать открытым параллельно с остальной работой.
///
/// Список постранично (по <see cref="PageSize"/> строк, как список игр в библиотеке Steam) +
/// поиск по имени файла — модпаки легко содержат 70-100+ модов, один сплошной скроллящийся
/// список неудобно и просматривать, и находить в нём конкретный файл. Отмеченное состояние
/// хранится отдельно от того, что сейчас отрисовано на экране (<see cref="_checkedNames"/>),
/// иначе переключение страницы/поиска сбрасывало бы чекбоксы вне видимой страницы.
/// </summary>
public partial class SelectExcludedModsWindow : Window
{
    private const int PageSize = 10;

    private readonly HashSet<string> _checkedNames;
    private List<string> _allJarNames = new();
    private List<string> _filteredJarNames = new();
    private int _currentPage;

    /// <summary>Моды, у которых fabric.mod.json честно объявляет "environment": "client" —
    /// автоматически предотмечаются как исключения при сканировании (см. ScanFolder). Не
    /// ловит моды, которые лгут в метаданных (например, better_tab заявляет "*", хотя на
    /// деле клиентский) — те по-прежнему только вручную, см. AdminGuide.</summary>
    private HashSet<string> _autoDetectedClientOnly = new(StringComparer.OrdinalIgnoreCase);

    public List<string> SelectedExcludedMods { get; private set; } = new();

    /// <summary>Все .jar, найденные при последнем сканировании — не только отмеченные.
    /// Вызывающая сторона использует это, чтобы не потерять записи ServerExcludeMods,
    /// не относящиеся к просканированной папке (например, введённые вручную для другого
    /// набора модов).</summary>
    public List<string> ScannedFileNames { get; private set; } = new();

    public SelectExcludedModsWindow(string? defaultFolder, IEnumerable<string> currentlyExcluded)
    {
        InitializeComponent();
        _checkedNames = new HashSet<string>(currentlyExcluded, StringComparer.OrdinalIgnoreCase);

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
        ErrorText.Visibility = Visibility.Collapsed;
        SearchTextBox.Text = string.Empty;
        _allJarNames = new List<string>();

        if (!Directory.Exists(folder))
        {
            ErrorText.Text = "Папка не найдена.";
            ErrorText.Visibility = Visibility.Visible;
            ApplyFilterAndRender();
            return;
        }

        _allJarNames = Directory.EnumerateFiles(folder, "*.jar", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OfType<string>()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        ScannedFileNames = _allJarNames;

        _autoDetectedClientOnly = new HashSet<string>(
            _allJarNames.Where(name => FabricModEnvironmentReader.IsClientOnly(Path.Combine(folder, name))),
            StringComparer.OrdinalIgnoreCase);
        _checkedNames.UnionWith(_autoDetectedClientOnly);

        if (_allJarNames.Count == 0)
        {
            ErrorText.Text = "В папке нет .jar файлов.";
            ErrorText.Visibility = Visibility.Visible;
        }

        AutoDetectSummaryText.Text = _autoDetectedClientOnly.Count == 0
            ? "Моды с честным \"environment\": \"client\" в метаданных не найдены — не значит, что клиентских модов нет, часть из них не декларирует это поле вообще."
            : $"Автоматически отмечено как client-only по метаданным (\"environment\": \"client\"): {_autoDetectedClientOnly.Count}. " +
              "Часть модов заявляет универсальность (\"*\"), но на деле клиентская и падает на сервере — это НЕ определяется автоматически, проверяй запуском сервера.";

        ApplyFilterAndRender();
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e) => ApplyFilterAndRender();

    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            ApplyFilterAndRender();
    }

    private void ApplyFilterAndRender()
    {
        var query = SearchTextBox.Text.Trim();
        _filteredJarNames = string.IsNullOrEmpty(query)
            ? _allJarNames
            : _allJarNames.Where(name => name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        _currentPage = 0;
        RenderPage();
    }

    private void RenderPage()
    {
        ModsListBox.Items.Clear();

        var pageCount = Math.Max(1, (int)Math.Ceiling(_filteredJarNames.Count / (double)PageSize));
        _currentPage = Math.Clamp(_currentPage, 0, pageCount - 1);

        foreach (var name in _filteredJarNames.Skip(_currentPage * PageSize).Take(PageSize))
        {
            // CheckBox.Content рендерится через AccessText — одиночное "_" в имени файла
            // (обычное дело для fabric-модов, например better_tab-...) читалось бы как
            // маркер мнемоники и пропадало бы из отображаемого текста. "__" — стандартный
            // способ показать буквальный "_". _checkedNames/name ниже не трогаем — реальное
            // имя файла для ServerExcludeMods берётся из замыкания, а не из Content.
            var displayName = name.Replace("_", "__");
            if (_autoDetectedClientOnly.Contains(name))
                displayName += "  [авто: environment=client]";

            var checkBox = new CheckBox
            {
                Content = displayName,
                IsChecked = _checkedNames.Contains(name),
                Margin = new Thickness(2)
            };
            checkBox.Checked += (_, _) => _checkedNames.Add(name);
            checkBox.Unchecked += (_, _) => _checkedNames.Remove(name);
            ModsListBox.Items.Add(checkBox);
        }

        PageInfoText.Text = _filteredJarNames.Count == 0
            ? "Ничего не найдено"
            : $"Страница {_currentPage + 1} из {pageCount} ({_filteredJarNames.Count} файлов)";

        PrevPageButton.IsEnabled = _currentPage > 0;
        NextPageButton.IsEnabled = _currentPage < pageCount - 1;
    }

    private void PrevPageButton_Click(object sender, RoutedEventArgs e)
    {
        _currentPage--;
        RenderPage();
    }

    private void NextPageButton_Click(object sender, RoutedEventArgs e)
    {
        _currentPage++;
        RenderPage();
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        // _checkedNames — источник истины по всем страницам/поиску, а не только по тому,
        // что отрисовано на экране сейчас (см. класс-комментарий).
        SelectedExcludedMods = _checkedNames.Where(name => _allJarNames.Contains(name, StringComparer.OrdinalIgnoreCase)).ToList();
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
