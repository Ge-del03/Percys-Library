// FileName: /Services/SettingsManager.cs
using System;
using System.IO;
using System.Xml.Serialization;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using ComicReader.Models;
using System.Collections.Generic; // Para List<T>
using ComicReader.Core.Abstractions; // Para LogLevel

namespace ComicReader.Services
{
    public class UserAppSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    // Eliminado: historial antiguo reemplazado por ContinueReadingService
    [System.Xml.Serialization.XmlIgnore]
    public BindingList<Bookmark> Bookmarks { get; set; } = new BindingList<Bookmark>(); // Nuevo: Marcadores

        private double _lastZoom = 1.0;
        public double LastZoom { get => _lastZoom; set { _lastZoom = value; OnPropertyChanged(nameof(LastZoom)); } }

        private double _lastRotationAngle = 0;
        public double LastRotationAngle { get => _lastRotationAngle; set { _lastRotationAngle = value; OnPropertyChanged(nameof(LastRotationAngle)); } }

        private bool _thumbnailsVisible = true;
        public bool ThumbnailsVisible { get => _thumbnailsVisible; set { _thumbnailsVisible = value; OnPropertyChanged(nameof(ThumbnailsVisible)); } }

        private string _lastOpenedFilePath = string.Empty;
        public string LastOpenedFilePath { get => _lastOpenedFilePath; set { _lastOpenedFilePath = value; OnPropertyChanged(nameof(LastOpenedFilePath)); } }

        private int _lastOpenedPage = 0;
        public int LastOpenedPage { get => _lastOpenedPage; set { _lastOpenedPage = value; OnPropertyChanged(nameof(LastOpenedPage)); } }

        private WindowState _lastWindowState = WindowState.Normal;
        public WindowState LastWindowState { get => _lastWindowState; set { _lastWindowState = value; OnPropertyChanged(nameof(LastWindowState)); } }

        private double _lastWindowWidth = 1024;
        public double LastWindowWidth { get => _lastWindowWidth; set { _lastWindowWidth = value; OnPropertyChanged(nameof(LastWindowWidth)); } }

        private double _lastWindowHeight = 768;
        public double LastWindowHeight { get => _lastWindowHeight; set { _lastWindowHeight = value; OnPropertyChanged(nameof(LastWindowHeight)); } }

        private ViewMode _currentViewMode = ViewMode.SinglePage;
        public ViewMode CurrentViewMode { get => _currentViewMode; set { _currentViewMode = value; OnPropertyChanged(nameof(CurrentViewMode)); } }

        private ReadingDirection _currentReadingDirection = ReadingDirection.LeftToRight;
        public ReadingDirection CurrentReadingDirection { get => _currentReadingDirection; set { _currentReadingDirection = value; OnPropertyChanged(nameof(CurrentReadingDirection)); } }

        private bool _autoFitOnLoad = true;
        public bool AutoFitOnLoad { get => _autoFitOnLoad; set { _autoFitOnLoad = value; OnPropertyChanged(nameof(AutoFitOnLoad)); } }

    // Ajuste por defecto al abrir: "Width", "Height" o "Screen"
    private string _defaultFitMode = "Width";
    public string DefaultFitMode { get => _defaultFitMode; set { _defaultFitMode = value; OnPropertyChanged(nameof(DefaultFitMode)); } }

    // Eliminado: ocultar UI en pantalla completa estándar

        private bool _smoothScrolling = true;
        public bool SmoothScrolling { get => _smoothScrolling; set { _smoothScrolling = value; OnPropertyChanged(nameof(SmoothScrolling)); } }

        private int _preloadCacheSize = 5;
        public int PreloadCacheSize { get => _preloadCacheSize; set { _preloadCacheSize = value; OnPropertyChanged(nameof(PreloadCacheSize)); } }

        private string _theme = "Dark";
        public string Theme { get => _theme; set { _theme = value; OnPropertyChanged(nameof(Theme)); } }

        private bool _showPageNumberOverlay = true;
        public bool ShowPageNumberOverlay { get => _showPageNumberOverlay; set { _showPageNumberOverlay = value; OnPropertyChanged(nameof(ShowPageNumberOverlay)); } }

