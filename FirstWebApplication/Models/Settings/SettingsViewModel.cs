namespace FirstWebApplication.Models.Settings
{
    public class SettingsViewModel
    {
        public string CurrentLanguage { get; set; } = "nb-NO";
        public string CurrentTheme { get; set; } = "light";

        // Liste over tilgjengelige språk for dropdown
        public Dictionary<string, string> AvailableLanguages { get; } = new()
        {
            { "nb-NO", "Norsk (Bokmål)" },
            { "en-US", "English" }
        };
    }
}