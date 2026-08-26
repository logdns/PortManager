using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PortManager.Services;

namespace PortManager.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardPage()
    {
        InitializeComponent();
        _ = LoadCountAsync();
    }

    private async Task LoadCountAsync() => RuleCount.Text = (await FirewallService.ListRulesAsync()).Count.ToString();
    private void AddPort_Click(object sender, RoutedEventArgs e) => Frame?.Navigate(typeof(AddPortPage));
    private void ListRules_Click(object sender, RoutedEventArgs e) => Frame?.Navigate(typeof(ListRulesPage));
    private void PortStatus_Click(object sender, RoutedEventArgs e) => Frame?.Navigate(typeof(PortStatusPage));
}
