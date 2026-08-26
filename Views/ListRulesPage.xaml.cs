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
    private bool _loaded;

    public ListRulesPage() => InitializeComponent();

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (_loaded)
            return;

        _loaded = true;
        await LoadRulesAsync();
    }

    private async Task LoadRulesAsync()
    {
        LoadingRing.IsActive = true;
        ErrorBar.IsOpen = false;

        try
        {
            _allRules = await FirewallService.ListRulesAsync();
            ApplyFilter();
        }
        catch (FirewallOperationException ex)
        {
            _allRules.Clear();
            Rules.Clear();
            EmptyState.Visibility = Visibility.Collapsed;
            ErrorBar.Message = $"{App.Text("Common_FirewallErrorDetail")}\n{ex.Message}";
            ErrorBar.IsOpen = true;
            UpdateCount();
        }
        finally
        {
            LoadingRing.IsActive = false;
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadRulesAsync();

    private void ApplyFilter()
    {
        var keyword = SearchBox.Text.Trim();
        var filtered = string.IsNullOrEmpty(keyword)
            ? _allRules
            : _allRules.Where(rule =>
                rule.Name.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                rule.PortDisplay.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();

        Rules.Clear();
        foreach (var rule in filtered)
            Rules.Add(rule);

        EmptyState.Visibility = Rules.Count == 0 && !ErrorBar.IsOpen
            ? Visibility.Visible
            : Visibility.Collapsed;
        UpdateCount();
    }

    private void UpdateCount() =>
        RuleCountText.Text = string.Format(App.Text("Rules_CountFormat"), Rules.Count);
}
