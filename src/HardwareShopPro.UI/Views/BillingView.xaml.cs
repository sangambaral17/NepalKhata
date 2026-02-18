using System.Windows.Controls;
using HardwareShopPro.UI.ViewModels;

namespace HardwareShopPro.UI.Views;

public partial class BillingView : UserControl
{
    public BillingView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Auto-search as user types — triggers search after each keystroke for fast product lookup.
    /// </summary>
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext is BillingViewModel vm && sender is TextBox tb)
        {
            var query = tb.Text?.Trim() ?? string.Empty;
            if (query.Length >= 1)
            {
                vm.SearchProductsCommand.Execute(query);
            }
            else
            {
                vm.ClearSearchResults();
            }
        }
    }
}
