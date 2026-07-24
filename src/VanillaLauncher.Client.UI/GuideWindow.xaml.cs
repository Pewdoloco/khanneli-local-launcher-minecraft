using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using VanillaLauncher.Client;
using VanillaLauncher.Client.UI.Localization;

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

        // Пока значение AppConfig.*Guide* — нетронутый дефолт движка (см. DefaultGuides.Resolve),
        // тело инструкции должно следовать за тумблером языка в MainWindow/AdminWindow, даже
        // если это окно уже открыто в момент переключения — иначе один и тот же нетронутый
        // дефолт показывал бы то английский chrome, то русское тело инструкции одновременно.
        Loc.Instance.PropertyChanged += Loc_PropertyChanged;
        Closed += (_, _) => Loc.Instance.PropertyChanged -= Loc_PropertyChanged;
    }

    private void Loc_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => UpdateText();

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
        var language = Loc.Instance.Language;

        Title = isAdmin ? Loc.Instance["Guide.TitleAdmin"] : Loc.Instance["Guide.TitleUser"];

        GuideTextBox.Text = (isAdmin, length) switch
        {
            (true, GuideLength.Full) => DefaultGuides.Resolve(_config.AdminGuideFull, DefaultGuides.AdminFull, DefaultGuides.AdminFullEn, language),
            (true, GuideLength.Manual) => DefaultGuides.Resolve(_config.AdminGuideManual, DefaultGuides.AdminManual, DefaultGuides.AdminManualEn, language),
            (true, GuideLength.Short) => DefaultGuides.Resolve(_config.AdminGuideShort, DefaultGuides.AdminShort, DefaultGuides.AdminShortEn, language),
            (false, GuideLength.Full) => DefaultGuides.Resolve(_config.ClientGuideFull, DefaultGuides.ClientFull, DefaultGuides.ClientFullEn, language),
            (false, GuideLength.Manual) => DefaultGuides.Resolve(_config.ClientGuideManual, DefaultGuides.ClientManual, DefaultGuides.ClientManualEn, language),
            (false, GuideLength.Short) => DefaultGuides.Resolve(_config.ClientGuideShort, DefaultGuides.ClientShort, DefaultGuides.ClientShortEn, language),
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
