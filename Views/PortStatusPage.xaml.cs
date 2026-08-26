using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PortManager.Services;

namespace PortManager.Views;

public sealed partial class PortStatusPage : Page
{
    public PortStatusPage() => InitializeComponent();

    private void PortSearch_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
            SearchButton_Click(sender, e);
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        if (double.IsNaN(PortSearchInput.Value) ||
            PortSearchInput.Value < 1 || PortSearchInput.Value > 65535)
        {
            ShowStatus(InfoBarSeverity.Warning, App.Text("Add_InvalidPort"), string.Empty);
            PortSearchInput.Focus(FocusState.Programmatic);
            return;
        }

        var port = (int)PortSearchInput.Value;
        ResultsList.ItemsSource = null;
        EmptyState.Visibility = Visibility.Collapsed;
        StatusBar.IsOpen = false;
        SearchButton.IsEnabled = false;
        SearchButtonText.Text = App.Text("Query_Searching");

        try
        {
            var rules = await FirewallService.QueryPortAsync(port);
            ResultsList.ItemsSource = rules;

            if (rules.Count == 0)
            {
                EmptyText.Text = string.Format(App.Text("Query_EmptyFormat"), port);
                EmptyState.Visibility = Visibility.Visible;
            }
            else
            {
                ShowStatus(InfoBarSeverity.Informational,
                    string.Format(App.Text("Query_ResultFormat"), port, rules.Count), string.Empty);
            }
        }
        catch (FirewallOperationException ex)
        {
            ShowStatus(InfoBarSeverity.Error, App.Text("Common_FirewallError"),
                $"{App.Text("Common_FirewallErrorDetail")}\n{ex.Message}");
        }
        finally
        {
            SearchButton.IsEnabled = true;
            SearchButtonText.Text = App.Text("Query_Button");
        }
    }

    private void ShowStatus(InfoBarSeverity severity, string title, string message)
    {
        StatusBar.Severity = severity;
        StatusBar.Title = title;
        StatusBar.Message = message;
        StatusBar.IsOpen = true;
    }
}
