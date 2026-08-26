using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PortManager.Models;
using PortManager.Services;

namespace PortManager.Views;

public sealed partial class PortStatusPage : Page
{
    public ObservableCollection<FirewallRule> Results { get; } = new();

    public PortStatusPage()
    {
        this.InitializeComponent();
    }

    private void PortSearch_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
            SearchButton_Click(sender, e);
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        var portStr = PortSearchInput.Text.Trim();
        if (!int.TryParse(portStr, out var port) || port < 1 || port > 65535)
        {
            await ShowMessageAsync("请输入有效的端口号 (1-65535)", false);
            return;
        }

        Results.Clear();
        SearchBtn.IsEnabled = false;
        SearchBtn.Content = "查询中... / Searching...";

        var rules = await FirewallService.QueryPortAsync(port);
        foreach (var r in rules)
            Results.Add(r);

        SearchBtn.IsEnabled = true;
        SearchBtn.Content = "查询 / Search";

    }

    private async Task ShowMessageAsync(string message, bool _)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "提示",
            Content = message,
            CloseButtonText = "确定"
        };
        await dialog.ShowAsync();
    }
}
