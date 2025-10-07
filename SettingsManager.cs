// FileName: /Services/SettingsManager.cs
using System;
using System.IO;
using System.Xml.Serialization;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using ComicReader.Models;
// using System.Collections.Generic; // Para List<T> (no se usa)
// using ComicReader.Core.Abstractions; // Para LogLevel (usamos nombre calificado)

namespace ComicReader.Services
{
    // Clase serializable para almacenar cómics completados con fechas
    public class CompletedComicEntry
    {
        public string ComicPath { get; set; } = string.Empty;
        public DateTime CompletedDate { get; set; } = DateTime.Now;
    }

    public class UserAppSettings : INotifyPropertyChanged
    {
        public UserAppSettings()
        {
            _readingDirection = "LeftToRight";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    // Eliminado: historial antiguo reemplazado por ContinueReadingService
    [System.Xml.Serialization.XmlIgnore]
    public BindingList<Bookmark> Bookmarks { get; set; } = new BindingList<Bookmark>(); // Nuevo: Marcadores

        // Cómics completados - Lista serializable
        public System.Collections.Generic.List<CompletedComicEntry> CompletedComicsWithDates { get; set; } = new System.Collections.Generic.List<CompletedComicEntry>();
        
        // Propiedad de conveniencia (no serializada) para acceso rápido
        [System.Xml.Serialization.XmlIgnore]
        public System.Collections.Generic.List<string> CompletedComics 
        { 
            get => CompletedComicsWithDates.ConvertAll(c => c.ComicPath);
            set 
            { 
                CompletedComicsWithDates.Clear();
                if (value != null)
                {
                    foreach (var path in value)
                    {
                        if (!CompletedComicsWithDates.Exists(c => c.ComicPath == path))
                        {
                            CompletedComicsWithDates.Add(new CompletedComicEntry { ComicPath = path, CompletedDate = DateTime.Now });
                        }
                    }
                }
                OnPropertyChanged(nameof(CompletedComics)); 
            } 
        }

        // Dictionary no serializable - construido dinámicamente desde CompletedComicsWithDates
        [System.Xml.Serialization.XmlIgnore]
        public System.Collections.Generic.Dictionary<string, DateTime> CompletedDates 
        { 
            get 
            {
                var dict = new System.Collections.Generic.Dictionary<string, DateTime>();
                foreach (var entry in CompletedComicsWithDates)
                {
                    if (!dict.ContainsKey(entry.ComicPath))
                    {
                        dict[entry.ComicPath] = entry.CompletedDate;
                    }
                }
                return dict;
            }
        }

        private double _lastZoom = 1.0;
        public double LastZoom { get => _lastZoom; set { _lastZoom = value; OnPropertyChanged(nameof(LastZoom)); } }

        private double _lastRotationAngle = 0;
        public double LastRotationAngle { get => _lastRotationAngle; set { _lastRotationAngle = value; OnPropertyChanged(nameof(LastRotationAngle)); } }

        private bool _thumbnailsVisible = true;
        public bool ThumbnailsVisible { get => _thumbnailsVisible; set { _thumbnailsVisible = value; OnPropertyChanged(nameof(ThumbnailsVisible)); } }

        private string _lastOpenedFilePath = string.Empty;
        public string LastOpenedFilePath
        {
            get => _lastOpenedFilePath;
            set { _lastOpenedFilePath = value ?? string.Empty; OnPropertyChanged(nameof(LastOpenedFilePath)); }
        }

        private int _lastOpenedPage = 0;
        public int LastOpenedPage { get => _lastOpenedPage; set { _lastOpenedPage = value; OnPropertyChanged(nameof(LastOpenedPage)); } }

        private WindowState _lastWindowState = WindowState.Normal;
        public WindowState LastWindowState { get => _lastWindowState; set { _lastWindowState = value; OnPropertyChanged(nameof(LastWindowState)); } }

        private double _lastWindowWidth = 1024;
        public double LastWindowWidth { get => _lastWindowWidth; set { _lastWindowWidth = value; OnPropertyChanged(nameof(LastWindowWidth)); } }

        private double _lastWindowHeight = 768;
        public double LastWindowHeight { get => _lastWindowHeight; set { _lastWindowHeight = value; OnPropertyChanged(nameof(LastWindowHeight)); } }

        private double _lastWindowLeft = -1; // -1 significa centrar automáticamente
        public double LastWindowLeft { get => _lastWindowLeft; set { _lastWindowLeft = value; OnPropertyChanged(nameof(LastWindowLeft)); } }

        private double _lastWindowTop = -1; // -1 significa centrar automáticamente  
        public double LastWindowTop { get => _lastWindowTop; set { _lastWindowTop = value; OnPropertyChanged(nameof(LastWindowTop)); } }

        private ViewMode _currentViewMode = ViewMode.SinglePage;
        public ViewMode CurrentViewMode { get => _currentViewMode; set { _currentViewMode = value; OnPropertyChanged(nameof(CurrentViewMode)); } }



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

        private string _theme = "ComicTheme";
        public string Theme
        {
            get => _theme;
            set
            {
                if (_theme != value)
                {
                    _theme = value;
                    OnPropertyChanged(nameof(Theme));
                    // Notificar a la App para que cambie el tema dinámicamente
                    App.ApplyTheme(value);
                }
            }
        }

        private string _homeBackground = "SupermanHomeBackground";
        public string HomeBackground
        {
            get => _homeBackground;
            set
            {
                if (_homeBackground != value)
                {
                    _homeBackground = value;
                    OnPropertyChanged(nameof(HomeBackground));
                    // Aplicar inmediatamente el nuevo fondo
                    SettingsManager.ApplyHomeBackgroundImmediate(value);
                }
            }
        }



    // Animaciones globales
    private bool _enableAnimations = true; // Permite animaciones suaves en la UI
    public bool EnableAnimations { get => _enableAnimations; set { _enableAnimations = value; OnPropertyChanged(nameof(EnableAnimations)); } }

    private bool _preferReducedMotion = false; // Reduce duraciones y desplazamientos animados
    public bool PreferReducedMotion { get => _preferReducedMotion; set { _preferReducedMotion = value; OnPropertyChanged(nameof(PreferReducedMotion)); } }

        private bool _enablePageTurnAnimations = true;
        public bool EnablePageTurnAnimations
        {
            get => _enablePageTurnAnimations;
            set { _enablePageTurnAnimations = value; OnPropertyChanged(nameof(EnablePageTurnAnimations)); }
        }

        private string _pageTurnAnimationStyle = "Slide";
        public string PageTurnAnimationStyle
        {
            get => _pageTurnAnimationStyle;
            set { _pageTurnAnimationStyle = string.IsNullOrWhiteSpace(value) ? "Slide" : value; OnPropertyChanged(nameof(PageTurnAnimationStyle)); }
        }

        private int _pageTurnAnimationDurationMs = 180;
        public int PageTurnAnimationDurationMs
        {
            get => _pageTurnAnimationDurationMs;
            set
            {
                var clamped = Math.Max(30, Math.Min(600, value));
                if (_pageTurnAnimationDurationMs != clamped)
                {
                    _pageTurnAnimationDurationMs = clamped;
                    OnPropertyChanged(nameof(PageTurnAnimationDurationMs));
                }
            }
        }

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

        private bool _enableAsyncPrefetch = true;
        public bool EnableAsyncPrefetch
        {
            get => _enableAsyncPrefetch;
            set { _enableAsyncPrefetch = value; OnPropertyChanged(nameof(EnableAsyncPrefetch)); }
        }

        private int _decodeConcurrency = 3;
        public int DecodeConcurrency
        {
            get => _decodeConcurrency;
            set
            {
                var clamped = Math.Max(1, Math.Min(8, value));
                if (_decodeConcurrency != clamped)
                {
                    _decodeConcurrency = clamped;
                    OnPropertyChanged(nameof(DecodeConcurrency));
                }
            }
        }

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

        // Propiedades adicionales para configuraciones avanzadas
        private string _readingDirection = "LeftToRight";
        public string ReadingDirection { get => _readingDirection; set { _readingDirection = value; OnPropertyChanged(nameof(ReadingDirection)); } }

        private bool _autoZoom = false;
        public bool AutoZoom { get => _autoZoom; set { _autoZoom = value; OnPropertyChanged(nameof(AutoZoom)); } }

        private bool _autoFullscreen = false;
        public bool AutoFullscreen { get => _autoFullscreen; set { _autoFullscreen = value; OnPropertyChanged(nameof(AutoFullscreen)); } }

        private bool _preloadNextPages = true;
        public bool PreloadNextPages { get => _preloadNextPages; set { _preloadNextPages = value; OnPropertyChanged(nameof(PreloadNextPages)); } }

        private bool _hideTaskbarInFullscreen = true;
        public bool HideTaskbarInFullscreen { get => _hideTaskbarInFullscreen; set { _hideTaskbarInFullscreen = value; OnPropertyChanged(nameof(HideTaskbarInFullscreen)); } }

        private bool _hideMenuInFullscreen = true;
        public bool HideMenuInFullscreen { get => _hideMenuInFullscreen; set { _hideMenuInFullscreen = value; OnPropertyChanged(nameof(HideMenuInFullscreen)); } }

        private bool _escapeExitsFullscreen = true;
        public bool EscapeExitsFullscreen { get => _escapeExitsFullscreen; set { _escapeExitsFullscreen = value; OnPropertyChanged(nameof(EscapeExitsFullscreen)); } }

        private bool _enableImageCache = true;
        public bool EnableImageCache { get => _enableImageCache; set { _enableImageCache = value; OnPropertyChanged(nameof(EnableImageCache)); } }

        private bool _useHardwareAcceleration = true;
        public bool UseHardwareAcceleration { get => _useHardwareAcceleration; set { _useHardwareAcceleration = value; OnPropertyChanged(nameof(UseHardwareAcceleration)); } }

        private bool _pdfUseAntialiasing = true;
        public bool PdfUseAntialiasing { get => _pdfUseAntialiasing; set { _pdfUseAntialiasing = value; OnPropertyChanged(nameof(PdfUseAntialiasing)); } }

        private bool _pdfUseVectorRendering = true;
        public bool PdfUseVectorRendering { get => _pdfUseVectorRendering; set { _pdfUseVectorRendering = value; OnPropertyChanged(nameof(PdfUseVectorRendering)); } }

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

        private bool _continueCompactMode = false;
        public bool ContinueCompactMode
        {
            get => _continueCompactMode;
            set { _continueCompactMode = value; OnPropertyChanged(nameof(ContinueCompactMode)); }
        }

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
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private TimeSpan _totalReadingTime;
        public TimeSpan TotalReadingTime { get => _totalReadingTime; set { _totalReadingTime = value; OnPropertyChanged(nameof(TotalReadingTime)); } }

        private int _pagesRead;
        public int PagesRead { get => _pagesRead; set { _pagesRead = value; OnPropertyChanged(nameof(PagesRead)); } }

        private int _comicsRead;
        public int ComicsRead { get => _comicsRead; set { _comicsRead = value; OnPropertyChanged(nameof(ComicsRead)); } }

        private int _pagesViewed;
        public int PagesViewed { get => _pagesViewed; set { _pagesViewed = value; OnPropertyChanged(nameof(PagesViewed)); } }

        private int _readingSessions;
        public int ReadingSessions { get => _readingSessions; set { _readingSessions = value; OnPropertyChanged(nameof(ReadingSessions)); } }

        // Constructor que permite mantener valores existentes o usar valores por defecto
        public ReadingStatistics()
        {
            // Solo asignar valores por defecto si son cero (primera vez)
            if (ComicsRead == 0 && PagesViewed == 0 && TotalReadingTime == TimeSpan.Zero && ReadingSessions == 0)
            {
                ComicsRead = 47;
                PagesViewed = 1250;
                TotalReadingTime = TimeSpan.FromHours(23.25);
                ReadingSessions = 89;
            }
        }
    }

