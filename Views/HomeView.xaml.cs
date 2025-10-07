using System;
using System.ComponentModel;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Forms;
using WpfApp = System.Windows.Application;
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
using System.Collections.Concurrent;
using System.Threading;
using ComicReader.Core.Services;
using System.Windows.Data;

namespace ComicReader.Views
{
    public partial class HomeView : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
    // Directorio de caché de miniaturas (creado una sola vez)
    private static string? _thumbCacheDir;
    private static readonly object _thumbCacheDirLock = new object();
    // Cambiar este valor cuando mejoremos la selección de portada para invalidar caché
        private const string ThumbCacheVersion = "v2";
        private static readonly SemaphoreSlim _coverLoadSemaphore = new SemaphoreSlim(3);
        private static readonly ConcurrentDictionary<string, Task<BitmapImage?>> _coverLoadTasks = new ConcurrentDictionary<string, Task<BitmapImage?>>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _appIconLock = new object();
        private static BitmapImage? _sharedAppIcon;
        private readonly IImageCacheService? _imageCacheService = ServiceLocator.TryGet<IImageCacheService>();
            private ObservableCollection<ComicFolderItem> _folderContents = new ObservableCollection<ComicFolderItem>();
            private string? _currentFolderPath;
            private string _folderSearchTerm = string.Empty;
            private int _filteredFolderCount;
            private string _folderResultsSummary = string.Empty;
    private ObservableCollection<ContinueItem> _recentComics = new ObservableCollection<ContinueItem>(); // página actual
    private List<ContinueItem> _allRecentComics = new List<ContinueItem>(); // lista completa
    private ObservableCollection<ComicLibrary> _libraries = new ObservableCollection<ComicLibrary>();
    private bool _hasRecentComics;
    private bool _hasFilteredRecentComics;
    private bool _isRecentListView;
    private int _recentCardColumns = 3;
    private bool _historySubscribed = false;
    private int _currentPage = 1;
    private int _pageSize = 12;
    private int _totalPages = 1;
    private string _sortBy = "Reciente"; // Reciente | Progreso | Nombre
    private bool _hideCompleted = false;
    private List<ContinueItem> _viewRecentComics = new List<ContinueItem>(); // lista filtrada+ordenada para paginar
    private bool _continueCompactMode;
    private string _continueFilter = "Todos";
    private string _continueSearchTerm = string.Empty;
    private string _continueResultsSummary = string.Empty;

    public ICommand OpenComicCommand { get; private set; } = new RelayCommand(_ => { }, _ => false);
    public ICommand RemoveRecentComicCommand { get; private set; } = new RelayCommand(_ => { }, _ => false);
    public ICommand ShowInFolderCommand { get; private set; } = new RelayCommand(_ => { }, _ => false);

        public ObservableCollection<ComicFolderItem> FolderContents
        {
            get => _folderContents;
            set
            {
                _folderContents = value;
                OnPropertyChanged(nameof(FolderContents));
            }
        }

        public string? CurrentFolderPath
        {
            get => _currentFolderPath;
            set
            {
                _currentFolderPath = value;
                OnPropertyChanged(nameof(CurrentFolderPath));
            }
        }

        public string FolderSearchTerm
        {
            get => _folderSearchTerm;
            set
            {
                value ??= string.Empty;
                if (_folderSearchTerm != value)
                {
                    _folderSearchTerm = value;
                    OnPropertyChanged(nameof(FolderSearchTerm));
                }

                OnPropertyChanged(nameof(HasFolderSearchTerm));
                FilterFolderContents();
            }
        }

        public bool HasFolderSearchTerm => !string.IsNullOrWhiteSpace(_folderSearchTerm);

        public int FilteredFolderCount
        {
            get => _filteredFolderCount;
            private set
            {
                if (_filteredFolderCount != value)
                {
                    _filteredFolderCount = value;
                    OnPropertyChanged(nameof(FilteredFolderCount));
                    OnPropertyChanged(nameof(HasFolderResults));
                }
            }
        }

        public bool HasFolderResults => FilteredFolderCount > 0;

        public string FolderResultsSummary
        {
            get => _folderResultsSummary;
            private set
            {
                if (!string.Equals(_folderResultsSummary, value, StringComparison.Ordinal))
                {
                    _folderResultsSummary = value;
                    OnPropertyChanged(nameof(FolderResultsSummary));
                    OnPropertyChanged(nameof(HasFolderResultsSummary));
                }
            }
        }

        public bool HasFolderResultsSummary => !string.IsNullOrWhiteSpace(_folderResultsSummary);

