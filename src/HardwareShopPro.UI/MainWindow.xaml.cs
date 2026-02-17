using System.Windows;

namespace HardwareShopPro.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DateDisplay.Text = DateTime.Now.ToString("dddd, d MMM yyyy");
    }
}