using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;

namespace HardwareShopPro.UI.ViewModels;

public partial class HelpViewModel : ViewModelBase
{
    [RelayCommand]
    private void OpenLink(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch { }
    }
}

public record ShortcutItem(string Action, string Key);
