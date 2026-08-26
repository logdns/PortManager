using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PortManager.Services;

namespace PortManager.Views;

public sealed partial class DashboardPage : Page
{
    private bool _loaded;

    public DashboardPage() => InitializeComponent();

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (_loaded)
            return;

        _loaded = true;
        await LoadCountAsync();
    }

    private async Task LoadCountAsync()
    {
        LoadingRing.IsActive = true;
        ErrorBar.IsOpen = false;
        FirewallStatusText.Text = App.Text("Dashboard_Loading");

        try
        {
            RuleCount.Text = (await FirewallService.ListRulesAsync()).Count.ToString();
            FirewallStatusText.Text = App.Text("Dashboard_Healthy");
        }
        catch (FirewallOperationException ex)
        {
            RuleCount.Text = "--";
            ErrorBar.Message = $"{App.Text("Common_FirewallErrorDetail")}\n{ex.Message}";
            ErrorBar.IsOpen = true;
        }
        finally
        {
            LoadingRing.IsActive = false;
        }
    }

    private async void RetryButton_Click(object sender, RoutedEventArgs e) => await LoadCountAsync();
    private void AddPort_Click(object sender, RoutedEventArgs e) => App.NavigateTo("AddPort");
    private void ListRules_Click(object sender, RoutedEventArgs e) => App.NavigateTo("ListRules");
    private void PortStatus_Click(object sender, RoutedEventArgs e) => App.NavigateTo("PortStatus");
    private void MoreFeatures_Click(object sender, RoutedEventArgs e) => App.NavigateTo("ComingSoon");
}
