using System;
using System.ComponentModel;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Forms;
using ComicReader.Models;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ComicReader.Services;
using System.Windows.Input;
using System.Collections.Generic;
using SharpCompress.Archives;
using SharpCompress.Readers;
using SharpCompress.Common;
using ComicReader.Commands;
using System.Windows.Media;
using System.Globalization;

namespace ComicReader.Views
{
    public partial class HomeView : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
    // Cambiar este valor cuando mejoremos la selección de portada para invalidar caché
    private const string ThumbCacheVersion = "v2";
        private ObservableCollection<ComicFolderItem> _folderContents;
        private string _currentFolderPath;
    private ObservableCollection<ContinueItem> _recentComics; // página actual
    private ObservableCollection<ContinueItem> _completedComics;
    private List<ContinueItem> _allRecentComics = new List<ContinueItem>(); // lista completa
        private ObservableCollection<ComicLibrary> _libraries;
    private bool _hasRecentComics;
    private bool _isRecentListView;
    private int _recentCardColumns = 3;
    private bool _historySubscribed = false;
    private int _currentPage = 1;
    private int _pageSize = 12;
    private int _totalPages = 1;
    private string _sortBy = "Reciente"; // Reciente | Progreso | Nombre
    private bool _hideCompleted = false;
    private List<ContinueItem> _viewRecentComics = new List<ContinueItem>(); // lista filtrada+ordenada para paginar

    public ICommand OpenComicCommand { get; private set; }
    public ICommand RemoveRecentComicCommand { get; private set; }
    public ICommand ReopenCompletedCommand { get; private set; }
    public ICommand ShowInFolderCommand { get; private set; }

        public ObservableCollection<ComicFolderItem> FolderContents
        {
            get => _folderContents;
            set
            {
                _folderContents = value;
                OnPropertyChanged(nameof(FolderContents));
            }
        }

        public string CurrentFolderPath
        {
            get => _currentFolderPath;
            set
            {
                _currentFolderPath = value;
                OnPropertyChanged(nameof(CurrentFolderPath));
            }
        }

        public ObservableCollection<ContinueItem> RecentComics
        {
            get => _recentComics;
            set
            {
                _recentComics = value;
                OnPropertyChanged(nameof(RecentComics));
            }
        }

