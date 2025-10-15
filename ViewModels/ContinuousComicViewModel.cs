using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ComicReader.Models;
using ComicReader.Services;
using ComicReader.Core.Abstractions;
using ComicReader.Core.Services;
using ComicReader.Core.Adapters;
using ComicReader.Commands;

namespace ComicReader.ViewModels
{
    public class ContinuousComicViewModel : INotifyPropertyChanged
    {
    private IComicPageLoader _loader;
    private readonly ILogService _log;
    private readonly IReadingStatsService _stats = ComicReader.Core.Services.ServiceLocator.TryGet<IReadingStatsService>();
    private bool _isLoading;
    private int _currentPage;
    private double _zoom = 1.0;
    private bool _isUserScroll = true; // Para evitar bucles al sincronizar scroll
    private int _overscan = 4;
    private int _releaseMultiplier = 3; // distancia en múltiplos de overscan para liberar

    public ObservableCollection<ComicPage> Pages { get; } = new();
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<int> CurrentPageChanged;

        public IComicPageLoader Loader
        {
            get => _loader;
            set
            {
                _loader = value;
                OnPropertyChanged(nameof(Loader));
                _ = LoadPagesAsync();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); OnPropertyChanged(nameof(IsNotLoading)); }
        }
        public bool IsNotLoading => !IsLoading;

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    OnPropertyChanged(nameof(CurrentPage));
                    OnPropertyChanged(nameof(CurrentPageDisplay));
                    OnPropertyChanged(nameof(CurrentPageOneBased));
                    // Actualizar bandera IsCurrent en cada página
                    foreach (var p in Pages)
                        p.IsCurrent = p.PageIndex == _currentPage;
                    CurrentPageChanged?.Invoke(_currentPage);
                    // Materializar entorno al cambiar página
                    RequestVisiblePagesMaterialization();
                    _stats?.RecordPageViewed(_currentPage + 1);
                }
            }
        }

        public int CurrentPageDisplay => _currentPage + 1;

        public double Zoom
        {
            get => _zoom;
            set
            {
                var clamped = Math.Max(0.25, Math.Min(4.0, value));
                if (Math.Abs(_zoom - clamped) > 0.0001)
                {
                    _zoom = clamped;
                    OnPropertyChanged(nameof(Zoom));
                    OnPropertyChanged(nameof(ZoomPercent));
                }
            }
        }

        public int PagesCount => Pages.Count;
        public int ZoomPercent => (int)Math.Round(Zoom * 100);
        public int CurrentPageOneBased
        {
            get => CurrentPage + 1;
            set
            {
                int target = value - 1;
                if (target >= 0 && target < Pages.Count)
                    CurrentPage = target;
            }
        }

    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand ResetZoomCommand { get; }
    public ICommand GoToPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PrevPageCommand { get; }
    public ICommand MoveLeftCommand { get; }
    public ICommand MoveRightCommand { get; }

        public ContinuousComicViewModel()
        {
            _log = ServiceLocator.TryGet<ILogService>() ?? new LogServiceAdapter();
            ZoomInCommand = new RelayCommand(_ => Zoom += 0.1);
            ZoomOutCommand = new RelayCommand(_ => Zoom -= 0.1);
            ResetZoomCommand = new RelayCommand(_ => Zoom = 1.0);
            GoToPageCommand = new RelayCommand(p =>
            {
                if (p is int i && i >= 0 && i < Pages.Count) CurrentPage = i;
            });
            NextPageCommand = new RelayCommand(_ => { if (CurrentPage + 1 < Pages.Count) CurrentPage++; });
            PrevPageCommand = new RelayCommand(_ => { if (CurrentPage - 1 >= 0) CurrentPage--; });
            // Respetar la dirección de lectura en los botones de navegación del panel
            MoveLeftCommand = new RelayCommand(_ =>
            {
                if (SettingsManager.Settings?.CurrentReadingDirection == ReadingDirection.RightToLeft)
                {
                    if (CurrentPage + 1 < Pages.Count) CurrentPage++;
                }
                else
                {
                    if (CurrentPage - 1 >= 0) CurrentPage--;
                }
            });
            MoveRightCommand = new RelayCommand(_ =>
            {
                if (SettingsManager.Settings?.CurrentReadingDirection == ReadingDirection.RightToLeft)
                {
                    if (CurrentPage - 1 >= 0) CurrentPage--;
                }
                else
                {
                    if (CurrentPage + 1 < Pages.Count) CurrentPage++;
                }
            });
        }

        private async Task LoadPagesAsync()
        {
            if (Loader == null) { _log?.Log("Loader nulo en ContinuousComicViewModel", LogLevel.Warning); return; }
            try
            {
                IsLoading = true;
                Pages.Clear();
                if (Loader.Pages == null || Loader.Pages.Count == 0)
                {
                    // Solo intentamos cargar si hay una ruta válida; si no, mantenemos el lector vacío
                    if (!string.IsNullOrWhiteSpace(Loader.FilePath))
                        await Loader.LoadComicAsync(Loader.FilePath);
                    else
                    {
                        IsLoading = false;
                        return;
                    }
                }
                int idx = 0;
                foreach (var p in Loader.Pages)
                {
                    // Imagen diferida (virtualización)
                    Pages.Add(new ComicPage { PageNumber = idx + 1, PageIndex = idx, FileName = p.FileName, Image = null, IsCurrent = false });
                    idx++;
                }
                OnPropertyChanged(nameof(PagesCount));
                CurrentPage = 0;
                _log?.Log($"Continuous view cargó {Pages.Count} páginas", LogLevel.Info);
                RequestVisiblePagesMaterialization();
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async void RequestVisiblePagesMaterialization()
        {
            if (Pages.Count == 0 || Loader == null) return;
            int center = CurrentPage;
            int start = Math.Max(0, center - _overscan);
            int end = Math.Min(Pages.Count - 1, center + _overscan);
            // Cargar visibles + overscan
            for (int i = start; i <= end; i++)
            {
                var page = Pages[i];
                if (page.Image == null)
                {
                    try
                    {
                        var bmp = await Loader.GetPageImageAsync(i);
                        page.Image = bmp;
                    }
                    catch (Exception ex)
                    {
                        _log?.Log($"Error materializando página {i}: {ex.Message}", LogLevel.Warning);
                    }
                }
            }
            // Liberar páginas lejanas
            int releaseDistance = _overscan * _releaseMultiplier;
            for (int i = 0; i < Pages.Count; i++)
            {
                if (Math.Abs(i - center) > releaseDistance)
                {
                    var page = Pages[i];
                    if (page.Image != null)
                    {
                        page.Image = null; // GC friendly; loader mantiene cache interno
                    }
                }
            }
        }

        public void BeginProgrammaticScroll() => _isUserScroll = false;
        public void EndProgrammaticScroll() => _isUserScroll = true;

        public bool ShouldReactToUserScroll => _isUserScroll;

        private void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
