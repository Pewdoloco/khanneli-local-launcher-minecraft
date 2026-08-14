using System.Windows;

namespace VanillaLauncher.Client.UI;

/// <summary>
/// Переключает тему (Theme.Light.xaml/Theme.Dark.xaml) во время выполнения — подменяет
/// соответствующую запись в Application.Resources.MergedDictionaries. Styles.xaml ссылается на
/// цветовые ключи через DynamicResource, поэтому подмена словаря мгновенно обновляет ВСЕ уже
/// открытые окна без явного перерисовывания (в отличие от Loc — там свой
/// INotifyPropertyChanged, здесь механизм WPF из коробки).
/// </summary>
public static class ThemeManager
{
    private const string LightSource = "Theme.Light.xaml";
    private const string DarkSource = "Theme.Dark.xaml";

    public static string Current { get; private set; } = "light";

    public static void Apply(string? theme)
    {
        var normalized = theme == "dark" ? "dark" : "light";
        Current = normalized;

        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var themeDict = dictionaries.FirstOrDefault(d =>
            d.Source is not null &&
            (d.Source.OriginalString.EndsWith(LightSource) || d.Source.OriginalString.EndsWith(DarkSource)));

        var newSource = new Uri(normalized == "dark" ? DarkSource : LightSource, UriKind.Relative);

        if (themeDict is not null)
        {
            var index = dictionaries.IndexOf(themeDict);
            dictionaries[index] = new ResourceDictionary { Source = newSource };
        }
        else
        {
            // На случай вызова до того, как App.xaml успел смержить свой словарь (не должно
            // происходить в обычном потоке запуска, но лучше не падать молча теряя тему).
            dictionaries.Insert(0, new ResourceDictionary { Source = newSource });
        }

        // Window.Background у уже открытых окон не подхватывает DynamicResource из implicit-
        // стиля (TargetType="Window" в Styles.xaml) при подмене словаря на лету — дочерние
        // элементы (кнопки, ListBox и т.д.) переоцениваются штатно через визуальное дерево, а
        // вот сам Background корневого окна почему-то остаётся от исходной темы. Явная
        // SetResourceReference на каждом открытом окне обходит это — создаёт живую ссылку на
        // ресурс на уровне конкретного инстанса, а не только через Style.
        foreach (Window window in Application.Current.Windows)
        {
            window.SetResourceReference(Window.BackgroundProperty, "WindowBackgroundBrush");
        }
    }
}
