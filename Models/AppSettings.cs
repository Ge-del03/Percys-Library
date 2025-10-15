namespace ComicReader.Services
{
    public class AppSettings
    {
        public string Theme { get; set; } = "Dark";
        public bool ThumbnailsVisible { get; set; } = true;
        public bool IsReadingMode { get; set; } = false;
        public bool IsNightMode { get; set; } = false;
        public bool RememberLastSession { get; set; } = true;
        public bool AutoFitOnLoad { get; set; } = true;
        public bool EnableZoomPan { get; set; } = true;
        public bool ShowPageNumberOverlay { get; set; } = true;
        public bool ShowLoadingIndicators { get; set; } = true;
    // Eliminado: fullscreen estándar
        // Puedes agregar más propiedades según lo que uses en SettingsManager
    }
}
