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
        StartButton.IsEnabled = enabled; StopButton.IsEnabled = enabled; TerminateButton.IsEnabled = enabled; DefaultButton.IsEnabled = enabled; UnregisterButton.IsEnabled = enabled; TerminalButton.IsEnabled = enabled; ExplorerButton.IsEnabled = enabled; VsCodeButton.IsEnabled = enabled; DiskUsageButton.IsEnabled = enabled;
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
    private async void TerminateButton_Click(object sender, RoutedEventArgs e) => await RunActionAsync("WslTerminate", WslService.TerminateAsync, App.Text("Wsl_Terminated"));
    private async void DefaultButton_Click(object sender, RoutedEventArgs e) => await RunActionAsync("WslSetDefault", WslService.SetDefaultAsync, App.Text("Wsl_DefaultSet"));
    private async void UnregisterButton_Click(object sender, RoutedEventArgs e) => await RunActionAsync("WslUnregister", WslService.UnregisterAsync, App.Text("Wsl_Unregistered"));
    private void TerminalButton_Click(object sender, RoutedEventArgs e)
    {
        if (DistributionList.SelectedItem is not WslDistributionModel distro) return;
        try { WslService.OpenTerminal(distro.Name); AuditLogService.Record("OpenWslTerminal", $"Distribution={distro.Name}"); }
        catch (Exception ex) { ShowError(ex.Message); AuditLogService.Record("OpenWslTerminal", ex.Message, false); }
    }

    private void ExplorerButton_Click(object sender, RoutedEventArgs e) => RunShellAction("OpenExplorer", () => WslService.OpenExplorer(SelectedName()));
    private void VsCodeButton_Click(object sender, RoutedEventArgs e) => RunShellAction("OpenVsCode", () => WslService.OpenVsCode(SelectedName()));
    private async void DiskUsageButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TrySelected(out var name)) return;
        await RunShellActionAsync("DiskUsage", async () => DiskUsageText.Text = await WslService.GetDiskUsageAsync(name));
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TrySelected(out var name) || string.IsNullOrWhiteSpace(ArchivePathBox.Text)) return;
        await RunShellActionAsync("ExportWsl", () => WslService.ExportAsync(name, ArchivePathBox.Text.Trim()));
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ArchivePathBox.Text)) return;
        var name = string.IsNullOrWhiteSpace(TaskNameBox.Text) ? "ImportedDistro" : TaskNameBox.Text.Trim();
        var target = string.IsNullOrWhiteSpace(MovePathBox.Text) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WSL", name) : MovePathBox.Text.Trim();
        await RunShellActionAsync("ImportWsl", () => WslService.ImportAsync(name, target, ArchivePathBox.Text.Trim()));
        await RefreshAsync();
    }

    private async void MoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TrySelected(out var name) || string.IsNullOrWhiteSpace(MovePathBox.Text)) return;
        await RunShellActionAsync("MoveWsl", () => WslService.MoveAsync(name, MovePathBox.Text.Trim()));
    }

    private async void MountButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DiskPathBox.Text)) return;
        await RunShellActionAsync("MountWslVhd", () => WslService.MountVhdAsync(DiskPathBox.Text.Trim()));
    }

    private async void UnmountButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DiskPathBox.Text)) return;
        await RunShellActionAsync("UnmountWslVhd", () => WslService.UnmountVhdAsync(DiskPathBox.Text.Trim()));
    }

    private async void AutostartCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (!TrySelected(out var name)) return;
        await RunShellActionAsync("WslAutostart", () => WslService.SetAutostartAsync(name, AutostartCheckBox.IsChecked == true));
    }

    private void ShutdownOnExitCheckBox_Click(object sender, RoutedEventArgs e) => WslService.ShutdownOnExit = ShutdownOnExitCheckBox.IsChecked == true;

    private async void ScheduleButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TrySelected(out var name) || string.IsNullOrWhiteSpace(TaskNameBox.Text) || string.IsNullOrWhiteSpace(TaskCommandBox.Text) || string.IsNullOrWhiteSpace(TaskStartTimeBox.Text)) return;
        var schedule = (TaskScheduleSelector.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "DAILY";
        await RunShellActionAsync("ScheduleWslCommand", () => WslService.ScheduleCommandAsync(TaskNameBox.Text.Trim(), name, TaskCommandBox.Text.Trim(), schedule, TaskStartTimeBox.Text.Trim()));
    }

    private async void RemoveScheduleButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TaskNameBox.Text)) return;
        await RunShellActionAsync("RemoveWslSchedule", () => WslService.RemoveScheduledCommandAsync(TaskNameBox.Text.Trim()));
    }

    private async void ProxyButton_Click(object sender, RoutedEventArgs e) => await RunShellActionAsync("SetWslProxy", () => WslService.SetHttpProxyAsync(ProxyBox.Text.Trim()));
    private async void AddProxyButton_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(ListenPortBox.Text, out var listen) && int.TryParse(ConnectPortBox.Text, out var port) && !string.IsNullOrWhiteSpace(ConnectAddressBox.Text))
            await RunShellActionAsync("AddWslPortForward", () => WslService.AddPortProxyAsync(listen, ConnectAddressBox.Text.Trim(), port));
    }
    private async void RemoveProxyButton_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(ListenPortBox.Text, out var listen)) await RunShellActionAsync("RemoveWslPortForward", () => WslService.RemovePortProxyAsync(listen));
    }

    private async void UsbRefreshButton_Click(object sender, RoutedEventArgs e)
    {
        try { UsbList.ItemsSource = await WslService.ListUsbDevicesAsync(); } catch (Exception ex) { ShowError(ex.Message); }
    }
    private async void UsbBindButton_Click(object sender, RoutedEventArgs e) => await RunUsbActionAsync(WslService.BindUsbDeviceAsync);
    private async void UsbAttachButton_Click(object sender, RoutedEventArgs e) => await RunUsbActionAsync(id => WslService.AttachUsbDeviceAsync(id, SelectedName()));
    private async void UsbDetachButton_Click(object sender, RoutedEventArgs e) => await RunUsbActionAsync(WslService.DetachUsbDeviceAsync);

    private async Task RunUsbActionAsync(Func<string, Task> operation)
    {
        if (UsbList.SelectedItem is not string value) return;
        var id = value.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? value;
        await RunShellActionAsync("UsbDevice", () => operation(id));
    }

    private string SelectedName() => (DistributionList.SelectedItem as WslDistributionModel)?.Name ?? string.Empty;
    private bool TrySelected(out string name) { name = SelectedName(); return !string.IsNullOrWhiteSpace(name); }
    private void RunShellAction(string action, Action operation) { try { operation(); AuditLogService.Record(action, SelectedName()); } catch (Exception ex) { ShowError(ex.Message); AuditLogService.Record(action, ex.Message, false); } }
    private async Task RunShellActionAsync(string action, Func<Task> operation)
    {
        SetBusy(true);
        try { await operation(); AuditLogService.Record(action, SelectedName()); StatusText.Text = App.Text("Wsl_ActionComplete"); }
        catch (Exception ex) { ShowError(ex.Message); AuditLogService.Record(action, ex.Message, false); }
        finally { SetBusy(false); }
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
