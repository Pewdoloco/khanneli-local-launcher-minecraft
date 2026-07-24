using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using VanillaLauncher.Client;

namespace VanillaLauncher.Client.UI;

public enum GuideRole
{
    Client,
    Admin
}

internal enum GuideLength
{
    Short,
    Full,
    Manual
}

/// <summary>
/// Встроенная инструкция — один и тот же переключаемый экран для клиента и админа
/// (роль + краткое/полное), открывается кнопкой "Инструкция" и из MainWindow, и из
/// AdminWindow с разной ролью по умолчанию. Немодальная (Show(), не ShowDialog()):
/// пока окно открыто, можно продолжать работать с остальным лаунчером.
/// </summary>
public partial class GuideWindow : Window
{
    private readonly AppConfig _config;

    public GuideWindow(AppConfig config, GuideRole defaultRole)
    {
        InitializeComponent();
        _config = config;

        if (defaultRole == GuideRole.Admin)
        {
            AdminRoleRadio.IsChecked = true;
        }
        else
        {
            ClientRoleRadio.IsChecked = true;
            AdminRoleRadio.Visibility = Visibility.Collapsed;
        }

        UpdateText();
    }

    private void RoleOrLength_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        UpdateText();
    }

    private void UpdateText()
    {
        var isAdmin = AdminRoleRadio.IsChecked == true;
        var length = ManualRadio.IsChecked == true ? GuideLength.Manual
            : FullRadio.IsChecked == true ? GuideLength.Full
            : GuideLength.Short;

        Title = isAdmin ? "Инструкция — Администратор" : "Инструкция — Пользователь";

        GuideTextBox.Text = (isAdmin, length) switch
        {
            (true, GuideLength.Full) => _config.AdminGuideFull,
            (true, GuideLength.Manual) => _config.AdminGuideManual,
            (true, GuideLength.Short) => _config.AdminGuideShort,
            (false, GuideLength.Full) => _config.ClientGuideFull,
            (false, GuideLength.Manual) => _config.ClientGuideManual,
            (false, GuideLength.Short) => _config.ClientGuideShort,
            _ => _config.ClientGuideShort,
        };
    }

    // --- Защита от "чужой" модальности ---
    //
    // WPF Window.ShowDialog() отключает (EnableWindow(false)) ВСЕ окна процесса, а не
    // только своего Owner — штатное поведение платформы, не баг конкретного окна. Значит
    // любой модальный диалог где угодно в лаунчере (SettingsWindow, SelectExcludedModsWindow,
    // AdminLoginWindow и т.д.) на время своего показа делает "Инструкцию" неинтерактивной,
    // хотя по смыслу это просто справочное окно и должно оставаться читаемым/прокручиваемым
    // всегда. Единственный надёжный способ противостоять этому — перехватить WM_ENABLE на
    // уровне Win32 и немедленно возвращать окну enabled-состояние обратно.
    private const int WmEnable = 0x000A;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        if (PresentationSource.FromVisual(this) is HwndSource source)
            source.AddHook(WndProc);
    }

    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmEnable && wParam == IntPtr.Zero)
        {
            NativeMethods.EnableWindow(hwnd, true);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool EnableWindow(IntPtr hWnd, bool bEnable);
    }
}