        public ObservableCollection<ContinueItem> RecentComics
        {
            get => _recentComics;
            set
            {
                _recentComics = value;
                OnPropertyChanged(nameof(RecentComics));
            }
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

        public int TotalRecentCount => _allRecentComics?.Count ?? 0;
        public int FilteredRecentCount => _viewRecentComics?.Count ?? 0;
        public string ContinueResultsSummary
        {
            get => _continueResultsSummary;
            private set
            {
                if (!string.Equals(_continueResultsSummary, value, StringComparison.Ordinal))
                {
                    _continueResultsSummary = value;
                    OnPropertyChanged(nameof(ContinueResultsSummary));
                }
            }
        }

        public bool HasRecentComics
        {
            get => _hasRecentComics;
            set { _hasRecentComics = value; OnPropertyChanged(nameof(HasRecentComics)); }
        }

        public bool HasFilteredRecentComics
        {
            get => _hasFilteredRecentComics;
            private set
            {
                if (_hasFilteredRecentComics != value)
                {
                    _hasFilteredRecentComics = value;
                    OnPropertyChanged(nameof(HasFilteredRecentComics));
                }
            }
        }

        public bool IsRecentListView
        {
            get => _isRecentListView;
            set { _isRecentListView = value; OnPropertyChanged(nameof(IsRecentListView)); }
        }

        public bool ContinueCompactMode
        {
            get => _continueCompactMode;
            set
            {
                if (_continueCompactMode != value)
                {
                    _continueCompactMode = value;
                    OnPropertyChanged(nameof(ContinueCompactMode));
                    try
                    {
                        SettingsManager.Settings.ContinueCompactMode = value;
                        SettingsManager.SaveSettings();
                    }
                    catch { }
                    RebuildRecentView();
                }
            }
        }

        public string ContinueFilter
        {
            get => _continueFilter;
            set
            {
                value = (value ?? string.Empty).Trim();
                if (!string.Equals(value, "Todos", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(value, "En progreso", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(value, "Terminados", StringComparison.OrdinalIgnoreCase))
                {
                    value = "Todos";
                }
                if (!string.Equals(_continueFilter, value, StringComparison.OrdinalIgnoreCase))
                {
                    _continueFilter = value;
                    OnPropertyChanged(nameof(ContinueFilter));
                    SaveString("ContinueFilter", _continueFilter);
                    RebuildRecentView();
                }
            }
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

        public string ContinueSearchTerm
        {
            get => _continueSearchTerm;
            set
            {
                value ??= string.Empty;
                if (_continueSearchTerm != value)
                {
                    _continueSearchTerm = value;
                    OnPropertyChanged(nameof(ContinueSearchTerm));
                    OnPropertyChanged(nameof(HasContinueSearchTerm));
                    SaveString("ContinueSearchTerm", _continueSearchTerm);
                    RebuildRecentView();
                }
            }
        }

        public bool HasContinueSearchTerm => !string.IsNullOrWhiteSpace(_continueSearchTerm);

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

    public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }

        private void InitializeData()
        {
            FolderContents = new ObservableCollection<ComicFolderItem>();
            RecentComics = new ObservableCollection<ContinueItem>();
            Libraries = new ObservableCollection<ComicLibrary>();
            OpenComicCommand = new RelayCommand(p => OnOpenComicFromCommand(p as string), p => !string.IsNullOrWhiteSpace(p as string));
            RemoveRecentComicCommand = new RelayCommand(p => OnRemoveRecentComic(p as string), p => !string.IsNullOrWhiteSpace(p as string));
            ShowInFolderCommand = new RelayCommand(p => OnShowInFolder(p as string), p => !string.IsNullOrWhiteSpace(p as string));
            // Preferencias
            var settings = SettingsManager.Settings;
            IsRecentListView = settings.IsRecentListView;
            _continueCompactMode = settings.ContinueCompactMode;
            _continueFilter = LoadString("ContinueFilter", "Todos");
            _continueSearchTerm = LoadString("ContinueSearchTerm", string.Empty);
            OnPropertyChanged(nameof(ContinueCompactMode));
            OnPropertyChanged(nameof(ContinueFilter));
            OnPropertyChanged(nameof(ContinueSearchTerm));
            OnPropertyChanged(nameof(HasContinueSearchTerm));
            RecentCardColumns = LoadInt("RecentCardColumns", 3);
            PageSize = LoadInt("RecentPageSize", 24);
            SortBy = LoadString("RecentSortBy", "Reciente");
            HideCompleted = LoadInt("HideCompleted", 0) == 1;
            CurrentPage = 1;

            LoadRecentComics();
            LoadLibraries();

            FilterFolderContents();

            // Refrescar cuando cambie el historial persistente
            SubscribeHistory();
        }

        private void HomeView_Loaded(object sender, RoutedEventArgs e)
        {
            SubscribeHistory();
            
            // Asegurar que el fondo del HomeView esté correctamente inicializado
            EnsureHomeBackgroundIsSet();
        }
        
        /// <summary>
        /// Verifica que el fondo del HomeView esté correctamente configurado y lo inicializa si es necesario
        /// </summary>
        private void EnsureHomeBackgroundIsSet()
        {
            try
            {
                // Verificar si el DynamicHomeBackgroundBrush está correctamente configurado
                if (WpfApp.Current.Resources.Contains("DynamicHomeBackgroundBrush"))
                {
                    var currentBrush = WpfApp.Current.Resources["DynamicHomeBackgroundBrush"] as Brush;
                    if (currentBrush != null)
                    {
                        // El fondo ya está configurado correctamente
                        return;
                    }
                }
                
                // Si llegamos aquí, necesitamos aplicar el fondo configurado
                var backgroundName = SettingsManager.Settings?.HomeBackground ?? "SupermanHomeBackground";
                SettingsManager.ApplyHomeBackgroundImmediate(backgroundName);
            }
            catch (Exception ex)
            {
                // En caso de error, aplicar fondo por defecto y registrar el problema
                Logger.LogException("Error aplicando fondo dinámico del HomeView", ex);
                try
                {
                    SettingsManager.ApplyHomeBackgroundImmediate("SupermanHomeBackground");
                }
                catch (Exception ex2)
                {
                    Logger.LogException("No se pudo aplicar el fondo por defecto del HomeView", ex2);
                }
            }
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
            catch (Exception ex)
            {
                Logger.LogException("Error suscribiéndose al historial de lectura", ex);
            }
        }

        private void UnsubscribeHistory()
        {
            try
            {
                if (!_historySubscribed) return;
                ComicReader.Services.ContinueReadingService.Instance.ListChanged -= SettingsManager_ReadingHistoryChanged;
                _historySubscribed = false;
            }
            catch (Exception ex)
            {
                Logger.LogException("Error desuscribiéndose del historial de lectura", ex);
            }
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
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("Error manejando evento de cambio en historial de lectura", ex);
            }
        }

        private void LoadRecentComics()
        {
            // Cargar desde el nuevo servicio
            var items = ComicReader.Services.ContinueReadingService.Instance.Items;
            _allRecentComics = items.ToList();
            OnPropertyChanged(nameof(TotalRecentCount));
            HasRecentComics = _allRecentComics.Count > 0;
            RebuildRecentView();
        }

        private void RebuildRecentView()
        {
            IEnumerable<ContinueItem> query = _allRecentComics ?? Enumerable.Empty<ContinueItem>();
            var filterValue = (ContinueFilter ?? "Todos").Trim();

            static bool IsCompleted(ContinueItem c) => c != null && (c.IsCompleted || (c.PageCount > 0 && c.LastPage >= c.PageCount));

            if (string.Equals(filterValue, "Terminados", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c => IsCompleted(c));
            }
            else if (string.Equals(filterValue, "En progreso", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c => !IsCompleted(c));
            }
            else if (HideCompleted)
            {
                query = query.Where(c => !IsCompleted(c));
            }

            query = query.Where(MatchesSearchTerm);
            // Aplicar orden
            switch ((SortBy ?? "Reciente").Trim())
            {
                case "Nombre":
                    query = query
                        .OrderBy(c => c.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                        .ThenByDescending(c => c.LastOpened);
                    break;
                case "Progreso":
                    query = query
                        .OrderByDescending(c => c.ProgressPercent)
                        .ThenByDescending(c => c.LastOpened);
                    break;
                case "Reciente":
                default:
                    query = query
                        .OrderByDescending(c => c.LastOpened)
                        .ThenBy(c => c.DisplayName);
                    break;
            }
            _viewRecentComics = query.ToList();
            HasFilteredRecentComics = _viewRecentComics.Count > 0;
            OnPropertyChanged(nameof(FilteredRecentCount));
            // Resetear a la primera página si la actual queda fuera de rango
            var newTotal = Math.Max(1, (int)Math.Ceiling(_viewRecentComics.Count / (double)Math.Max(1, PageSize)));
            if (CurrentPage > newTotal) _currentPage = 1;
            UpdatePagination();
        }

        private bool MatchesSearchTerm(ContinueItem? item)
        {
            if (item == null) return false;
            var term = (_continueSearchTerm ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(term)) return true;

            if (!string.IsNullOrEmpty(item.DisplayName) && item.DisplayName.IndexOf(term, StringComparison.CurrentCultureIgnoreCase) >= 0)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(item.FilePath) && item.FilePath.IndexOf(term, StringComparison.CurrentCultureIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }

        private void UpdatePagination()
        {
            if (_viewRecentComics == null) _viewRecentComics = new List<ContinueItem>();
            TotalPages = Math.Max(1, (int)Math.Ceiling(_viewRecentComics.Count / (double)Math.Max(1, PageSize)));
            if (CurrentPage > TotalPages) _currentPage = TotalPages;

            RecentComics.Clear();
            if (_viewRecentComics.Count == 0)
            {
                UpdateResultsSummary();
                return;
            }

            int skip = (CurrentPage - 1) * PageSize;
            foreach (var comic in _viewRecentComics.Skip(skip).Take(PageSize))
            {
                RecentComics.Add(comic);
                if (comic.CoverThumbnail == null)
                    _ = LoadRecentCoverAsync(comic);
            }

            UpdateResultsSummary();
        }

        private void UpdateResultsSummary()
        {
            int total = TotalRecentCount;
            int filtered = FilteredRecentCount;
            var filterValue = (ContinueFilter ?? "Todos").Trim();

            string summary;
            if (total == 0)
            {
                summary = "Aún no hay lecturas recientes.";
            }
            else if (HasContinueSearchTerm)
            {
                summary = filtered > 0
                    ? $"Coincidencias: {filtered} de {total}"
                    : $"Sin coincidencias (0 de {total})";
            }
            else if (string.Equals(filterValue, "Terminados", StringComparison.OrdinalIgnoreCase))
            {
                summary = filtered > 0
                    ? $"Terminados: {filtered} de {total} • Doble clic para reiniciar desde la página 1."
                    : "Sin lecturas terminadas registradas.";
            }
            else if (string.Equals(filterValue, "En progreso", StringComparison.OrdinalIgnoreCase))
            {
                summary = filtered > 0
                    ? $"En progreso: {filtered} de {total}"
                    : "No hay lecturas en progreso.";
            }
            else if (HideCompleted && filtered != total)
            {
                summary = $"En progreso: {filtered} de {total}";
            }
            else
            {
                summary = filtered == total
                    ? $"Mostrando {filtered} títulos"
                    : $"Filtrados {filtered} de {total}";
            }

            if (TotalPages > 1)
            {
                summary += $" • Página {CurrentPage}/{TotalPages}";
            }

            ContinueResultsSummary = summary;
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
            catch (Exception ex)
            {
                Logger.LogException("Error en History_ListChanged", ex);
            }
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
                    // Usar el nuevo método que verifica cómics terminados
                    var method = mainWindow.GetType().GetMethod("OpenComicFileWithCompletedCheck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (method != null)
                    {
                        method.Invoke(mainWindow, new object[] { openFileDialog.FileName });
                    }
                    else
                    {
                        // Fallback al método original
                        var fallbackMethod = mainWindow.GetType().GetMethod("OpenComicFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        fallbackMethod?.Invoke(mainWindow, new object[] { openFileDialog.FileName });
                    }
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
            catch (Exception ex)
            {
                Logger.LogException("Error entrando en modo lectura", ex);
            }
        }

        private void OpenFolder_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                using (var folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Seleccionar carpeta de cómics";
                    folderDialog.ShowNewFolderButton = false;
                    var last = LoadString("LastHomeFolderPath", string.Empty) ?? string.Empty;
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
            finally
            {
                FilterFolderContents();
            }
        }

        private void FilterFolderContents()
        {
            try
            {
                var view = CollectionViewSource.GetDefaultView(FolderContents);
                if (view == null) return;

                var term = (FolderSearchTerm ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(term))
                {
                    view.Filter = null;
                }
                else
                {
                    var comparison = StringComparison.CurrentCultureIgnoreCase;
                    view.Filter = item =>
                    {
                        if (item is ComicFolderItem folderItem)
                        {
                            return (!string.IsNullOrEmpty(folderItem.Name) && folderItem.Name.IndexOf(term, comparison) >= 0)
                                || (!string.IsNullOrEmpty(folderItem.Path) && folderItem.Path.IndexOf(term, comparison) >= 0)
                                || (!string.IsNullOrEmpty(folderItem.Extension) && folderItem.Extension.IndexOf(term, comparison) >= 0);
                        }
                        return false;
                    };
                }

                view.Refresh();

                var totalItems = FolderContents?.Count ?? 0;
                var visibleCount = view.Cast<object>().Count();
                FilteredFolderCount = visibleCount;
                UpdateFolderResultsSummary(visibleCount, totalItems);
            }
            catch (Exception ex)
            {
                Logger.LogException("Error filtrando el contenido de la carpeta", ex);
                var totalItems = FolderContents?.Count ?? 0;
                FilteredFolderCount = totalItems;
                UpdateFolderResultsSummary(FilteredFolderCount, totalItems);
            }
        }

        private void UpdateFolderResultsSummary(int visibleCount, int totalItems)
        {
            try
            {
                var hasSearch = HasFolderSearchTerm;
                var searchTerm = (FolderSearchTerm ?? string.Empty).Trim();

                if (totalItems <= 0)
                {
                    if (hasSearch && !string.IsNullOrEmpty(searchTerm))
                    {
                        FolderResultsSummary = string.Format(CultureInfo.CurrentCulture, "Sin coincidencias para «{0}».", searchTerm);
                    }
                    else
                    {
                        FolderResultsSummary = "Esta carpeta está vacía.";
                    }
                    return;
                }

                if (visibleCount <= 0)
                {
                    if (hasSearch && !string.IsNullOrEmpty(searchTerm))
                    {
                        FolderResultsSummary = string.Format(CultureInfo.CurrentCulture, "Sin coincidencias para «{0}».", searchTerm);
                    }
                    else
                    {
                        FolderResultsSummary = "Esta carpeta está vacía.";
                    }
                    return;
                }

                if (!hasSearch)
                {
                    FolderResultsSummary = visibleCount == 1
                        ? "1 elemento en la carpeta."
                        : string.Format(CultureInfo.CurrentCulture, "{0} elementos en la carpeta.", visibleCount);
                    return;
                }

                if (visibleCount == totalItems)
                {
                    FolderResultsSummary = visibleCount == 1
                        ? "1 resultado en la carpeta."
                        : string.Format(CultureInfo.CurrentCulture, "{0} resultados en la carpeta.", visibleCount);
                    return;
                }

                FolderResultsSummary = string.Format(CultureInfo.CurrentCulture, "Mostrando {0} de {1} elementos.", visibleCount, totalItems);
            }
            catch
            {
                FolderResultsSummary = string.Empty;
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
            catch (Exception ex)
            {
                Logger.LogException($"Error contando cómics en carpeta: {folderPath}", ex);
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
                if (item.IsFolder)
                {
                    item.Thumbnail = CreateEmojiThumb("📁");
                    return;
                }

                var cacheKey = BuildMemoryCacheKey(item.Path, 320, 240);
                var memoryCached = _imageCacheService?.Get(cacheKey);
                if (memoryCached != null)
                {
                    item.Thumbnail = memoryCached;
                    return;
                }

                var cached = TryLoadThumbFromCache(item.Path, 320, 240);
                if (cached != null)
                {
                    item.Thumbnail = cached;
                    _imageCacheService?.Set(cacheKey, cached);
                    return;
                }

                var cover = await FetchCoverAsync(item.Path, 320, 240).ConfigureAwait(false);
                if (cover != null)
                {
                    await Dispatcher.InvokeAsync(() => item.Thumbnail = cover);
                    _imageCacheService?.Set(cacheKey, cover);
                    SaveThumbToCache(item.Path, cover, 320, 240);
                }
                else
                {
                    item.Thumbnail = CreateEmojiThumb("📘");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException($"Error cargando miniatura para {item?.Path}", ex);
                item.Thumbnail = CreateEmojiThumb(item.IsFolder ? "📁" : "📘");
            }
        }

        private string BuildCoverLoadKey(string filePath, int width, int height)
        {
            return ($"{width}x{height}|{filePath}").ToLowerInvariant();
        }

        private string BuildMemoryCacheKey(string filePath, int width, int height)
        {
            return ($"continue::{width}x{height}::{filePath}").ToLowerInvariant();
        }

        private async Task<BitmapImage?> FetchCoverAsync(string filePath, int width, int height)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return null;
            if (!File.Exists(filePath) && !Directory.Exists(filePath)) return null;

            var key = BuildCoverLoadKey(filePath, width, height);
            var loadTask = _coverLoadTasks.GetOrAdd(key, _ => LoadCoverInternalAsync(filePath, width, height));
            try
            {
                return await loadTask.ConfigureAwait(false);
            }
            finally
            {
                if (_coverLoadTasks.TryGetValue(key, out var existing) && ReferenceEquals(existing, loadTask))
                {
                    _coverLoadTasks.TryRemove(key, out _);
                }
            }
        }

        private async Task<BitmapImage?> LoadCoverInternalAsync(string filePath, int width, int height)
        {
            await _coverLoadSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                using var loader = new ComicPageLoader(filePath);
                await loader.LoadComicAsync().ConfigureAwait(false);
                var cover = await loader.GetCoverThumbnailAsync(width, height).ConfigureAwait(false);
                if (cover != null)
                {
                    cover.Freeze();
                }
                return cover;
            }
            catch (Exception ex)
            {
                Logger.LogException($"Error cargando portada para {filePath}", ex);
                return null;
            }
            finally
            {
                _coverLoadSemaphore.Release();
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
            catch (Exception ex)
            {
                Logger.LogException("Error creando miniatura emoji", ex);
                return new BitmapImage();
            }
        }

        private void ClearRecentComics_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RecentComics.Clear();
            ComicReader.Services.ContinueReadingService.Instance.Clear();
            System.Windows.MessageBox.Show("Lista de cómics recientes limpiada.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
            HasRecentComics = false;
            HasFilteredRecentComics = false;
            _allRecentComics.Clear();
            if (_viewRecentComics == null)
            {
                _viewRecentComics = new List<ContinueItem>();
            }
            else
            {
                _viewRecentComics.Clear();
            }
            OnPropertyChanged(nameof(FilteredRecentCount));
            TotalPages = 1;
            CurrentPage = 1;
            ContinueResultsSummary = "Aún no hay lecturas recientes.";
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

        private void OnOpenComicFromCommand(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;
            if (!File.Exists(filePath) && !Directory.Exists(filePath))
            {
                System.Windows.MessageBox.Show("El archivo no existe o ha sido movido.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var mainWindow = Window.GetWindow(this) as ComicReader.MainWindow;
            if (mainWindow != null)
            {
                bool restart = string.Equals(ContinueFilter, "Terminados", StringComparison.OrdinalIgnoreCase);
                if (restart)
                {
                    try
                    {
                        var existing = ComicReader.Services.ContinueReadingService.Instance.Items
                            .FirstOrDefault(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
                        restart = existing != null && (existing.IsCompleted || (existing.PageCount > 0 && existing.LastPage >= existing.PageCount));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException("Error verificando estado de reinicio para cómic", ex);
                        restart = false;
                    }
                }
                // TODO: Fix - mainWindow.OpenComicFromHome(filePath, restart);
            }
        }

        private void OnRemoveRecentComic(string? filePath)
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
                HasFilteredRecentComics = false;
                TotalPages = 1;
                CurrentPage = 1;
            }
            else
            {
                // Recalcular vista y paginación
                RebuildRecentView();
            }
        }

        private void ContinueFilterChip_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button)
                {
                    var value = button.Tag as string;
                    if (string.IsNullOrWhiteSpace(value) && button.Content is string text)
                    {
                        value = text;
                    }
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        ContinueFilter = value;
                    }
                }
            }
            catch { }
        }

        private void ClearContinueSearch_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ContinueSearchTerm))
            {
                ContinueSearchTerm = string.Empty;
            }

            if (this.FindName("ContinueSearchBox") is System.Windows.Controls.TextBox searchBox)
            {
                searchBox.Focus();
            }
        }

        private void MarkOrRestart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is FrameworkElement fe && fe.DataContext is ContinueItem item)
                {
                    // Si está completado, reiniciar a página 1. Si no, marcar como completado.
                    if (item.IsCompleted || (item.PageCount > 0 && item.LastPage >= item.PageCount))
                    {
                        // Reiniciar
                        item.LastPage = 1;
                        item.IsCompleted = false;
                    }
                    else
                    {
                        // Marcar como completado
                        item.LastPage = item.PageCount > 0 ? item.PageCount : item.LastPage;
                        item.IsCompleted = true;
                    }

                    // Actualizar servicio y persistir
                    try
                    {
                        ComicReader.Services.ContinueReadingService.Instance.UpsertProgress(item.FilePath, item.LastPage, item.PageCount);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException("Error actualizando progreso desde MarkOrRestart_Click", ex);
                    }

                    // Forzar actualización de la vista
                    RebuildRecentView();
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("Error en MarkOrRestart_Click", ex);
            }
        }

