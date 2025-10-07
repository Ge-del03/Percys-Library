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
    private IComicPageLoader? _loader;
    private readonly ILogService _log;
    private readonly IReadingStatsService _stats = ComicReader.Core.Services.ServiceLocator.TryGet<IReadingStatsService>();
    private bool _isLoading;
    private int _currentPage;
    private double _zoom = 1.0;
    private bool _isUserScroll = true; // Para evitar bucles al sincronizar scroll
    private int _overscan = 4;
    private int _releaseMultiplier = 3; // distancia en múltiplos de overscan para liberar
    private DateTime _lastPageSetAt = DateTime.MinValue;
    private int? _pendingTargetPage = null;
    private System.Threading.CancellationTokenSource? _materializeCts;

    public ObservableCollection<ComicPage> Pages { get; } = new();
        // Exponer algunos ajustes finos (se pueden enlazar a settings en el futuro)
        public int Overscan
        {
            get => _overscan;
            set { _overscan = Math.Max(1, Math.Min(24, value)); OnPropertyChanged(nameof(Overscan)); }
        }

        public int MaxDecodeConcurrency { get; set; } = 3;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<int>? CurrentPageChanged;

        public IComicPageLoader Loader
        {
            get => _loader!;
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
                    var now = DateTime.UtcNow;
                    bool isRapid = (now - _lastPageSetAt).TotalMilliseconds < 180;
                    _lastPageSetAt = now;
                    // Coalescencia: si hay cambios muy rápidos, salta directo al último solicitado
                    if (isRapid && _pendingTargetPage.HasValue)
                    {
                        _currentPage = _pendingTargetPage.Value;
                        _pendingTargetPage = value;
                    }
                    else
                    {
                        _currentPage = value;
                        _pendingTargetPage = value;
                    }
                    OnPropertyChanged(nameof(CurrentPage));
                    OnPropertyChanged(nameof(CurrentPageDisplay));
                    OnPropertyChanged(nameof(CurrentPageOneBased));
                    // Actualizar bandera IsCurrent en cada página
                    foreach (var p in Pages)
                        p.IsCurrent = p.PageIndex == _currentPage;
                    CurrentPageChanged?.Invoke(_currentPage);
                    // Materializar entorno al cambiar página (más amplio si el cambio es muy rápido)
                    RequestVisiblePagesMaterialization(isRapid ? Math.Min(16, _overscan * 3) : (int?)null);
                    // Precargar vecinos para mantener fluidez si sigue avanzando
                    _ = PreloadNeighborsAsync(_currentPage, isRapid ? 4 : 2);
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
                if (SettingsManager.Settings?.ReadingDirection == "RightToLeft")
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
                if (SettingsManager.Settings?.ReadingDirection == "RightToLeft")
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
                var pagesList = Loader.Pages;
                var enumerable = pagesList != null ? (System.Collections.Generic.IEnumerable<ComicPage>)pagesList : Array.Empty<ComicPage>();
                foreach (var p in enumerable)
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

        public async void RequestVisiblePagesMaterialization(int? overscanOverride = null)
        {
            if (Pages.Count == 0 || Loader == null) return;
            // cancelar solicitudes anteriores si las hay (el usuario puede estar desplazándose rápidamente)
            try { _materializeCts?.Cancel(); } catch { }
            _materializeCts = new System.Threading.CancellationTokenSource();
            var ct = _materializeCts.Token;
            int center = CurrentPage;
                int overscan = Math.Max(1, overscanOverride ?? _overscan);
            int start = Math.Max(0, center - overscan);
            int end = Math.Min(Pages.Count - 1, center + overscan);
            // Cargar visibles + overscan con concurrencia limitada
            var tasks = new System.Collections.Generic.List<Task>();
            var sem = new System.Threading.SemaphoreSlim(Math.Max(1, MaxDecodeConcurrency));
            for (int i = start; i <= end; i++)
            {
                var idx = i;
                var page = Pages[idx];
                if (page.Image == null)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await sem.WaitAsync(ct).ConfigureAwait(false);
                        try
                        {
                            ct.ThrowIfCancellationRequested();
                            // Páginas más cercanas primero (prioridad).
                            var bmp = await Loader.GetPageImageAsync(idx);
                            // Asignar en hilo de UI
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                try { page.Image = bmp; } catch { }
                            });
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception ex)
                        {
                            _log?.Log($"Error materializando página {idx}: {ex.Message}", LogLevel.Warning);
                        }
                        finally { try { sem.Release(); } catch { } }
                    }, ct));
                }
            }
            try { await Task.WhenAll(tasks); } catch { }
                // Fase de asentamiento: si no está cancelado y el usuario desaceleró, ampliar un poco el buffer
                if (!ct.IsCancellationRequested && !overscanOverride.HasValue)
                {
                    try
                    {
                        await Task.Delay(60, ct);
                        if (!ct.IsCancellationRequested)
                            RequestVisiblePagesMaterialization(_overscan + 2);
                    }
                    catch { }
                }
            // Liberar páginas lejanas
            int releaseDistance = overscan * _releaseMultiplier;
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

        private async Task PreloadNeighborsAsync(int center, int depth)
        {
            if (Loader == null || Pages.Count == 0) return;
            // ceder el turno para no bloquear el hilo de UI y evitar la advertencia de async sin await
            await Task.Yield();
            for (int d = 1; d <= depth; d++)
            {
                int i1 = center + d;
                int i2 = center - d;
                if (i1 >= 0 && i1 < Pages.Count)
                {
                    try { _ = Loader.GetPageImageAsync(i1); } catch { }
                }
                if (i2 >= 0 && i2 < Pages.Count)
                {
                    try { _ = Loader.GetPageImageAsync(i2); } catch { }
                }
            }
        }

        public void BeginProgrammaticScroll() => _isUserScroll = false;
        public void EndProgrammaticScroll() => _isUserScroll = true;

        public bool ShouldReactToUserScroll => _isUserScroll;

    private void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