        private int _pdfRenderDpi = 150;
        public int PdfRenderDpi { get => _pdfRenderDpi; set { _pdfRenderDpi = value; OnPropertyChanged(nameof(PdfRenderDpi)); } }

    // Tamaño objetivo de render para PDFs (usado por Docnet.Core)
    private int _pdfRenderWidth = 1600;
    public int PdfRenderWidth { get => _pdfRenderWidth; set { _pdfRenderWidth = Math.Max(200, value); OnPropertyChanged(nameof(PdfRenderWidth)); } }

    private int _pdfRenderHeight = 2200;
    public int PdfRenderHeight { get => _pdfRenderHeight; set { _pdfRenderHeight = Math.Max(200, value); OnPropertyChanged(nameof(PdfRenderHeight)); } }

        private bool _enableMemoryOptimization = true;
        public bool EnableMemoryOptimization { get => _enableMemoryOptimization; set { _enableMemoryOptimization = value; OnPropertyChanged(nameof(EnableMemoryOptimization)); } }

        private bool _showLoadingIndicators = true;
        public bool ShowLoadingIndicators { get => _showLoadingIndicators; set { _showLoadingIndicators = value; OnPropertyChanged(nameof(ShowLoadingIndicators)); } }

    private bool _rememberLastSession = false;
        public bool RememberLastSession { get => _rememberLastSession; set { _rememberLastSession = value; OnPropertyChanged(nameof(RememberLastSession)); } }

        // --- Nuevas Propiedades de Lectura ---
        private bool _isReadingMode = false; // Modo lectura sin distracciones
        public bool IsReadingMode { get => _isReadingMode; set { _isReadingMode = value; OnPropertyChanged(nameof(IsReadingMode)); } }

        private bool _isNightMode = false; // Modo nocturno
        public bool IsNightMode { get => _isNightMode; set { _isNightMode = value; OnPropertyChanged(nameof(IsNightMode)); } }

        private bool _autoAdvancePages = false; // Avance automático de páginas
        public bool AutoAdvancePages { get => _autoAdvancePages; set { _autoAdvancePages = value; OnPropertyChanged(nameof(AutoAdvancePages)); } }

    private int _autoAdvanceInterval = 5; // Intervalo en segundos para avance automático
    public int AutoAdvanceInterval { get => _autoAdvanceInterval; set { _autoAdvanceInterval = Math.Max(1, Math.Min(3600, value)); OnPropertyChanged(nameof(AutoAdvanceInterval)); } }

        private bool _enableZoomPan = true; // Habilitar arrastre de imagen zoomeada
        public bool EnableZoomPan { get => _enableZoomPan; set { _enableZoomPan = value; OnPropertyChanged(nameof(EnableZoomPan)); } }

        private bool _enableContinuousScroll = false; // Modo de lectura continua (scroll vertical)
        public bool EnableContinuousScroll { get => _enableContinuousScroll; set { _enableContinuousScroll = value; OnPropertyChanged(nameof(EnableContinuousScroll)); } }

    // Límite de caché de páginas (LRU)
    private int _pageCacheLimit = 60;
    public int PageCacheLimit { get => _pageCacheLimit; set { _pageCacheLimit = value; OnPropertyChanged(nameof(PageCacheLimit)); } }

    // Ventana de prefetch direccional
    private int _prefetchWindow = 4;
    public int PrefetchWindow { get => _prefetchWindow; set { _prefetchWindow = value; OnPropertyChanged(nameof(PrefetchWindow)); } }

        private bool _showPageProgressIndicator = true; // Indicador de progreso de página
        public bool ShowPageProgressIndicator { get => _showPageProgressIndicator; set { _showPageProgressIndicator = value; OnPropertyChanged(nameof(ShowPageProgressIndicator)); } }

        private double _brightness = 1.0; // Brillo de la imagen (0.0 a 2.0)
        public double Brightness { get => _brightness; set { _brightness = value; OnPropertyChanged(nameof(Brightness)); } }

        private double _contrast = 1.0; // Contraste de la imagen (0.0 a 2.0)
        public double Contrast { get => _contrast; set { _contrast = value; OnPropertyChanged(nameof(Contrast)); } }

        // --- Estadísticas de Lectura (Conceptual) ---
        public ReadingStatistics Statistics { get; set; } = new ReadingStatistics();

