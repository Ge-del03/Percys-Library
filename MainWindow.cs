using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
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
using ComicReader.ViewModels;

namespace ComicReader
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ComicPageLoader _comicLoader = new ComicPageLoader();
        private int _currentPageIndex;
        private DashboardView _dashboardView;
        private LibraryCatalogView _catalogView;
        private SeriesView _seriesView;
        private StatsView _statsView;
        private SettingsView _settingsView;
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

        private Views.ContinuousComicView _continuousView;

        private long _pageLoadSeq = 0;
        private long _thumbLoadSeq = 0;
        private bool _isImmersive = false;

        private bool _immersiveTransitionBusy = false;
        private bool _suppressThumbListSelectionChange = false;

        private double _normalWidth = 1200;
        private double _normalHeight = 800;
        private double _normalLeft = -1;
        private double _normalTop = -1;
        private bool _windowStateInitialized = false;

        public ComicPageLoader ComicLoader => _comicLoader;
        public int CurrentPageIndex => _currentPageIndex;
        public double zoomLevel => _zoomFactor;

        public MainViewModel MainViewModel { get; } = new MainViewModel();

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
            }
        }

        public bool IsComicViewActive => _isComicOpen;
        public bool IsReaderViewActive => CurrentView == _readerScrollViewer || CurrentView == _continuousView || _isComicOpen;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = MainViewModel;

            InitializeViewsAndServices();

            this.Drop += Window_Drop;
            this.KeyDown += MainWindow_KeyDown;
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
            this.AllowDrop = true;

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

                    var oneBased = idx + 1;
                    var pageCount = _comicLoader?.PageCount ?? 0;
                    ComicReader.Services.ContinueReadingService.Instance.UpsertProgress(_comicLoader?.FilePath, oneBased, pageCount);
                    if (pageCount > 0 && oneBased >= pageCount)
                    {
                        _dashboardView?.RefreshData();
                    }
                }
                catch { }
            };

            if (SettingsManager.Settings is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged += (_, __) =>
                {
                    try { this.Dispatcher.BeginInvoke(new Action(ApplySettingsRuntime)); } catch { }
                };
            }

            ConfigureInitialWindowState();
        }

        private void InitializeViewsAndServices()
        {
            _currentPageIndex = 0;
            SettingsManager.LoadSettings();

            _dashboardView = new DashboardView();
            _catalogView = new LibraryCatalogView();
            _seriesView = new SeriesView();
            _statsView = new StatsView();
            _settingsView = new SettingsView();

            NavigationService.Instance.Navigated += OnNavigated;
            ShowDashboardView();
        }

        private void OnNavigated(AppViewType viewType, object parameter)
        {
            switch (viewType)
            {
                case AppViewType.Dashboard:
                    ShowDashboardView();
                    break;
                case AppViewType.Catalog:
                    ShowCatalogView();
                    break;
                case AppViewType.Series:
                    ShowSeriesView();
                    break;
                case AppViewType.Stats:
                    ShowStatsView();
                    break;
                case AppViewType.Settings:
                    ShowSettingsView();
                    break;
                case AppViewType.Reader:
                    if (parameter is string path)
                    {
                        OpenComicFile(path);
                    }
                    else if (parameter is ComicItem comic)
                    {
                        OpenComicFile(comic.FilePath);
                    }
                    else
                    {
                        EnterReadingMode();
                    }
                    break;
            }
        }

        private void NavDashboard_Click(object sender, RoutedEventArgs e) => NavigationService.Instance.NavigateTo(AppViewType.Dashboard);
        private void NavCatalog_Click(object sender, RoutedEventArgs e) => NavigationService.Instance.NavigateTo(AppViewType.Catalog);
        private void NavSeries_Click(object sender, RoutedEventArgs e) => NavigationService.Instance.NavigateTo(AppViewType.Series);
        private void NavStats_Click(object sender, RoutedEventArgs e) => NavigationService.Instance.NavigateTo(AppViewType.Stats);

        private void ShowDashboardView()
        {
            _dashboardView?.RefreshData();
            SetMainCanvasView(_dashboardView);
        }

        private void ShowCatalogView()
        {
            SetMainCanvasView(_catalogView);
        }

        private void ShowSeriesView()
        {
            _seriesView?.LoadSeriesData(_catalogView?.ViewModel?.AllComics);
            SetMainCanvasView(_seriesView);
        }

        private void ShowStatsView()
        {
            SetMainCanvasView(_statsView);
        }

        private void ShowSettingsView()
        {
            SetMainCanvasView(_settingsView);
        }

        private void SetMainCanvasView(object view)
        {
            try { Interlocked.Increment(ref _pageLoadSeq); } catch { }
            try { Interlocked.Increment(ref _thumbLoadSeq); } catch { }

            CurrentView = view;
            if (this.FindName("MainContentArea") is ContentControl content)
                content.Content = view;

            _isComicOpen = false;
            OnPropertyChanged(nameof(IsComicViewActive));
            OnPropertyChanged(nameof(IsReaderViewActive));

            HideThumbnailsPanel();
            SetReaderTopBarVisible(false);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (this.WindowState != WindowState.Maximized)
            {
                this.Width = _normalWidth;
                this.Height = _normalHeight;
            }
        }

        private void ConfigureInitialWindowState()
        {
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.MinWidth = 900;
            this.MinHeight = 650;
            var s = SettingsManager.Settings;
            if (s != null)
            {
                _normalWidth = Math.Max(900, s.LastWindowWidth);
                _normalHeight = Math.Max(650, s.LastWindowHeight);
                this.Width = _normalWidth;
                this.Height = _normalHeight;
                if (s.LastWindowState == WindowState.Maximized)
                {
                    this.Loaded += (_, __) =>
                    {
                        try { this.WindowState = WindowState.Maximized; } catch { }
                    };
                }
                else
                {
                    CenterWindowOnScreen();
                }
            }
            else
            {
                this.Width = _normalWidth;
                this.Height = _normalHeight;
            }
            
            this.SizeToContent = SizeToContent.Manual;
            
            this.Loaded += (s, e) =>
            {
                if (this.WindowState != WindowState.Maximized)
                {
                    this.Width = _normalWidth;
                    this.Height = _normalHeight;
                }
                
                _normalLeft = this.Left;
                _normalTop = this.Top;
                _normalWidth = this.ActualWidth;
                _normalHeight = this.ActualHeight;
                _windowStateInitialized = true;
            };

            this.LocationChanged += (s, e) =>
            {
                if (this.WindowState == WindowState.Normal && _windowStateInitialized && this.IsLoaded)
                {
                    _normalLeft = this.Left;
                    _normalTop = this.Top;
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

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                try { Interlocked.Increment(ref _thumbLoadSeq); } catch { }
            }
            catch { }
        }

        private void RefreshThumbnailPanelForCurrentComic()
        {
            try
            {
                if (!_isComicOpen || _comicLoader?.Pages == null) return;
                var list = this.FindName("ThumbList") as System.Windows.Controls.ListBox;
                if (list == null) return;

                _suppressThumbListSelectionChange = true;
                try
                {
                    list.ItemsSource = null;
                    list.Items.Clear();
                    list.ItemsSource = _comicLoader.Pages;
                    list.SelectedIndex = Math.Max(0, Math.Min(_currentPageIndex, _comicLoader.Pages.Count - 1));
                    try { list.Items.Refresh(); } catch { }
                    list.ScrollIntoView(list.SelectedItem);
                }
                finally { _suppressThumbListSelectionChange = false; }

                foreach (var p in _comicLoader.Pages)
                {
                    p.Thumbnail = null;
                }

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

        public async void ReRenderCurrentPdfIfAny()
        {
            try
            {
                var path = _comicLoader?.FilePath;
                if (string.IsNullOrEmpty(path)) return;
                var ext = System.IO.Path.GetExtension(path)?.ToLowerInvariant();
                if (ext != ".pdf") return;
                int page = _currentPageIndex;
                await _comicLoader.LoadComicAsync(path);
                if (page >= 0 && page < _comicLoader.Pages.Count)
                {
                    _currentPageIndex = page;
                }
                LoadCurrentPage();
            }
            catch { }
        }

        private void OpenSettingsDialog_Click(object sender, RoutedEventArgs e)
        {
            ShowSettingsView();
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
                    content.Content = _readerScrollViewer;
                CurrentView = _readerScrollViewer;
            }

            SetReaderTopBarVisible(true);
            _isComicOpen = true;
            OnPropertyChanged(nameof(IsComicViewActive));
            OnPropertyChanged(nameof(IsReaderViewActive));

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
                try { _continuousView.ItemSpacing = SettingsManager.Settings?.ReadingSpacing ?? 8; } catch { }
                if (this.FindName("MainContentArea") is ContentControl content)
                {
                    content.Content = _continuousView;
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
                    content.Content = _readerScrollViewer;
                }
                CurrentView = _readerScrollViewer;
            }
        }

        public void EnterReadingMode()
        {
            try { Interlocked.Increment(ref _pageLoadSeq); } catch { }
            try { Interlocked.Increment(ref _thumbLoadSeq); } catch { }
            _isComicOpen = false;
            _currentPageIndex = 0;
            try { _comicLoader?.ClearCurrent(); } catch { }
            try { SettingsManager.Settings.LastOpenedFilePath = null; SettingsManager.Settings.LastOpenedPage = 0; SettingsManager.SaveSettings(); } catch { }
            EnsureReaderScaffold();
            SetReaderTopBarVisible(true);
            try { UpdatePageIndicator(); } catch { }
            OnPropertyChanged(nameof(IsComicViewActive));
            OnPropertyChanged(nameof(IsReaderViewActive));
        }

        private void EnterReadingMode_Click(object sender, RoutedEventArgs e) => EnterReadingMode();

        private async void LoadCurrentPage()
        {
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
                        var requestId = Interlocked.Increment(ref _pageLoadSeq);
                        var prevScaling = System.Windows.Media.RenderOptions.GetBitmapScalingMode(_currentComicImage);
                        System.Windows.Media.RenderOptions.SetBitmapScalingMode(_currentComicImage, System.Windows.Media.BitmapScalingMode.LowQuality);
                        var bmp = await _comicLoader.GetPageImageAsync(_currentPageIndex);
                        if (requestId != Volatile.Read(ref _pageLoadSeq))
                        {
                            System.Windows.Media.RenderOptions.SetBitmapScalingMode(_currentComicImage, prevScaling);
                            return;
                        }
                        var page = _comicLoader.Pages[_currentPageIndex];
                        page.Image = bmp;
                        _currentComicImage.Source = bmp;
                        System.Windows.Media.RenderOptions.SetBitmapScalingMode(_currentComicImage, prevScaling);

                        UpdatePageIndicator();
                        ApplyZoomToImage();
                        ApplyReadingModeEffects();
                        _stats?.RecordPageViewed(_currentPageIndex + 1);
                        SettingsManager.Settings.LastOpenedFilePath = _comicLoader.FilePath;
                        SettingsManager.Settings.LastOpenedPage = _currentPageIndex;
                        SettingsManager.SaveSettings();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar la página: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Seleccionar Cómic",
                Filter = "Archivos de Cómic|*.cbz;*.cbr;*.pdf;*.epub|Todos los archivos|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                OpenComicFile(openFileDialog.FileName);
            }
        }

        private async void OpenComicFile(string filePath)
        {
            try
            {
                Interlocked.Increment(ref _pageLoadSeq);
                Interlocked.Increment(ref _thumbLoadSeq);
                if (this.FindName("PageIndicator") is TextBlock pi) pi.Text = "Cargando...";
                await _comicLoader.LoadComicAsync(filePath);
                _comicLoader.RefreshTuningFromSettings();
                
                if (_comicLoader.Pages.Count > 0)
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
                    }
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
                    _stats?.StartSession(filePath, _comicLoader.ComicTitle, _comicLoader.Pages.Count);
                    
                    ComicReader.Services.ContinueReadingService.Instance.UpsertProgress(filePath, _currentPageIndex + 1, _comicLoader.Pages.Count);
                    try { _dashboardView?.RefreshData(); } catch { }

                    ShowComicView();
                    try { UpdatePageIndicator(); } catch { }
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
                if (SettingsManager.Settings?.EnableContinuousScroll == true && _continuousView != null)
                {
                    _continuousView.ScrollToPage(target);
                    UpdatePageIndicator();
                }
                else
                {
                    Interlocked.Increment(ref _pageLoadSeq);
                    LoadCurrentPage();
                    UpdatePageIndicator();
                }
                try { if (_isComicOpen) ComicReader.Services.ContinueReadingService.Instance.UpsertProgress(_comicLoader.FilePath, _currentPageIndex + 1, _comicLoader.PageCount); } catch { }
            }
        }

        public void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPageIndex < _comicLoader.Pages.Count - 1)
            {
                int target = _currentPageIndex + 1;
                _currentPageIndex = target;
                if (SettingsManager.Settings?.EnableContinuousScroll == true && _continuousView != null)
                {
                    _continuousView.ScrollToPage(target);
                    UpdatePageIndicator();
                }
                else
                {
                    Interlocked.Increment(ref _pageLoadSeq);
                    LoadCurrentPage();
                    UpdatePageIndicator();
                }
                try
                {
                    if (_isComicOpen)
                    {
                        var oneBased = _currentPageIndex + 1;
                        ComicReader.Services.ContinueReadingService.Instance.UpsertProgress(_comicLoader.FilePath, oneBased, _comicLoader.PageCount);
                        if (oneBased >= _comicLoader.PageCount)
                        {
                            _dashboardView?.RefreshData();
                        }
                    }
                }
                catch { }
            }
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_readerScrollViewer != null)
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
            this.Cursor = Cursors.Hand;
        }

        private void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isPanning)
            {
                this.Cursor = Cursors.Arrow;
            }
        }

        private void UpdatePageIndicator()
        {
            bool noComic = !_isComicOpen || _comicLoader == null || _comicLoader.Pages == null || _comicLoader.Pages.Count == 0;
            if (this.FindName("PageIndicator") is TextBlock pi2)
            {
                pi2.Text = noComic ? "— / —" : $"Página {_currentPageIndex + 1} de {_comicLoader.Pages.Count}";
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
            }
        }

        public void ZoomIn_Click(object sender, RoutedEventArgs e) => AdjustZoom(1.25);
        public void ZoomOut_Click(object sender, RoutedEventArgs e) => AdjustZoom(0.8);
        public void Zoom100_Click(object sender, RoutedEventArgs e) => SetZoom(1.0);

        private void AdjustZoom(double factor) => SetZoom(_zoomFactor * factor);

        private void SetZoom(double newZoom)
        {
            _zoomFactor = Math.Max(0.1, Math.Min(8.0, newZoom));
            ApplyZoomToImage();
        }

        private void ApplyZoomToImage()
        {
            if (_currentComicImage != null)
            {
                var transformGroup = new System.Windows.Media.TransformGroup();
                transformGroup.Children.Add(new System.Windows.Media.ScaleTransform(_zoomFactor, _zoomFactor));
                if (_rotationAngle != 0)
                {
                    transformGroup.Children.Add(new System.Windows.Media.RotateTransform(_rotationAngle));
                }
                _currentComicImage.RenderTransform = transformGroup;
                _currentComicImage.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            }
        }

        private void FitToScreen()
        {
            if (_currentComicImage?.Parent is ScrollViewer scrollViewer && _currentComicImage.Source != null)
            {
                var imageSource = _currentComicImage.Source;
                double containerWidth = scrollViewer.ActualWidth;
                double containerHeight = scrollViewer.ActualHeight;
                double scaleX = containerWidth / imageSource.Width;
                double scaleY = containerHeight / imageSource.Height;
                _zoomFactor = Math.Min(scaleX, scaleY);
                ApplyZoomToImage();
            }
        }

        private void FitToWidth()
        {
            if (_currentComicImage?.Parent is ScrollViewer scrollViewer && _currentComicImage.Source != null)
            {
                _zoomFactor = scrollViewer.ActualWidth / _currentComicImage.Source.Width;
                ApplyZoomToImage();
            }
        }

        private void ApplyFitToHeight()
        {
            if (_currentComicImage?.Parent is ScrollViewer scrollViewer && _currentComicImage.Source != null)
            {
                _zoomFactor = scrollViewer.ActualHeight / _currentComicImage.Source.Height;
                ApplyZoomToImage();
            }
        }

        private async void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Seleccionar carpeta de cómics para escanear e indexar";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var scanner = new FolderScannerService();
                var items = await scanner.ScanDirectoryAsync(dialog.SelectedPath);
                if (_catalogView?.ViewModel != null)
                {
                    _catalogView.ViewModel.AllComics = new System.Collections.ObjectModel.ObservableCollection<ComicItem>(items);
                    MainViewModel.TotalComicsCount = items.Count;
                    long totalBytes = items.Sum(i => i.FileSize);
                    MainViewModel.TotalStorageText = $"{Math.Round((double)totalBytes / (1024 * 1024 * 1024), 1)} GB en disco";
                    SidebarComicsCountText.Text = $"{items.Count} Cómics Indexados";
                    SidebarStorageText.Text = MainViewModel.TotalStorageText;
                }
                NavigationService.Instance.NavigateTo(AppViewType.Catalog);
            }
        }

        public void Rotate_Click(object sender, RoutedEventArgs e)
        {
            _rotationAngle = (_rotationAngle + 90) % 360;
            ApplyZoomToImage();
        }

        private void ApplyReadingModeEffects()
        {
            if (_currentComicImage == null) return;
            if (_isNightMode)
            {
                this.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 20, 20));
            }
            else
            {
                this.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 17, 21));
            }
        }

        private void ToggleThumbnails_Click(object sender, RoutedEventArgs e)
        {
            _thumbnailsVisible = !_thumbnailsVisible;
            var panel = this.FindName("ThumbPanel") as FrameworkElement;
            var col = this.FindName("ThumbCol") as System.Windows.Controls.ColumnDefinition;
            var list = this.FindName("ThumbList") as System.Windows.Controls.ListBox;
            if (_thumbnailsVisible)
            {
                if (panel != null) panel.Visibility = Visibility.Visible;
                if (col != null) col.Width = new GridLength(240);
                if (list != null)
                {
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
                }
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
            }
        }

        private void ThumbList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressThumbListSelectionChange) return;
            if (!_isComicOpen || _comicLoader.Pages.Count == 0) return;
            var list = sender as ListBox;
            if (list?.SelectedIndex >= 0 && list.SelectedIndex < _comicLoader.Pages.Count)
            {
                _currentPageIndex = list.SelectedIndex;
                LoadCurrentPage();
                UpdatePageIndicator();
            }
        }

        public void ImmersiveFullScreen_Click(object sender, RoutedEventArgs e) => ToggleImmersiveFullScreen();

        private void ToggleImmersiveFullScreen()
        {
            if (_immersiveTransitionBusy) return;
            _immersiveTransitionBusy = true;
            try
            {
                if (!_isImmersive)
                {
                    this.WindowStyle = WindowStyle.None;
                    this.ResizeMode = ResizeMode.NoResize;
                    this.WindowState = WindowState.Maximized;
                    _isImmersive = true;
                }
                else
                {
                    this.WindowStyle = WindowStyle.SingleBorderWindow;
                    this.ResizeMode = ResizeMode.CanResize;
                    this.WindowState = WindowState.Normal;
                    _isImmersive = false;
                }
            }
            finally { _immersiveTransitionBusy = false; }
        }

        private void EnsureAutoAdvanceBehavior() { }

        public void ApplySettingsRuntime() { }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (!_isComicOpen) return;
            switch (e.Key)
            {
                case Key.Left:
                case Key.PageUp:
                    PrevPage_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.Right:
                case Key.PageDown:
                case Key.Space:
                    NextPage_Click(null, null);
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

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled) return;
            MainWindow_KeyDown(sender, e);
        }

        private void DragWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    MaximizeRestore_Click(sender, e);
                    return;
                }
                try { DragMove(); } catch { }
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);

        private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                SystemCommands.RestoreWindow(this);
            else
                SystemCommands.MaximizeWindow(this);
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void CenterWindowOnScreen()
        {
            var workingArea = SystemParameters.WorkArea;
            Width = Math.Min(1200, workingArea.Width * 0.85);
            Height = Math.Min(800, workingArea.Height * 0.85);
            Left = workingArea.Left + (workingArea.Width - Width) / 2;
            Top = workingArea.Top + (workingArea.Height - Height) / 2;
        }

        private void Window_StateChanged(object sender, EventArgs e) { }

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

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            SettingsManager.SaveSettings();
        }
    }
}