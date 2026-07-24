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
        MessageBox.Show(
            string.Format(Loc.Instance["App.UnhandledError.Message"], e.Exception.Message),
            Loc.Instance["App.UnhandledError.Title"],
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }
}

