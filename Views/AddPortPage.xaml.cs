using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using PortManager.Services;

namespace PortManager.Views;

public sealed partial class AddPortPage : Page
{
    public AddPortPage()
    {
        this.InitializeComponent();
    }

    private void PortInput_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        PreviewPort.Text = string.IsNullOrWhiteSpace(PortInput.Text) ? "—" : PortInput.Text;

        var proto = ProtocolSelector.SelectedItem as RadioButton;
        PreviewProtocol.Text = proto?.Tag?.ToString() ?? "TCP";

        var dir = DirectionSelector.SelectedItem as RadioButton;
        PreviewDirection.Text = dir?.Tag?.ToString() switch
        {
            "in"   => "入站",
            "out"  => "出站",
            "Both" => "入站+出站",
            _      => "入站"
        };
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        // 验证端口
        var portStr = PortInput.Text.Trim();
        if (string.IsNullOrEmpty(portStr) || !int.TryParse(portStr, out var port) || port < 1 || port > 65535)
        {
            PortError.Text = "请输入有效的端口号 (1-65535)";
            PortError.Visibility = Visibility.Visible;
            return;
        }
        PortError.Visibility = Visibility.Collapsed;

        // 获取参数
        var protoRadio = ProtocolSelector.SelectedItem as RadioButton;
        var protocol = protoRadio?.Tag?.ToString() ?? "TCP";

        var dirRadio = DirectionSelector.SelectedItem as RadioButton;
        var direction = dirRadio?.Tag?.ToString() ?? "in";

        var ruleName = string.IsNullOrWhiteSpace(RuleNameInput.Text)
            ? $"Custom_Port_{port}_{protocol}"
            : RuleNameInput.Text.Trim();

        // 禁用按钮
        AddButton.IsEnabled = false;
        AddButton.Content = "正在添加... / Adding...";

        // 调用服务
        var result = await FirewallService.AddRuleAsync(port, protocol, direction, ruleName);

        // 恢复按钮
        AddButton.IsEnabled = true;
        AddButton.Content = "添加规则 / Add";

        // 显示结果
        ResultPanel.Visibility = Visibility.Visible;
        ResultTitle.Text = result.Success ? "添加成功" : "部分失败";
        ResultTitle.Foreground = new SolidColorBrush(result.Success
            ? Microsoft.UI.ColorHelper.FromArgb(255, 0x27, 0xAE, 0x60)
            : Microsoft.UI.ColorHelper.FromArgb(255, 0xE7, 0x4C, 0x3C));
        ResultDetail.Text = result.Message + $"\n规则名: {ruleName} | 端口: {port} | 协议: {protocol}";
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        PortInput.Text = "";
        ProtocolSelector.SelectedIndex = 0;
        DirectionSelector.SelectedIndex = 0;
        RuleNameInput.Text = "";
        PortError.Visibility = Visibility.Collapsed;
        ResultPanel.Visibility = Visibility.Collapsed;
        UpdatePreview();
    }
}
