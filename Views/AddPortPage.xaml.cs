using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PortManager.Services;

namespace PortManager.Views;

public sealed partial class AddPortPage : Page
{
    public AddPortPage()
    {
        InitializeComponent();
        UpdatePreview();
    }

    private void Form_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) => UpdatePreview();
    private void Form_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdatePreview();

    private void UpdatePreview()
    {
        if (PreviewPort is null)
            return;

        PreviewPort.Text = double.IsNaN(PortInput.Value)
            ? "--"
            : ((int)PortInput.Value).ToString();
        PreviewProtocol.Text = (ProtocolSelector.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "TCP";
        PreviewDirection.Text = (DirectionSelector.SelectedItem as RadioButton)?.Tag?.ToString() switch
        {
            "out" => App.Text("Add_Outbound"),
            "Both" => App.Text("Add_Both"),
            _ => App.Text("Add_Inbound")
        };
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (double.IsNaN(PortInput.Value) || PortInput.Value < 1 || PortInput.Value > 65535)
        {
            ShowResult(InfoBarSeverity.Warning, App.Text("Add_InvalidPort"), string.Empty);
            PortInput.Focus(FocusState.Programmatic);
            return;
        }

        var port = (int)PortInput.Value;
        var protocol = (ProtocolSelector.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "TCP";
        var direction = (DirectionSelector.SelectedItem as RadioButton)?.Tag?.ToString() ?? "in";
        var ruleName = string.IsNullOrWhiteSpace(RuleNameInput.Text)
            ? $"PortManager_{port}_{protocol}"
            : RuleNameInput.Text.Trim();

        AddButton.IsEnabled = false;
        AddButtonText.Text = App.Text("Add_Adding");
        ResultBar.IsOpen = false;

        try
        {
            var result = await FirewallService.AddRuleAsync(port, protocol, direction, ruleName);
            var title = result.Success
                ? App.Text("Add_Success")
                : result.SuccessCount > 0 ? App.Text("Add_Partial") : App.Text("Add_Failed");
            var message = string.Format(App.Text("Add_ResultFormat"),
                result.SuccessCount, result.FailedCount, ruleName);
            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                message += $"\n{result.ErrorMessage}";
            ShowResult(result.Success ? InfoBarSeverity.Success : InfoBarSeverity.Error, title, message);
            AuditLogService.Record("AddRule", message, result.Success);
        }
        catch (FirewallOperationException ex)
        {
            AuditLogService.Record("AddRule", ex.Message, false);
            ShowResult(InfoBarSeverity.Error, App.Text("Common_FirewallError"),
                $"{App.Text("Common_FirewallErrorDetail")}\n{ex.Message}");
        }
        finally
        {
            AddButton.IsEnabled = true;
            AddButtonText.Text = App.Text("Add_Button");
        }
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        PortInput.Value = double.NaN;
        ProtocolSelector.SelectedIndex = 0;
        DirectionSelector.SelectedIndex = 0;
        RuleNameInput.Text = string.Empty;
        ResultBar.IsOpen = false;
        UpdatePreview();
    }

    private void ShowResult(InfoBarSeverity severity, string title, string message)
    {
        ResultBar.Severity = severity;
        ResultBar.Title = title;
        ResultBar.Message = message;
        ResultBar.IsOpen = true;
    }
}
