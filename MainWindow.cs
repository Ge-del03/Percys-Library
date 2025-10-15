using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
// using System.Windows.Input; // ya está incluido arriba
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using Microsoft.Win32;
using ComicReader.Views;
using ComicReader.Services;
using ComicReader.Models;
using System.Windows.Input;
using ComicReader.Core.Abstractions;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using WinForms = System.Windows.Forms;
using ComicReader.Utils;
// using System.Threading; // duplicado eliminado

namespace ComicReader
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
    private ComicPageLoader _comicLoader = new ComicPageLoader();
    private int _currentPageIndex;
    private HomeView _homeView; // Se inicializa tras cargar Settings
    private SettingsView _settingsView; // Se inicializa tras cargar Settings
        private Image _currentComicImage;
    private ScrollViewer _readerScrollViewer;
    private Grid _readerCenterGrid;
    private bool _isPanning = false;
    private System.Windows.Point _panStartPoint;
    private double _panStartVerticalOffset;
    private double _panStartHorizontalOffset;
    private double _zoomFactor = 1.0;
        private bool _isComicOpen = false;
    private object _currentView;
        private bool _isNightMode = false;
        private bool _isReadingMode = false;
        private bool _thumbnailsVisible = false;
    private double _rotationAngle = 0;
    private IReadingStatsService _stats => ComicReader.Core.Services.ServiceLocator.TryGet<IReadingStatsService>();
        // Vista de lectura continua (scroll)
        private Views.ContinuousComicView _continuousView;
            // Control de configuración rápida del modo lectura (panel flotante)
            private ComicReader.Views.ReadingCoreControl _readingCoreControl;
    // Secuencia para cancelar cargas obsoletas
    private long _pageLoadSeq = 0;
    // Secuencia independiente para miniaturas (no se invalida al cambiar de página)
    private long _thumbLoadSeq = 0;
    // Estado de pantalla completa inmersiva y overlay
    private bool _isImmersive = false;
    // Guardado de estado de ventana para inmersivo
    private double _savedLeft = double.NaN;
    private double _savedTop = double.NaN;
    private double _savedWidth = double.NaN;
    private double _savedHeight = double.NaN;
    private double _savedOverlayOpacity = 0.96;
    private bool _savedOverlayHit = true;
    // Estado overlay (modo inmersivo usa _savedOverlayOpacity/_savedOverlayHit)
    private WindowStyle _savedWindowStyle = WindowStyle.SingleBorderWindow;
    private ResizeMode _savedResizeMode = ResizeMode.CanResize;
    private WindowState _savedWindowState = WindowState.Normal;
    private Visibility _savedTitleBarVisibility = Visibility.Visible;
    private Visibility _savedTopBarVisibility = Visibility.Visible;
    private Visibility _savedThumbPanelVisibility = Visibility.Collapsed;
    private GridLength _savedThumbColWidth = new GridLength(0);
    private System.Windows.Media.Brush _savedBackgroundBrush = null;
    private bool _savedTopmost = false;
    private ScrollBarVisibility _savedVerticalScrollBarVisibility = ScrollBarVisibility.Auto;
    private ScrollBarVisibility _savedHorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
    private System.Windows.Media.BitmapScalingMode _savedImageScalingMode = System.Windows.Media.BitmapScalingMode.Unspecified;
    private bool _immersiveTransitionBusy = false;
    private bool _savedThumbColWidthSet = false;
    // Evitar recursión al sincronizar selección del panel de miniaturas
    private bool _suppressThumbListSelectionChange = false;
    // Dirección de la última navegación: -1=prev, 1=next, 0=neutra
    private int _lastNavDirection = 0;

        // Variables para guardar el estado de la ventana cuando está en modo Normal
        private double _normalWidth = 1000;
        private double _normalHeight = 700;
        private double _normalLeft = -1;
        private double _normalTop = -1;
        private bool _windowStateInitialized = false;

        // Propiedades públicas para acceso desde Views
        public ComicPageLoader ComicLoader => _comicLoader;
        public int CurrentPageIndex => _currentPageIndex;
    // Compatibilidad: algunas rutas del diseñador buscan 'zoomLevel'
    public double zoomLevel => _zoomFactor;

        public event PropertyChangedEventHandler PropertyChanged;

        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged(nameof(CurrentView));
                OnPropertyChanged(nameof(IsComicViewActive));
                OnPropertyChanged(nameof(IsReaderViewActive));
                OnPropertyChanged(nameof(IsHomeViewActive));
                OnPropertyChanged(nameof(IsSettingsViewActive));
            }
        }

        public bool IsComicViewActive => _isComicOpen;
        public bool IsReaderViewActive => CurrentView == _readerScrollViewer || CurrentView == _continuousView || _isComicOpen;
        public bool IsHomeViewActive => CurrentView == _homeView;
        public bool IsSettingsViewActive => CurrentView == _settingsView;

        public MainWindow()
        {
            // Carga el XAML de la ventana. Sin esta llamada, la UI queda en blanco.
            InitializeComponent();

            // Inicialización adicional específica de la app
            try
            {
                _readingCoreControl = new ComicReader.Views.ReadingCoreControl();
                _readingCoreControl.ViewModeChanged += (mode) =>
                {
                    // Persistir la preferencia y aplicar de forma mínima
                    try
                    {
                        if (SettingsManager.Settings != null)
                        {
                            SettingsManager.Settings.EnableContinuousScroll = mode == "ContinuousScroll";
                            SettingsManager.SaveSettings();
                        }
                    }
                    catch { }
                    // Forzar reconstrucción del scaffold de lector
                    try { EnsureReaderScaffold(); } catch { }
                };
                _readingCoreControl.SpacingChanged += (px) =>
                {
                    try
                    {
                        if (SettingsManager.Settings != null)
                        {
                            SettingsManager.Settings.ReadingSpacing = px;
                            SettingsManager.SaveSettings();
                        }
                    }
                    catch { }
                    // Aplicar separación si estamos en vista continua
                    try
                    {
                        if (_continuousView != null)
                            _continuousView.ItemSpacing = px;
                    }
                    catch { }
                };
                _readingCoreControl.CloseRequested += () =>
                {
                    try
                    {
                        // Cerrar el panel: restaurar el content principal
                        if (this.FindName("MainContentArea") is ContentControl cc)
                        {
                            // Si hay un reader activo, mantenerlo; solo ocultamos el panel overlay
                            if (cc.Content == _readingCoreControl) cc.Content = CurrentView;
                        }
                    }
                    catch { }

                    // Registrar atajo Ctrl+L para alternar panel de lectura
                    try
                    {
                        var toggle = new RoutedCommand();
                        toggle.InputGestures.Add(new KeyGesture(Key.L, ModifierKeys.Control));
                        this.CommandBindings.Add(new CommandBinding(toggle, (s, e) => ShowReadingQuickPanel()));
                    }
                    catch { }
                };
            }
            catch { }
            InitializeComponents();
            DataContext = this;
            // Suscribir eventos removidos del XAML
            this.Drop += Window_Drop;
            this.KeyDown += MainWindow_KeyDown;
            // Interceptar teclas antes de que controles internos (p.ej. ScrollViewer) las consuman
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
            this.AllowDrop = true;
            // Inicializar vista continua y sincronizar eventos de página actual
            _continuousView = new Views.ContinuousComicView();
            _continuousView.ViewModel.CurrentPageChanged += (idx) =>
            {
                _currentPageIndex = idx;
                try { if (this.FindName("PageIndicator") is TextBlock pi) pi.Text = $"Página {idx + 1} de {_comicLoader.PageCount}"; } catch { }
                try
                {
                    _stats?.RecordPageViewed(idx + 1);
                    SettingsManager.Settings.LastOpenedFilePath = _comicLoader.FilePath;
                    SettingsManager.Settings.LastOpenedPage = idx;
                    SettingsManager.SaveSettings();
                    // Actualizar progreso en el nuevo servicio de "Seguir leyendo"
                    var oneBased = idx + 1;
                    var pageCount = _comicLoader?.PageCount ?? 0;
                    ComicReader.Services.ContinueReadingService.Instance.UpsertProgress(_comicLoader?.FilePath, oneBased, pageCount);
                    if (pageCount > 0 && oneBased >= pageCount)
                    {
                        // UpsertProgress now moves items to CompletedItems when progress >= pageCount.
                        // No debemos eliminar aquí (antes se borraba). Solo refrescar la vista para que muestre el cambio.
                        _homeView?.RefreshRecent();
                    }
                }
                catch { }
            };
            // Escuchar cambios de configuración y aplicarlos en caliente
            if (SettingsManager.Settings is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged += (_, __) =>
                {
                    try { this.Dispatcher.BeginInvoke(new Action(ApplySettingsRuntime)); } catch { }
                };
            }
            
            // Configurar ventana inicial
            ConfigureInitialWindowState();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // Asegurar que la ventana tenga el tamaño correcto al inicializarse
            if (this.WindowState != WindowState.Maximized)
            {
                this.Width = _normalWidth;
                this.Height = _normalHeight;
            }
        }

        private void ConfigureInitialWindowState()
        {
            // Configurar tamaño y posición inicial de manera más agresiva
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.MinWidth = 800;
            this.MinHeight = 600;
            // Restaurar desde Settings si existe
            var s = SettingsManager.Settings;
            if (s != null)
            {
                // Tamaño normal recordado
                _normalWidth = Math.Max(800, s.LastWindowWidth);
                _normalHeight = Math.Max(600, s.LastWindowHeight);
                // Aplicar tamaño inicialmente solo si no está maximizada
                this.Width = _normalWidth;
                this.Height = _normalHeight;
                // Estado recordado
                if (s.LastWindowState == WindowState.Maximized)
                {
                    // Diferir a Loaded para evitar parpadeos
                    this.Loaded += (_, __) =>
                    {
                        try { this.WindowState = WindowState.Maximized; } catch { }
                    };
                }
                else
                {
                    // Centrar en pantalla si estamos en estado normal
                    CenterWindowOnScreen();
                }
            }
            else
            {
                // Fallback
                this.Width = _normalWidth;
                this.Height = _normalHeight;
            }
            
            // Forzar el tamaño inmediatamente
            this.SizeToContent = SizeToContent.Manual;
            
            // Evento para inicialización después de cargar
            this.Loaded += (s, e) =>
            {
                // Forzar tamaño y posición después de cargar
                if (this.WindowState != WindowState.Maximized)
                {
                    this.Width = _normalWidth;
                    this.Height = _normalHeight;
                }
                
                // Guardar posición inicial después de que la ventana se muestre
                _normalLeft = this.Left;
                _normalTop = this.Top;
                _normalWidth = this.ActualWidth;
                _normalHeight = this.ActualHeight;
                _windowStateInitialized = true;
                
                System.Diagnostics.Debug.WriteLine($"Inicializado: L={_normalLeft}, T={_normalTop}, W={_normalWidth}, H={_normalHeight}");
            };
            
            // Solo rastrear cambios manuales del usuario (simplificado)
            this.LocationChanged += (s, e) =>
            {
                if (this.WindowState == WindowState.Normal && _windowStateInitialized && this.IsLoaded)
                {
                    _normalLeft = this.Left;
                    _normalTop = this.Top;
                    // Guardar en settings (persistirá al cerrar)
                    try
                    {
                        SettingsManager.Settings.LastWindowState = this.WindowState;
                        SettingsManager.Settings.LastWindowWidth = this.ActualWidth;
                        SettingsManager.Settings.LastWindowHeight = this.ActualHeight;
                    }
                    catch { }
                }
            };
            
            this.SizeChanged += (s, e) =>
            {
                if (this.WindowState == WindowState.Normal && _windowStateInitialized && this.IsLoaded)
                {
                    _normalWidth = this.ActualWidth;
                    _normalHeight = this.ActualHeight;
                    // Guardar en settings (persistirá al cerrar)
                    try
                    {
                        SettingsManager.Settings.LastWindowState = this.WindowState;
                        SettingsManager.Settings.LastWindowWidth = this.ActualWidth;
                        SettingsManager.Settings.LastWindowHeight = this.ActualHeight;
                    }
                    catch { }
                }
            };
        }

        private void InitializeComponents()
        {
            _currentPageIndex = 0;
            // Asegurar que los ajustes estén cargados antes de crear vistas que dependan de ellos
            SettingsManager.LoadSettings();

            // Crear vistas DESPUÉS de cargar Settings para que se suscriban a la instancia correcta
            _homeView = new HomeView();
            _settingsView = new SettingsView();

            Title = "Percy's Library";
            CurrentView = _homeView;
            // Inicio siempre en Home: no abrir automáticamente la última sesión
            this.Loaded += (s, e) =>
            {
                // Dejamos la posibilidad de mostrar un botón "Continuar última sesión" en Home en el futuro.
            };
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ShowWelcomeScreen()
        {
            // Cancelar cargas/miniaturas pendientes
            try { Interlocked.Increment(ref _pageLoadSeq); } catch { }
            try { Interlocked.Increment(ref _thumbLoadSeq); } catch { }
            if (this.FindName("MainContentArea") is ContentControl content)
                content.Content = _homeView;
            CurrentView = _homeView;
            _isComicOpen = false;
            OnPropertyChanged(nameof(IsComicViewActive));
            // Asegurar que el panel de miniaturas no quede visible fuera del lector
            HideThumbnailsPanel();
            // Ocultar barra del lector fuera del modo lectura
            SetReaderTopBarVisible(false);
        }

        private void ShowHomeView()
        {
            // Cancelar cargas/miniaturas pendientes
            try { Interlocked.Increment(ref _pageLoadSeq); } catch { }
            try { Interlocked.Increment(ref _thumbLoadSeq); } catch { }
            if (this.FindName("MainContentArea") is ContentControl content)
                content.Content = _homeView;
            CurrentView = _homeView;
            _isComicOpen = false;
            OnPropertyChanged(nameof(IsComicViewActive));
            OnPropertyChanged(nameof(IsReaderViewActive));
            try { _currentComicImage?.ClearValue(Image.SourceProperty); } catch { }
            // Cerrar sesión de lectura cuando volvemos a inicio
            try { _stats?.EndSession(); } catch { }
            // Refrescar recientes al volver a inicio
            try { _homeView?.RefreshRecent(); } catch { }
            // Ocultar miniaturas si estaban activas
            HideThumbnailsPanel();
            // Ocultar barra del lector
            SetReaderTopBarVisible(false);
        }

        private void ShowSettingsView()
        {
            // Cancelar cargas/miniaturas pendientes
            try { Interlocked.Increment(ref _pageLoadSeq); } catch { }
            try { Interlocked.Increment(ref _thumbLoadSeq); } catch { }
            if (this.FindName("MainContentArea") is ContentControl content)
                content.Content = _settingsView;
            CurrentView = _settingsView;
            _isComicOpen = false;
            OnPropertyChanged(nameof(IsComicViewActive));
            OnPropertyChanged(nameof(IsReaderViewActive));
            // Ocultar miniaturas si estaban activas
            HideThumbnailsPanel();
            // Ocultar barra del lector
            SetReaderTopBarVisible(false);
        }

        private void HideThumbnailsPanel()
        {
            try
            {
                var panel = this.FindName("ThumbPanel") as FrameworkElement;
                var col = this.FindName("ThumbCol") as System.Windows.Controls.ColumnDefinition;
                var list = this.FindName("ThumbList") as System.Windows.Controls.ListBox;
                if (panel != null) panel.Visibility = Visibility.Collapsed;
                if (col != null) col.Width = new GridLength(0);
                if (list != null)
                {
                    list.ItemsSource = null;
                    list.SelectedIndex = -1;
                }
                _thumbnailsVisible = false;
                // Invalida cargas de miniaturas en curso
                try { Interlocked.Increment(ref _thumbLoadSeq); } catch { }
            }
            catch { }
        }

        /// <summary>
        /// Refresca el panel de miniaturas con las páginas del cómic actualmente cargado.
        /// Usar cuando el panel ya estaba visible y se abre un nuevo archivo, para evitar que se muestren las miniaturas del cómic anterior.
        /// </summary>
        private void RefreshThumbnailPanelForCurrentComic()
        {
            try
            {
                if (!_isComicOpen || _comicLoader?.Pages == null) return;
                var list = this.FindName("ThumbList") as System.Windows.Controls.ListBox;
                if (list == null) return;

                // Forzar rebind: primero limpiar ItemsSource
                _suppressThumbListSelectionChange = true;
                try
                {
                    list.ItemsSource = null;
                    list.Items.Clear();
                    // Asignar nueva fuente y selección actual
                    list.ItemsSource = _comicLoader.Pages;
                    list.SelectedIndex = Math.Max(0, Math.Min(_currentPageIndex, _comicLoader.Pages.Count - 1));
                    try { list.Items.Refresh(); } catch { }
                    // Llevar a la vista la miniatura actual
                    list.ScrollIntoView(list.SelectedItem);
                }
                finally { _suppressThumbListSelectionChange = false; }

                // Limpiar miniaturas antiguas para evitar parpadeos de referencias cruzadas
                foreach (var p in _comicLoader.Pages)
                {
                    p.Thumbnail = null;
                }

                // Cargar en segundo plano las miniaturas del cómic activo con guardas de secuencia
                long startSeq = Interlocked.Increment(ref _thumbLoadSeq);
                var loaderRef = _comicLoader;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        int count = loaderRef?.Pages?.Count ?? 0;
                        int maxDegree = Math.Max(2, Math.Min(Environment.ProcessorCount, 6));
                        using var gate = new System.Threading.SemaphoreSlim(maxDegree);
                        var tasks = Enumerable.Range(0, count).Select(async i =>
                        {
                            await gate.WaitAsync();
                            try
                            {
                                var thumb = await loaderRef.GetPageThumbnailAsync(i, 180, 240);
                                var idx = i;
                                this.Dispatcher.Invoke(() =>
                                {
                                    if (!ReferenceEquals(_comicLoader, loaderRef)) return;
                                    if (Interlocked.Read(ref _thumbLoadSeq) != startSeq) return;
                                    if (idx >= 0 && idx < _comicLoader.Pages.Count)
                                        _comicLoader.Pages[idx].Thumbnail = thumb;
                                });
                            }
                            finally { gate.Release(); }
                        }).ToArray();
                        await Task.WhenAll(tasks);
                    }
                    catch { }
                });
            }
            catch { }
        }
        
        // Re-renderizar PDF actual con los nuevos ajustes (si aplica)
        public async void ReRenderCurrentPdfIfAny()
        {
            try
            {
                var path = _comicLoader?.FilePath;
                if (string.IsNullOrEmpty(path)) return;
                var ext = System.IO.Path.GetExtension(path)?.ToLowerInvariant();
                if (ext != ".pdf") return;
                // Guardar página actual para restaurar
                int page = _currentPageIndex;
                // Limpiar cache del loader y recargar
                await _comicLoader.LoadComicAsync(path);
                // Restaurar página si es válida
                if (page >= 0 && page < _comicLoader.Pages.Count)
                {
                    _currentPageIndex = page;
                }
                LoadCurrentPage();
                // Si está activa la vista continua, pedirle que recargue
                try { _continuousView?.ViewModel?.RequestVisiblePagesMaterialization(); } catch { }
            }
            catch (Exception ex)
            {
                try { MessageBox.Show($"No se pudo re-renderizar el PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning); } catch { }
            }
        }

        private void OpenSettingsDialog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SettingsDialog();
                dlg.Owner = this;
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo abrir Configuración: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowComicView()
        {
            bool useContinuous = SettingsManager.Settings?.EnableContinuousScroll == true;
            if (useContinuous)
            {
                _continuousView.ComicLoader = _comicLoader;
                if (this.FindName("MainContentArea") is ContentControl content)
                    content.Content = _continuousView;
                CurrentView = _continuousView;
            }
            else
            {
                if (_currentComicImage == null)
                {
                    _currentComicImage = new Image
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Stretch = System.Windows.Media.Stretch.Uniform
                    };
                    _currentComicImage.MouseLeftButtonDown += Image_MouseLeftButtonDown;
                    _currentComicImage.MouseLeftButtonUp += Image_MouseLeftButtonUp;
                    _currentComicImage.MouseMove += Image_MouseMove;
                    _currentComicImage.MouseEnter += Image_MouseEnter;
                    _currentComicImage.MouseLeave += Image_MouseLeave;
                    _readerCenterGrid = new Grid
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    _readerCenterGrid.Children.Add(_currentComicImage);
                    // Si se hace clic en el fondo (zonas en blanco), devolver el foco al lector
                    _readerCenterGrid.MouseDown += (s, e2) =>
                    {
                        try { _readerScrollViewer?.Focus(); Keyboard.Focus(_readerScrollViewer); } catch { }
                    };

                    _readerScrollViewer = new ScrollViewer
                    {
                        Content = _readerCenterGrid,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        CanContentScroll = false,
                        PanningMode = PanningMode.Both,
                        Focusable = true
                    };
                    _readerScrollViewer.PreviewMouseWheel += (s, eargs) =>
                    {
                        // Si hay contenido desplazable, deja que el ScrollViewer maneje la rueda
                        try
                        {
                            if (_readerScrollViewer != null && _readerScrollViewer.ScrollableHeight > 0.5)
                            {
                                // No marcar como handled para permitir el comportamiento por defecto del ScrollViewer
                                return;
                            }
                        }
                        catch { }
                    };
                    // Si el usuario hace clic en el área blanca del ScrollViewer, devolver foco al lector
                    _readerScrollViewer.PreviewMouseDown += (s, eargs) =>
                    {
                        try { _readerScrollViewer?.Focus(); Keyboard.Focus(_readerScrollViewer); } catch { }
                    };
                }
                if (this.FindName("MainContentArea") is ContentControl content)
                    content.Content = _readerScrollViewer;
                CurrentView = _readerScrollViewer;
            }
            // Mostrar barra del lector en modo lectura
            SetReaderTopBarVisible(true);
            _isComicOpen = true;
            OnPropertyChanged(nameof(IsComicViewActive));
            OnPropertyChanged(nameof(IsReaderViewActive));
            // Restaurar preferencia de miniaturas
            try
            {
                var wantThumbs = SettingsManager.Settings?.ThumbnailsVisible == true;
                if (wantThumbs)
                {
                    if (!_thumbnailsVisible)
                    {
                        ToggleThumbnails_Click(null, null);
                    }
                    else
                    {
                        // Ya está visible: refrescar la lista para este cómic
                        RefreshThumbnailPanelForCurrentComic();
                    }
                }
                else
                {
                    HideThumbnailsPanel();
                }
            }
            catch { }
            if (!useContinuous)
            {
                LoadCurrentPage();
            }

            // Aplicar ajuste por defecto después de cargar
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                var mode = SettingsManager.Settings?.DefaultFitMode?.ToLowerInvariant();
                switch (mode)
                {
                    case "height":
                        ApplyFitToHeight();
                        break;
                    case "screen":
                        FitToScreen();
                        break;
                    case "width":
                    default:
                        FitToWidth();
                        break;
                }
                _isNightMode = SettingsManager.Settings?.IsNightMode == true;
                _isReadingMode = SettingsManager.Settings?.IsReadingMode == true;
                ApplyReadingModeEffects();
                EnsureAutoAdvanceBehavior();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void SetReaderTopBarVisible(bool visible)
        {
            try
            {
                if (this.FindName("ReaderTopBar") is FrameworkElement topBar)
                {
                    topBar.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch { }
        }

        private void EnsureReaderScaffold()
        {
            bool useContinuous = SettingsManager.Settings?.EnableContinuousScroll == true;
            if (useContinuous)
            {
                _continuousView.ComicLoader = _comicLoader;
                // Aplicar espaciado configurado
                try { _continuousView.ItemSpacing = SettingsManager.Settings?.ReadingSpacing ?? 8; } catch { }
                if (this.FindName("MainContentArea") is ContentControl content)
                {
                    // Si el panel de lectura rápida está activo, mantenerlo como overlay en su ContentHost
                    if (content.Content == _readingCoreControl)
                    {
                        // No sustituir el control de overlay
                    }
                    else
                    {
                        content.Content = _continuousView;
                    }
                }
                CurrentView = _continuousView;
            }
            else
            {
                if (_currentComicImage == null)
                {
                    _currentComicImage = new Image
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Stretch = System.Windows.Media.Stretch.Uniform
                    };
                    _currentComicImage.MouseLeftButtonDown += Image_MouseLeftButtonDown;
                    _currentComicImage.MouseLeftButtonUp += Image_MouseLeftButtonUp;
                    _currentComicImage.MouseMove += Image_MouseMove;
                    _currentComicImage.MouseEnter += Image_MouseEnter;
                    _currentComicImage.MouseLeave += Image_MouseLeave;
                    _readerCenterGrid = new Grid
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    _readerCenterGrid.Children.Add(_currentComicImage);
                    _readerCenterGrid.MouseDown += (s, e2) =>
                    {
                        try { _readerScrollViewer?.Focus(); Keyboard.Focus(_readerScrollViewer); } catch { }
                    };

                    _readerScrollViewer = new ScrollViewer
                    {
                        Content = _readerCenterGrid,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        CanContentScroll = false,
                        PanningMode = PanningMode.Both,
                        Focusable = true
                    };
                    _readerScrollViewer.PreviewMouseWheel += (s, eargs) => { };
                    _readerScrollViewer.PreviewMouseDown += (s, eargs) =>
                    {
                        try { _readerScrollViewer?.Focus(); Keyboard.Focus(_readerScrollViewer); } catch { }
                    };
                }
                if (this.FindName("MainContentArea") is ContentControl content)
                {
                    if (content.Content == _readingCoreControl)
                    {
                        // dejar el overlay si está abierto
                    }
                    else
                    {
                        content.Content = _readerScrollViewer;
                    }
                }
                CurrentView = _readerScrollViewer;
            }
        }

        public void EnterReadingMode()
        {
            // No abre archivo aún. Solo muestra el andamiaje del lector y la barra superior.
            try { Interlocked.Increment(ref _pageLoadSeq); } catch { }
            try { Interlocked.Increment(ref _thumbLoadSeq); } catch { }
            // Resetear estado de cómic abierto
            _isComicOpen = false;
            _currentPageIndex = 0;
            try { _comicLoader?.ClearCurrent(); } catch { }
            try { SettingsManager.Settings.LastOpenedFilePath = null; SettingsManager.Settings.LastOpenedPage = 0; SettingsManager.SaveSettings(); } catch { }
            EnsureReaderScaffold();
            // Mostrar un placeholder amigable en el centro si no hay imagen
            try
            {
                if (_currentComicImage != null)
                {
                    _currentComicImage.Source = null;
                }
                if (_readerCenterGrid != null)
                {
                    // Agregar un texto de ayuda si aún no existe
                    var existing = _readerCenterGrid.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Tag as string == "ReaderPlaceholder");
                    if (existing == null)
                    {
                        var help = new TextBlock
                        {
                            Tag = "ReaderPlaceholder",
                            Text = "No hay cómic abierto. Usa '📂 Abrir' para cargar uno.",
                            Foreground = System.Windows.Media.Brushes.Gray,
                            FontSize = 18,
                            Margin = new Thickness(12),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Center
                        };
                        _readerCenterGrid.Children.Add(help);
                    }
                }
            }
            catch { }
            SetReaderTopBarVisible(true);
            // Mostrar el panel de ajustes rápidos si se desea
            try
            {
                if (SettingsManager.Settings?.ShowReadingQuickPanel == true)
                {
                    ShowReadingQuickPanel();
                }
            }
            catch { }
            // Limpiar indicador/slider para que no muestre la última página del cómic anterior
            try { UpdatePageIndicator(); } catch { }
            OnPropertyChanged(nameof(IsComicViewActive));
            OnPropertyChanged(nameof(IsReaderViewActive));
        }

        private void EnterReadingMode_Click(object sender, RoutedEventArgs e) => EnterReadingMode();

        private void RemoveReaderPlaceholder()
        {
            try
            {
                if (_readerCenterGrid != null)
                {
                    var existing = _readerCenterGrid.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Tag as string == "ReaderPlaceholder");
                    if (existing != null) _readerCenterGrid.Children.Remove(existing);
                }
            }
            catch { }
        }

        private void ToggleReadingQuickPanel_Click(object sender, RoutedEventArgs e)
        {
            try { ShowReadingQuickPanel(); } catch { }
        }

        // Mostrar/ocultar el panel flotante de configuración rápida del lector
        public void ShowReadingQuickPanel()
        {
            try
            {
                if (_readingCoreControl == null) return;
                // Asegurar que los valores actuales se reflejan
                try { _readingCoreControl.SetViewMode(SettingsManager.Settings?.EnableContinuousScroll == true ? "ContinuousScroll" : "SinglePage"); } catch { }
                try { _readingCoreControl.SetSpacing(SettingsManager.Settings?.ReadingSpacing ?? 8); } catch { }

                var host = this.FindName("ReadingQuickOverlayHost") as ContentControl;
                if (host != null)
                {
                    if (host.Content == _readingCoreControl)
                    {
                        host.Content = null;
                        host.Visibility = Visibility.Collapsed;
                        return;
                    }
                    host.Content = _readingCoreControl;
                    host.Visibility = Visibility.Visible;
                }
            }
            catch { }
        }

    private async void LoadCurrentPage()
        {
            // En modo continuo, la materialización la gestiona ContinuousComicView
            if (SettingsManager.Settings?.EnableContinuousScroll == true) return;
            if (_comicLoader.Pages.Count > 0 && _currentPageIndex >= 0 && _currentPageIndex < _comicLoader.Pages.Count)
            {
                try
                {
                    if (_currentComicImage != null)
                    {
                        if (SettingsManager.Settings?.ShowLoadingIndicators == true)
                        {
                            if (this.FindName("PageIndicator") is TextBlock piLoad) piLoad.Text = "Cargando...";
                        }
                        // Id de petición para descartar resultados obsoletos
                        var requestId = Interlocked.Increment(ref _pageLoadSeq);
                        // Suavizar el cambio: escalado LowQuality temporal
                        var prevScaling = System.Windows.Media.RenderOptions.GetBitmapScalingMode(_currentComicImage);
                        System.Windows.Media.RenderOptions.SetBitmapScalingMode(_currentComicImage, System.Windows.Media.BitmapScalingMode.LowQuality);
                        var bmp = await _comicLoader.GetPageImageAsync(_currentPageIndex);
                        // Si cambió la página durante la carga, descartar
                        if (requestId != Volatile.Read(ref _pageLoadSeq))
                        {
                            System.Windows.Media.RenderOptions.SetBitmapScalingMode(_currentComicImage, prevScaling);
                            return;
                        }
                        // Mantener el modelo actualizado
                        var page = _comicLoader.Pages[_currentPageIndex];
                        // Aplicar brillo/contraste si procede
                        var s = SettingsManager.Settings;
                        if (s != null && (Math.Abs(s.Brightness - 1.0) > 0.001 || Math.Abs(s.Contrast - 1.0) > 0.001))
                        {
                            try
                            {
                                var adjusted = ImageAdjuster.ApplyBrightnessContrast(bmp, s.Brightness, s.Contrast);
                                page.Image = adjusted as BitmapImage ?? bmp;
                                // Si no es BitmapImage, al menos mostrarla en el control
                                _currentComicImage.Source = adjusted;
                            }
                            catch { page.Image = bmp; }
                        }
                        else
                        {
                            page.Image = bmp;
                        }
                        // Transición suave: crear una superposición con la imagen anterior y desvanecerla
                        Image overlay = null;
                        try
                        {
                            if (_readerCenterGrid != null && _currentComicImage.Source != null)
                            {
                                overlay = new Image
                                {
                                    Source = _currentComicImage.Source,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    Stretch = _currentComicImage.Stretch,
                                    Opacity = 1.0
                                };
                                Panel.SetZIndex(overlay, 10);
                                _readerCenterGrid.Children.Add(overlay);
                            }
                        }
                        catch { }

                        // Cambiar a la nueva imagen (si no se cambió ya por ajuste)
                        if (_currentComicImage.Source == null || ReferenceEquals(_currentComicImage.Source, overlay?.Source))
                        {
                            _currentComicImage.Source = page.Image ?? bmp;
                        }
                        System.Windows.Media.RenderOptions.SetBitmapScalingMode(_currentComicImage, prevScaling);
                        
                        // Añadir un deslizamiento sutil según dirección de navegación
                        try
                        {
                            int dir = _lastNavDirection;
                            if (dir != 0)
                            {
                                // Ajustar por dirección de lectura
                                var readingDirRtl = SettingsManager.Settings?.CurrentReadingDirection == ReadingDirection.RightToLeft;
                                int visualDir = readingDirRtl ? -dir : dir; // en RTL, next va a la izquierda
                                var tt = new System.Windows.Media.TranslateTransform();
                                _currentComicImage.RenderTransform = new System.Windows.Media.TransformGroup
                                {
                                    Children = new System.Windows.Media.TransformCollection
                                    {
                                        new System.Windows.Media.ScaleTransform(_zoomFactor, _zoomFactor),
                                        tt
                                    }
                                };
                                _currentComicImage.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                                double fromX = visualDir > 0 ? 24 : -24;
                                tt.X = fromX;
                                var slide = new System.Windows.Media.Animation.DoubleAnimation
                                {
                                    From = fromX,
                                    To = 0,
                                    Duration = new Duration(TimeSpan.FromMilliseconds(160)),
                                    EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                                };
                                tt.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, slide);
                            }
                        }
                        catch { }

                        // Iniciar fundido de la superposición (si existe)
                        if (overlay != null)
                        {
                            try
                            {
                                var fade = new System.Windows.Media.Animation.DoubleAnimation
                                {
                                    From = 1.0,
                                    To = 0.0,
                                    Duration = new Duration(TimeSpan.FromMilliseconds(160)),
                                    EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
                                };
                                fade.Completed += (_, __) =>
                                {
                                    try { _readerCenterGrid.Children.Remove(overlay); } catch { }
                                };
                                overlay.BeginAnimation(UIElement.OpacityProperty, fade);
                            }
                            catch { try { _readerCenterGrid.Children.Remove(overlay); } catch { } }
                        }
                        UpdatePageIndicator();
                        ApplyZoomToImage();
                        ApplyReadingModeEffects();
                        // Registrar página vista (1-based) en estadísticas
                        _stats?.RecordPageViewed(_currentPageIndex + 1);
                        // Guardar progreso
                        SettingsManager.Settings.LastOpenedFilePath = _comicLoader.FilePath;
                        SettingsManager.Settings.LastOpenedPage = _currentPageIndex;
                        SettingsManager.SaveSettings();
                        
                        // Precargar páginas adyacentes para navegación más fluida
                        await PreloadAdjacentPages();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar la página: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task PreloadAdjacentPages()
        {
            if (SettingsManager.Settings?.EnableContinuousScroll == true) { await Task.CompletedTask; return; }
            try
            {
                // Usa la ventana de prefetch configurada (bidireccional) para preparar varias páginas alrededor
                _comicLoader.PreloadPages(_currentPageIndex);
            }
            catch { }
            await Task.CompletedTask;
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHomeView();
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Seleccionar Cómic",
                Filter = "Archivos de Cómic|*.cbz;*.cbr;*.pdf;*.epub|" +
                        "Archivos CBZ|*.cbz|" +
                        "Archivos CBR|*.cbr|" +
                        "Archivos PDF|*.pdf|" +
                        "Archivos EPUB|*.epub|" +
                        "Imágenes|*.jpg;*.jpeg;*.png;*.gif;*.bmp|" +
                        "Todos los archivos|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                OpenComicFile(openFileDialog.FileName);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSettingsView();
        }

    private async void OpenComicFile(string filePath)
        {
            try
            {
                // Invalida cualquier carga de páginas/miniaturas anterior
                Interlocked.Increment(ref _pageLoadSeq);
                Interlocked.Increment(ref _thumbLoadSeq);
                RemoveReaderPlaceholder();
                if (this.FindName("PageIndicator") is TextBlock pi) pi.Text = "Cargando...";
                await _comicLoader.LoadComicAsync(filePath);
                _comicLoader.RefreshTuningFromSettings();
                
                if (_comicLoader.Pages.Count > 0)
                {
                    // Reset de UI del lector por si venimos del menú
                    if (_currentComicImage == null)
                    {
                        _currentComicImage = new Image
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Stretch = System.Windows.Media.Stretch.Uniform
                        };
                        // Asegurar manejadores para pan/zoom
                        _currentComicImage.MouseLeftButtonDown += Image_MouseLeftButtonDown;
                        _currentComicImage.MouseLeftButtonUp += Image_MouseLeftButtonUp;
                        _currentComicImage.MouseMove += Image_MouseMove;
                        _currentComicImage.MouseEnter += Image_MouseEnter;
                        _currentComicImage.MouseLeave += Image_MouseLeave;
                    }
                    // Si el panel de miniaturas sigue abierto desde un cómic anterior, refrescar su contenido ahora
                    if (_thumbnailsVisible)
                    {
                        RefreshThumbnailPanelForCurrentComic();
                    }
                    if (_readerCenterGrid == null)
                    {
                        _readerCenterGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    }
                    _readerCenterGrid.Children.Clear();
                    _readerCenterGrid.Children.Add(_currentComicImage);
                    if (_readerScrollViewer == null)
                    {
                        _readerScrollViewer = new ScrollViewer
                        {
                            Content = _readerCenterGrid,
                            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                            CanContentScroll = false
                        };
                    }
                    else
                    {
                        _readerScrollViewer.Content = _readerCenterGrid;
                    }
                    // Determinar página inicial por progreso previo (servicio nuevo)
                    int startIndex = 0;
                    try
                    {
                        var existing = ComicReader.Services.ContinueReadingService.Instance.Items
                            .FirstOrDefault(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
                        if (existing != null)
                        {
                            int last1 = Math.Max(1, existing.LastPage);
                            startIndex = Math.Min(_comicLoader.Pages.Count - 1, last1 - 1);
                        }
                    }
                    catch { }
                    _currentPageIndex = Math.Max(0, Math.Min(_comicLoader.Pages.Count - 1, startIndex));
                    // Iniciar sesión de lectura
                    _stats?.StartSession(filePath, _comicLoader.ComicTitle, _comicLoader.Pages.Count);
                    
                    // Registrar en "Seguir leyendo" con la página correcta
                    ComicReader.Services.ContinueReadingService.Instance.UpsertProgress(filePath, _currentPageIndex + 1, _comicLoader.Pages.Count);
                    try { _homeView?.RefreshRecent(); } catch { }

                    ShowComicView();
                    // Normalizar UI de paginación al abrir correctamente
                    try { UpdatePageIndicator(); } catch { }
                    // Entrar automáticamente en modo inmersivo si está activado en ajustes
                    try
                    {
                        if (SettingsManager.Settings?.AutoEnterImmersiveOnOpen == true)
                        {
                            ToggleImmersiveFullScreen();
                        }
                    }
                    catch { }
                    // En modo continuo, desplazarse a la página guardada
                    if (SettingsManager.Settings?.EnableContinuousScroll == true && _continuousView != null)
                    {
                        _continuousView.ScrollToPage(_currentPageIndex);
                    }
                    EnsureAutoAdvanceBehavior();
                }
                else
                {
                    MessageBox.Show("No se pudieron cargar las páginas del cómic.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir el archivo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPageIndex > 0)
            {
                int target = _currentPageIndex - 1;
                _currentPageIndex = target;
                _lastNavDirection = -1;
                if (SettingsManager.Settings?.EnableContinuousScroll == true && _continuousView != null)
                {
                    _continuousView.ScrollToPage(target);
                    UpdatePageIndicator();
                }
                else
                {
                    // Cancelar carga anterior y cargar nueva
                    Interlocked.Increment(ref _pageLoadSeq);
                    LoadCurrentPage();
                    UpdatePageIndicator();
                    // Prefetch adicional direccional (dos páginas más atrás)
                    try
                    {
                        int p2 = _currentPageIndex - 1; // ya se precarga -1 en LoadCurrentPage
                        int p3 = _currentPageIndex - 2;
                        if (p3 >= 0) _ = _comicLoader.GetPageImageAsync(p3);
                        // si hay hueco, también una más (p4)
                        int p4 = _currentPageIndex - 3;
                        if (p4 >= 0) _ = _comicLoader.GetPageImageAsync(p4);
                    }
                    catch { }
                    // Si el panel de miniaturas está visible, solo sincronizar selección; evitar recarga masiva
                    if (_thumbnailsVisible)
                    {
                        try
                        {
                            var list = this.FindName("ThumbList") as System.Windows.Controls.ListBox;
                            if (list != null)
                            {
                                _suppressThumbListSelectionChange = true;
                                try
                                {
                                    list.ItemsSource = _comicLoader.Pages;
                                    list.SelectedIndex = _currentPageIndex;
                                    // Desplazar la miniatura actual a la vista
                                    list.ScrollIntoView(list.SelectedItem);
                                }
                                finally { _suppressThumbListSelectionChange = false; }
                            }
                        }
                        catch { }
                    }
                }
                // Actualizar progreso en servicio
                try { if (_isComicOpen) ComicReader.Services.ContinueReadingService.Instance.UpsertProgress(_comicLoader.FilePath, _currentPageIndex + 1, _comicLoader.PageCount); } catch { }
            }
        }

        public void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPageIndex < _comicLoader.Pages.Count - 1)
            {
                int target = _currentPageIndex + 1;
                _currentPageIndex = target;
                _lastNavDirection = 1;
                if (SettingsManager.Settings?.EnableContinuousScroll == true && _continuousView != null)
                {
                    _continuousView.ScrollToPage(target);
                    UpdatePageIndicator();
                }
                else
                {
                    // Cancelar carga anterior y cargar nueva
                    Interlocked.Increment(ref _pageLoadSeq);
                    LoadCurrentPage();
                    UpdatePageIndicator();
                    // Prefetch adicional direccional (dos/tres páginas más adelante)
                    try
                    {
                        int n2 = _currentPageIndex + 1; // ya se precarga +1 en LoadCurrentPage
                        int n3 = _currentPageIndex + 2;
                        if (n3 < _comicLoader.Pages.Count) _ = _comicLoader.GetPageImageAsync(n3);
                        int n4 = _currentPageIndex + 3;
                        if (n4 < _comicLoader.Pages.Count) _ = _comicLoader.GetPageImageAsync(n4);
                    }
                    catch { }
                }
                // Actualizar progreso en servicio y, si estamos en última página, eliminar de "Seguir leyendo"
                try
                {
                    if (_isComicOpen)
                    {
                        var oneBased = _currentPageIndex + 1;
                        ComicReader.Services.ContinueReadingService.Instance.UpsertProgress(_comicLoader.FilePath, oneBased, _comicLoader.PageCount);
                        if (oneBased >= _comicLoader.PageCount)
                        {
                            // UpsertProgress moves the item to CompletedItems when at 100%.
                            // No eliminar aquí (antes borrábamos). Solo refrescar la vista para mostrar el cambio.
                            _homeView?.RefreshRecent();
                        }
                    }
                }
                catch { }
            }
        }

        public void GoToPage_Click(object sender, RoutedEventArgs e)
        {
            if (!_isComicOpen || _comicLoader.Pages.Count == 0) return;

            var dialog = new GoToPageDialog(_comicLoader.Pages.Count, _currentPageIndex + 1);
            if (dialog.ShowDialog() == true)
            {
                int targetPage = dialog.SelectedPage - 1; // Convert to 0-based index
                if (targetPage >= 0 && targetPage < _comicLoader.Pages.Count)
                {
                    _lastNavDirection = targetPage > _currentPageIndex ? 1 : (targetPage < _currentPageIndex ? -1 : 0);
                    _currentPageIndex = targetPage;
                    if (SettingsManager.Settings?.EnableContinuousScroll == true && _continuousView != null)
                    {
                        // Desplazar en modo continuo
                        _continuousView.ScrollToPage(targetPage);
                    }
                    else
                    {
                        // Modo clásico
                        LoadCurrentPage();
                    }
                    UpdatePageIndicator();
                    // Actualizar progreso y manejar finalización
                    try
                    {
                        var oneBased = _currentPageIndex + 1;
                        ComicReader.Services.ContinueReadingService.Instance.UpsertProgress(_comicLoader.FilePath, oneBased, _comicLoader.PageCount);
                        if (oneBased >= _comicLoader.PageCount)
                        {
                            // UpsertProgress handles completed state. Solo refrescar vista.
                            _homeView?.RefreshRecent();
                        }
                    }
                    catch { }
                }
            }
        }

        private void UpdatePageIndicator()
        {
            bool noComic = !_isComicOpen || _comicLoader == null || _comicLoader.Pages == null || _comicLoader.Pages.Count == 0;
            if (this.FindName("PageIndicator") is TextBlock pi2)
            {
                if (SettingsManager.Settings?.ShowPageNumberOverlay == true)
                {
                    pi2.Visibility = Visibility.Visible;
                    pi2.Text = noComic ? "— / —" : $"Página {_currentPageIndex + 1} de {_comicLoader.Pages.Count}";
                }
                else
                {
                    pi2.Visibility = Visibility.Collapsed;
                }
            }
            if (this.FindName("PageSlider") is Slider slider)
            {
                if (noComic)
                {
                    slider.Minimum = 1;
                    slider.Maximum = 1;
                    slider.Value = 1;
                    slider.IsEnabled = false;
                }
                else
                {
                    slider.Minimum = 1;
                    slider.Maximum = Math.Max(1, _comicLoader.Pages.Count);
                    slider.Value = _currentPageIndex + 1;
                    slider.IsEnabled = true;
                }
            }
            // Mantener sincronizada la selección del panel de miniaturas si está visible
            if (_thumbnailsVisible)
            {
                try
                {
                    var list = this.FindName("ThumbList") as System.Windows.Controls.ListBox;
                    if (list != null && ReferenceEquals(list.ItemsSource, _comicLoader.Pages))
                    {
                        _suppressThumbListSelectionChange = true;
                        try
                        {
                            list.SelectedIndex = Math.Max(0, Math.Min(_currentPageIndex, _comicLoader.Pages.Count - 1));
                            list.ScrollIntoView(list.SelectedItem);
                        }
                        finally { _suppressThumbListSelectionChange = false; }
                    }
                }
                catch { }
            }
        }

        private bool _isSliderChanging = false;
        private void PageSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isSliderChanging) return;
            if (!_isComicOpen || _comicLoader.Pages.Count == 0) return;

            int newPage = (int)Math.Round(e.NewValue) - 1;
            newPage = Math.Max(0, Math.Min(_comicLoader.Pages.Count - 1, newPage));
            if (newPage != _currentPageIndex)
            {
                _currentPageIndex = newPage;
                if (SettingsManager.Settings?.EnableContinuousScroll == true && _continuousView != null)
                {
                    _continuousView.ScrollToPage(newPage);
                }
                else
                {
                    LoadCurrentPage();
                }
                _isSliderChanging = true;
                try { UpdatePageIndicator(); } finally { _isSliderChanging = false; }
                // Actualizar progreso en servicio y manejar finalización
                try
                {
                    if (_isComicOpen)
                    {
                        var oneBased = _currentPageIndex + 1;
                        ComicReader.Services.ContinueReadingService.Instance.UpsertProgress(_comicLoader.FilePath, oneBased, _comicLoader.PageCount);
                        if (oneBased >= _comicLoader.PageCount)
                        {
                            // UpsertProgress moves to completed; solo refrescar la vista.
                            _homeView?.RefreshRecent();
                        }
                    }
                }
                catch { }
            }
        }

        public void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            AdjustZoom(1.25); // Incremento de 25%
        }

        public void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            AdjustZoom(0.8); // Decremento de 20%
        }

        public void ZoomReset_Click(object sender, RoutedEventArgs e)
        {
            SetZoom(1.0);
        }

        public void Zoom25_Click(object sender, RoutedEventArgs e) => SetZoom(0.25);
        public void Zoom50_Click(object sender, RoutedEventArgs e) => SetZoom(0.5);
        public void Zoom75_Click(object sender, RoutedEventArgs e) => SetZoom(0.75);
        public void Zoom100_Click(object sender, RoutedEventArgs e) => SetZoom(1.0);
        public void Zoom125_Click(object sender, RoutedEventArgs e) => SetZoom(1.25);
        public void Zoom150_Click(object sender, RoutedEventArgs e) => SetZoom(1.5);
        public void Zoom200_Click(object sender, RoutedEventArgs e) => SetZoom(2.0);
        public void Zoom400_Click(object sender, RoutedEventArgs e) => SetZoom(4.0);

        private void AdjustZoom(double factor)
        {
            double newZoom = _zoomFactor * factor;
            SetZoom(newZoom);
        }

        private void SetZoom(double newZoom)
        {
            // Limitar zoom entre 10% y 800%
            _zoomFactor = Math.Max(0.1, Math.Min(8.0, newZoom));
            ApplyZoomToImage();
            UpdateZoomIndicator();
            _lastZoomChange = DateTime.UtcNow;
        }

    private double _lastZoomFactor = 1.0;
    private DateTime _lastZoomChange = DateTime.MinValue;
    private static readonly TimeSpan _interactionGrace = TimeSpan.FromSeconds(2);

        private void ApplyZoomToImage()
        {
            if (_currentComicImage != null)
            {
                // Capturar centro relativo antes del cambio de zoom
                ScrollViewer sv = null;
                if (_currentComicImage.Parent is Grid g && g.Parent is ScrollViewer sv1) sv = sv1;
                double centerRel = 0.0;
                double prevExtent = 0.0;
                if (sv != null)
                {
                    prevExtent = sv.ExtentHeight;
                    var oldCenter = sv.VerticalOffset + sv.ViewportHeight / 2.0;
                    centerRel = prevExtent > 0 ? (oldCenter / prevExtent) : 0.0;
                }

                var transformGroup = new System.Windows.Media.TransformGroup();
                
                // Aplicar zoom
                transformGroup.Children.Add(new System.Windows.Media.ScaleTransform(_zoomFactor, _zoomFactor));
                
                // Aplicar rotación si existe
                if (_rotationAngle != 0)
                {
                    transformGroup.Children.Add(new System.Windows.Media.RotateTransform(_rotationAngle));
                }
                
                _currentComicImage.RenderTransform = transformGroup;
                _currentComicImage.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                
                // Reposicionar manteniendo el mismo centro relativo tras el nuevo layout
                if (sv != null)
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            var newExtent = sv.ExtentHeight;
                            var desiredCenter = newExtent * centerRel;
                            var targetOffset = Math.Max(0, desiredCenter - sv.ViewportHeight / 2.0);
                            // Limitar para no exceder el contenido
                            var maxOffset = Math.Max(0, newExtent - sv.ViewportHeight);
                            sv.ScrollToVerticalOffset(Math.Min(targetOffset, maxOffset));
                        }
                        catch { }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }

                _lastZoomFactor = _zoomFactor;
            }
        }

        private void UpdateZoomIndicator()
        {
            // El indicador de zoom se actualizará cuando se implemente en el XAML
            // Por ahora guardamos el valor para futuro uso
            System.Diagnostics.Debug.WriteLine($"Zoom: {(_zoomFactor * 100):F0}%");
        }

        // Mejora: zoom con Ctrl + rueda del ratón
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Delta > 0) AdjustZoom(1.1);
                else AdjustZoom(0.9);
                e.Handled = true;
            }
            else
            {
                // Navegación con rueda del mouse si no hay Ctrl; opcionalmente invertida
                if (_isComicOpen && _comicLoader.Pages.Count > 0 && SettingsManager.Settings?.EnableContinuousScroll != true)
                {
                    int delta = e.Delta;
                    bool invert = SettingsManager.Settings?.InvertScrollWheel == true;
                    if ((delta > 0 && !invert) || (delta < 0 && invert))
                        PrevPage_Click(null, null);
                    else if ((delta < 0 && !invert) || (delta > 0 && invert))
                        NextPage_Click(null, null);
                    e.Handled = true;
                }
            }
            base.OnPreviewMouseWheel(e);
        }

        // Desplaza dentro de la página actual. Devuelve true si se desplazó, false si ya estaba en el límite.
        private bool ScrollWithinPage(bool down)
        {
            try
            {
                if (_readerScrollViewer == null) return false;
                var sv = _readerScrollViewer;
                // Altura que se puede desplazar
                double maxOffset = Math.Max(0, sv.ScrollableHeight);
                double cur = sv.VerticalOffset;
                if (maxOffset < 0.5)
                {
                    // No hay scroll vertical disponible
                    return false;
                }

                // Paso configurable: proporción del alto visible
                double ratio = SettingsManager.Settings?.PageScrollStepRatio > 0 ? SettingsManager.Settings.PageScrollStepRatio : 0.9;
                double step = Math.Max(24, sv.ViewportHeight * ratio);
                double target = down ? Math.Min(maxOffset, cur + step) : Math.Max(0, cur - step);
                // Si no hay cambio efectivo, estamos en el límite
                if (Math.Abs(target - cur) < 0.5)
                {
                    return false;
                }
                sv.ScrollToVerticalOffset(target);
                return true;
            }
            catch { return false; }
        }

        // Desplaza en modo continuo dentro del ScrollViewer interno.
        // Devuelve true si se produjo desplazamiento.
        private bool ScrollContinuousWithinView(bool down)
        {
            try
            {
                if (_continuousView == null) return false;
                // El ScrollViewer de la vista continua se llama "ContentScroll"
                var svObj = _continuousView.FindName("ContentScroll") as ScrollViewer;
                if (svObj == null) return false;

                double maxOffset = Math.Max(0, svObj.ScrollableHeight);
                double cur = svObj.VerticalOffset;
                if (maxOffset < 0.5)
                {
                    return false;
                }
                double ratio = SettingsManager.Settings?.PageScrollStepRatio > 0 ? SettingsManager.Settings.PageScrollStepRatio : 0.9;
                double step = Math.Max(24, svObj.ViewportHeight * ratio);
                double target = down ? Math.Min(maxOffset, cur + step) : Math.Max(0, cur - step);
                if (Math.Abs(target - cur) < 0.5) return false;
                svObj.ScrollToVerticalOffset(target);
                return true;
            }
            catch { return false; }
        }

        private void FitScreen_Click(object sender, RoutedEventArgs e)
        {
            FitToScreen();
        }

        private void FitToScreen()
        {
            if (_currentComicImage?.Parent is ScrollViewer scrollViewer && _currentComicImage.Source != null)
            {
                var imageSource = _currentComicImage.Source;
                double imageWidth = imageSource.Width;
                double imageHeight = imageSource.Height;
                double containerWidth = scrollViewer.ActualWidth - scrollViewer.Padding.Left - scrollViewer.Padding.Right;
                double containerHeight = scrollViewer.ActualHeight - scrollViewer.Padding.Top - scrollViewer.Padding.Bottom;
                // Calcular factor de escala que permita que toda la imagen sea visible
                double scaleX = containerWidth / imageWidth;
                double scaleY = containerHeight / imageHeight;
                
                _zoomFactor = Math.Min(scaleX, scaleY);
                ApplyZoomToImage();
                UpdateZoomIndicator();
                _lastZoomChange = DateTime.UtcNow;
            }
        }

        private void FitToWidth()
        {
            if (_currentComicImage?.Parent is ScrollViewer scrollViewer && _currentComicImage.Source != null)
            {
                var imageSource = _currentComicImage.Source;
                double imageWidth = imageSource.Width;
                double containerWidth = scrollViewer.ActualWidth - scrollViewer.Padding.Left - scrollViewer.Padding.Right;
                
                _zoomFactor = containerWidth / imageWidth;
                ApplyZoomToImage();
                UpdateZoomIndicator();
                _lastZoomChange = DateTime.UtcNow;
            }
        }

        private void ApplyFitToHeight()
        {
            if (_currentComicImage?.Parent is ScrollViewer scrollViewer && _currentComicImage.Source != null)
            {
                var imageSource = _currentComicImage.Source;
                double imageHeight = imageSource.Height;
                
                double containerHeight = scrollViewer.ActualHeight - scrollViewer.Padding.Top - scrollViewer.Padding.Bottom;
                
                _zoomFactor = containerHeight / imageHeight;
                ApplyZoomToImage();
                UpdateZoomIndicator();
                _lastZoomChange = DateTime.UtcNow;
            }
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    OpenComicFile(files[0]);
                }
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (!_isComicOpen) return;

            // Atajos básicos de navegación
            switch (e.Key)
            {
                case Key.Up:
                    if (SettingsManager.Settings?.EnableContinuousScroll == true)
                    {
                        // En modo continuo: forzar scroll del visor aunque el foco no esté dentro
                        if (ScrollContinuousWithinView(down: false)) { e.Handled = true; return; }
                        // si no hay desplazamiento posible, dejamos seguir para otras teclas
                    }
                    if (!ScrollWithinPage(down: false))
                    {
                        // Ya está en el tope: ir a página anterior y posicionar al fondo
                        PrevPage_Click(null, null);
                        // Ajustar al final tras cargar
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                if (_readerScrollViewer != null)
                                {
                                    _readerScrollViewer.ScrollToVerticalOffset(Math.Max(0, _readerScrollViewer.ScrollableHeight));
                                }
                            }
                            catch { }
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    }
                    e.Handled = true;
                    break;
                case Key.Down:
                    if (SettingsManager.Settings?.EnableContinuousScroll == true)
                    {
                        // En modo continuo: forzar scroll del visor aunque el foco no esté dentro
                        if (ScrollContinuousWithinView(down: true)) { e.Handled = true; return; }
                        // si no hay desplazamiento posible, dejamos seguir para otras teclas
                    }
                    if (!ScrollWithinPage(down: true))
                    {
                        // Ya está al fondo: ir a página siguiente y posicionar arriba
                        NextPage_Click(null, null);
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try { _readerScrollViewer?.ScrollToVerticalOffset(0); } catch { }
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    }
                    e.Handled = true;
                    break;
                case Key.Left:
                case Key.PageUp:
                case Key.A:
                case Key.K:
                    if (SettingsManager.Settings?.CurrentReadingDirection == ReadingDirection.RightToLeft) NextPage_Click(null, null); else PrevPage_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.Right:
                case Key.PageDown:
                case Key.Space:
                case Key.D:
                case Key.J:
                    // Respetar preferencia de usar barra espaciadora para avanzar
                    if (e.Key != Key.Space || SettingsManager.Settings?.SpacebarNextPage != false)
                    {
                        if (SettingsManager.Settings?.CurrentReadingDirection == ReadingDirection.RightToLeft) PrevPage_Click(null, null); else NextPage_Click(null, null);
                        e.Handled = true;
                    }
                    break;
                case Key.Home:
                    _currentPageIndex = 0;
                    if (SettingsManager.Settings?.EnableContinuousScroll == true && _continuousView != null)
                        _continuousView.ScrollToPage(0);
                    else
                    {
                        Interlocked.Increment(ref _pageLoadSeq);
                        LoadCurrentPage();
                    }
                    UpdatePageIndicator();
                    // Guardar progreso en Home
                    try { ComicReader.Services.ContinueReadingService.Instance.UpsertProgress(_comicLoader.FilePath, _currentPageIndex + 1, _comicLoader.PageCount); } catch { }
                    e.Handled = true;
                    break;
                case Key.End:
                    _currentPageIndex = _comicLoader.Pages.Count - 1;
                    if (SettingsManager.Settings?.EnableContinuousScroll == true && _continuousView != null)
                        _continuousView.ScrollToPage(_currentPageIndex);
                    else
                    {
                        Interlocked.Increment(ref _pageLoadSeq);
                        LoadCurrentPage();
                    }
                    UpdatePageIndicator();
                    // Guardar progreso y eliminar si es última página
                    try
                    {
                        var oneBased = _currentPageIndex + 1;
                        ComicReader.Services.ContinueReadingService.Instance.UpsertProgress(_comicLoader.FilePath, oneBased, _comicLoader.PageCount);
                        if (oneBased >= _comicLoader.PageCount)
                        {
                            // UpsertProgress migrará a completados; solo refrescar la vista
                            _homeView?.RefreshRecent();
                        }
                    }
                    catch { }
                    e.Handled = true;
                    break;
            }

            // Atajos con Ctrl
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.OemPlus:
                    case Key.Add:
                        ZoomIn_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.OemMinus:
                    case Key.Subtract:
                        ZoomOut_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.D0:
                    case Key.NumPad0:
                        ZoomReset_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.G:
                        GoToPage_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.R:
                        Rotate_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.F:
                        FitToScreen();
                        e.Handled = true;
                        break;
                    case Key.C:
                        // Centrar la página actual en el viewport
                        if (_currentComicImage?.Parent is Grid g && g.Parent is ScrollViewer sv)
                        {
                            var center = (sv.ExtentHeight - sv.ViewportHeight) / 2.0;
                            sv.ScrollToVerticalOffset(Math.Max(0, center));
                        }
                        e.Handled = true;
                        break;
                }
            }

            // Teclas especiales
            switch (e.Key)
            {
                case Key.F12:
                    ToggleImmersiveFullScreen();
                    e.Handled = true;
                    break;
                case Key.N:
                    ToggleNightMode_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.M:
                    ToggleReadingMode_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.S:
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
                    {
                        SepiaMode_Click(null, null);
                        e.Handled = true;
                    }
                    break;
                // Shift+C reservado previamente para alto contraste
                case Key.C:
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
                    {
                        HighContrastMode_Click(null, null);
                        e.Handled = true;
                    }
                    break;
                case Key.T:
                    ToggleThumbnails_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    if (_isImmersive)
                    {
                        ToggleImmersiveFullScreen();
                        e.Handled = true;
                    }
                    break;
            }
        }

        // Captura previa de teclas para cuando el foco está en zonas en blanco o no interactivas
        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Si ya fue manejado por algún control, no hacer nada
            if (e.Handled) return;
            // Reutilizar la lógica principal de teclas para navegación
            MainWindow_KeyDown(sender, e);
        }

        protected override void OnClosed(EventArgs e)
        {
            // Cerrar sesión de lectura si está activa y actualizar historial final
            try
            {
                if (_isComicOpen && _comicLoader != null && !string.IsNullOrEmpty(_comicLoader.FilePath))
                {
                    ComicReader.Services.ContinueReadingService.Instance.UpsertProgress(_comicLoader.FilePath, _currentPageIndex + 1, _comicLoader.PageCount);
                }
            }
            catch { }
            _stats?.EndSession();
            // Persistir estado de ventana al salir
            try
            {
                SettingsManager.Settings.LastWindowState = this.WindowState;
                if (this.WindowState == WindowState.Normal)
                {
                    SettingsManager.Settings.LastWindowWidth = this.ActualWidth;
                    SettingsManager.Settings.LastWindowHeight = this.ActualHeight;
                }
            }
            catch { }
            SettingsManager.SaveSettings();
            base.OnClosed(e);
        }

        // Event Handlers para la barra de título
        private void DragWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Doble clic en la barra: alternar maximizar/restaurar
                if (e.ClickCount == 2)
                {
                    MaximizeRestore_Click(sender, e);
                    return;
                }

                // No arrastrar si el click fue sobre un botón u otro control interactivo
                if (e.OriginalSource is DependencyObject dobj)
                {
                    if (FindAncestor<System.Windows.Controls.Button>(dobj) != null)
                        return;
                }

                // Si está maximizada, restaurar y posicionar bajo el cursor para arrastrar
                if (this.WindowState == WindowState.Maximized)
                {
                    // Porcentaje horizontal donde se agarró
                    double percentX = e.GetPosition(this).X / this.ActualWidth;

                    // Posición del cursor en pantalla
                    var screenPoint = PointToScreen(e.GetPosition(this));

                    SaveCurrentWindowPosition(); // guarda valores actuales antes de restaurar
                    RestoreWindowFromMaximized();

                    // Recolocar para que el punto de agarre quede bajo el cursor
                    this.Left = screenPoint.X - this.Width * percentX;
                    this.Top = screenPoint.Y - 10; // pequeño offset hacia arriba
                }

                // Permitir arrastrar
                try { DragMove(); }
                catch { /* ignora errores durante drag */ }
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            try { SystemCommands.MinimizeWindow(this); } catch { }
        }

        private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                try { SystemCommands.RestoreWindow(this); } catch { RestoreWindowFromMaximized(); }
            }
            else
            {
                // Maximizar ventana
                SaveCurrentWindowPosition();
                try { SystemCommands.MaximizeWindow(this); } catch { WindowState = WindowState.Maximized; }
            }
        }

        private void RestoreWindowFromMaximized()
        {
            // Paso 1: Cambiar a estado Normal
            WindowState = WindowState.Normal;
            
            // Paso 2: Forzar valores inmediatamente
            if (_windowStateInitialized && _normalLeft >= 0 && _normalTop >= 0)
            {
                // Aplicar tamaño y posición inmediatamente
                this.Left = _normalLeft;
                this.Top = _normalTop;
                this.Width = _normalWidth;
                this.Height = _normalHeight;
            }
            else
            {
                // Primera vez - centrar
                var workingArea = SystemParameters.WorkArea;
                this.Width = 1000;
                this.Height = 700;
                this.Left = workingArea.Left + (workingArea.Width - this.Width) / 2;
                this.Top = workingArea.Top + (workingArea.Height - this.Height) / 2;
                
                // Guardar estos valores
                _normalLeft = this.Left;
                _normalTop = this.Top;
                _normalWidth = this.Width;
                _normalHeight = this.Height;
                _windowStateInitialized = true;
            }
            
            // Paso 3: Usar UpdateLayout para forzar la actualización
            this.UpdateLayout();
            
            // Paso 4: Aplicar valores nuevamente después del layout
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.Left = _normalLeft;
                this.Top = _normalTop;
                this.Width = _normalWidth;
                this.Height = _normalHeight;
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void SaveCurrentWindowPosition()
        {
            if (WindowState == WindowState.Normal)
            {
                _normalLeft = this.Left;
                _normalTop = this.Top;
                _normalWidth = this.Width;
                _normalHeight = this.Height;
                _windowStateInitialized = true;
                
                // Log para debug
                System.Diagnostics.Debug.WriteLine($"Guardado: L={_normalLeft}, T={_normalTop}, W={_normalWidth}, H={_normalHeight}");
            }
        }

        private void RestoreWindowPosition()
        {
            if (_windowStateInitialized && _normalLeft >= 0 && _normalTop >= 0)
            {
                // Verificar que la posición esté dentro de los límites de pantalla
                var workingArea = SystemParameters.WorkArea;
                
                var targetLeft = _normalLeft;
                var targetTop = _normalTop;
                var targetWidth = _normalWidth;
                var targetHeight = _normalHeight;
                
                // Ajustar si está fuera de límites
                if (targetLeft < workingArea.Left) targetLeft = workingArea.Left + 50;
                if (targetTop < workingArea.Top) targetTop = workingArea.Top + 50;
                if (targetLeft + targetWidth > workingArea.Right) targetLeft = workingArea.Right - targetWidth - 50;
                if (targetTop + targetHeight > workingArea.Bottom) targetTop = workingArea.Bottom - targetHeight - 50;
                
                // Aplicar nueva posición y tamaño
                Left = targetLeft;
                Top = targetTop;
                Width = targetWidth;
                Height = targetHeight;
            }
            else
            {
                // Centrar en pantalla si no hay posición guardada
                CenterWindowOnScreen();
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            // Dejar que el sistema gestione el tamaño al maximizar para evitar glitches
            if (WindowState == WindowState.Maximized)
            {
                // Nada extra aquí; XAML y estilos mantienen la barra visible
            }

            // Actualizar icono del botón de maximizar/restaurar
            // Actualizar icono del botón si el elemento existe
            var btn = this.TryFindResource("MaxRestoreButton"); // fallback si no está generado

            // Guardar estado actual en Settings
            try
            {
                SettingsManager.Settings.LastWindowState = this.WindowState;
                if (this.WindowState == WindowState.Normal)
                {
                    SettingsManager.Settings.LastWindowWidth = this.ActualWidth;
                    SettingsManager.Settings.LastWindowHeight = this.ActualHeight;
                }
                SettingsManager.SaveSettings();
            }
            catch { }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match)
                    return match;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void CenterWindowOnScreen()
        {
            var workingArea = SystemParameters.WorkArea;
            
            // Establecer tamaño razonable
            Width = Math.Min(1000, workingArea.Width * 0.8);
            Height = Math.Min(700, workingArea.Height * 0.8);
            
            // Centrar perfectamente
            Left = workingArea.Left + (workingArea.Width - Width) / 2;
            Top = workingArea.Top + (workingArea.Height - Height) / 2;
            
            // Actualizar variables
            _normalLeft = Left;
            _normalTop = Top;
            _normalWidth = Width;
            _normalHeight = Height;
            _windowStateInitialized = true;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            try { SystemCommands.CloseWindow(this); } catch { Close(); }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Seleccionar carpeta con imágenes de cómic";
            dialog.UseDescriptionForTitle = true;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OpenComicFile(dialog.SelectedPath);
            }
        }

        

        public void Rotate_Click(object sender, RoutedEventArgs e)
        {
            _rotationAngle = (_rotationAngle + 90) % 360;
            ApplyZoomToImage();
            
            // Ajustar el ajuste después de la rotación para mantener la imagen visible
            if (_rotationAngle == 90 || _rotationAngle == 270)
            {
                // En 90° y 270°, la imagen está rotada, ajustar según el modo actual
                var currentFitMode = GetCurrentFitMode();
                if (currentFitMode == "width")
                {
                    ApplyFitToHeight(); // Cambiar a fit height cuando rotamos
                }
                else if (currentFitMode == "height")
                {
                    FitToWidth(); // Cambiar a fit width cuando rotamos
                }
            }
        }

        public void RotateLeft_Click(object sender, RoutedEventArgs e)
        {
            _rotationAngle = (_rotationAngle - 90 + 360) % 360;
            ApplyZoomToImage();
        }

        public void RotateReset_Click(object sender, RoutedEventArgs e)
        {
            _rotationAngle = 0;
            ApplyZoomToImage();
        }

        private string GetCurrentFitMode()
        {
            // Determinar el modo de ajuste actual basado en el zoom factor
            if (_currentComicImage?.Parent is ScrollViewer scrollViewer && _currentComicImage.Source != null)
            {
                var imageSource = _currentComicImage.Source;
                double containerWidth = scrollViewer.ActualWidth;
                double containerHeight = scrollViewer.ActualHeight;
                
                double widthRatio = containerWidth / imageSource.Width;
                double heightRatio = containerHeight / imageSource.Height;
                
                if (Math.Abs(_zoomFactor - widthRatio) < 0.01)
                    return "width";
                else if (Math.Abs(_zoomFactor - heightRatio) < 0.01)
                    return "height";
                else if (Math.Abs(_zoomFactor - Math.Min(widthRatio, heightRatio)) < 0.01)
                    return "screen";
            }
            return "custom";
        }

        public void ToggleNightMode_Click(object sender, RoutedEventArgs e)
        {
            _isNightMode = !_isNightMode;
            SettingsManager.Settings.IsNightMode = _isNightMode;
            SettingsManager.SaveSettings();
            ApplyReadingModeEffects();
        }

        public void ToggleReadingMode_Click(object sender, RoutedEventArgs e)
        {
            _isReadingMode = !_isReadingMode;
            SettingsManager.Settings.IsReadingMode = _isReadingMode;
            SettingsManager.SaveSettings();
            ApplyReadingModeEffects();
        }

        public void SepiaMode_Click(object sender, RoutedEventArgs e)
        {
            ToggleSepiaMode();
        }

        public void HighContrastMode_Click(object sender, RoutedEventArgs e)
        {
            ToggleHighContrastMode();
        }

        private bool _sepiaMode = false;
        private bool _highContrastMode = false;

        private void ToggleSepiaMode()
        {
            _sepiaMode = !_sepiaMode;
            ApplyReadingModeEffects();
        }

        private void ToggleHighContrastMode()
        {
            _highContrastMode = !_highContrastMode;
            ApplyReadingModeEffects();
        }

        private void ApplyReadingModeEffects()
        {
            if (_currentComicImage == null) return;

            // Implementación simplificada de efectos usando opacidad y filtros básicos
            var transformGroup = new System.Windows.Media.TransformGroup();
            
            // Aplicar zoom y rotación existentes
            transformGroup.Children.Add(new System.Windows.Media.ScaleTransform(_zoomFactor, _zoomFactor));
            if (_rotationAngle != 0)
            {
                transformGroup.Children.Add(new System.Windows.Media.RotateTransform(_rotationAngle));
            }
            
            _currentComicImage.RenderTransform = transformGroup;

            // Cambiar el fondo según el modo usando colores básicos
            if (_isNightMode)
            {
                this.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 20, 20));
                _currentComicImage.Opacity = 1.0;
            }
            else if (_sepiaMode)
            {
                this.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 230, 210));
                _currentComicImage.Opacity = 1.0;
            }
            else
            {
                this.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                _currentComicImage.Opacity = 1.0;
                _currentComicImage.Effect = null;
            }
        }

        

    // Eliminado: soporte de pantalla completa estándar (solo se mantiene modo inmersivo)

        private void EnsureReaderInputFocus()
        {
            try
            {
                // Priorizar enfocar el ScrollViewer del lector si existe
                if (_readerScrollViewer != null)
                {
                    _readerScrollViewer.Focus();
                }
                else
                {
                    this.Focus();
                }
                this.Activate();
                Keyboard.Focus(_readerScrollViewer ?? (IInputElement)this);
            }
            catch { }
        }

        // Eliminado: ToggleFullScreen y UI asociada

        private void ToggleThumbnails_Click(object sender, RoutedEventArgs e)
        {
            _thumbnailsVisible = !_thumbnailsVisible;
            var panel = this.FindName("ThumbPanel") as FrameworkElement;
            var col = this.FindName("ThumbCol") as System.Windows.Controls.ColumnDefinition;
            var list = this.FindName("ThumbList") as System.Windows.Controls.ListBox;
            if (_thumbnailsVisible)
            {
                // Mostrar el panel de inmediato
                if (panel != null) panel.Visibility = Visibility.Visible;
                if (col != null) col.Width = new GridLength(260);
                if (list != null)
                {
                    // Forzar rebind limpiando primero
                    _suppressThumbListSelectionChange = true;
                    try
                    {
                        list.ItemsSource = null;
                        list.Items.Clear();
                        list.ItemsSource = _comicLoader?.Pages;
                        list.SelectedIndex = _currentPageIndex;
                        list.ScrollIntoView(list.SelectedItem);
                    }
                    finally { _suppressThumbListSelectionChange = false; }
                    // Cargar miniaturas en segundo plano para no bloquear la apertura del panel
                    long startSeq = Interlocked.Increment(ref _thumbLoadSeq);
                    var loaderRef = _comicLoader;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            int count = loaderRef?.Pages?.Count ?? 0;
                            int maxDegree = Math.Max(2, Math.Min(Environment.ProcessorCount, 6));
                            using var gate = new System.Threading.SemaphoreSlim(maxDegree);
                            var tasks = Enumerable.Range(0, count).Select(async i =>
                            {
                                await gate.WaitAsync();
                                try
                                {
                                    var thumb = await loaderRef.GetPageThumbnailAsync(i, 180, 240);
                                    var idx = i;
                                    this.Dispatcher.Invoke(() =>
                                    {
                                        // Evitar escribir si cambió el cómic o la secuencia
                                        if (!ReferenceEquals(_comicLoader, loaderRef)) return;
                                        if (Interlocked.Read(ref _thumbLoadSeq) != startSeq) return;
                                        if (idx >= 0 && idx < _comicLoader.Pages.Count)
                                            _comicLoader.Pages[idx].Thumbnail = thumb;
                                    });
                                }
                                finally { gate.Release(); }
                            }).ToArray();
                            await Task.WhenAll(tasks);
                        }
                        catch { }
                    });
                }
                try { SettingsManager.Settings.ThumbnailsVisible = true; SettingsManager.SaveSettings(); } catch { }
            }
            else
            {
                if (panel != null) panel.Visibility = Visibility.Collapsed;
                if (col != null) col.Width = new GridLength(0);
                if (list != null)
                {
                    list.ItemsSource = null;
                    list.SelectedIndex = -1;
                }
                // Invalida cargas de miniaturas en curso
                try { Interlocked.Increment(ref _thumbLoadSeq); } catch { }
                try { SettingsManager.Settings.ThumbnailsVisible = false; SettingsManager.SaveSettings(); } catch { }
            }
        }

        private void ThumbList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_suppressThumbListSelectionChange) return;
            if (!_isComicOpen || _comicLoader.Pages.Count == 0) return;
            var list = sender as System.Windows.Controls.ListBox;
            if (list?.SelectedIndex >= 0 && list.SelectedIndex < _comicLoader.Pages.Count)
            {
                _currentPageIndex = list.SelectedIndex;
                LoadCurrentPage();
                UpdatePageIndicator();
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SettingsManager.SaveSettings();
        }

        // Métodos públicos para acceso desde otras vistas
        public void OpenComicAsync(string filePath, int initialPage = 0)
        {
            OpenComicFile(filePath);
            if (_comicLoader.Pages.Count > initialPage && initialPage >= 0)
            {
                _currentPageIndex = initialPage;
                if (SettingsManager.Settings?.EnableContinuousScroll == true && _continuousView != null)
                {
                    _continuousView.ScrollToPage(_currentPageIndex);
                }
                else
                {
                    Interlocked.Increment(ref _pageLoadSeq);
                    LoadCurrentPage();
                }
                try { ComicReader.Services.ContinueReadingService.Instance.UpsertProgress(_comicLoader.FilePath, _currentPageIndex + 1, _comicLoader.PageCount); } catch { }
            }
        }

        // --- Overlay Auto-Hide ---
        private System.Windows.Threading.DispatcherTimer _overlayHideTimer;
        private bool _overlayVisible = true;
        private readonly TimeSpan _overlayIdleDelay = TimeSpan.FromSeconds(2.5);
        private readonly Duration _overlayFadeDuration = new Duration(TimeSpan.FromMilliseconds(280));
        private FrameworkElement _readerOverlay;
        private System.Windows.Threading.DispatcherTimer _autoAdvanceTimer;
    // Cursor auto-oculto en modo inmersivo
    private System.Windows.Threading.DispatcherTimer _cursorHideTimer;
    private bool _cursorHidden = false;
    private TimeSpan _cursorIdleDelay = TimeSpan.FromSeconds(2.5);

        private FrameworkElement GetOverlayElement()
        {
            var ov = this.FindName("ReaderOverlay") as FrameworkElement;
            if (ov == null)
                ov = this.FindName("ReaderTopBar") as FrameworkElement;
            return ov;
        }

        private void EnsureOverlayBehavior()
        {
            if (_readerOverlay == null)
            {
                _readerOverlay = GetOverlayElement();
                if (_readerOverlay != null)
                {
                    _readerOverlay.Opacity = 0.96; // visible inicial
                    _readerOverlay.IsHitTestVisible = true;
                }
            }
            if (_overlayHideTimer == null)
            {
                var secs = SettingsManager.Settings?.HideOverlayDelaySeconds;
                var delay = secs.HasValue && secs.Value > 0 ? TimeSpan.FromSeconds(secs.Value) : _overlayIdleDelay;
                _overlayHideTimer = new System.Windows.Threading.DispatcherTimer { Interval = delay };
                _overlayHideTimer.Tick += (_, __) =>
                {
                    // Respetar preferencia de ocultar solo en inmersivo
                    if (SettingsManager.Settings?.HideOverlayOnlyInImmersive == true && !_isImmersive)
                        return;
                    HideReaderOverlay();
                };
                _overlayHideTimer.Start();
            }
            this.MouseMove -= MainWindow_MouseMoveForOverlay;
            this.MouseMove += MainWindow_MouseMoveForOverlay;
        }

                // Eliminado: lógica antigua de historial reemplazada por ContinueReadingService

        private void MainWindow_MouseMoveForOverlay(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Si se configuró ocultar overlay solo en inmersivo, no hacer nada en modo normal
            if (SettingsManager.Settings?.HideOverlayOnlyInImmersive == true && !_isImmersive)
                return;
            if (!_overlayVisible)
            {
                ShowReaderOverlay();
            }
            ResetOverlayTimer();
            if (_isImmersive)
            {
                if (_cursorHidden && !_isPanning)
                {
                    this.Cursor = Cursors.Arrow;
                    _cursorHidden = false;
                }
                if (_cursorHideTimer != null)
                {
                    _cursorHideTimer.Stop();
                    _cursorHideTimer.Start();
                }
            }
        }

        private void ResetOverlayTimer()
        {
            if (_overlayHideTimer == null) return;
            _overlayHideTimer.Stop();
            var secs = SettingsManager.Settings?.HideOverlayDelaySeconds;
            var delay = secs.HasValue && secs.Value > 0 ? TimeSpan.FromSeconds(secs.Value) : _overlayIdleDelay;
            _overlayHideTimer.Interval = delay;
            _overlayHideTimer.Start();
        }

        private void ShowReaderOverlay()
        {
            if (_readerOverlay == null) return;
            _overlayVisible = true;
            _readerOverlay.IsHitTestVisible = true;
            AnimateOverlayOpacity(_readerOverlay, _readerOverlay.Opacity, 0.96);
        }

        private void HideReaderOverlay()
        {
            if (_readerOverlay == null) return;
            _overlayVisible = false;
            AnimateOverlayOpacity(_readerOverlay, _readerOverlay.Opacity, 0.0, () =>
            {
                if (!_overlayVisible && _readerOverlay != null)
                {
                    _readerOverlay.IsHitTestVisible = false;
                }
            });
        }

        private void AnimateOverlayOpacity(UIElement element, double from, double to, Action completed = null)
        {
            var fade = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = from,
                To = to,
                Duration = _overlayFadeDuration,
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
            };
            if (completed != null)
            {
                fade.Completed += (_, __) => completed();
            }
            element.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        // Hook overlay después de cargar la ventana
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            // Mantener compatibilidad si existe ReaderOverlay, si no, no hacer nada
            _readerOverlay = GetOverlayElement();
            EnsureOverlayBehavior();
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (SettingsManager.Settings?.EnableZoomPan == true && _readerScrollViewer != null && CanPan())
            {
                _isPanning = true;
                _panStartPoint = e.GetPosition(this);
                _panStartVerticalOffset = _readerScrollViewer.VerticalOffset;
                _panStartHorizontalOffset = _readerScrollViewer.HorizontalOffset;
                _currentComicImage.CaptureMouse();
                this.Cursor = Cursors.Hand;
                e.Handled = true;
            }
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                _currentComicImage.ReleaseMouseCapture();
                this.Cursor = Cursors.Arrow;
                e.Handled = true;
            }
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning && _readerScrollViewer != null)
            {
                var pos = e.GetPosition(this);
                var dy = pos.Y - _panStartPoint.Y;
                var dx = pos.X - _panStartPoint.X;
                _readerScrollViewer.ScrollToVerticalOffset(_panStartVerticalOffset - dy);
                _readerScrollViewer.ScrollToHorizontalOffset(_panStartHorizontalOffset - dx);
            }
        }

        private void Image_MouseEnter(object sender, MouseEventArgs e)
        {
            if (SettingsManager.Settings?.EnableZoomPan == true && CanPan())
            {
                this.Cursor = Cursors.Hand;
            }
        }

        private void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isPanning)
            {
                this.Cursor = Cursors.Arrow;
            }
        }

        private bool CanPan()
        {
            if (_readerScrollViewer == null) return false;
            bool canH = _readerScrollViewer.ScrollableWidth > 0.5;
            bool canV = _readerScrollViewer.ScrollableHeight > 0.5;
            return canH || canV || Math.Abs(_zoomFactor - 1.0) > 0.01;
        }

        public void ImmersiveFullScreen_Click(object sender, RoutedEventArgs e)
        {
            ToggleImmersiveFullScreen();
        }

        private void ToggleImmersiveFullScreen()
        {
            if (_immersiveTransitionBusy) return; // evitar reentrada
            _immersiveTransitionBusy = true;
            try
            {
            if (!_isImmersive)
            {
                // Guardar estado
                _savedWindowStyle = this.WindowStyle;
                _savedResizeMode = this.ResizeMode;
                _savedWindowState = this.WindowState;
                _savedBackgroundBrush = this.Background;
                _savedTopmost = this.Topmost;
                // Guardar bounds si estamos en ventana normal
                if (this.WindowState == WindowState.Normal)
                {
                    _savedLeft = this.Left;
                    _savedTop = this.Top;
                    _savedWidth = this.Width;
                    _savedHeight = this.Height;
                }

                var titleBar = this.FindName("CustomTitleBar") as FrameworkElement;
                var topBar = this.FindName("ReaderTopBar") as FrameworkElement;
                var thumbPanel = this.FindName("ThumbPanel") as FrameworkElement;
                var thumbCol = this.FindName("ThumbCol") as System.Windows.Controls.ColumnDefinition;

                // Ocultar overlay si existe (ReaderOverlay o ReaderTopBar)
                var ov = GetOverlayElement();
                if (ov != null)
                {
                    _savedOverlayOpacity = ov.Opacity;
                    _savedOverlayHit = ov.IsHitTestVisible;
                    ov.Opacity = 0.0;
                    ov.IsHitTestVisible = false;
                }
                var pi = this.FindName("PageIndicator") as FrameworkElement;
                if (pi != null) pi.Visibility = Visibility.Collapsed;

                // Entrar en pantalla completa total (cubrir taskbar) usando bounds del monitor actual
                this.WindowStyle = WindowStyle.None;
                this.ResizeMode = ResizeMode.NoResize;
                var hwnd = new WindowInteropHelper(this).Handle;
                var screen = WinForms.Screen.FromHandle(hwnd);
                var bounds = screen.Bounds;
                this.Topmost = true;
                this.WindowState = WindowState.Normal; // necesario para aplicar tamaño exacto
                // Convertir de píxeles a DIPs para WPF (DPI-aware)
                var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(this);
                double scaleX = dpi.DpiScaleX <= 0 ? 1.0 : dpi.DpiScaleX;
                double scaleY = dpi.DpiScaleY <= 0 ? 1.0 : dpi.DpiScaleY;
                this.Left = bounds.Left / scaleX;
                this.Top = bounds.Top / scaleY;
                this.Width = Math.Max(100, bounds.Width / scaleX);
                this.Height = Math.Max(100, bounds.Height / scaleY);
                this.Background = System.Windows.Media.Brushes.Black;
                if (titleBar != null)
                {
                    _savedTitleBarVisibility = titleBar.Visibility;
                    titleBar.Visibility = Visibility.Collapsed;
                }
                if (topBar != null)
                {
                    _savedTopBarVisibility = topBar.Visibility;
                    topBar.Visibility = Visibility.Collapsed;
                }
                if (thumbPanel != null)
                {
                    _savedThumbPanelVisibility = thumbPanel.Visibility;
                    thumbPanel.Visibility = Visibility.Collapsed;
                }
                if (thumbCol != null)
                {
                    _savedThumbColWidth = thumbCol.Width;
                    _savedThumbColWidthSet = true;
                    thumbCol.Width = new GridLength(0);
                }
                // Ocultar barra de desplazamiento para no distraer
                if (_readerScrollViewer != null)
                {
                    _savedVerticalScrollBarVisibility = _readerScrollViewer.VerticalScrollBarVisibility;
                    _savedHorizontalScrollBarVisibility = _readerScrollViewer.HorizontalScrollBarVisibility;
                    _readerScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                    _readerScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                }
                // Mejorar calidad de escalado
                if (_currentComicImage != null)
                {
                    _savedImageScalingMode = System.Windows.Media.RenderOptions.GetBitmapScalingMode(_currentComicImage);
                    System.Windows.Media.RenderOptions.SetBitmapScalingMode(_currentComicImage, System.Windows.Media.BitmapScalingMode.HighQuality);
                }
                _isImmersive = true;
                // Asegurar foco para que las teclas y la rueda funcionen
                EnsureReaderInputFocus();
                // Iniciar temporizador para ocultar cursor
                // Actualizar delay desde Settings
                var cursorSecs = SettingsManager.Settings?.HideCursorDelaySeconds;
                _cursorIdleDelay = (cursorSecs.HasValue && cursorSecs.Value > 0) ? TimeSpan.FromSeconds(cursorSecs.Value) : TimeSpan.FromSeconds(2.5);
                if (_cursorHideTimer == null)
                {
                    _cursorHideTimer = new System.Windows.Threading.DispatcherTimer { Interval = _cursorIdleDelay };
                    _cursorHideTimer.Tick += (_, __) =>
                    {
                        if (_isImmersive && !_isPanning)
                        {
                            this.Cursor = Cursors.None;
                            _cursorHidden = true;
                        }
                        _cursorHideTimer.Stop();
                    };
                }
                _cursorHideTimer.Stop();
                _cursorHideTimer.Interval = _cursorIdleDelay;
                _cursorHideTimer.Start();
                // Mantener pantalla activa mientras está inmersivo
                TryKeepDisplayAwake(true);

                // Fundido suave si está activado
                if (SettingsManager.Settings?.FadeOnFullscreenTransitions == true)
                {
                    try
                    {
                        var fade = new System.Windows.Media.Animation.DoubleAnimation
                        {
                            From = 0.0,
                            To = 1.0,
                            Duration = new Duration(TimeSpan.FromMilliseconds(160))
                        };
                        this.BeginAnimation(Window.OpacityProperty, fade);
                    }
                    catch { }
                }
            }
            else
            {
                // Restaurar overlay (ReaderOverlay o ReaderTopBar)
                var ov = GetOverlayElement();
                if (ov != null)
                {
                    ov.Opacity = _savedOverlayOpacity;
                    ov.IsHitTestVisible = _savedOverlayHit;
                    _overlayVisible = ov.Opacity > 0.01;
                }
                var pi = this.FindName("PageIndicator") as FrameworkElement;
                if (pi != null)
                {
                    pi.Visibility = (SettingsManager.Settings?.ShowPageNumberOverlay == true) ? Visibility.Visible : Visibility.Collapsed;
                }

                // Salir de pantalla completa inmersiva
                this.WindowStyle = _savedWindowStyle;
                this.ResizeMode = _savedResizeMode;
                // Restaurar tamaño/posición si se entró desde ventana normal
                if (_savedWindowState == WindowState.Normal)
                {
                    this.Topmost = _savedTopmost;
                    this.WindowState = WindowState.Normal;
                    if (!double.IsNaN(_savedLeft)) this.Left = _savedLeft;
                    if (!double.IsNaN(_savedTop)) this.Top = _savedTop;
                    if (!double.IsNaN(_savedWidth)) this.Width = _savedWidth;
                    if (!double.IsNaN(_savedHeight)) this.Height = _savedHeight;
                }
                else
                {
                    // Si era Maximize/otro estado, restaurar directamente
                    this.WindowState = _savedWindowState;
                    this.Topmost = _savedTopmost;
                }
                this.Background = _savedBackgroundBrush ?? this.Background;

                var titleBar = this.FindName("CustomTitleBar") as FrameworkElement;
                var topBar = this.FindName("ReaderTopBar") as FrameworkElement;
                var thumbPanel = this.FindName("ThumbPanel") as FrameworkElement;
                var thumbCol = this.FindName("ThumbCol") as System.Windows.Controls.ColumnDefinition;
                // Restaurar elementos visuales y layout ocultos en inmersivo
                if (titleBar != null)
                {
                    titleBar.Visibility = _savedTitleBarVisibility;
                }
                if (topBar != null)
                {
                    topBar.Visibility = _savedTopBarVisibility;
                }
                if (thumbPanel != null)
                {
                    thumbPanel.Visibility = _savedThumbPanelVisibility;
                }
                if (thumbCol != null && _savedThumbColWidthSet)
                {
                    thumbCol.Width = _savedThumbColWidth;
                }

                // Restaurar elementos de desplazamiento y escalado
                if (_readerScrollViewer != null)
                {
                    _readerScrollViewer.VerticalScrollBarVisibility = _savedVerticalScrollBarVisibility;
                    _readerScrollViewer.HorizontalScrollBarVisibility = _savedHorizontalScrollBarVisibility;
                }
                if (_currentComicImage != null)
                {
                    System.Windows.Media.RenderOptions.SetBitmapScalingMode(_currentComicImage, _savedImageScalingMode);
                    try { _currentComicImage.ReleaseMouseCapture(); } catch { }
                    _isPanning = false;
                }
                _isImmersive = false;
                // Restaurar cursor y detener timer
                if (_cursorHideTimer != null)
                {
                    _cursorHideTimer.Stop();
                }
                this.Cursor = Cursors.Arrow;
                _cursorHidden = false;
                // Volver a la política de energía normal
                TryKeepDisplayAwake(false);

                // Reanudar overlay/timers y aplicar efectos visuales del modo lectura
                EnsureOverlayBehavior();
                ApplyReadingModeEffects();
                // Reasegurar foco de lectura tras el cambio
                EnsureReaderInputFocus();
                // Forzar un relayout después de cambiar chrome/estado
                try { this.UpdateLayout(); this.InvalidateVisual(); } catch { }

                // Fundido al restaurar si está activado
                if (SettingsManager.Settings?.FadeOnFullscreenTransitions == true)
                {
                    try
                    {
                        var fade = new System.Windows.Media.Animation.DoubleAnimation
                        {
                            From = 0.0,
                            To = 1.0,
                            Duration = new Duration(TimeSpan.FromMilliseconds(160))
                        };
                        this.BeginAnimation(Window.OpacityProperty, fade);
                    }
                    catch { }
                }
            }
            }
            finally
            {
                _immersiveTransitionBusy = false;
            }
        }

        private void EnsureAutoAdvanceBehavior()
        {
            if (_autoAdvanceTimer == null)
            {
                _autoAdvanceTimer = new System.Windows.Threading.DispatcherTimer();
                _autoAdvanceTimer.Tick += (s, e) =>
                {
                    if (!_isComicOpen || _comicLoader.Pages.Count == 0) return;
                    if (!SettingsManager.Settings?.AutoAdvancePages ?? true) return;
                    // Pausar autoavance si está interactuando el usuario
                    if (_isPanning || (DateTime.UtcNow - _lastZoomChange) < _interactionGrace) return;
                    if (_currentPageIndex < _comicLoader.Pages.Count - 1)
                    {
                        NextPage_Click(null, null);
                    }
                    else if (SettingsManager.Settings?.AutoAdvanceLoop == true)
                    {
                        _currentPageIndex = 0;
                        LoadCurrentPage();
                        UpdatePageIndicator();
                    }
                    else
                    {
                        _autoAdvanceTimer?.Stop();
                    }
                };
            }

            var intervalSec = Math.Max(1, SettingsManager.Settings?.AutoAdvanceInterval ?? 5);
            _autoAdvanceTimer.Interval = TimeSpan.FromSeconds(intervalSec);

            if (SettingsManager.Settings?.AutoAdvancePages == true)
            {
                _autoAdvanceTimer.Start();
            }
            else
            {
                _autoAdvanceTimer.Stop();
            }
        }

        // Permite aplicar cambios de configuración en caliente cuando el usuario pulsa "Aplicar" en el diálogo
        public void ApplySettingsRuntime()
        {
            try
            {
                _comicLoader?.RefreshTuningFromSettings();
                EnsureAutoAdvanceBehavior();
                ResetOverlayTimer();
                // Actualizar timers con nuevos valores
                var secs = SettingsManager.Settings?.HideOverlayDelaySeconds;
                if (_overlayHideTimer != null)
                {
                    var newInterval = (secs.HasValue && secs.Value > 0) ? TimeSpan.FromSeconds(secs.Value) : _overlayHideTimer.Interval;
                    _overlayHideTimer.Interval = newInterval;
                }
                var cursorSecs = SettingsManager.Settings?.HideCursorDelaySeconds;
                if (_cursorHideTimer != null)
                {
                    var newInterval = (cursorSecs.HasValue && cursorSecs.Value > 0) ? TimeSpan.FromSeconds(cursorSecs.Value) : _cursorHideTimer.Interval;
                    _cursorHideTimer.Interval = newInterval;
                }
                _isNightMode = SettingsManager.Settings?.IsNightMode == true;
                _isReadingMode = SettingsManager.Settings?.IsReadingMode == true;
                ApplyReadingModeEffects();

                // Si hay un cómic abierto, asegurar que el modo de lectura actual refleja las preferencias (scroll continuo vs página única)
                if (_isComicOpen)
                {
                    bool wantContinuous = SettingsManager.Settings?.EnableContinuousScroll == true;
                    bool isContinuous = CurrentView == _continuousView;
                    if (wantContinuous != isContinuous)
                    {
                        // Cambiar de vista respetando el cómic cargado
                        ShowComicView();
                    }
                    else if (!wantContinuous)
                    {
                        // Re-aplicar el ajuste inicial cuando estamos en modo no continuo
                        var mode = SettingsManager.Settings?.DefaultFitMode?.ToLowerInvariant();
                        switch (mode)
                        {
                            case "height":
                                ApplyFitToHeight();
                                break;
                            case "screen":
                                FitToScreen();
                                break;
                            case "width":
                            default:
                                FitToWidth();
                                break;
                        }
                        // Reaplicar brillo/contraste en la imagen actual
                        TryApplyBrightnessContrastToCurrentPageImage();
                    }

                    // En modo continuo, pedir materialización de visibles si hay cambios que afecten al renderizado
                    if (wantContinuous)
                    {
                        try
                        {
                            _continuousView?.ViewModel?.RequestVisiblePagesMaterialization();
                            // Reaplicar brillo/contraste en elementos visibles
                            _continuousView?.ReapplyBrightnessContrastVisible();
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private void TryApplyBrightnessContrastToCurrentPageImage()
        {
            try
            {
                if (_currentComicImage == null) return;
                var s = SettingsManager.Settings;
                if (s == null) return;
                if (_comicLoader == null || _comicLoader.Pages == null || _currentPageIndex < 0 || _currentPageIndex >= _comicLoader.Pages.Count) return;
                var page = _comicLoader.Pages[_currentPageIndex];
                var baseImage = page?.Image; // asumimos que es la imagen original (BitmapImage)
                if (baseImage == null) return;
                if (Math.Abs(s.Brightness - 1.0) < 0.001 && Math.Abs(s.Contrast - 1.0) < 0.001)
                {
                    _currentComicImage.Source = baseImage;
                    return;
                }
                var adjusted = ImageAdjuster.ApplyBrightnessContrast(baseImage, s.Brightness, s.Contrast);
                _currentComicImage.Source = adjusted;
            }
            catch { }
        }

        // Mantener la pantalla activa en modo inmersivo (evita que se apague/atenué)
        [DllImport("kernel32.dll")]
        private static extern uint SetThreadExecutionState(uint esFlags);
        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_DISPLAY_REQUIRED = 0x00000002;
        private void TryKeepDisplayAwake(bool enable)
        {
            try
            {
                if (enable)
                {
                    SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED);
                }
                else
                {
                    SetThreadExecutionState(ES_CONTINUOUS);
                }
            }
            catch { }
        }
    }
}