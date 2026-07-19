using System.Windows;
using VanillaLauncher.Admin;

namespace VanillaLauncher.Client.UI;

public partial class SetAdminPasswordWindow : Window
{
    private readonly AdminAuthService _authService;

    public SetAdminPasswordWindow(AdminAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
        Loaded += (_, _) => PasswordBox1.Focus();
    }

    private void SetButton_Click(object sender, RoutedEventArgs e)
    {
        var password = PasswordBox1.Password;
        var confirm = PasswordBox2.Password;

        if (string.IsNullOrEmpty(password))
        {
            ShowError("Пароль не может быть пустым.");
            return;
        }

        if (password != confirm)
        {
            ShowError("Пароли не совпадают.");
            return;
        }

        _authService.SetPassword(password);
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
