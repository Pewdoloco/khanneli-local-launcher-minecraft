using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VanillaLauncher.Client.UI;

/// <summary>
/// Присоединённое свойство "серый пример-подсказка внутри пустого TextBox" — единый
/// механизм вместо копипасты VisualBrush-стилей на каждое поле. Работает и статически из
/// XAML (Watermark.Hint="Pewdoloco"), и динамически из code-behind (Watermark.SetHint(box,
/// значение_с_сервера)) — второе нужно для VersionTextBox в AdminWindow, где подсказка
/// известна только после запроса к GitHub API, а не заранее на этапе разметки.
///
/// Реализовано в code-behind, а не через XAML DataTrigger + RelativeSource-биндинг внутри
/// VisualBrush.Visual — у VisualBrush отдельное, не связанное с основным деревом, визуальное
/// дерево, и RelativeSource/FindAncestor через его границу ненадёжен.
/// </summary>
public static class Watermark
{
    public static readonly DependencyProperty HintProperty =
        DependencyProperty.RegisterAttached("Hint", typeof(string), typeof(Watermark),
            new PropertyMetadata(null, OnHintChanged));

    public static void SetHint(DependencyObject element, string? value) => element.SetValue(HintProperty, value);
    public static string? GetHint(DependencyObject element) => (string?)element.GetValue(HintProperty);

    private static void OnHintChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox box)
            return;

        box.TextChanged -= Refresh;
        box.TextChanged += Refresh;
        ApplyOrClear(box);

        void Refresh(object sender, TextChangedEventArgs args) => ApplyOrClear(box);
    }

    private static void ApplyOrClear(TextBox box)
    {
        var hint = GetHint(box);

        if (string.IsNullOrEmpty(box.Text) && !string.IsNullOrEmpty(hint))
        {
            var text = new TextBlock
            {
                Text = hint,
                Foreground = Brushes.Gray,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(4, 0, 0, 0)
            };
            box.Background = new VisualBrush(text)
            {
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Center,
                Stretch = Stretch.None
            };
        }
        else
        {
            box.ClearValue(Control.BackgroundProperty);
        }
    }
}
