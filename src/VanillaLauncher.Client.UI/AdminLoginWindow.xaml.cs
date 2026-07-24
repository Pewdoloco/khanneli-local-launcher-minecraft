using System.Windows;
using VanillaLauncher.Admin;
using VanillaLauncher.Client.UI.Localization;

namespace VanillaLauncher.Client.UI;

public partial class AdminLoginWindow : Window
{
    private readonly AdminAuthService _authService;

    public AdminLoginWindow(AdminAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
        Loaded += (_, _) => PasswordBox.Focus();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (_authService.VerifyPassword(PasswordBox.Password))
        {
            DialogResult = true;
            return;
        }

        ErrorText.Text = Loc.Instance["AdminLogin.WrongPassword"];
        ErrorText.Visibility = Visibility.Visible;
        PasswordBox.Clear();
        PasswordBox.Focus();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
