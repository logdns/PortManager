using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PortManager.Models;
using PortManager.Services;

namespace PortManager.Views;

public sealed partial class ListRulesPage : Page
{
    public ObservableCollection<FirewallRule> Rules { get; } = new();
    private List<FirewallRule> _allRules = new();

    public ListRulesPage()
    {
        this.InitializeComponent();
        _ = LoadRulesAsync();
    }

    private async Task LoadRulesAsync()
    {
        LoadingRing.IsActive = true;
        Rules.Clear();

        _allRules = await FirewallService.ListRulesAsync();

        foreach (var r in _allRules)
            Rules.Add(r);

        LoadingRing.IsActive = false;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var keyword = SearchBox.Text.Trim().ToLower();
        Rules.Clear();

        var filtered = string.IsNullOrEmpty(keyword)
            ? _allRules
            : _allRules.Where(r =>
                r.Name.ToLower().Contains(keyword) ||
                r.LocalPort.Contains(keyword)).ToList();

        foreach (var r in filtered)
            Rules.Add(r);
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _ = LoadRulesAsync();
    }

    private void RulesList_ItemClick(object sender, ItemClickEventArgs e)
    {
        // 可扩展：点击规则跳转详情
    }
}
