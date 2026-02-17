using System.Windows;
using System.Windows.Input;
using HardwareShopPro.UI.ViewModels;

namespace HardwareShopPro.UI.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        // Pass password to ViewModel (PasswordBox doesn't support binding for security)
        if (DataContext is LoginViewModel vm)
            vm.Password = PasswordBox.Password;
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is LoginViewModel vm)
        {
            vm.Password = PasswordBox.Password;
            vm.LoginCommand.Execute(null);
        }
    }
}
