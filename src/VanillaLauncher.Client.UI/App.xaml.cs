using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;
using VanillaLauncher.Client;
using VanillaLauncher.Client.UI.Localization;

namespace VanillaLauncher.Client.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    // Один общий журнал на весь запуск приложения (MainWindow + AdminWindow пишут в один и тот
    // же файл — так события читаются одной хронологией, а не разбегаются по разным файлам).
    // Статическое поле, не ленивое свойство — должно существовать до конструктора MainWindow
    // (создаётся StartupUri сразу после OnStartup).
    public static readonly FileLogger Logger = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        // Язык и тема применяются до создания стартового окна (StartupUri), чтобы MainWindow
        // сразу отрисовалась на сохранённых значениях, не мигая дефолтными (RU/светлая) на долю
        // секунды. Best-effort: AppConfig.Load() сама не бросает на пустом/отсутствующем файле
        // (см. AppConfig.Load), но на всякий случай не роняем запуск, если что-то пойдёт не так
        // на этом самом раннем этапе — язык/тема просто останутся дефолтными.
        try
        {
            var config = AppConfig.Load();
            Loc.Instance.Language = config.Language;
            ThemeManager.Apply(config.Theme);
        }
        catch { /* см. комментарий выше — язык/тема останутся дефолтными */ }

        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    // Друзья, которым раздаётся лаунчер, не должны видеть стектрейс — вместо краша
    // показываем понятное сообщение и продолжаем работу приложения, если это возможно.
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Полный ToString() (стектрейс + вложенные InnerException), не только e.Exception.Message —
        // в UI-диалоге показываем короткое сообщение, но в файл пишем всё, что нужно для
        // разбора причины уже после закрытия приложения.
        Logger.Log($"НЕОБРАБОТАННОЕ ИСКЛЮЧЕНИЕ: {e.Exception}");

        // Явный владелец — то же самое, зачем он проставлен во всех остальных MessageBox.Show
        // по коду (AdminWindow/MainWindow): без него WPF не гарантированно центрирует диалог на
        // активном окне пользователя (Client или Admin), диалог может выскочить не там, где
        // сейчас находится внимание администратора/игрока. IsActive — на случай краша, когда
        // активного окна нет (например, между окнами) — тогда фоллбек на MainWindow, а если и
        // его почему-то ещё/уже нет — на оверлоад без владельца (центрируется на экране).
        var owner = Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? MainWindow;
        var message = string.Format(Loc.Instance["App.UnhandledError.Message"], e.Exception.Message);
        var title = Loc.Instance["App.UnhandledError.Title"];

        if (owner is not null)
            MessageBox.Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        else
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Dispose();
        base.OnExit(e);
    }
}

