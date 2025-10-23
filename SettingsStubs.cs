using System;
using System.ComponentModel;
using ComicReader.Services;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ComicReader
{
    // Stub mínimo para reemplazar el subsystem de configuración eliminado.
    // Provee propiedades usadas por otras partes del proyecto y métodos no-operativos para persistencia.
    public class UserAppSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void Raise([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // Propiedades comunes referenciadas en el código
        public int PageCacheLimit { get; set; } = 50;
        public int PrefetchWindow { get; set; } = 4;
        public int ConcurrencyCap { get; set; } = 3;

        public int PdfRenderWidth { get; set; } = 1600;
        public int PdfRenderHeight { get; set; } = 2200;
        public int PdfRenderDpi { get; set; } = 150;

        public bool EnableEagerPreload { get; set; } = false;
        public bool EnableEagerPreloadInMemory { get; set; } = false;
        public int EagerPreloadConcurrency { get; set; } = 3;
        public int EagerPreloadMemoryLimitPages { get; set; } = 20;
        public int EagerPreloadDiskMaxFiles { get; set; } = 500;

        public string LastOpenedFilePath { get; set; }
        public int LastOpenedPage { get; set; }
    // Ventana
    public double LastWindowWidth { get; set; } = 1000;
    public double LastWindowHeight { get; set; } = 700;
    public System.Windows.WindowState LastWindowState { get; set; } = System.Windows.WindowState.Normal;

    private string _theme = "ComicClassic";
    public string Theme { get => _theme; set { _theme = value; Raise(); } }
    private string _fontFamily = "Segoe UI";
    public string FontFamily { get => _fontFamily; set { _fontFamily = value; Raise(); } }
        private string _accentColor = "#FFAA00";
        public string AccentColor
        {
            get => _accentColor;
            set
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(value)) _accentColor = "#FFAA00";
                    else
                    {
                        var s = value.Trim();
                        if (!s.StartsWith("#")) s = "#" + s;
                        if (s.Length == 7) s = "#FF" + s.Substring(1);
                        _accentColor = s;
                    }
                }
                catch { _accentColor = "#FFAA00"; }
                Raise();
            }
        }
        public string RenderQuality { get; set; } = "Balanced";

    // Comic-style appearance options
    public string ComicBubbleStyle { get; set; } = "Rounded"; // Rounded | Square | Speech
        private double _halftoneIntensity = 0.25;
        public double HalftoneIntensity
        {
            get => _halftoneIntensity;
            set { _halftoneIntensity = Math.Max(0.0, Math.Min(1.0, value)); }
        } // 0..1

        private double _panelSpacing = 8.0;
        public double PanelSpacing { get => _panelSpacing; set { _panelSpacing = Math.Max(0, value); } } // padding between panels/cards

        public double CaptionFontSize { get; set; } = 14.0;
    public string InkEffect { get; set; } = "Outline"; // None | Outline | Sketch
    public bool UseDisplayFont { get; set; } = false; // use display (comic) font for headings
        
            // New appearance options
            private bool _showPanelOutlines = true;
            public bool ShowPanelOutlines { get => _showPanelOutlines; set { _showPanelOutlines = value; Raise(); } }
            private bool _enableHalftone = true;
            public bool EnableHalftone { get => _enableHalftone; set { _enableHalftone = value; Raise(); } }
            private string _themeAccentVariant = "Default";
            public string ThemeAccentVariant { get => _themeAccentVariant; set { _themeAccentVariant = value; Raise(); } } // Default | HighContrast | Muted

        // Developer / diagnostics toggles
        private bool _enablePerfLogs;
        public bool EnablePerfLogs
        {
            get => _enablePerfLogs;
            set { _enablePerfLogs = value; Raise(); }
        }

        private bool _enableDeveloperLogs;
        public bool EnableDeveloperLogs
        {
            get => _enableDeveloperLogs;
            set { _enableDeveloperLogs = value; Raise(); }
        }

    // Apariencia y ajustes de imagen
    public double Brightness { get; set; } = 1.0;
    public double Contrast { get; set; } = 1.0;

        public bool EnableContinuousScroll { get; set; } = false;
        public bool IsNightMode { get; set; } = false;
        public bool IsReadingMode { get; set; } = false;
        public bool ThumbnailsVisible { get; set; } = true;
        public string DefaultFitMode { get; set; } = "FitWidth";
    public ReadingDirection CurrentReadingDirection { get; set; } = ReadingDirection.LeftToRight;

    // UI mode toggles
    private bool _isRecentListView;
    public bool IsRecentListView
    {
        get => _isRecentListView;
        set { _isRecentListView = value; Raise(); }
    }

    // Misc
    public bool AutoEnterImmersiveOnOpen { get; set; } = false;
    public bool InvertScrollWheel { get; set; } = false;
    public bool HideOverlayOnlyInImmersive { get; set; } = false;
    public int HideCursorDelaySeconds { get; set; } = 3;
    public bool FadeOnFullscreenTransitions { get; set; } = true;
    public bool AutoAdvancePages { get; set; } = false;
    public int AutoAdvanceInterval { get; set; } = 5;

        public bool EnableZoomPan { get; set; } = true;
        public bool SmoothScrolling { get; set; } = true;
        public bool ShowPageNumberOverlay { get; set; } = true;
        public bool RememberLastSession { get; set; } = true;

        public double PageScrollStepRatio { get; set; } = 0.9;
        public bool SpacebarNextPage { get; set; } = true;

        // Otros ajustes conservadores
        public bool ShowLoadingIndicators { get; set; } = true;
        public bool AutoAdvanceLoop { get; set; } = false;
        public int HideOverlayDelaySeconds { get; set; } = 3;

        // Mutadores con notificación (si otras partes dependen de PropertyChanged)
        private int _exampleBacking;
        public int ExampleBacking
        {
            get => _exampleBacking;
            set { _exampleBacking = value; Raise(); }
        }
    }

    public static class SettingsManager
    {
        // Singleton settings instance
        public static UserAppSettings Settings { get; private set; } = new UserAppSettings();

        private static readonly string AppFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PercysLibrary");
        private static readonly string SettingsPath = System.IO.Path.Combine(AppFolder, "settings.json");

        public static void SaveSettings()
        {
            try
            {
                if (!System.IO.Directory.Exists(AppFolder)) System.IO.Directory.CreateDirectory(AppFolder);
                var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                var json = System.Text.Json.JsonSerializer.Serialize(Settings, options);
                System.IO.File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }

        public static void SaveNow()
        {
            SaveSettings();
        }

        public static void LoadSettings()
        {
            try
            {
                if (System.IO.File.Exists(SettingsPath))
                {
                    var json = System.IO.File.ReadAllText(SettingsPath);
                    var opts = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var obj = System.Text.Json.JsonSerializer.Deserialize<UserAppSettings>(json, opts);
                    if (obj != null) Settings = obj;
                }
            }
            catch { }
        }

        public static void ResetToDefaults()
        {
            Settings = new UserAppSettings();
            SaveSettings();
        }

        public static Task FlushPendingSavesAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
