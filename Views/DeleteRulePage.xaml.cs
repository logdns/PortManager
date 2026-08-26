using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PortManager.Models;
using PortManager.Services;

namespace PortManager.Views;

public sealed partial class DeleteRulePage : Page
{
    public ObservableCollection<FirewallRule> Rules { get; } = new();

    public DeleteRulePage()
    {
        this.InitializeComponent();
        _ = LoadRulesAsync();
    }

    private async Task LoadRulesAsync()
    {
        Rules.Clear();
        var list = await FirewallService.ListRulesAsync();
        foreach (var r in list)
            Rules.Add(r);
        RulesList.ItemsSource = Rules;
        EmptyState.Visibility = Rules.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var ruleName = btn.Tag?.ToString();
        if (string.IsNullOrEmpty(ruleName)) return;

        var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "确认删除",
            Content = $"确定要删除规则 \"{ruleName}\" 吗？",
            PrimaryButtonText = "删除",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var ok = await FirewallService.DeleteRuleAsync(ruleName);
            await ShowToastAsync(ok ? $"规则 \"{ruleName}\" 已删除" : $"删除失败，请检查名称", ok);
            if (ok)
                await LoadRulesAsync();
        }
    }

    private async void DeleteManual_Click(object sender, RoutedEventArgs e)
    {
        var ruleName = ManualNameInput.Text.Trim();
        if (string.IsNullOrEmpty(ruleName))
        {
            await ShowToastAsync("请输入规则名称", false);
            return;
        }

        var ok = await FirewallService.DeleteRuleAsync(ruleName);
        await ShowToastAsync(ok ? $"规则 \"{ruleName}\" 已删除" : $"未找到规则 \"{ruleName}\"", ok);
        if (ok)
        {
            ManualNameInput.Text = "";
            await LoadRulesAsync();
        }
    }

    private async Task ShowToastAsync(string message, bool success)
    {
        var panel = new Border
        {
            Background = new SolidColorBrush(success
                ? Microsoft.UI.ColorHelper.FromArgb(255, 0x27, 0xAE, 0x60)
                : Microsoft.UI.ColorHelper.FromArgb(255, 0xE7, 0x4C, 0x3C)),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(16, 8, 16, 8),
            Child = new TextBlock { Text = message, Foreground = new SolidColorBrush(Microsoft.UI.Colors.White) }
        };
        // 简单提示：利用 ContentDialog
        var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Content = panel,
            CloseButtonText = "确定"
        };
        await dialog.ShowAsync();
    }
}
