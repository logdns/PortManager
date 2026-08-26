using Microsoft.UI.Xaml;
using PortManager.Services;
using System.Runtime.InteropServices;
using System.Text;

namespace PortManager;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private static ResourceDictionary? _languageDictionary;

    public App()
    {
        try
        {
            InitializeComponent();
            SetLanguage(AppLanguage.Chinese);
            UnhandledException += OnUnhandledException;
            LogStartup("Application object initialized.");
        }
        catch (Exception ex)
        {
            ReportStartupFailure("Application initialization failed", ex);
            throw;
        }
    }

    public static string Text(string key) => Current.Resources[key]?.ToString() ?? key;

    public static void NavigateTo(string tag) => ((App)Current)._mainWindow?.NavigateTo(tag);

    public static void SetLanguage(AppLanguage language)
    {
        LanguageState.Current = language;

        if (_languageDictionary is not null)
            Current.Resources.MergedDictionaries.Remove(_languageDictionary);

        var fileName = language == AppLanguage.English
            ? "Strings.en-US.xaml"
            : "Strings.zh-CN.xaml";
        _languageDictionary = new ResourceDictionary
        {
            Source = new Uri($"ms-appx:///Localization/{fileName}")
        };
        Current.Resources.MergedDictionaries.Add(_languageDictionary);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            LogStartup("OnLaunched started.");
            _mainWindow = new MainWindow();
            _mainWindow.Activate();
            LogStartup("MainWindow activated.");
        }
        catch (Exception ex)
        {
            ReportStartupFailure("Unable to open the main window", ex);
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs args)
    {
        args.Handled = true;
        ReportStartupFailure("Unexpected application error", args.Exception);
    }

    private static string LogPath
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PortManager", "startup.log");

    private static void LogStartup(string message)
    {
        try
        {
            var directory = Path.GetDirectoryName(LogPath)!;
            Directory.CreateDirectory(directory);
            File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
        }
        catch
        {
            // Logging must never prevent the application from starting.
        }
    }

    private static void ReportStartupFailure(string message, Exception exception)
    {
        var details = new StringBuilder()
            .AppendLine(message)
            .AppendLine()
            .AppendLine(exception.Message)
            .AppendLine()
            .Append("诊断日志: ").Append(LogPath)
            .ToString();
        LogStartup($"{message}: {exception}");

        try
        {
            MessageBoxW(IntPtr.Zero, details, "Port Manager", 0x10 | 0x1000);
        }
        catch
        {
            // The process may be failing before user32 is available.
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
