using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PortManager.Views;

namespace PortManager;

public sealed partial class MainWindow : Window
{
    private bool _english;

    public MainWindow()
    {
        this.InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void LanguageButton_Click(object sender, RoutedEventArgs e)
    {
        _english = !_english;
        AppTitleBar.Title = _english ? "Port Manager" : "电脑端口管理";
        LanguageButton.Content = _english ? "English / 中文" : "中文 / English";

        var labels = _english
            ? new[] { "Dashboard", "Add port", "Port rules", "Delete rule", "Query port", "More features" }
            : new[] { "概览", "添加端口", "端口列表", "删除规则", "端口查询", "更多功能" };
        for (var i = 0; i < 5; i++)
            ((NavigationViewItem)NavView.MenuItems[i]).Content = labels[i];
        ((NavigationViewItem)NavView.MenuItems[5]).Content = labels[5];
        if (NavView.FooterMenuItems.Count > 0)
            ((NavigationViewItem)NavView.FooterMenuItems[0]).Content = _english ? "About" : "关于";
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item)
            return;

        var tag = item.Tag?.ToString();
        if (string.IsNullOrEmpty(tag))
            return;

        Type? pageType = tag switch
        {
            "Dashboard"   => typeof(DashboardPage),
            "AddPort"     => typeof(AddPortPage),
            "ListRules"   => typeof(ListRulesPage),
            "DeleteRule"  => typeof(DeleteRulePage),
            "PortStatus"  => typeof(PortStatusPage),
            "ComingSoon"  => typeof(ComingSoonPage),
            "About"       => typeof(AboutPage),
            _             => null
        };

        if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }
}
