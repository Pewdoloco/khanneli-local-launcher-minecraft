using System.Windows;

namespace VanillaLauncher.Client.UI;

/// <summary>
/// Встроенная инструкция — открывается кнопкой "Инструкция" в MainWindow (клиентский текст)
/// или AdminWindow (админский текст). Немодальная (Show(), не ShowDialog()) и без общего
/// владельца-блокировки: пока окно открыто, можно продолжать работать с остальным лаунчером —
/// это справочный материал, а не диалог, требующий решения перед продолжением.
/// </summary>
public partial class GuideWindow : Window
{
    private readonly string _shortText;
    private readonly string _fullText;

    public GuideWindow(string title, string shortText, string fullText)
    {
        InitializeComponent();
        Title = title;
        _shortText = shortText;
        _fullText = fullText;
        GuideTextBox.Text = _shortText;
    }

    private void ModeRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        GuideTextBox.Text = FullRadio.IsChecked == true ? _fullText : _shortText;
    }
}
