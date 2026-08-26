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
        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new SizeInt32(1100, 720));

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "PortManager.ico");
        if (File.Exists(iconPath))
            appWindow.SetIcon(iconPath);

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 760;
            presenter.PreferredMinimumHeight = 520;
            presenter.IsMinimizable = true;
            presenter.IsMaximizable = true;
            presenter.IsResizable = true;
        }
    }

    private static void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        App.LogStartup("Main window closed.");
        Application.Current.Exit();
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
            "ComingSoon" => typeof(ComingSoonPage),
            "About" => typeof(AboutPage),
            _ => null
        };

        if (pageType is null)
            return;

        _currentTag = tag;
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
}
