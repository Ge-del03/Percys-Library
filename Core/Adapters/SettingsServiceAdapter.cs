using ComicReader.Core.Abstractions;
using ComicReader.Services;

namespace ComicReader.Core.Adapters
{
    public class SettingsServiceAdapter : ISettingsService
    {
        public string Theme => SettingsManager.Settings.Theme;
        public void Save() => SettingsManager.SaveSettings();
    }
}
