using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PortManager.Models;
using PortManager.Services;

namespace PortManager.Views;

public sealed partial class WslDashboardPage : Page
{
    private bool _loaded;
    private bool _busy;
    private bool _wslInstalled;

    public WslDashboardPage() => InitializeComponent();

    private async void Page_Loaded(object sender, RoutedEventArgs e) { if (_loaded) return; _loaded = true; await RefreshAsync(); }
    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        SetBusy(true);
        try
        {
            var selectedName = (DistributionList.SelectedItem as WslDistributionModel)?.Name;
            var status = await WslService.GetStatusAsync();
            _wslInstalled = status.IsInstalled;
            var rows = status.Distributions.ToList();
            DistributionList.ItemsSource = rows;
            var selected = rows.FirstOrDefault(row => row.Name == selectedName) ?? rows.FirstOrDefault();
            DistributionList.SelectedItem = selected;
            EmptyText.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ErrorBar.IsOpen = false;
            UpdateSetupPanel(status);
        }
        catch (Exception ex)
        {
            _wslInstalled = false;
            SetupPanel.Visibility = Visibility.Collapsed;
            ShowError(ex.Message);
            AuditLogService.Record("ListWslDistributions", ex.Message, false);
        }
        finally { SetBusy(false); }
    }

    private void UpdateSetupPanel(WslStatusModel status)
    {
        var needsInstall = !status.IsInstalled;
        var needsDistribution = status.IsInstalled && !status.HasDistributions;
        SetupPanel.Visibility = needsInstall || needsDistribution ? Visibility.Visible : Visibility.Collapsed;
        InstallButton.Visibility = needsInstall ? Visibility.Visible : Visibility.Collapsed;
        InstallDistributionButton.Visibility = needsDistribution ? Visibility.Visible : Visibility.Collapsed;
        HelpButton.Visibility = needsInstall ? Visibility.Visible : Visibility.Collapsed;
        SetupTitle.Text = App.Text(needsInstall ? "Wsl_NotInstalledTitle" : "Wsl_NoDistributionTitle");
        SetupMessage.Text = App.Text(needsInstall ? "Wsl_NotInstalledMessage" : "Wsl_NoDistributionMessage");
        StatusText.Text = App.Text(needsInstall ? "Wsl_StatusNotInstalled" : needsDistribution ? "Wsl_StatusNoDistribution" : "Wsl_StatusReady");
    }

    private void DistributionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var distro = DistributionList.SelectedItem as WslDistributionModel;
        SelectedText.Text = distro is null ? App.Text("Wsl_Select") : string.Format(App.Text("Wsl_SelectedFormat"), distro.Name, distro.State, distro.Version);
        var enabled = distro is not null && _wslInstalled && !_busy;
        StartButton.IsEnabled = enabled; StopButton.IsEnabled = enabled; DefaultButton.IsEnabled = enabled; TerminalButton.IsEnabled = enabled;
    }

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await WslService.InstallAsync();
            StatusText.Text = App.Text("Wsl_InstallStarted");
            AuditLogService.Record("InstallWsl", "wsl.exe --install");
        }
        catch (Exception ex) { ShowError(ex.Message); AuditLogService.Record("InstallWsl", ex.Message, false); }
    }

    private async void InstallDistributionButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await WslService.InstallDistributionAsync();
            StatusText.Text = App.Text("Wsl_InstallStarted");
            AuditLogService.Record("InstallWslDistribution", "Ubuntu");
        }
        catch (Exception ex) { ShowError(ex.Message); AuditLogService.Record("InstallWslDistribution", ex.Message, false); }
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            WslService.OpenInstallHelp();
            AuditLogService.Record("OpenWslInstallHelp", "https://aka.ms/wslinstall");
        }
        catch (Exception ex) { ShowError(ex.Message); AuditLogService.Record("OpenWslInstallHelp", ex.Message, false); }
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e) => await RunActionAsync("WslStart", WslService.StartAsync, App.Text("Wsl_Started"));
    private async void StopButton_Click(object sender, RoutedEventArgs e) => await RunActionAsync("WslStop", WslService.StopAsync, App.Text("Wsl_Stopped"));
    private async void DefaultButton_Click(object sender, RoutedEventArgs e) => await RunActionAsync("WslSetDefault", WslService.SetDefaultAsync, App.Text("Wsl_DefaultSet"));
    private void TerminalButton_Click(object sender, RoutedEventArgs e)
    {
        if (DistributionList.SelectedItem is not WslDistributionModel distro) return;
        try { WslService.OpenTerminal(distro.Name); AuditLogService.Record("OpenWslTerminal", $"Distribution={distro.Name}"); }
        catch (Exception ex) { ShowError(ex.Message); AuditLogService.Record("OpenWslTerminal", ex.Message, false); }
    }

    private async Task RunActionAsync(string action, Func<string, Task> operation, string success)
    {
        if (DistributionList.SelectedItem is not WslDistributionModel distro) return;
        SetBusy(true);
        try { await operation(distro.Name); AuditLogService.Record(action, $"Distribution={distro.Name}"); await RefreshAsync(); SelectedText.Text = success; }
        catch (Exception ex) { ShowError(ex.Message); AuditLogService.Record(action, ex.Message, false); }
        finally { SetBusy(false); }
    }

    private void SetBusy(bool busy) { _busy = busy; LoadingRing.IsActive = busy; RefreshButton.IsEnabled = !busy; DistributionList.IsEnabled = !busy; DistributionList_SelectionChanged(DistributionList, null!); }
    private void ShowError(string message) { ErrorBar.Title = App.Text("Wsl_Error"); ErrorBar.Message = message; ErrorBar.IsOpen = true; }
}