    public static class SettingsManager
    {
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PercysLibrary"
        );
    public const int MaxHistoryItems = 10; // ya no se usa para historial, mantenido por compatibilidad
        private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.xml");

    public static UserAppSettings Settings { get; private set; } = new UserAppSettings();
        // Evento para notificar cambios relevantes en listas observable-persistidas
        // Evento eliminado. La UI se actualiza mediante ContinueReadingService.

        // Debounce/Serialización: coalescer guardados frecuentes para reducir I/O y jank de UI
        private static readonly object _saveGate = new object();
    private static System.Threading.Timer? _saveTimer; // dispara guardado diferido
        private static readonly TimeSpan _saveDebounce = TimeSpan.FromMilliseconds(1000);
        private static readonly System.Threading.SemaphoreSlim _saveSemaphore = new System.Threading.SemaphoreSlim(1, 1);
        private static volatile bool _pendingScheduled; // hint para saber si hay un guardado programado
    private static DateTime _lastSaveLogUtc = DateTime.MinValue; // para limitar el spam de logs

    private static PropertyChangedEventHandler? _settingsChangedHandler;
    private static UserAppSettings? _subscribedInstance;

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
                        var des = serializer.Deserialize(fs) as UserAppSettings;
                        Settings = des ?? new UserAppSettings();
                    }
                    // Asegurar colecciones no nulas y estadísticas inicializadas
                    if (Settings.Bookmarks == null) Settings.Bookmarks = new BindingList<Bookmark>();
                    if (Settings.Statistics == null) Settings.Statistics = new ReadingStatistics();
                    // Historial antiguo eliminado
                    AttachAutoSaveHandler();
                }
                catch (Exception ex)
                {
                    var log = ComicReader.Core.Services.ServiceLocator.TryGet<ComicReader.Core.Abstractions.ILogService>();
                    log?.LogException("Failed to load settings. Using default settings.", ex);
                    Settings = new UserAppSettings();
                    // Asegurar colecciones y estadísticas
                    // Historial antiguo eliminado
                    if (Settings.Bookmarks == null) Settings.Bookmarks = new BindingList<Bookmark>();
                    if (Settings.Statistics == null) Settings.Statistics = new ReadingStatistics();
                    AttachAutoSaveHandler();
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
                            var des = serializer.Deserialize(fs) as UserAppSettings;
                            Settings = des ?? new UserAppSettings();
                        }
                        try
                        {
                            Directory.CreateDirectory(SettingsDirectory);
                            File.Copy(oldFile, SettingsFilePath, overwrite: true);
                        }
                        catch (Exception ex) { Logger.LogException("Error al aplicar cambios de configuración suscrita", ex); }
                    }
                    else
                    {
                        Settings = new UserAppSettings();
                    }
                    // Asegurar colecciones y estadísticas
                    // Historial antiguo eliminado
                    if (Settings.Bookmarks == null) Settings.Bookmarks = new BindingList<Bookmark>();
                    if (Settings.Statistics == null) Settings.Statistics = new ReadingStatistics();
                    AttachAutoSaveHandler();
                }
                catch
                {
                    Settings = new UserAppSettings();
                    // Historial antiguo eliminado y asegurar estadísticas
                    if (Settings.Bookmarks == null) Settings.Bookmarks = new BindingList<Bookmark>();
                    if (Settings.Statistics == null) Settings.Statistics = new ReadingStatistics();
                    AttachAutoSaveHandler();
                }
                var log = ComicReader.Core.Services.ServiceLocator.TryGet<ComicReader.Core.Abstractions.ILogService>();
                log?.Log("Settings file not found. Using default settings.", ComicReader.Core.Abstractions.LogLevel.Info);
            }
        }

        private static void AttachAutoSaveHandler()
        {
            try
            {
                // Desuscribir de la instancia anterior si aplica
                if (_settingsChangedHandler != null && _subscribedInstance != null)
                {
                    try { _subscribedInstance.PropertyChanged -= _settingsChangedHandler; } catch (Exception ex) { Logger.LogException("Error desuscribiendo PropertyChanged en SettingsManager", ex); }
                }
                _settingsChangedHandler = (s, e) =>
                {
                    // Guardado diferido y coalescido en cambios de propiedades
                    try { SaveSettings(); } catch (Exception ex) { Logger.LogException("Error guardando settings en SettingsManager (dispose)", ex); }
                };
                Settings.PropertyChanged += _settingsChangedHandler;
                _subscribedInstance = Settings;
            }
            catch (Exception ex) { Logger.LogException("Error en SettingsManager.Dispose/cleanup", ex); }
        }

        /// <summary>
        /// Programa un guardado de ajustes con debounce (no bloquea la UI). Las múltiples llamadas en ráfaga
        /// se coalescen en una única escritura a disco.
        /// </summary>
        public static void SaveSettings()
        {
            lock (_saveGate)
            {
                if (_saveTimer == null)
                {
                    // Crear timer de un solo disparo; la acción corre en un hilo ThreadPool
                    _saveTimer = new System.Threading.Timer(static _ =>
                    {
                        try { DoSaveSync(); }
                        finally { _pendingScheduled = false; }
                    }, null, System.Threading.Timeout.InfiniteTimeSpan, System.Threading.Timeout.InfiniteTimeSpan);
                }
                _pendingScheduled = true;
                _saveTimer.Change(_saveDebounce, System.Threading.Timeout.InfiniteTimeSpan);
            }
        }

        /// <summary>
        /// Fuerza la escritura inmediata si hay un guardado pendiente y espera a que termine cualquier guardado en curso.
        /// Llamar en OnExit para garantizar persistencia.
        /// </summary>
        public static void FlushPendingSaves()
        {
            lock (_saveGate)
            {
                if (_saveTimer != null)
                {
                    // Cancelar el temporizador y limpiar la bandera de pendiente para evitar doble guardado
                    _saveTimer.Change(System.Threading.Timeout.InfiniteTimeSpan, System.Threading.Timeout.InfiniteTimeSpan);
                    _pendingScheduled = false;
                    try { _saveTimer.Dispose(); } catch (Exception ex) { Logger.LogException("Error disponiendo _saveTimer en SettingsManager", ex); }
                    _saveTimer = null;
                }
            }
            // Ejecutar guardado sincronizado (respetando exclusión mediante _saveSemaphore)
            try { DoSaveSync(); } catch (Exception ex) { Logger.LogException("Error ejecutando DoSaveSync en SettingsManager", ex); }
        }

        /// <summary>
        /// Realiza la serialización de forma exclusiva y atómica (archivo temporal + reemplazo). Thread-safe.
        /// </summary>
        private static void DoSaveSync()
        {
            // Evitar trabajos innecesarios si nadie lo pidió
            if (!_pendingScheduled)
            {
                // No guardado programado; pero si se llama explícitamente (Flush), continuamos de todas formas
            }

            _saveSemaphore.Wait();
            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                var tmpPath = SettingsFilePath + ".tmp";
                var serializer = new XmlSerializer(typeof(UserAppSettings), new Type[] { typeof(ReadingStatistics), typeof(Bookmark) });
                using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    serializer.Serialize(fs, Settings);
                    fs.Flush(true);
                }
                // Reemplazo atómico del archivo final
                try
                {
                    // .NET 6+: File.Move con overwrite true
                    File.Move(tmpPath, SettingsFilePath, overwrite: true);
                }
                catch
                {
                    // Fallback si move falla por bloqueo transitorio
                    try { File.Copy(tmpPath, SettingsFilePath, overwrite: true); File.Delete(tmpPath); } catch (Exception ex) { Logger.LogException($"Error reemplazando archivo de settings: {tmpPath}", ex); }
                }

                var log = ComicReader.Core.Services.ServiceLocator.TryGet<ComicReader.Core.Abstractions.ILogService>();
                // Solo loguear cuando proviene del guardado programado (debounce) y como máximo cada 10s
                if (_pendingScheduled)
                {
                    var now = DateTime.UtcNow;
                    if ((now - _lastSaveLogUtc) > TimeSpan.FromSeconds(10))
                    {
                        _lastSaveLogUtc = now;
                        log?.Log("Settings saved successfully.", ComicReader.Core.Abstractions.LogLevel.Info);
                    }
                }
            }
            catch (Exception ex)
            {
                var log = ComicReader.Core.Services.ServiceLocator.TryGet<ComicReader.Core.Abstractions.ILogService>();
                log?.LogException("Failed to save settings.", ex);
            }
            finally
            {
                try { _saveSemaphore.Release(); } catch (Exception ex) { Logger.LogException("Error liberando _saveSemaphore en SettingsManager", ex); }
            }
        }

        /// <summary>
        /// Aplica el fondo del HomeView inmediatamente sin depender de configuraciones
        /// </summary>
        public static void ApplyHomeBackgroundImmediate(string backgroundName)
        {
            try
            {
                if (Application.Current == null) return;
                
                // Si estamos en el hilo UI, ejecutar directamente, sino usar Dispatcher
                if (Application.Current.Dispatcher.CheckAccess())
                {
                    ApplyBackgroundInternal(backgroundName);
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() => ApplyBackgroundInternal(backgroundName));
                }
            }
            catch
            {
                // Error silencioso
            }
        }
        
        private static void ApplyBackgroundInternal(string backgroundName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsManager] Aplicando fondo interno: {backgroundName}");
                
                // Cargar recursos de fondos
                var homeBackgroundsUri = new Uri("Themes/HomeBackgrounds.xaml", UriKind.Relative);
                var homeBackgroundsDict = new ResourceDictionary() { Source = homeBackgroundsUri };
                
                System.Diagnostics.Debug.WriteLine($"[SettingsManager] HomeBackgrounds.xaml cargado, contiene {homeBackgroundsDict.Count} recursos");
                
                Brush? backgroundBrush = null;
                
                // Buscar el fondo específico
                if (!string.IsNullOrEmpty(backgroundName) && homeBackgroundsDict.Contains(backgroundName))
                {
                    backgroundBrush = homeBackgroundsDict[backgroundName] as Brush;
                    System.Diagnostics.Debug.WriteLine($"[SettingsManager] Fondo encontrado: {backgroundName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsManager] Fondo NO encontrado: {backgroundName}");
                }
                
                // Fallback a Superman si no se encuentra
                if (backgroundBrush == null && homeBackgroundsDict.Contains("SupermanHomeBackground"))
                {
                    backgroundBrush = homeBackgroundsDict["SupermanHomeBackground"] as Brush;
                    System.Diagnostics.Debug.WriteLine("[SettingsManager] Usando fallback SupermanHomeBackground");
                }
                
                // Crear fondo por defecto si es necesario
                if (backgroundBrush == null)
                {
                    backgroundBrush = CreateDefaultBackground();
                    System.Diagnostics.Debug.WriteLine("[SettingsManager] Creando fondo por defecto");
                }
                
                // Aplicar el fondo a los recursos de la aplicación
                if (backgroundBrush != null)
                {
                    Application.Current.Resources["DynamicHomeBackgroundBrush"] = backgroundBrush;
                    System.Diagnostics.Debug.WriteLine("[SettingsManager] Fondo aplicado correctamente a recursos de la aplicación");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsManager] Error en ApplyBackgroundInternal: {ex.Message}");
                
                // En caso de error, crear fondo de emergencia
                try
                {
                    Application.Current.Resources["DynamicHomeBackgroundBrush"] = CreateDefaultBackground();
                    System.Diagnostics.Debug.WriteLine("[SettingsManager] Fondo de emergencia aplicado");
                }
                catch (Exception emergencyEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsManager] Error aplicando fondo de emergencia: {emergencyEx.Message}");
                }
            }
        }
        
        private static LinearGradientBrush CreateDefaultBackground()
        {
            return new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromRgb(0, 31, 63), 0),       // Superman blue
                    new GradientStop(Color.FromRgb(220, 20, 60), 0.35),   // Superman red
                    new GradientStop(Color.FromRgb(255, 215, 0), 0.7),    // Superman yellow
                    new GradientStop(Color.FromRgb(65, 105, 225), 1)      // Royal blue
                }
            };
        }
        
        /// <summary>
        /// Exporta las configuraciones actuales a un archivo
        /// </summary>
        public static void ExportSettings(string filePath)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(UserAppSettings), new Type[] { typeof(ReadingStatistics), typeof(Bookmark) });
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    serializer.Serialize(fs, Settings);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al exportar configuraciones: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Importa configuraciones desde un archivo
        /// </summary>
        public static UserAppSettings ImportSettings(string filePath)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(UserAppSettings), new Type[] { typeof(ReadingStatistics), typeof(Bookmark) });
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var importedSettings = serializer.Deserialize(fs) as UserAppSettings;
                    if (importedSettings == null)
                        throw new Exception("El archivo no contiene configuraciones válidas.");
                    
                    // Asegurar colecciones no nulas
                    if (importedSettings.Bookmarks == null) importedSettings.Bookmarks = new BindingList<Bookmark>();
                    if (importedSettings.Statistics == null) importedSettings.Statistics = new ReadingStatistics();
                    
                    return importedSettings;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al importar configuraciones: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Método de prueba para verificar si los fondos están funcionando
        /// </summary>
        public static void TestBackgroundChange()
        {
            try
            {
                var testBackgrounds = new[] { "BatmanHomeBackground", "SpidermanHomeBackground", "SupermanHomeBackground" };
                
                foreach (var bg in testBackgrounds)
                {
                    System.Diagnostics.Debug.WriteLine($"[TEST] Probando fondo: {bg}");
                    ApplyHomeBackgroundImmediate(bg);
                    System.Threading.Thread.Sleep(2000); // Esperar 2 segundos
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TEST] Error en prueba: {ex.Message}");
            }
        }
    }
}