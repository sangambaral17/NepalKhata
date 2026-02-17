using System.Windows.Controls;
using System.Windows.Input;
using HardwareShopPro.UI.ViewModels;

namespace HardwareShopPro.UI.Views;

public partial class ProductListView : UserControl
{
    public ProductListView()
    {
        InitializeComponent();
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ProductListViewModel vm && vm.SelectedProduct != null)
        {
            vm.OpenEditDialogCommand.Execute(null);
        }
    }
}
