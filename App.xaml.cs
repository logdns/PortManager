using Microsoft.UI.Xaml;
using PortManager.Services;

namespace PortManager;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private static ResourceDictionary? _languageDictionary;

    public App()
    {
        this.InitializeComponent();
        SetLanguage(AppLanguage.Chinese);
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
        _mainWindow = new MainWindow();
        _mainWindow.Activate();
    }
}
