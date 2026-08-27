using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PortManager.Services;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace PortManager.Views;

public sealed partial class RuleTransferPage : Page
{
    private bool _loaded;
    public RuleTransferPage() => InitializeComponent();

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (_loaded) return;
        _loaded = true;
        await RefreshCountAsync();
    }

    private async Task RefreshCountAsync()
    {
        try
        {
            var rules = await FirewallService.ListRulesAsync();
            RuleCountText.Text = string.Format(App.Text("Transfer_RuleCountFormat"), rules.Count);
        }
        catch (Exception ex) { RuleCountText.Text = ex.Message; }
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true, App.Text("Transfer_Exporting"));
        try
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            InitializeWithWindow.Initialize(picker, WindowHandle());
            picker.SuggestedFileName = "PortManager-rules";
            picker.FileTypeChoices.Add("JSON", new List<string> { ".json" });
            var file = await picker.PickSaveFileAsync();
            if (file is null) return;
            var rules = await FirewallService.ListRulesAsync();
            await FileIO.WriteTextAsync(file, RuleTransferService.Serialize(rules));
            FileText.Text = string.Format(App.Text("Transfer_ExportSuccess"), file.Name, rules.Count);
            ShowStatus(InfoBarSeverity.Success, FileText.Text);
            AuditLogService.Record("ExportRules", $"Exported {rules.Count} rule(s).");
        }
        catch (Exception ex) { ShowStatus(InfoBarSeverity.Error, App.Text("Transfer_Error"), ex.Message); AuditLogService.Record("ExportRules", ex.Message, false); }
        finally { SetBusy(false); }
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            InitializeWithWindow.Initialize(picker, WindowHandle());
            picker.FileTypeFilter.Add(".json");
            var file = await picker.PickSingleFileAsync();
            if (file is null) return;
            SetBusy(true, App.Text("Transfer_Importing"));
            var document = RuleTransferService.Parse(await FileIO.ReadTextAsync(file));
            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = App.Text("Transfer_ConfirmImportTitle"),
                Content = string.Format(App.Text("Transfer_ConfirmImportFormat"), file.Name, document.Rules.Count),
                PrimaryButtonText = App.Text("Common_Confirm"),
                CloseButtonText = App.Text("Common_Cancel"),
                DefaultButton = ContentDialogButton.Primary
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
            var result = await RuleTransferService.ImportAsync(document.Rules);
            var message = string.Format(App.Text("Transfer_ImportSuccess"), result.SuccessCount, result.FailedCount);
            ShowStatus(result.Success ? InfoBarSeverity.Success : InfoBarSeverity.Warning, message, result.ErrorMessage);
            AuditLogService.Record("ImportRules", message, result.Success);
            await RefreshCountAsync();
        }
        catch (Exception ex) { ShowStatus(InfoBarSeverity.Error, App.Text("Transfer_Error"), ex.Message); AuditLogService.Record("ImportRules", ex.Message, false); }
        finally { SetBusy(false); }
    }

    private IntPtr WindowHandle() => WindowNative.GetWindowHandle((App.Current as App)!.MainWindow);
    private void SetBusy(bool busy, string? text = null) { ExportButton.IsEnabled = !busy; ImportButton.IsEnabled = !busy; LoadingRing.IsActive = busy; if (text is not null) FileText.Text = text; }
    private void ShowStatus(InfoBarSeverity severity, string title, string? message = null) { StatusBar.Severity = severity; StatusBar.Title = title; StatusBar.Message = message ?? string.Empty; StatusBar.IsOpen = true; }
}
