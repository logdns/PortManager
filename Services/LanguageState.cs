namespace PortManager.Services;

public enum AppLanguage
{
    Chinese,
    English
}

public static class LanguageState
{
    public static AppLanguage Current { get; set; } = AppLanguage.Chinese;

    public static bool IsEnglish => Current == AppLanguage.English;
}