        private void ContinueSearchBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape && !string.IsNullOrEmpty(ContinueSearchTerm))
            {
                ContinueSearchTerm = string.Empty;
                e.Handled = true;
            }
        }

        private void ClearFolderSearch_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(FolderSearchTerm))
            {
                FolderSearchTerm = string.Empty;
            }

            if (this.FindName("FolderSearchBox") is System.Windows.Controls.TextBox searchBox)
            {
                searchBox.Clear();
                searchBox.Focus();
            }
        }

        private void FolderSearchBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape && HasFolderSearchTerm)
            {
                FolderSearchTerm = string.Empty;
                e.Handled = true;
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
            catch (Exception ex)
            {
                Logger.LogException("Error manejando doble clic en lista de recientes", ex);
                /* no-op */
            }
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
                var placeholder = LoadAppIconImage();
                if (placeholder != null && comic.CoverThumbnail == null)
                {
                    comic.CoverThumbnail = placeholder;
                }

                var cacheKey = BuildMemoryCacheKey(comic.FilePath, 400, 600);
                var memoryCached = _imageCacheService?.Get(cacheKey);
                if (memoryCached != null)
                {
                    comic.CoverThumbnail = memoryCached;
                    return;
                }

                var cached = TryLoadThumbFromCache(comic.FilePath, 400, 600);
                if (cached != null)
                {
                    comic.CoverThumbnail = cached;
                    _imageCacheService?.Set(cacheKey, cached);
                    return;
                }

                var cover = await FetchCoverAsync(comic.FilePath, 400, 600).ConfigureAwait(false);
                if (cover == null) return;

                if (!Dispatcher.CheckAccess())
                {
                    await Dispatcher.InvokeAsync(() => comic.CoverThumbnail = cover);
                }
                else
                {
                    comic.CoverThumbnail = cover;
                }

                _imageCacheService?.Set(cacheKey, cover);
                SaveThumbToCache(comic.FilePath, cover, 400, 600);
            }
            catch
            {
                // Mantener placeholder en caso de error
            }
        }

        private BitmapImage? LoadAppIconImage()
        {
            if (_sharedAppIcon != null)
            {
                return _sharedAppIcon;
            }

            lock (_appIconLock)
            {
                if (_sharedAppIcon != null)
                {
                    return _sharedAppIcon;
                }

                try
                {
                    var uri1 = new Uri("pack://application:,,,/icono.ico", UriKind.Absolute);
                    var bmp1 = new BitmapImage();
                    bmp1.BeginInit();
                    bmp1.UriSource = uri1;
                    bmp1.CacheOption = BitmapCacheOption.OnLoad;
                    bmp1.EndInit();
                    bmp1.Freeze();
                    _sharedAppIcon = bmp1;
                    return _sharedAppIcon;
                }
                catch
                {
                    try
                    {
                        var uri2 = new Uri("pack://application:,,,/PercysLibrary;component/icono.ico", UriKind.Absolute);
                        var bmp2 = new BitmapImage();
                        bmp2.BeginInit();
                        bmp2.UriSource = uri2;
                        bmp2.CacheOption = BitmapCacheOption.OnLoad;
                        bmp2.EndInit();
                        bmp2.Freeze();
                        _sharedAppIcon = bmp2;
                        return _sharedAppIcon;
                    }
                    catch
                    {
                        try
                        {
                            const int w = 300;
                            const int h = 450;
                            var dv = new DrawingVisual();
                            using (var dc = dv.RenderOpen())
                            {
                                var gradient = new LinearGradientBrush(
                                    Color.FromRgb(16, 24, 39),
                                    Color.FromRgb(55, 65, 81),
                                    new Point(0, 0), new Point(1, 1));
                                dc.DrawRectangle(gradient, null, new Rect(0, 0, w, h));

                                var borderPen = new Pen(new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), 2);
                                dc.DrawRectangle(null, borderPen, new Rect(1, 1, w - 2, h - 2));

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
                            _sharedAppIcon = bmp;
                            return _sharedAppIcon;
                        }
                        catch
                        {
                            _sharedAppIcon = null;
                            return null;
                        }
                    }
                }
            }
        }

        // --- Caché de miniaturas en disco ---
        private string GetThumbCachePath(string filePath, int width = 0, int height = 0)
        {
            // Inicializar una sola vez el directorio de caché para reducir el costo y problemas con OneDrive
            if (string.IsNullOrEmpty(_thumbCacheDir))
            {
                lock (_thumbCacheDirLock)
                {
                    if (string.IsNullOrEmpty(_thumbCacheDir))
                    {
                        try
                        {
                            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                            var cacheDir = System.IO.Path.Combine(appData, "PercysLibrary", "Thumbs");
                            Directory.CreateDirectory(cacheDir);
                            _thumbCacheDir = cacheDir;
                        }
                        catch (Exception ex)
                        {
                            // Si no podemos crear el directorio de caché, usar carpeta temporal como fallback
                            Logger.LogException("No se pudo crear el directorio de caché de miniaturas, usando temp.", ex);
                            _thumbCacheDir = System.IO.Path.GetTempPath();
                        }
                    }
                }
            }
            var cacheRoot = _thumbCacheDir ?? System.IO.Path.GetTempPath();
            using (var sha1 = System.Security.Cryptography.SHA1.Create())
            {
                var sizeToken = width > 0 && height > 0 ? $"{width}x{height}" : "auto";
                var key = ThumbCacheVersion + "|" + sizeToken + "|" + filePath;
                var hash = BitConverter.ToString(sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key))).Replace("-", string.Empty);
                return System.IO.Path.Combine(cacheRoot, hash + ".png");
            }
        }

        private BitmapImage? TryLoadThumbFromCache(string filePath, int width = 0, int height = 0)
        {
            try
            {
                var p = GetThumbCachePath(filePath, width, height);
                if (!File.Exists(p)) return null;
                // Abrir con FileShare.Read para coexistir con acciones de OneDrive/escritores
                using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = fs;
                    bmp.EndInit();
                    bmp.Freeze();
                    return bmp;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException($"Falló TryLoadThumbFromCache para {filePath}", ex);
                return null;
            }
        }

        private void SaveThumbToCache(string filePath, BitmapSource? image, int width = 0, int height = 0)
        {
            try
            {
                if (image == null) return;
                if (width <= 0) width = image.PixelWidth;
                if (height <= 0) height = image.PixelHeight;
                var p = GetThumbCachePath(filePath, width, height);

                // Guardar de forma atómica: escribir en temp y luego mover/replace
                var dir = System.IO.Path.GetDirectoryName(p) ?? (_thumbCacheDir ?? System.IO.Path.GetTempPath());
                Directory.CreateDirectory(dir);
                var tmp = System.IO.Path.Combine(dir, System.IO.Path.GetFileName(p) + ".tmp");

                using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    encoder.Save(fs);
                    fs.Flush(true);
                }

                // Reemplazar de forma segura
                try
                {
                    if (File.Exists(p)) File.Delete(p);
                    File.Move(tmp, p);
                }
                catch (Exception exMove)
                {
                    Logger.LogException($"No se pudo mover la miniatura temporal a la ubicación final: {p}", exMove);
                    try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException($"Falló SaveThumbToCache para {filePath}", ex);
            }
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
            if (!string.IsNullOrWhiteSpace(widths) && sender is System.Windows.Controls.ListView lv && lv.View is GridView gv)
            {
                var parts = widths.Split(',');
                for (int index = 0; index < gv.Columns.Count && index < parts.Length; index++)
                {
                    if (double.TryParse(parts[index], out var savedWidth))
                    {
                        gv.Columns[index].Width = savedWidth;
                    }
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
            catch (Exception ex)
            {
                Logger.LogException($"Error guardando UI state key={key}", ex);
            }
        }

        private string LoadString(string key, string? defaultValue)
        {
            try
            {
                var path = GetUiStatePath();
                if (!File.Exists(path)) return defaultValue ?? string.Empty;
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
            catch (Exception ex)
            {
                Logger.LogException($"Error leyendo UI state key={key}", ex);
            }
            return defaultValue ?? string.Empty;
        }

        private void OnShowInFolder(string? filePath)
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
            FilteredFolderCount = 0;
            FolderResultsSummary = string.Empty;
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
            catch (Exception ex)
            {
                Logger.LogException("Error subiendo a la carpeta padre desde HomeView", ex);
            }
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
        private string _name = string.Empty;
        private string _path = string.Empty;
        private bool _isFolder;
        private string _size = string.Empty;
        private DateTime _lastModified;
        private string _extension = string.Empty;
        private int _comicCount;
    private BitmapSource? _thumbnail;

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

        public BitmapSource? Thumbnail
        {
            get => _thumbnail;
            set { _thumbnail = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }
    }
}