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
        // Язык применяется до создания стартового окна (StartupUri), чтобы MainWindow сразу
        // отрисовалась на сохранённом языке, не мигая дефолтным RU на долю секунды. Best-effort:
        // AppConfig.Load() сама не бросает на пустом/отсутствующем файле (см. AppConfig.Load),
        // но на всякий случай не роняем запуск, если что-то пойдёт не так на этом самом раннем
        // этапе — язык просто останется дефолтным ("ru").
        try
        {
            Loc.Instance.Language = AppConfig.Load().Language;
        }
        catch { /* см. комментарий выше — язык останется дефолтным */ }

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

        MessageBox.Show(
            string.Format(Loc.Instance["App.UnhandledError.Message"], e.Exception.Message),
            Loc.Instance["App.UnhandledError.Title"],
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Dispose();
        base.OnExit(e);
    }
}