        // --- Avanzado / Experimentos ---
        private bool _autoAdvanceLoop = false; // Cuando llega a la última página, vuelve al inicio si está activo
        public bool AutoAdvanceLoop { get => _autoAdvanceLoop; set { _autoAdvanceLoop = value; OnPropertyChanged(nameof(AutoAdvanceLoop)); } }

        private int _hideOverlayDelaySeconds = 3; // Tiempo (seg) para ocultar la superposición en modo lectura/pantalla completa
        public int HideOverlayDelaySeconds { get => _hideOverlayDelaySeconds; set { _hideOverlayDelaySeconds = Math.Max(1, Math.Min(15, value)); OnPropertyChanged(nameof(HideOverlayDelaySeconds)); } }

    // Tiempo (seg) para ocultar el cursor en modo pantalla completa inmersiva
    private int _hideCursorDelaySeconds = 3;
    public int HideCursorDelaySeconds { get => _hideCursorDelaySeconds; set { _hideCursorDelaySeconds = Math.Max(1, Math.Min(15, value)); OnPropertyChanged(nameof(HideCursorDelaySeconds)); } }

        private bool _invertScrollWheel = false; // Invierte el sentido de la rueda del mouse para pasar página
        public bool InvertScrollWheel { get => _invertScrollWheel; set { _invertScrollWheel = value; OnPropertyChanged(nameof(InvertScrollWheel)); } }

    // Si está activo, el overlay se auto-oculta solo en pantalla completa inmersiva
    private bool _hideOverlayOnlyInImmersive = true;
    public bool HideOverlayOnlyInImmersive { get => _hideOverlayOnlyInImmersive; set { _hideOverlayOnlyInImmersive = value; OnPropertyChanged(nameof(HideOverlayOnlyInImmersive)); } }

        // Navegación: permitir que la barra espaciadora avance de página (modo clásico)
        private bool _spacebarNextPage = true;
        public bool SpacebarNextPage { get => _spacebarNextPage; set { _spacebarNextPage = value; OnPropertyChanged(nameof(SpacebarNextPage)); } }

        // Proporción de desplazamiento al usar teclas (0.1 a 1.0 del alto visible)
        private double _pageScrollStepRatio = 0.9;
        public double PageScrollStepRatio
        {
            get => _pageScrollStepRatio;
            set
            {
                var clamped = Math.Max(0.1, Math.Min(1.0, value));
                _pageScrollStepRatio = clamped; OnPropertyChanged(nameof(PageScrollStepRatio));
            }
        }

        // Preferencia de vista para 'Recientes': true = Lista, false = Tarjetas
        private bool _isRecentListView = false;
        public bool IsRecentListView { get => _isRecentListView; set { _isRecentListView = value; OnPropertyChanged(nameof(IsRecentListView)); } }

    // Preferencias de Pantalla Completa/Modo Inmersivo
    private bool _autoEnterImmersiveOnOpen = false; // Entrar automáticamente en inmersivo al abrir cómics
    public bool AutoEnterImmersiveOnOpen { get => _autoEnterImmersiveOnOpen; set { _autoEnterImmersiveOnOpen = value; OnPropertyChanged(nameof(AutoEnterImmersiveOnOpen)); } }

    // Eliminado: opción para pantalla completa estándar (solo inmersivo permanece)

    private bool _fadeOnFullscreenTransitions = true; // Fundido al entrar/salir de fullscreen/inmersivo
    public bool FadeOnFullscreenTransitions { get => _fadeOnFullscreenTransitions; set { _fadeOnFullscreenTransitions = value; OnPropertyChanged(nameof(FadeOnFullscreenTransitions)); } }

