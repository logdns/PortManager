using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PortManager.Services;
using PortManager.Views;
using Windows.Graphics;

namespace PortManager;

public sealed partial class MainWindow : Window
{
    private bool _isReady;
    private bool _allowClose;
    private bool _shutdownStarted;
    private AppWindow? _appWindow;
    private IntPtr _windowHandle;
    private string _currentTag = "Dashboard";

    public MainWindow()
    {
        InitializeComponent();
        ConfigureWindow();
        ApplyLanguage();
        Closed += MainWindow_Closed;
        _isReady = true;
    }

    public void NavigateTo(string tag)
    {
        var item = FindNavigationItem(tag);
        if (item is not null)
            NavView.SelectedItem = item;
        else
            NavigateFrame(tag);
    }

    private void ConfigureWindow()
    {
        Title = App.Text("App_Title");
        _windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(_windowHandle);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        _appWindow.Closing += AppWindow_Closing;
        _appWindow.Resize(new SizeInt32(1100, 720));

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Win-XinAi-De-Tools.ico");
        if (File.Exists(iconPath))
        {
            _appWindow.SetIcon(iconPath);
            try
            {
                TrayIconService.Initialize(_windowHandle, iconPath, RestoreFromTray, ExitFromTray);
            }
            catch (Exception ex)
            {
                App.LogStartup($"Tray icon initialization failed: {ex.Message}");
            }
        }

        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 760;
            presenter.PreferredMinimumHeight = 520;
            presenter.IsMinimizable = true;
            presenter.IsMaximizable = true;
            presenter.IsResizable = true;
        }
    }

    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        // Automated smoke tests send WM_CLOSE and must be able to terminate deterministically.
        if (_allowClose || string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase)) return;
        args.Cancel = true;
        var dialog = new ContentDialog
        {
            XamlRoot = NavView.XamlRoot,
            Title = App.Text("Window_CloseTitle"),
            Content = App.Text("Window_CloseContent"),
            PrimaryButtonText = App.Text("Window_Exit"),
            SecondaryButtonText = App.Text("Window_Minimize"),
            CloseButtonText = App.Text("Common_Cancel"),
            DefaultButton = ContentDialogButton.Close
        };
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Secondary)
        {
            MinimizeToTray();
        }
        else if (result == ContentDialogResult.Primary)
        {
            ExitApplication();
        }
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        App.LogStartup("Main window closed.");
        ExitApplication();
    }

    private void MinimizeToTray()
    {
        if (_windowHandle == IntPtr.Zero)
            return;

        ShowWindow(_windowHandle, SwHide);
        App.LogStartup("Main window hidden to the notification area.");
    }

    private void RestoreFromTray()
    {
        if (_windowHandle != IntPtr.Zero)
            ShowWindow(_windowHandle, SwShow);
        if (_appWindow?.Presenter is OverlappedPresenter presenter)
            presenter.Restore();
        _appWindow?.Show();
        Activate();
    }

    private void ExitFromTray()
    {
        ExitApplication();
    }

    private void ExitApplication()
    {
        if (_shutdownStarted)
            return;

        _shutdownStarted = true;
        _allowClose = true;
        App.LogStartup("Application shutdown requested.");
        if (WslService.ShutdownOnExit)
        {
            try { WslService.ShutdownAll(); App.LogStartup("WSL distributions terminated on exit."); }
            catch (Exception ex) { App.LogStartup($"WSL shutdown on exit failed: {ex.Message}"); }
        }
        TrayIconService.Dispose();
        Application.Current.Exit();
        ExitProcess(0);
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        if (NavView.SelectedItem is null)
            NavView.SelectedItem = DashboardItem;
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
            NavigateFrame(tag);
    }

    private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        if (_currentTag is "ConnectionMonitor" or "RuleTransfer" or "AuditLog" or "NetworkSettings" or "SmbSettings" or "WslDashboard")
            NavigateTo("ComingSoon");
        else if (_currentTag != "Dashboard")
            NavigateTo("Dashboard");
    }

    private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isReady)
            return;

        var language = LanguageSelector.SelectedIndex == 1
            ? AppLanguage.English
            : AppLanguage.Chinese;
        if (LanguageState.Current == language)
            return;

        App.SetLanguage(language);
        ApplyLanguage();
        NavigateFrame(_currentTag, force: true);
    }

    private void ApplyLanguage()
    {
        Title = App.Text("App_Title");
        PaneTitle.Text = App.Text("App_Title");
        DashboardItem.Content = App.Text("Nav_Dashboard");
        AddPortItem.Content = App.Text("Nav_AddPort");
        RulesItem.Content = App.Text("Nav_Rules");
        DeleteItem.Content = App.Text("Nav_Delete");
        QueryItem.Content = App.Text("Nav_Query");
        NetworkItem.Content = App.Text("Nav_Network");
        SmbItem.Content = App.Text("Nav_Smb");
        WslItem.Content = App.Text("Nav_Wsl");
        MoreItem.Content = App.Text("Nav_More");
        AboutItem.Content = App.Text("Nav_About");
        LanguageHeader.Text = App.Text("Language_Header");
        ChineseOption.Content = App.Text("Language_Chinese");
        EnglishOption.Content = App.Text("Language_English");
    }

    private void NavigateFrame(string tag, bool force = false)
    {
        var pageType = tag switch
        {
            "Dashboard" => typeof(DashboardPage),
            "AddPort" => typeof(AddPortPage),
            "ListRules" => typeof(ListRulesPage),
            "DeleteRule" => typeof(DeleteRulePage),
            "PortStatus" => typeof(PortStatusPage),
            "NetworkSettings" => typeof(NetworkSettingsPage),
            "SmbSettings" => typeof(SmbSettingsPage),
            "WslDashboard" => typeof(WslDashboardPage),
            "ComingSoon" => typeof(ComingSoonPage),
            "ConnectionMonitor" => typeof(ConnectionMonitorPage),
            "RuleTransfer" => typeof(RuleTransferPage),
            "AuditLog" => typeof(AuditLogPage),
            "About" => typeof(AboutPage),
            _ => null
        };

        if (pageType is null)
            return;

        _currentTag = tag;
        NavView.IsBackButtonVisible = tag is "ConnectionMonitor" or "RuleTransfer" or "AuditLog" or "NetworkSettings" or "SmbSettings" or "WslDashboard"
            ? NavigationViewBackButtonVisible.Visible
            : NavigationViewBackButtonVisible.Collapsed;
        NavView.IsBackEnabled = tag is "ConnectionMonitor" or "RuleTransfer" or "AuditLog" or "NetworkSettings" or "SmbSettings" or "WslDashboard";
        if ((force || ContentFrame.CurrentSourcePageType != pageType) && ContentFrame.Navigate(pageType))
            App.LogStartup($"Navigation completed: {pageType.Name}.");
    }

    private NavigationViewItem? FindNavigationItem(string tag)
    {
        foreach (var item in NavView.MenuItems.Concat(NavView.FooterMenuItems))
        {
            if (item is NavigationViewItem navigationItem && navigationItem.Tag as string == tag)
                return navigationItem;
        }

        return null;
    }

    private const int SwHide = 0;
    private const int SwShow = 5;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr window, int command);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern void ExitProcess(uint exitCode);
}
