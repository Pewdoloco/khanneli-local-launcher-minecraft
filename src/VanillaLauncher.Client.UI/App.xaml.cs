using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;

namespace VanillaLauncher.Client.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    // Друзья, которым раздаётся лаунчер, не должны видеть стектрейс — вместо краша
    // показываем понятное сообщение и продолжаем работу приложения, если это возможно.
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"Произошла непредвиденная ошибка:\n\n{e.Exception.Message}\n\nЛаунчер попробует продолжить работу.",
            "VanillaLauncher — ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }
}