        public ObservableCollection<ContinueItem> CompletedComics
        {
            get => _completedComics;
            set { _completedComics = value; OnPropertyChanged(nameof(CompletedComics)); }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                var newVal = Math.Max(1, value);
                if (_currentPage != newVal)
                {
                    _currentPage = newVal;
                    OnPropertyChanged(nameof(CurrentPage));
                    UpdatePagination();
                }
            }
        }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                var newVal = Math.Max(4, Math.Min(60, value));
                if (_pageSize != newVal)
                {
                    _pageSize = newVal;
                    OnPropertyChanged(nameof(PageSize));
                    SaveInt("RecentPageSize", _pageSize);
                    UpdatePagination();
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            private set
            {
                if (_totalPages != value)
                {
                    _totalPages = Math.Max(1, value);
                    OnPropertyChanged(nameof(TotalPages));
                    OnPropertyChanged(nameof(HasMultiplePages));
                }
            }
        }

        public bool HasMultiplePages => TotalPages > 1;

        public ObservableCollection<ComicLibrary> Libraries
        {
            get => _libraries;
            set
            {
                _libraries = value;
                OnPropertyChanged(nameof(Libraries));
            }
        }

        public bool HasRecentComics
        {
            get => _hasRecentComics;
            set { _hasRecentComics = value; OnPropertyChanged(nameof(HasRecentComics)); OnPropertyChanged(nameof(HasAnyContinueItems)); }
        }

        public bool IsRecentListView
        {
            get => _isRecentListView;
            set { _isRecentListView = value; OnPropertyChanged(nameof(IsRecentListView)); }
        }

        public int RecentCardColumns
        {
            get => _recentCardColumns;
            set
            {
                _recentCardColumns = Math.Max(1, Math.Min(6, value));
                OnPropertyChanged(nameof(RecentCardColumns));
            }
        }

        public string SortBy
        {
            get => _sortBy;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) value = "Reciente";
                if (_sortBy != value)
                {
                    _sortBy = value;
                    OnPropertyChanged(nameof(SortBy));
                    SaveString("RecentSortBy", _sortBy);
                    RebuildRecentView();
                }
            }
        }

        public bool HideCompleted
        {
            get => _hideCompleted;
            set
            {
                if (_hideCompleted != value)
                {
                    _hideCompleted = value;
                    OnPropertyChanged(nameof(HideCompleted));
                    SaveInt("HideCompleted", _hideCompleted ? 1 : 0);
                    RebuildRecentView();
                }
            }
        }

        public HomeView()
        {
            InitializeComponent();
            this.DataContext = this;
            InitializeData();
            // Asegurar re-suscripción al cargar y liberar al descargar
            this.Loaded += HomeView_Loaded;
            this.Unloaded += HomeView_Unloaded;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void InitializeData()
        {
            FolderContents = new ObservableCollection<ComicFolderItem>();
            RecentComics = new ObservableCollection<ContinueItem>();
            Libraries = new ObservableCollection<ComicLibrary>();
            OpenComicCommand = new RelayCommand(p => OnOpenComicFromCommand(p as string), p => !string.IsNullOrWhiteSpace(p as string));
            RemoveRecentComicCommand = new RelayCommand(p => OnRemoveRecentComic(p as string), p => !string.IsNullOrWhiteSpace(p as string));
            ReopenCompletedCommand = new RelayCommand(p => OnReopenCompleted(p as string), p => !string.IsNullOrWhiteSpace(p as string));
            ShowInFolderCommand = new RelayCommand(p => OnShowInFolder(p as string), p => !string.IsNullOrWhiteSpace(p as string));
            // Preferencias
            IsRecentListView = SettingsManager.Settings.IsRecentListView;
            RecentCardColumns = LoadInt("RecentCardColumns", 3);
            PageSize = LoadInt("RecentPageSize", 24);
            SortBy = LoadString("RecentSortBy", "Reciente");
            HideCompleted = LoadInt("HideCompleted", 0) == 1;
            CurrentPage = 1;

            LoadRecentComics();
            LoadCompletedComics();

            // Debug: imprimir contadores iniciales
            try { System.Diagnostics.Debug.WriteLine($"HomeView.InitializeData: Items={ComicReader.Services.ContinueReadingService.Instance.Items.Count}, Completed={ComicReader.Services.ContinueReadingService.Instance.CompletedItems.Count}"); } catch { }

            // Suscribir cambios directos en las colecciones para refresco inmediato
            try
            {
                var svc = ComicReader.Services.ContinueReadingService.Instance;
                svc.Items.CollectionChanged -= Svc_ItemsChanged;
                svc.CompletedItems.CollectionChanged -= Svc_CompletedItemsChanged;
                svc.Items.CollectionChanged += Svc_ItemsChanged;
                svc.CompletedItems.CollectionChanged += Svc_CompletedItemsChanged;
            }
            catch { }
            LoadLibraries();

            // Refrescar cuando cambie el historial persistente
            SubscribeHistory();
        }

        private void HomeView_Loaded(object sender, RoutedEventArgs e)
        {
            SubscribeHistory();
        }

        private void HomeView_Unloaded(object sender, RoutedEventArgs e)
        {
            // Opcional: dejar suscrito para actualizaciones en background
            // UnsubscribeHistory();
        }

        private void SubscribeHistory()
        {
            try
            {
                if (_historySubscribed) return;
                ComicReader.Services.ContinueReadingService.Instance.ListChanged += SettingsManager_ReadingHistoryChanged;
                _historySubscribed = true;
            }
            catch { }
        }

        private void UnsubscribeHistory()
        {
            try
            {
                if (!_historySubscribed) return;
                ComicReader.Services.ContinueReadingService.Instance.ListChanged -= SettingsManager_ReadingHistoryChanged;
                _historySubscribed = false;
            }
            catch { }
        }

        private void SettingsManager_ReadingHistoryChanged()
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(LoadRecentComics);
                }
                else
                {
                    LoadRecentComics();
                    LoadCompletedComics();
                }
            }
            catch { }
        }

        private void LoadCompletedComics()
        {
            try
            {
                var items = ComicReader.Services.ContinueReadingService.Instance.CompletedItems;
                CompletedComics = new ObservableCollection<ContinueItem>(items ?? new ObservableCollection<ContinueItem>());
                OnPropertyChanged(nameof(CompletedCount));
                OnPropertyChanged(nameof(HasAnyContinueItems));
            }
            catch { }
        }

        public int CompletedCount => CompletedComics?.Count ?? 0;

        private void LoadRecentComics()
        {
            // Cargar desde el nuevo servicio
            var items = ComicReader.Services.ContinueReadingService.Instance.Items;
            _allRecentComics = items.ToList();
            HasRecentComics = _allRecentComics.Count > 0;
            RebuildRecentView();
        }

        // Indica si hay elementos en "Seguir leyendo" o en "Completados"
        public bool HasAnyContinueItems => HasRecentComics || (CompletedComics?.Count ?? 0) > 0;


        private void RebuildRecentView()
        {
            // Aplicar filtro
            IEnumerable<ContinueItem> query = _allRecentComics ?? new List<ContinueItem>();
            if (HideCompleted)
            {
                query = query.Where(c => !(c.IsCompleted || (c.PageCount > 0 && c.LastPage >= c.PageCount)));
            }
            // Aplicar orden
            switch ((SortBy ?? "Reciente").Trim())
            {
                case "Nombre":
                    query = query.OrderBy(c => c.DisplayName, StringComparer.CurrentCultureIgnoreCase);
                    break;
                case "Progreso":
                    query = query.OrderByDescending(c => c.ProgressPercent).ThenByDescending(c => c.LastOpened);
                    break;
                case "Reciente":
                default:
                    query = query.OrderByDescending(c => c.LastOpened).ThenBy(c => c.DisplayName);
                    break;
            }
            _viewRecentComics = query.ToList();
            // Resetear a la primera página si la actual queda fuera de rango
            var newTotal = Math.Max(1, (int)Math.Ceiling(_viewRecentComics.Count / (double)Math.Max(1, PageSize)));
            if (CurrentPage > newTotal) _currentPage = 1;
            UpdatePagination();
        }

        private void UpdatePagination()
        {
            if (_viewRecentComics == null) _viewRecentComics = new List<ContinueItem>();
            TotalPages = Math.Max(1, (int)Math.Ceiling(_viewRecentComics.Count / (double)Math.Max(1, PageSize)));
            if (CurrentPage > TotalPages) _currentPage = TotalPages;

            RecentComics.Clear();
            if (_viewRecentComics.Count == 0) return;

            int skip = (CurrentPage - 1) * PageSize;
            foreach (var comic in _viewRecentComics.Skip(skip).Take(PageSize))
            {
                RecentComics.Add(comic);
                if (comic.CoverThumbnail == null)
                    _ = LoadRecentCoverAsync(comic);
            }
        }

        private void History_ListChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
        {
            // Reconstruir la vista simple para mantener orden y límite
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(LoadRecentComics);
                }
                else
                {
                    LoadRecentComics();
                }
            }
            catch { }
        }

        // Permitir que MainWindow fuerce un refresco al volver a Inicio
        public void RefreshRecent() => LoadRecentComics();

        private void LoadLibraries()
        {
            // Cargar bibliotecas guardadas
            Libraries.Clear();
            Libraries.Add(new ComicLibrary { Name = "Mi Biblioteca", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Comics", ComicCount = 0 });
            Libraries.Add(new ComicLibrary { Name = "Favoritos", Path = "", ComicCount = 0 });
        }

        private void Svc_ItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Svc_ItemsChanged: Items={ComicReader.Services.ContinueReadingService.Instance.Items.Count}");
                // Recargar recientes en UI
                if (!Dispatcher.CheckAccess()) Dispatcher.Invoke(LoadRecentComics);
                else LoadRecentComics();
                OnPropertyChanged(nameof(HasAnyContinueItems));
            }
            catch { }
        }

        private void Svc_CompletedItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Svc_CompletedItemsChanged: Completed={ComicReader.Services.ContinueReadingService.Instance.CompletedItems.Count}");
                if (!Dispatcher.CheckAccess()) Dispatcher.Invoke(LoadCompletedComics);
                else LoadCompletedComics();
                OnPropertyChanged(nameof(HasAnyContinueItems));
            }
            catch { }
        }

        // Navegación del carrusel de Completados
        private void CompletedCarouselPrev_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sv = this.FindName("CompletedScrollViewer") as System.Windows.Controls.ScrollViewer;
                if (sv == null) return;
                // Mover hacia la izquierda por el ancho aproximado de una tarjeta
                double offset = sv.ViewportWidth * 0.7;
                double target = Math.Max(0, sv.HorizontalOffset - offset);
                sv.ScrollToHorizontalOffset(target);
            }
            catch { }
        }

        private void CompletedCarouselNext_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sv2 = this.FindName("CompletedScrollViewer") as System.Windows.Controls.ScrollViewer;
                if (sv2 == null) return;
                double offset2 = sv2.ViewportWidth * 0.7;
                double max = sv2.ExtentWidth - sv2.ViewportWidth;
                double target = Math.Min(max, sv2.HorizontalOffset + offset2);
                sv2.ScrollToHorizontalOffset(target);
            }
            catch { }
        }

        private void OpenFile_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Abrir Comic",
                Filter = "Archivos de Comic|*.cbz;*.cbr;*.zip;*.rar;*.pdf;*.tar;*.epub|Todos los archivos|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var mainWindow = Window.GetWindow(this) as global::ComicReader.MainWindow;
                if (mainWindow != null)
                {
                    // Usar el método privado de MainWindow mediante reflexión o crear método público
                    var method = mainWindow.GetType().GetMethod("OpenComicFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    method?.Invoke(mainWindow, new object[] { openFileDialog.FileName });
                }
            }
        }

        private void EnterReadingMode_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var win = System.Windows.Window.GetWindow(this) as ComicReader.MainWindow;
                win?.EnterReadingMode();
            }
            catch { }
        }

        private void OpenFolder_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                using (var folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Seleccionar carpeta de cómics";
                    folderDialog.ShowNewFolderButton = false;
                    var last = LoadString("LastHomeFolderPath", string.Empty);
                    if (!string.IsNullOrWhiteSpace(last) && Directory.Exists(last))
                        folderDialog.SelectedPath = last;

                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        NavigateToFolder(folderDialog.SelectedPath);
                        SaveString("LastHomeFolderPath", folderDialog.SelectedPath);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"No se pudo abrir la carpeta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadFolderContents(string folderPath)
        {
            try
            {
                FolderContents.Clear();
                var directory = new DirectoryInfo(folderPath);

                // Agregar carpetas
                foreach (var subdir in directory.GetDirectories())
                {
                    var folderItem = new ComicFolderItem
                    {
                        Name = subdir.Name,
                        Path = subdir.FullName,
                        IsFolder = true,
                        Size = "",
                        ComicCount = CountComicsInFolder(subdir.FullName)
                    };
                    FolderContents.Add(folderItem);
                }

                // Agregar archivos de cómic
                var comicExtensions = new[] { ".cbz", ".cbr", ".cb7", ".cbt", ".zip", ".rar", ".7z", ".tar", ".pdf", ".epub", ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".heic", ".tif", ".tiff", ".avif" };
                foreach (var file in directory.GetFiles().Where(f => comicExtensions.Contains(f.Extension.ToLower())))
                {
                    var comicItem = new ComicFolderItem
                    {
                        Name = Path.GetFileNameWithoutExtension(file.Name),
                        Path = file.FullName,
                        IsFolder = false,
                        Size = FormatFileSize(file.Length),
                        LastModified = file.LastWriteTime,
                        Extension = file.Extension.ToUpper().Substring(1)
                    };

                    // Cargar miniatura de forma asíncrona
                    _ = LoadThumbnailAsync(comicItem);
                    FolderContents.Add(comicItem);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al cargar carpeta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int CountComicsInFolder(string folderPath)
        {
            try
            {
                var comicExtensions = new[] { ".cbz", ".cbr", ".cb7", ".cbt", ".zip", ".rar", ".7z", ".tar", ".pdf", ".epub", ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".heic", ".tif", ".tiff", ".avif" };
                return Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                    .Count(f => comicExtensions.Contains(Path.GetExtension(f).ToLower()));
            }
            catch
            {
                return 0;
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }

        private async Task LoadThumbnailAsync(ComicFolderItem item)
        {
            try
            {
                // Miniatura usando el loader central para archivos de cómic; icono para carpetas.
                if (item.IsFolder)
                {
                    // Placeholder simple para carpeta
                    item.Thumbnail = CreateEmojiThumb("📁");
                    return;
                }

                // Reutilizar caché de portadas de recientes para rapidez
                var cached = TryLoadThumbFromCache(item.Path);
                if (cached != null)
                {
                    item.Thumbnail = cached;
                    return;
                }

                BitmapImage cover = null;
                await Task.Run(async () =>
                {
                    using var loader = new ComicPageLoader(item.Path);
                    await loader.LoadComicAsync();
                    cover = await loader.GetCoverThumbnailAsync(320, 240);
                });

                if (cover != null)
                {
                    cover.Freeze();
                    item.Thumbnail = cover;
                    SaveThumbToCache(item.Path, cover);
                }
                else
                {
                    item.Thumbnail = CreateEmojiThumb("📘");
                }
            }
            catch
            {
                item.Thumbnail = CreateEmojiThumb(item.IsFolder ? "📁" : "📘");
            }
        }

        private BitmapImage CreateEmojiThumb(string emoji)
        {
            try
            {
                const int w = 320; const int h = 180;
                var dv = new DrawingVisual();
                using (var dc = dv.RenderOpen())
                {
                    dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(16,23,41)), null, new Rect(0,0,w,h));
                    var ft = new FormattedText(emoji, CultureInfo.CurrentUICulture, System.Windows.FlowDirection.LeftToRight,
                        new Typeface(new FontFamily("Segoe UI Emoji"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                        64, Brushes.White, 1.25);
                    dc.DrawText(ft, new Point((w-ft.Width)/2, (h-ft.Height)/2));
                }
                var rtb = new RenderTargetBitmap(w,h,96,96, PixelFormats.Pbgra32);
                rtb.Render(dv);
                using var ms = new MemoryStream();
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(rtb));
                enc.Save(ms);
                ms.Position = 0;
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch { return null; }
        }

        private void ClearRecentComics_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RecentComics.Clear();
            ComicReader.Services.ContinueReadingService.Instance.Clear();
            System.Windows.MessageBox.Show("Lista de cómics recientes limpiada.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
            HasRecentComics = false;
            _allRecentComics.Clear();
            TotalPages = 1;
            CurrentPage = 1;
            // Also refresh completed
            LoadCompletedComics();
        }

        private void ClearCompleted_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var result = System.Windows.MessageBox.Show("¿Vaciar la lista de completados? Esto no eliminará los archivos.", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
                ComicReader.Services.ContinueReadingService.Instance.ClearCompleted();
                LoadCompletedComics();
                ToastWindow.ShowToast("Completados borrados");
            }
            catch { }
        }

        private void OnReopenCompleted(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;
            OnOpenComicFromCommand(filePath);
        }

        private void CompletedMenu_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (!(sender is System.Windows.FrameworkElement fe)) return;
                var filePath = fe.Tag as string ?? (fe.DataContext is ContinueItem ci ? ci.FilePath : null);
                if (string.IsNullOrWhiteSpace(filePath)) return;
                var menu = new System.Windows.Controls.ContextMenu();
                var miRemove = new System.Windows.Controls.MenuItem { Header = "Eliminar de completados" };
                miRemove.Click += (s, _) => { ComicReader.Services.ContinueReadingService.Instance.Remove(filePath); LoadCompletedComics(); ToastWindow.ShowToast("Eliminado de completados"); };
                var miRate = new System.Windows.Controls.MenuItem { Header = "Valorar" };
                miRate.Click += (s, _) => {
                    var win = new RatingWindow(); win.Owner = Window.GetWindow(this);
                    if (win.ShowDialog() == true)
                    {
                        var item = ComicReader.Services.ContinueReadingService.Instance.CompletedItems.FirstOrDefault(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
                        if (item != null) { item.Rating = win.Stars; item.Review = win.Comment; ComicReader.Services.ContinueReadingService.Instance.Save(); ToastWindow.ShowToast("Valoración guardada"); }
                    }
                };
                var miShare = new System.Windows.Controls.MenuItem { Header = "Compartir" };
                miShare.Click += (s, _) => {
                    // Generar enlace simple (file://)
                    var link = new System.Uri(filePath).AbsoluteUri;
                    System.Windows.Clipboard.SetText(link);
                    ToastWindow.ShowToast("Enlace copiado al portapapeles");
                };
                menu.Items.Add(miRemove);
                menu.Items.Add(miRate);
                menu.Items.Add(miShare);
                menu.IsOpen = true;
            }
            catch { }
        }


        // Métodos para los nuevos botones
        private void OpenComicStats_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var statsWindow = new ComicStatsWindow();
            statsWindow.Owner = Window.GetWindow(this);
            statsWindow.ShowDialog();
        }

        private void OpenFavorites_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var favoritesWindow = new FavoritesWindow();
            favoritesWindow.Owner = Window.GetWindow(this);
            favoritesWindow.ShowDialog();
        }

        // Nuevos métodos para funcionalidades avanzadas
        private void SearchComics_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Abrir ventana de búsqueda avanzada
            var searchWindow = new ComicSearchWindow();
            searchWindow.Owner = Window.GetWindow(this);
            searchWindow.ShowDialog();
        }

        private void ShowStats_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Abrir ventana de estadísticas
            var statsWindow = new ComicStatsWindow();
            statsWindow.Owner = Window.GetWindow(this);
            statsWindow.ShowDialog();
        }

        private void ShowAnnotations_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Abrir herramientas de anotación
            var annotationWindow = new AnnotationToolsWindow();
            annotationWindow.Owner = Window.GetWindow(this);
            annotationWindow.ShowDialog();
        }

        private void StartPresentation_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Iniciar modo presentación
            var presentationWindow = new PresentationModeWindow();
            presentationWindow.Owner = Window.GetWindow(this);
            presentationWindow.Show();
        }

        private void CreateLibrary_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Crear nueva biblioteca
            var library = new ComicLibrary
            {
                Name = $"Nueva Biblioteca {Libraries.Count + 1}",
                Path = "",
                ComicCount = 0,
                DateCreated = DateTime.Now
            };
            Libraries.Add(library);
            System.Windows.MessageBox.Show($"Biblioteca '{library.Name}' creada exitosamente.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenRecentComic_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ContinueItem comic)
            {
                if (File.Exists(comic.FilePath) || Directory.Exists(comic.FilePath))
                {
                    var mainWindow = Window.GetWindow(this) as global::ComicReader.MainWindow;
                    if (mainWindow != null)
                    {
                        var method = mainWindow.GetType().GetMethod("OpenComicFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        method?.Invoke(mainWindow, new object[] { comic.FilePath });
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("El archivo no existe o ha sido movido.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void OnOpenComicFromCommand(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;
            if (!File.Exists(filePath) && !Directory.Exists(filePath))
            {
                System.Windows.MessageBox.Show("El archivo no existe o ha sido movido.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var mainWindow = Window.GetWindow(this) as global::ComicReader.MainWindow;
            if (mainWindow != null)
            {
                var method = mainWindow.GetType().GetMethod("OpenComicFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(mainWindow, new object[] { filePath });
            }
        }

        private void OnRemoveRecentComic(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;
            // Confirmación antes de quitar
            try
            {
                var name = System.IO.Path.GetFileName(filePath);
                var result = System.Windows.MessageBox.Show(
                    $"¿Quitar \"{name}\" de 'Seguir leyendo'?\nEl archivo no se elimina del disco.",
                    "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
            }
            catch { }
            // Quitar de colección y de settings
            var item = RecentComics.FirstOrDefault(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            if (item != null) RecentComics.Remove(item);
            // Borrar usando servicio y repaginar
            ComicReader.Services.ContinueReadingService.Instance.Remove(filePath);
            _allRecentComics = ComicReader.Services.ContinueReadingService.Instance.Items.ToList();
            HasRecentComics = _allRecentComics.Count > 0;
            if (!HasRecentComics)
            {
                RecentComics.Clear();
                TotalPages = 1;
                CurrentPage = 1;
            }
            else
            {
                // Recalcular vista y paginación
                RebuildRecentView();
            }
        }

        // Handlers de paginación
        private void PrevRecentPage_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
            }
        }

        private void RecentComicsListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.ListView lv && lv.SelectedItem is ContinueItem item && !string.IsNullOrWhiteSpace(item.FilePath))
                {
                    OnOpenComicFromCommand(item.FilePath);
                }
            }
            catch { /* no-op */ }
        }

        private void NextRecentPage_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
            }
        }

        private void FirstRecentPage_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (TotalPages > 0)
            {
                CurrentPage = 1;
            }
        }

        private void LastRecentPage_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (TotalPages > 0)
            {
                CurrentPage = TotalPages;
            }
        }

        // Navegación del carrusel "Seguir leyendo"
        private void ContinueCarouselPrev_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var sv = this.FindName("ContinueCarouselScroll") as System.Windows.Controls.ScrollViewer;
                if (sv == null) return;
                // Desplazar una "página" menos el padding
                double page = sv.ViewportWidth * 0.9;
                sv.ScrollToHorizontalOffset(Math.Max(0, sv.HorizontalOffset - page));
            }
            catch { }
        }

        private void ContinueCarouselNext_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var sv = this.FindName("ContinueCarouselScroll") as System.Windows.Controls.ScrollViewer;
                if (sv == null) return;
                double page = sv.ViewportWidth * 0.9;
                sv.ScrollToHorizontalOffset(Math.Min(sv.ScrollableWidth, sv.HorizontalOffset + page));
            }
            catch { }
        }

        private void TabContinue_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                TabContinue.IsChecked = true;
                TabCompleted.IsChecked = false;
                ShowContinueSection();
            }
            catch { }
        }

        private void TabCompleted_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                TabContinue.IsChecked = false;
                TabCompleted.IsChecked = true;
                ShowCompletedSection();
            }
            catch { }
        }

        private void ShowCompletedSection()
        {
            try
            {
                LoadCompletedComics();
                CompletedPanel.Visibility = System.Windows.Visibility.Visible;
                var sb = new System.Windows.Media.Animation.Storyboard();
                var da = new System.Windows.Media.Animation.DoubleAnimation(0, 1, new System.Windows.Duration(TimeSpan.FromMilliseconds(220)));
                System.Windows.Media.Animation.Storyboard.SetTarget(da, CompletedPanel);
                System.Windows.Media.Animation.Storyboard.SetTargetProperty(da, new System.Windows.PropertyPath("Opacity"));
                sb.Children.Add(da);
                sb.Begin();
                // Hide continue elements
                ContinueCarouselRoot.Visibility = System.Windows.Visibility.Collapsed;
                RecentComicsListView.Visibility = System.Windows.Visibility.Collapsed;
            }
            catch { }
        }

        private void ShowContinueSection()
        {
            try
            {
                CompletedPanel.Visibility = System.Windows.Visibility.Collapsed;
                CompletedPanel.Opacity = 0;
                // Restore continue view
                ContinueCarouselRoot.Visibility = IsRecentListView ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
                RecentComicsListView.Visibility = IsRecentListView ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
            catch { }
        }

        // "Ir a página" handlers
        private void GoToRecentPage_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var tb = this.FindName("GoToPageTextBox") as System.Windows.Controls.TextBox;
                if (tb == null) return;
                if (int.TryParse(tb.Text, out var target))
                {
                    if (target < 1) target = 1;
                    if (target > TotalPages) target = TotalPages;
                    CurrentPage = target;
                }
            }
            catch { }
        }

        private void GoToPageTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Solo números
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void GoToPageTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                GoToRecentPage_Click(sender, e);
            }
        }

        private async Task LoadRecentCoverAsync(ContinueItem comic)
        {
            try
            {
                // Placeholder rápido: usa icono de la app si no hay miniatura
                var placeholder = LoadAppIconImage();
                comic.CoverThumbnail = placeholder;

                // Intentar cargar desde caché
                var cached = TryLoadThumbFromCache(comic.FilePath);
                if (cached != null)
                {
                    comic.CoverThumbnail = cached;
                    return;
                }

                // Estrategia unificada: usar el loader central SIEMPRE (archivos y carpetas).
                // Esto habilita detección de formato real (CBR renombrados), y soporte de más imágenes (webp/heic/gif/bmp).
                try
                {
                    if (File.Exists(comic.FilePath) || Directory.Exists(comic.FilePath))
                    {
                        BitmapImage cover = null;
                        await Task.Run(async () =>
                        {
                            using var loader = new ComicPageLoader(comic.FilePath);
                            await loader.LoadComicAsync();
                            cover = await loader.GetCoverThumbnailAsync(400, 600);
                        });

                        if (cover != null)
                        {
                            cover.Freeze();
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                comic.CoverThumbnail = cover;
                                SaveThumbToCache(comic.FilePath, cover);
                            });
                        }
                    }
                }
                catch { }
            }
            catch
            {
                // Mantener placeholder en caso de error
            }
        }

        private BitmapImage LoadAppIconImage()
        {
            try
            {
                // Intentar cargar desde recursos con distintas variantes de pack URI
                // 1) Recurso en la raíz
                var uri1 = new Uri("pack://application:,,,/icono.ico", UriKind.Absolute);
                var bmp1 = new BitmapImage();
                bmp1.BeginInit();
                bmp1.UriSource = uri1;
                bmp1.CacheOption = BitmapCacheOption.OnLoad;
                bmp1.EndInit();
                bmp1.Freeze();
                return bmp1;
            }
            catch
            {
                try
                {
                    // 2) Recurso con assembly explícito
                    var uri2 = new Uri("pack://application:,,,/PercysLibrary;component/icono.ico", UriKind.Absolute);
                    var bmp2 = new BitmapImage();
                    bmp2.BeginInit();
                    bmp2.UriSource = uri2;
                    bmp2.CacheOption = BitmapCacheOption.OnLoad;
                    bmp2.EndInit();
                    bmp2.Freeze();
                    return bmp2;
                }
                catch
                {
                    // 3) Fallback: generar una imagen placeholder en memoria (300x450)
                    try
                    {
                        const int w = 300;
                        const int h = 450;

                        var dv = new DrawingVisual();
                        using (var dc = dv.RenderOpen())
                        {
                            // Fondo degradado
                            var gradient = new LinearGradientBrush(
                                Color.FromRgb(16, 24, 39),
                                Color.FromRgb(55, 65, 81),
                                new Point(0, 0), new Point(1, 1));
                            dc.DrawRectangle(gradient, null, new Rect(0, 0, w, h));

                            // Marco
                            var borderPen = new Pen(new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), 2);
                            dc.DrawRectangle(null, borderPen, new Rect(1, 1, w - 2, h - 2));

                            // Texto central
                            var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
                            var ft = new FormattedText(
                                "Sin portada",
                                CultureInfo.CurrentUICulture,
                                System.Windows.FlowDirection.LeftToRight,
                                typeface,
                                28,
                                Brushes.White,
                                1.0);
                            var textPos = new Point((w - ft.Width) / 2, (h - ft.Height) / 2);
                            dc.DrawText(ft, textPos);
                        }

                        var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                        rtb.Render(dv);

                        // Convertir a BitmapImage mediante un PNG en memoria
                        using var ms = new MemoryStream();
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(rtb));
                        encoder.Save(ms);
                        ms.Position = 0;

                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = ms;
                        bmp.EndInit();
                        bmp.Freeze();
                        return bmp;
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
        }

        // --- Caché de miniaturas en disco ---
        private string GetThumbCachePath(string filePath)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = System.IO.Path.Combine(appData, "PercysLibrary", "Thumbs");
            Directory.CreateDirectory(dir);
            using (var sha1 = System.Security.Cryptography.SHA1.Create())
            {
                var key = ThumbCacheVersion + "|" + filePath;
                var hash = BitConverter.ToString(sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key))).Replace("-", string.Empty);
                return System.IO.Path.Combine(dir, hash + ".png");
            }
        }

        private BitmapImage TryLoadThumbFromCache(string filePath)
        {
            try
            {
                var p = GetThumbCachePath(filePath);
                if (!File.Exists(p)) return null;
                var bmp = new BitmapImage();
                using (var fs = File.OpenRead(p))
                {
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = fs;
                    bmp.EndInit();
                    bmp.Freeze();
                }
                return bmp;
            }
            catch { return null; }
        }

        private void SaveThumbToCache(string filePath, BitmapSource image)
        {
            try
            {
                var p = GetThumbCachePath(filePath);
                using (var fs = File.Open(p, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    encoder.Save(fs);
                }
            }
            catch { }
        }

        private void ShowRecentInFolder_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                // Tomar el elemento seleccionado en la lista de recientes (vía FindName para evitar rojos en diseñador)
                var listBox = this.FindName("RecentComicsList") as System.Windows.Controls.ListBox;
                if (listBox?.SelectedItem is ContinueItem comic && !string.IsNullOrEmpty(comic.FilePath))
                {
                    if (File.Exists(comic.FilePath))
                    {
                        var argument = "/select, \"" + comic.FilePath + "\"";
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = argument,
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("El archivo no existe o ha sido movido.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Selecciona un cómic de la lista para mostrarlo en la carpeta.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"No se pudo abrir el Explorador: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ToggleRecentView_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            IsRecentListView = !IsRecentListView;
            SettingsManager.Settings.IsRecentListView = IsRecentListView;
            SettingsManager.SaveSettings();
        }

        private void IncreaseCardColumns_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RecentCardColumns++;
            SaveInt("RecentCardColumns", RecentCardColumns);
        }

        private void DecreaseCardColumns_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RecentCardColumns--;
            SaveInt("RecentCardColumns", RecentCardColumns);
        }

        private void RecentComicsListView_Loaded(object sender, RoutedEventArgs e)
        {
            // Aplicar anchos si están guardados
            var widths = LoadString("RecentListColumnWidths", string.Empty);
            if (!string.IsNullOrWhiteSpace(widths))
            {
                var parts = widths.Split(',');
                if (parts.Length == 5 && sender is System.Windows.Controls.ListView lv && lv.View is GridView gv)
                {
                    // Proteger ante cambios en el número de columnas
                    int available = gv.Columns.Count;
                    if (available >= 1 && double.TryParse(parts[0], out var w0)) gv.Columns[0].Width = w0;
                    if (available >= 2 && double.TryParse(parts[1], out var w1)) gv.Columns[1].Width = w1;
                    if (available >= 3 && double.TryParse(parts[2], out var w2)) gv.Columns[2].Width = w2;
                    if (available >= 4 && double.TryParse(parts[3], out var w3)) gv.Columns[3].Width = w3;
                    if (available >= 5 && double.TryParse(parts[4], out var w4)) gv.Columns[4].Width = w4;
                }
            }

            // Suscribir para guardar cambios cuando usuario ajusta anchos
            if (sender is System.Windows.Controls.ListView listView)
            {
                listView.SizeChanged -= RecentListView_SizeChanged;
                listView.SizeChanged += RecentListView_SizeChanged;
            }
        }

        private void RecentListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.ListView lv && lv.View is GridView gv)
            {
                var widths = string.Join(",", gv.Columns.Select(c => c.Width.ToString("F0")));
                SaveString("RecentListColumnWidths", widths);
            }
        }

        // --- utilidades simples de kvp en AppData ---
        private string GetUiStatePath()
        {
            var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PercysLibrary");
            Directory.CreateDirectory(dir);
            return System.IO.Path.Combine(dir, "ui_state.ini");
        }

        private void SaveInt(string key, int value)
        {
            SaveString(key, value.ToString());
        }

        private int LoadInt(string key, int defaultValue)
        {
            var s = LoadString(key, null);
            if (int.TryParse(s, out var v)) return v;
            return defaultValue;
        }

        private void SaveString(string key, string value)
        {
            try
            {
                var path = GetUiStatePath();
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (File.Exists(path))
                {
                    foreach (var line in File.ReadAllLines(path))
                    {
                        var idx = line.IndexOf('=');
                        if (idx > 0)
                        {
                            var k = line.Substring(0, idx);
                            var val = line.Substring(idx + 1);
                            dict[k] = val;
                        }
                    }
                }
                dict[key] = value;
                File.WriteAllLines(path, dict.Select(kv => kv.Key + "=" + kv.Value));
            }
            catch { }
        }

        private string LoadString(string key, string defaultValue)
        {
            try
            {
                var path = GetUiStatePath();
                if (!File.Exists(path)) return defaultValue;
                foreach (var line in File.ReadAllLines(path))
                {
                    var idx = line.IndexOf('=');
                    if (idx > 0)
                    {
                        var k = line.Substring(0, idx);
                        if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                            return line.Substring(idx + 1);
                    }
                }
            }
            catch { }
            return defaultValue;
        }

        private void OnShowInFolder(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;
            try
            {
                if (File.Exists(filePath))
                {
                    var argument = "/select, \"" + filePath + "\"";
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = argument,
                        UseShellExecute = true
                    });
                }
                else if (Directory.Exists(filePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = filePath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    System.Windows.MessageBox.Show("El archivo no existe o ha sido movido.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"No se pudo abrir el Explorador: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RecentItem_DoubleClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.ListBoxItem item && item.DataContext is ContinueItem comic)
            {
                OnOpenComicFromCommand(comic.FilePath);
            }
        }

        private void OpenFolderItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ComicFolderItem item)
            {
                if (item.IsFolder)
                {
                    LoadFolderContents(item.Path);
                }
                else
                {
                    // Abrir cómic
                    var mainWindow = Window.GetWindow(this) as global::ComicReader.MainWindow;
                    if (mainWindow != null)
                    {
                        var method = mainWindow.GetType().GetMethod("OpenComicFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        method?.Invoke(mainWindow, new object[] { item.Path });
                    }
                }
            }
        }
        
        // Permite que MainWindow navegue directamente a una carpeta y muestre su contenido
        public void NavigateToFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                System.Windows.MessageBox.Show("La carpeta seleccionada no existe.", "Carpeta no válida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            CurrentFolderPath = folderPath;
            LoadFolderContents(folderPath);
            SaveString("LastHomeFolderPath", folderPath);
        }

        private void BackToHome_Click(object sender, RoutedEventArgs e)
        {
            // Limpiar vista de carpeta
            CurrentFolderPath = null;
            FolderContents.Clear();
        }

        private void UpFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CurrentFolderPath)) return;
                var parent = Directory.GetParent(CurrentFolderPath);
                if (parent == null) return;
                NavigateToFolder(parent.FullName);
            }
            catch { }
        }

        private void OpenCurrentFolderExternal_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(CurrentFolderPath) && Directory.Exists(CurrentFolderPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = CurrentFolderPath,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"No se pudo abrir el Explorador: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Nuevas clases de modelo
    public class ComicFolderItem : INotifyPropertyChanged
    {
        private string _name;
        private string _path;
        private bool _isFolder;
        private string _size;
        private DateTime _lastModified;
        private string _extension;
        private int _comicCount;
        private BitmapSource _thumbnail;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Path
        {
            get => _path;
            set { _path = value; OnPropertyChanged(); }
        }

        public bool IsFolder
        {
            get => _isFolder;
            set { _isFolder = value; OnPropertyChanged(); }
        }

        public string Size
        {
            get => _size;
            set { _size = value; OnPropertyChanged(); }
        }

        public DateTime LastModified
        {
            get => _lastModified;
            set { _lastModified = value; OnPropertyChanged(); }
        }

        public string Extension
        {
            get => _extension;
            set { _extension = value; OnPropertyChanged(); }
        }

        public int ComicCount
        {
            get => _comicCount;
            set { _comicCount = value; OnPropertyChanged(); }
        }

        public BitmapSource Thumbnail
        {
            get => _thumbnail;
            set { _thumbnail = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}