        // Última carpeta visitada en Home (explorador integrado)
        private string _lastHomeFolderPath = string.Empty;
        public string LastHomeFolderPath { get => _lastHomeFolderPath; set { _lastHomeFolderPath = value; OnPropertyChanged(nameof(LastHomeFolderPath)); } }
    }

    public enum ViewMode
    {
        SinglePage,
        TwoPage,
        TwoPageCover,
        ContinuousScroll // Nuevo
    }


    // Nueva clase para estadísticas de lectura
    public class ReadingStatistics : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private TimeSpan _totalReadingTime;
        public TimeSpan TotalReadingTime { get => _totalReadingTime; set { _totalReadingTime = value; OnPropertyChanged(nameof(TotalReadingTime)); } }

        private int _pagesRead;
        public int PagesRead { get => _pagesRead; set { _pagesRead = value; OnPropertyChanged(nameof(PagesRead)); } }

        // Puedes añadir más estadísticas aquí
    }

    public static class SettingsManager
    {
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PercysLibrary"
        );
    public const int MaxHistoryItems = 10; // ya no se usa para historial, mantenido por compatibilidad
        private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.xml");

    public static UserAppSettings Settings { get; private set; }
        // Evento para notificar cambios relevantes en listas observable-persistidas
        // Evento eliminado. La UI se actualiza mediante ContinueReadingService.

        static SettingsManager()
        {
            LoadSettings();
        }

        public static void LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(UserAppSettings), new Type[] { typeof(ReadingStatistics), typeof(Bookmark) });
                    using (FileStream fs = new FileStream(SettingsFilePath, FileMode.Open))
                    {
                        Settings = (UserAppSettings)serializer.Deserialize(fs);
                    }
                    // Asegurar colecciones no nulas
                    if (Settings.Bookmarks == null) Settings.Bookmarks = new BindingList<Bookmark>();
                    // Historial antiguo eliminado
                }
                catch (Exception ex)
                {
                    var log = ComicReader.Core.Services.ServiceLocator.TryGet<ComicReader.Core.Abstractions.ILogService>();
                    log?.LogException("Failed to load settings. Using default settings.", ex);
                    Settings = new UserAppSettings();
                    // Asegurar colecciones
                    // Historial antiguo eliminado
                    if (Settings.Bookmarks == null) Settings.Bookmarks = new BindingList<Bookmark>();
                }
            }
            else
            {
                // Intentar cargar desde ubicación antigua
                try
                {
                    var oldDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ComicReader");
                    var oldFile = Path.Combine(oldDir, "settings.xml");
                    if (File.Exists(oldFile))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(UserAppSettings), new Type[] { typeof(ReadingStatistics), typeof(Bookmark) });
                        using (FileStream fs = new FileStream(oldFile, FileMode.Open))
                        {
                            Settings = (UserAppSettings)serializer.Deserialize(fs);
                        }
                        try
                        {
                            Directory.CreateDirectory(SettingsDirectory);
                            File.Copy(oldFile, SettingsFilePath, overwrite: true);
                        }
                        catch { }
                    }
                    else
                    {
                        Settings = new UserAppSettings();
                    }
                    // Asegurar colecciones
                    // Historial antiguo eliminado
                    if (Settings.Bookmarks == null) Settings.Bookmarks = new BindingList<Bookmark>();
                }
                catch
                {
                    Settings = new UserAppSettings();
                    // Historial antiguo eliminado
                    if (Settings.Bookmarks == null) Settings.Bookmarks = new BindingList<Bookmark>();
                }
                var log = ComicReader.Core.Services.ServiceLocator.TryGet<ComicReader.Core.Abstractions.ILogService>();
                log?.Log("Settings file not found. Using default settings.", ComicReader.Core.Abstractions.LogLevel.Info);
            }
        }

        public static void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                XmlSerializer serializer = new XmlSerializer(typeof(UserAppSettings), new Type[] { typeof(ReadingStatistics), typeof(Bookmark) });
                using (FileStream fs = new FileStream(SettingsFilePath, FileMode.Create))
                {
                    serializer.Serialize(fs, Settings);
                }
                var log = ComicReader.Core.Services.ServiceLocator.TryGet<ComicReader.Core.Abstractions.ILogService>();
                log?.Log("Settings saved successfully.", ComicReader.Core.Abstractions.LogLevel.Info);
                // Sin notificación: ContinueReadingService gestiona cambios de "Seguir leyendo".
            }
            catch (Exception ex)
            {
                var log = ComicReader.Core.Services.ServiceLocator.TryGet<ComicReader.Core.Abstractions.ILogService>();
                log?.LogException("Failed to save settings.", ex);
                // Guardar sin mostrar MessageBox para evitar interrupciones en procesos automáticos
            }
        }
    }
}