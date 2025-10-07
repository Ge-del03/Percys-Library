// FileName: /Services/ComicPageLoader.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.Tar; // Para CBT
using SharpCompress.Archives.SevenZip; // Para CB7
using SharpCompress.Archives.Zip; // Para CBZ
using SharpCompress.Common; // Para PasswordProtectedException
using ComicReader.Models;
using System.Drawing; // Para Bitmap
using System.Collections.Concurrent;
// using System.Windows.Threading; // no utilizado
// EPUB deshabilitado: el paquete VersOne.Epub fue retirado para reducir dependencias
#if SUPPORT_DJVU
using DjvuNet; // Para DJVU (requiere NuGet: DjvuNet)
#endif
#if SUPPORT_PDF
using Docnet.Core; // Render PDF
using Docnet.Core.Models;
using Docnet.Core.Readers;
#endif
// PDF support will be conditional based on availability
// using SixLabors.ImageSharp; // Para WebP/HEIC (requiere NuGet: SixLabors.ImageSharp.WebP, SixLabors.ImageSharp.Heif)
// using SixLabors.ImageSharp.Processing; // Para redimensionar imágenes

using ComicReader.Core.Abstractions;
using ComicReader.Core.Services;
// duplicate using removed
using ComicReader.Core.Adapters;
namespace ComicReader.Services
{
    public partial class ComicPageLoader : IDisposable, IComicPageLoader
    {
    private string _filePath = string.Empty;
    private List<Models.ComicPage> _pages = new List<Models.ComicPage>();
    private readonly IImageCacheService _imageCacheService;
    // Ancho de decodificación efectivo (override temporal, no persistente)
    private int? _decodeTargetWidthOverride;
    public int? DecodeTargetWidthOverride { get => _decodeTargetWidthOverride; set { _decodeTargetWidthOverride = value; } }
    private int _pageCacheLimit = 60; // configurable luego
    private ILogService? _log;
    private int _prefetchWindow = 4;
    private object _lock = new object(); // Para sincronizar acceso a recursos compartidos
    private int _contentVersion = 0; // Incrementa cada vez que se carga un nuevo cómic
    // Limitar decodificaciones concurrentes para evitar thrashing de CPU/IO
    private System.Threading.SemaphoreSlim _decodeSemaphore = new System.Threading.SemaphoreSlim(3);
    private int _decodeConcurrency = 3;
    // Tipo real del archivo comprimido detectado por firma para manejar CBR mal renombrados
    private ArchiveKind _archiveKind = ArchiveKind.None;
    // Archivo comprimido abierto e índice para acceso rápido a entradas (cuando aplica)
    private IArchive? _openedArchive;
    private Dictionary<string, IArchiveEntry>? _entryIndex;

    // Documentos cargados para formatos especiales
#if SUPPORT_PDF
    private IDocReader? _pdfDocument;
#endif
#if SUPPORT_DJVU
    private DjvuDocument _djvuDocument;
#endif

        public List<Models.ComicPage> Pages => _pages;
        public string ComicTitle => string.IsNullOrEmpty(_filePath) ? "Cómic sin título" : Path.GetFileNameWithoutExtension(_filePath);
        public int PageCount => _pages.Count;
    public string FilePath => _filePath;

        public ComicPageLoader()
        {
            _imageCacheService = ServiceLocator.Get<IImageCacheService>();
            InitLog();
            _log?.Log("Initializing ComicPageLoader (empty constructor)");
            ApplySettingsParameters();
        }

        public ComicPageLoader(string filePath)
        {
            _filePath = filePath;
            _imageCacheService = ServiceLocator.Get<IImageCacheService>();
            InitLog();
            _log?.Log($"Initializing ComicPageLoader for: {_filePath}");
            ApplySettingsParameters();
        }

        // Limpia el cómic actual (ruta y páginas) sin disponer la instancia
        public void ClearCurrent()
        {
            try
            {
#if SUPPORT_PDF
                if (_pdfDocument != null) { try { _pdfDocument.Dispose(); } catch { } _pdfDocument = null; }
#endif
#if SUPPORT_DJVU
                if (_djvuDocument != null) { try { _djvuDocument.Dispose(); } catch { } _djvuDocument = null; }
#endif
                if (_openedArchive != null) { try { _openedArchive.Dispose(); } catch { } _openedArchive = null; }
            }
            catch { }
            _filePath = string.Empty;
            _archiveKind = ArchiveKind.None;
            try { _pages.Clear(); } catch { }
            _imageCacheService.Clear();
            try { _entryIndex?.Clear(); _entryIndex = null; } catch { }
        }

        private void InitLog()
        {
            _log = ServiceLocator.TryGet<ILogService>() ?? new LogServiceAdapter();
        }

        private void ApplySettingsParameters()
        {
            try
            {
                if (SettingsManager.Settings != null)
                {
                    if (SettingsManager.Settings.PageCacheLimit > 0) _pageCacheLimit = SettingsManager.Settings.PageCacheLimit;
                    if (SettingsManager.Settings.PrefetchWindow > 0) _prefetchWindow = SettingsManager.Settings.PrefetchWindow;
                    UpdateDecodeConcurrency(SettingsManager.Settings.DecodeConcurrency);
                }
            }
            catch { }
        }

        private void UpdateDecodeConcurrency(int desired)
        {
            try
            {
                var clamped = Math.Max(1, Math.Min(8, desired));
                if (_decodeConcurrency == clamped && _decodeSemaphore != null) return;
                _decodeConcurrency = clamped;
                var newSemaphore = new System.Threading.SemaphoreSlim(_decodeConcurrency);
                var old = System.Threading.Interlocked.Exchange(ref _decodeSemaphore, newSemaphore);
                try { old?.Dispose(); } catch { }
            }
            catch { }
        }

        // Permitir actualizar parámetros en caliente
        public void RefreshTuningFromSettings()
        {
            ApplySettingsParameters();
        }

    public async Task LoadComicAsync(string? filePath = null)
        {
            if (filePath != null)
            {
                _filePath = filePath;
            }
            
            // Nueva versión de contenido: invalida cachés y evita condiciones de carrera
            System.Threading.Interlocked.Increment(ref _contentVersion);
            _pages.Clear();
            _imageCacheService.Clear();
            _archiveKind = ArchiveKind.None;
#if SUPPORT_PDF
            if (_pdfDocument != null) { try { _pdfDocument.Dispose(); } catch (Exception ex) { Logger.LogException("Error disposing PDF document", ex); } _pdfDocument = null; }
#endif
#if SUPPORT_DJVU
            if (_djvuDocument != null) { try { _djvuDocument.Dispose(); } catch (Exception ex) { Logger.LogException("Error disposing DJVU document", ex); } _djvuDocument = null; }
#endif

            try
            {
                if (string.IsNullOrEmpty(_filePath))
                {
                    throw new ArgumentException("No se ha especificado un archivo para cargar");
                }
                
                if (!File.Exists(_filePath) && !Directory.Exists(_filePath)) // Puede ser una carpeta
                {
                    throw new FileNotFoundException($"El archivo o carpeta no se encontró: {_filePath}");
                }

                string ext = Path.GetExtension(_filePath).ToLowerInvariant();

                if (Directory.Exists(_filePath))
                {
                    await LoadFolderAsync(_filePath);
                }
                else
                {
                    switch (ext)
                    {
                        case ".cbz":
                            _archiveKind = ArchiveKind.Zip;
                            await LoadCBZAsync();
                            break;
                        case ".cbr":
                            _archiveKind = DetectArchiveKindFromFile(_filePath, ext);
                            await LoadArchiveByKindAsync(_archiveKind == ArchiveKind.None ? ArchiveKind.Rar : _archiveKind);
                            break;
                        case ".cbt":
                            _archiveKind = ArchiveKind.Tar;
                            await LoadCBTAsync();
                            break;
                        case ".cb7":
                            _archiveKind = ArchiveKind.SevenZip;
                            await LoadCB7Async();
                            break;
                        case ".pdf":
#if SUPPORT_PDF
                            try
                            {
                                int targetWidth = Math.Max(200, SettingsManager.Settings?.PdfRenderWidth ?? 1600);
                                int targetHeight = Math.Max(200, SettingsManager.Settings?.PdfRenderHeight ?? 2200);
                                _pdfDocument = DocLib.Instance.GetDocReader(_filePath, new PageDimensions(targetWidth, targetHeight));
                                int pageCount = _pdfDocument.GetPageCount();
                                for (int i = 0; i < pageCount; i++)
                                {
                                    _pages.Add(new Models.ComicPage(i + 1, $"PDF_Page_{i + 1}", null));
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogException($"Error al cargar PDF: {_filePath}", ex);
                                _pages.Clear();
                                _pages.Add(new Models.ComicPage(1, "PDF_Error", CreatePlaceholderImage($"Error PDF\n{ex.Message}", 500, 700)));
                            }
#else
                            _pages.Add(new Models.ComicPage(1, "PDF_Info", CreatePlaceholderImage("Instala soporte PDF para renderizar", 500, 700)));
#endif
                            break;
                        case ".djvu":
                            await LoadDJVUAsync();
                            break;
                        default:
                            if (IsImageFile(_filePath))
                            {
                                _pages.Add(new Models.ComicPage(1, _filePath, null));
                            }
                            else
                            {
                                // Último recurso: intentar detección por firma
                                var detected = DetectArchiveKindFromFile(_filePath, ext);
                                if (detected != ArchiveKind.None)
                                {
                                    await LoadArchiveByKindAsync(detected);
                                }
                                else
                                {
                                    _pages.Add(new Models.ComicPage(1, "Unsupported", CreatePlaceholderImage("Formato no soportado", 500, 700)));
                                }
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException($"Error en LoadComicAsync para {_filePath}", ex);
                _pages.Clear();
                _pages.Add(new Models.ComicPage(1, "Error", CreatePlaceholderImage($"Error\n{ex.Message}", 500, 700)));
            }
        }

        // EPUB: método eliminado

        private async Task LoadDJVUAsync()
        {
            await Task.Run(() =>
            {
                try
                {
#if SUPPORT_DJVU
                    _djvuDocument?.Dispose();
                    _djvuDocument = new DjvuDocument(_filePath);
                    int pageCount = _djvuDocument.Pages.Count;
                    for (int i = 0; i < pageCount; i++)
                    {
                        _pages.Add(new Models.ComicPage(i + 1, $"DJVU_Page_{i + 1}", null));
                    }
#else
                    _pages.Add(new Models.ComicPage(1, "DJVU_Info", CreatePlaceholderImage("Instala soporte DJVU para renderizar", 500, 700)));
#endif
                }
                catch (Exception ex)
                {
                    Logger.LogException($"Error al cargar DJVU: {_filePath}", ex);
                    _pages.Clear();
                    _pages.Add(new Models.ComicPage(1, "DJVU_Error", CreatePlaceholderImage($"Error DJVU\n{ex.Message}", 500, 700)));
                }
            });
        }

        // --- Métodos Auxiliares de Carga ---

        private void AddImageEntries(IEnumerable<string> entryKeys, Func<string, Stream> streamProvider)
        {
            var sortedEntries = entryKeys
                .Where(e => !string.IsNullOrEmpty(e) && !e.EndsWith("/") && IsSupportedImageExtension(Path.GetExtension(e)))
                .OrderBy(e => e, StringComparer.OrdinalIgnoreCase)
                .ToList();

            for (int i = 0; i < sortedEntries.Count; i++)
            {
                _pages.Add(new Models.ComicPage(i + 1, sortedEntries[i], CreatePlaceholderImage("IMG", 200, 200)));
            }
        }

        private bool IsSupportedImageExtension(string extension)
        {
            return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".gif", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".tif", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".webp", StringComparison.OrdinalIgnoreCase) || // Requiere librería
                   extension.Equals(".heic", StringComparison.OrdinalIgnoreCase);   // Requiere librería
        }

        // --- Métodos para Obtener Imágenes ---

        public async Task<BitmapImage?> GetPageImageAsync(int pageNumber)
        {
            var localVersion = System.Threading.Volatile.Read(ref _contentVersion);
            int idx = ClampPageIndex(pageNumber);
            if (idx == -1) return CreatePlaceholderImage("Sin imagen", 200, 200);

            int targetWidthForCache = GetEffectiveDecodeTargetWidth();
            string cacheKey = $"{FilePath}|v{localVersion}|p{idx}|w{targetWidthForCache}";

            var cachedImage = _imageCacheService.Get(cacheKey);
            if (cachedImage != null)
            {
                return cachedImage;
            }

            await _decodeSemaphore.WaitAsync();
            BitmapImage? image = null;
            try
            {
                image = await Task.Run(() => LoadImageFromSource(idx));
            }
            finally
            {
                try { _decodeSemaphore.Release(); } catch { }
            }

            if (image == null) image = CreatePlaceholderImage("Sin imagen", 200, 200);

            if (localVersion == System.Threading.Volatile.Read(ref _contentVersion))
            {
                _imageCacheService.Set(cacheKey, image);
            }

            return image ?? CreatePlaceholderImage("Sin imagen", 200, 200);
        }

        // Obtener miniatura de una página (decodificada a tamaño pequeño)
        public async Task<BitmapImage> GetPageThumbnailAsync(int pageNumber, int width = 200, int height = 300)
        {
            var localVersion = System.Threading.Volatile.Read(ref _contentVersion);
            int idx = ClampPageIndex(pageNumber);
            if (idx == -1) return CreatePlaceholderImage("Sin imagen", width, height);

            string cacheKey = $"{FilePath}|v{localVersion}|p{idx}|thumb|w{width}h{height}";

            var cachedImage = _imageCacheService.Get(cacheKey);
            if (cachedImage != null)
            {
                return cachedImage;
            }

            var image = await Task.Run(() => LoadThumbnailFromSource(idx, width, height));
            if (image == null) image = CreatePlaceholderImage("Sin imagen", width, height);

            if (localVersion == System.Threading.Volatile.Read(ref _contentVersion))
            {
                _imageCacheService.Set(cacheKey, image);
            }
            return image;
        }

        private BitmapImage LoadImageFromSource(int pageNumber)
        {
            int idx = ClampPageIndex(pageNumber);
            if (idx == -1) return CreatePlaceholderImage("Sin imagen", 200, 200);

            string fileName = _pages[idx].FileName;
            BitmapImage img = CreatePlaceholderImage("Sin imagen", 200, 200);
            string ext = Path.GetExtension(_filePath).ToLowerInvariant();
            string pageExt = Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;

            if (Directory.Exists(_filePath)) // Es una carpeta de imágenes
            {
                using (var stream = File.OpenRead(fileName))
                {
                    img = CreateBitmapImage(stream, pageExt);
                }
            }
            else
            {
                switch (ext)
                {
                    case ".cbz":
                    case ".cbr":
                    case ".cbt":
                    case ".cb7":
                        // Usar archivo abierto e índice para evitar reabrir por cada página
                        var entryRef = TryGetArchiveEntry(fileName);
                        if (entryRef != null)
                        {
                            // Algunos formatos no soportan acceso concurrente: usamos lock de lectura
                            lock (_archiveReadLock)
                            {
                                using (var es = entryRef.OpenEntryStream())
                                {
                                    int cap = (entryRef.Size > 0 && entryRef.Size <= int.MaxValue) ? (int)entryRef.Size + 16 : 0;
                                    using (var ms = cap > 0 ? new MemoryStream(cap) : new MemoryStream())
                                    {
                                        es.CopyTo(ms);
                                        ms.Position = 0;
                                        img = CreateBitmapImage(ms, pageExt);
                                    }
                                }
                            }
                        }
                        break;
                    case ".pdf":
#if SUPPORT_PDF
                        if (_pdfDocument != null)
                        {
                            lock (_lock)
                            {
                                using (var pageReader = _pdfDocument.GetPageReader(pageNumber))
                                {
                                    int w = pageReader.GetPageWidth();
                                    int h = pageReader.GetPageHeight();
                                    var bytes = pageReader.GetImage();
                                    img = RawBgraToBitmapImage(bytes, w, h);
                                }
                            }
                        }
                        else
#endif
                        {
                            img = CreatePlaceholderImage("PDF", 200, 200);
                        }
                        break;
                    case ".epub":
                        // Deshabilitado: EpubBook no implementa IDisposable y referencias a imageContent eliminadas para evitar errores de compilación.
                        break;
                    case ".djvu":
#if SUPPORT_DJVU
                        if (_djvuDocument != null)
                        {
                            lock (_lock)
                            {
                                int dpi = Math.Max(72, SettingsManager.Settings?.PdfRenderDpi ?? 150);
                                using (var bitmap = _djvuDocument.Pages[pageNumber].RenderImage(dpi, dpi))
                                {
                                    img = BitmapToImageSource(bitmap);
                                }
                            }
                        }
                        else
#endif
                        {
                            img = CreatePlaceholderImage("DJVU", 200, 200);
                        }
                        break;
                    // Para imágenes sueltas (webp, heic, etc.)
                    case ".jpg": case ".jpeg": case ".png": case ".gif": case ".bmp":
                    case ".webp": case ".heic":
                        using (var stream = File.OpenRead(_filePath))
                        {
                            img = CreateBitmapImage(stream, ext);
                        }
                        break;
                }
            }
            return img;
        }

        private BitmapImage LoadThumbnailFromSource(int pageNumber, int width, int height)
        {
            int idx = ClampPageIndex(pageNumber);
            if (idx == -1) return CreatePlaceholderImage("Sin imagen", width, height);
            string fileName = _pages[idx].FileName;
            string ext = Path.GetExtension(_filePath).ToLowerInvariant();
            string pageExt = Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;

            if (Directory.Exists(_filePath))
            {
                using (var stream = File.OpenRead(fileName))
                {
                    return CreateThumbnailImage(stream, width, height, pageExt);
                }
            }
            else
            {
                switch (ext)
                {
                    case ".cbz":
                    case ".cbr":
                    case ".cbt":
                    case ".cb7":
                        var entryRef = TryGetArchiveEntry(fileName);
                        if (entryRef != null)
                        {
                            lock (_archiveReadLock)
                            {
                                using (var es = entryRef.OpenEntryStream())
                                {
                                    int cap = (entryRef.Size > 0 && entryRef.Size <= int.MaxValue) ? (int)entryRef.Size + 16 : 0;
                                    using (var ms = cap > 0 ? new MemoryStream(cap) : new MemoryStream())
                                    { es.CopyTo(ms); ms.Position = 0; return CreateThumbnailImage(ms, width, height, pageExt); }
                                }
                            }
                        }
                        break;
                    case ".pdf":
#if SUPPORT_PDF
                        if (_pdfDocument != null)
                        {
                            lock (_lock)
                            {
                                using (var pageReader = _pdfDocument.GetPageReader(pageNumber))
                                {
                                    int w = pageReader.GetPageWidth();
                                    int h = pageReader.GetPageHeight();
                                    var bytes = pageReader.GetImage();
                                    using (var ms = EncodePngFromBgra(bytes, w, h))
                                    { ms.Position = 0; return CreateThumbnailImage(ms, width, height, ".png"); }
                                }
                            }
                        }
                        else
#endif
                        {
                            return CreatePlaceholderImage("PDF", width, height);
                        }
                    case ".djvu":
#if SUPPORT_DJVU
                        if (_djvuDocument != null)
                        {
                            lock (_lock)
                            {
                                int dpi = Math.Max(72, SettingsManager.Settings?.PdfRenderDpi ?? 150);
                                using (var bitmap = _djvuDocument.Pages[pageNumber].RenderImage(dpi, dpi))
                                using (var ms = new MemoryStream())
                                {
                                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                    ms.Position = 0; return CreateThumbnailImage(ms, width, height, ".png");
                                }
                            }
                        }
                        else
#endif
                        {
                            return CreatePlaceholderImage("DJVU", width, height);
                        }
                    case ".jpg": case ".jpeg": case ".png": case ".gif": case ".bmp":
                    case ".webp": case ".heic":
                        using (var stream = File.OpenRead(_filePath))
                        { return CreateThumbnailImage(stream, width, height, ext); }
                }
            }
            return CreatePlaceholderImage("Sin imagen", width, height);
        }

        // --- Archivo abierto e indexación ---
        private readonly object _archiveReadLock = new object();

        private void IndexArchiveEntries()
        {
            _entryIndex = new Dictionary<string, IArchiveEntry>(StringComparer.OrdinalIgnoreCase);
            _pages.Clear();
            if (_openedArchive == null) return;
            var list = _openedArchive.Entries
                .Where(e => !e.IsDirectory && IsSupportedImageExtension(Path.GetExtension(e.Key)))
                .OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
            int i = 0;
            foreach (var entry in list)
            {
                _entryIndex[entry.Key] = entry;
                _pages.Add(new Models.ComicPage(++i, entry.Key, null));
            }
        }

        private IArchiveEntry? TryGetArchiveEntry(string key)
        {
            if (_entryIndex != null && _entryIndex.TryGetValue(key, out var e)) return e;
            return null;
        }

        // --- Cargadores específicos ---
        private async Task LoadFolderAsync(string path)
        {
            await Task.Run(() =>
            {
                try
                {
                    var files = Directory.GetFiles(path)
                        .Where(IsImageFile)
                        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    _pages.Clear();
                    int i = 0;
                    foreach (var f in files)
                    {
                        _pages.Add(new Models.ComicPage(++i, f, null));
                    }
                    try { _openedArchive?.Dispose(); } catch { }
                    _openedArchive = null; _entryIndex = null;
                }
                catch (Exception ex)
                {
                    Logger.LogException($"Error al cargar carpeta: {path}", ex);
                }
            });
        }

        private async Task LoadCBZAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    try { _openedArchive?.Dispose(); } catch { }
                    _openedArchive = ZipArchive.Open(_filePath);
                    IndexArchiveEntries();
                }
                catch (Exception ex)
                {
                    Logger.LogException($"Error al abrir CBZ: {_filePath}", ex);
                    _openedArchive = null; _entryIndex = null;
                }
            });
        }

        private async Task LoadCBRAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    try { _openedArchive?.Dispose(); } catch { }
                    _openedArchive = RarArchive.Open(_filePath);
                    IndexArchiveEntries();
                }
                catch (Exception ex)
                {
                    Logger.LogException($"Error al abrir CBR: {_filePath}", ex);
                    _openedArchive = null; _entryIndex = null;
                }
            });
        }

        private async Task LoadCB7Async()
        {
            await Task.Run(() =>
            {
                try
                {
                    try { _openedArchive?.Dispose(); } catch { }
                    _openedArchive = SevenZipArchive.Open(_filePath);
                    IndexArchiveEntries();
                }
                catch (Exception ex)
                {
                    Logger.LogException($"Error al abrir CB7: {_filePath}", ex);
                    _openedArchive = null; _entryIndex = null;
                }
            });
        }

        private async Task LoadCBTAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    try { _openedArchive?.Dispose(); } catch { }
                    _openedArchive = TarArchive.Open(_filePath);
                    IndexArchiveEntries();
                }
                catch (Exception ex)
                {
                    Logger.LogException($"Error al abrir CBT: {_filePath}", ex);
                    _openedArchive = null; _entryIndex = null;
                }
            });
        }

        // Ruta optimizada para formatos comunes usando extensión; fallback a ImageSharp si no es compatible
        private int GetEffectiveDecodeTargetWidth()
        {
            try { return Math.Max(600, _decodeTargetWidthOverride ?? (SettingsManager.Settings?.PdfRenderWidth ?? 2000)); }
            catch { return 1600; }
        }

        private BitmapImage CreateBitmapImage(Stream stream, string extension)
        {
            try
            {
                if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".gif" || extension == ".bmp")
                {
                    // Decodificación con downscale preventivo para evitar cargar imágenes gigantes a resolución completa.
                    // Usamos el ancho objetivo desde Settings (PdfRenderWidth como aproximación) o 2000px por defecto.
                    int targetWidth = GetEffectiveDecodeTargetWidth();
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.DecodePixelWidth = targetWidth; // WPF mantendrá aspect ratio
                    img.StreamSource = stream;
                    img.EndInit();
                    img.Freeze();
                    return img;
                }

                // Fallback a ImageSharp para formatos no compatibles nativamente (webp/heic/etc.)
                stream.Position = 0;
                var image = SixLabors.ImageSharp.Image.Load(stream);
                using (var ms = new MemoryStream())
                {
                    // Nota: evitamos dependencias de procesamiento (Mutate/Resize) para no añadir paquetes.
                    // Elegimos PNG y dejaremos que WPF haga el downscale con DecodePixelWidth igualmente
                    image.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                    ms.Position = 0;
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.StreamSource = ms;
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.DecodePixelWidth = GetEffectiveDecodeTargetWidth();
                    img.EndInit();
                    img.Freeze();
                    return img;
                }
            }
            catch
            {
                // Fallback a BitmapImage estándar si ImageSharp falla
                var img = new BitmapImage();
                img.BeginInit();
                img.StreamSource = stream;
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.DecodePixelWidth = GetEffectiveDecodeTargetWidth();
                img.EndInit();
                img.Freeze();
                return img;
            }
        }

        // Mantener compatibilidad con llamadas existentes
        private BitmapImage CreateBitmapImage(Stream stream)
        {
            return CreateBitmapImage(stream, string.Empty);
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                return CreateBitmapImage(ms);
            }
        }

        // Conversión para Docnet.Core (buffer BGRA)
        private BitmapImage RawBgraToBitmapImage(byte[] bgraBytes, int width, int height)
        {
            var dpiX = 96d;
            var dpiY = 96d;
            var pixelFormat = System.Windows.Media.PixelFormats.Bgra32;
            var stride = (width * pixelFormat.BitsPerPixel + 7) / 8;
            var bmp = BitmapSource.Create(width, height, dpiX, dpiY, pixelFormat, null, bgraBytes, stride);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                ms.Position = 0;
                return CreateBitmapImage(ms);
            }
        }

        // Devuelve un MemoryStream con PNG a partir de BGRA (útil para miniaturas)
        private MemoryStream EncodePngFromBgra(byte[] bgraBytes, int width, int height)
        {
            var dpiX = 96d;
            var dpiY = 96d;
            var pixelFormat = System.Windows.Media.PixelFormats.Bgra32;
            var stride = (width * pixelFormat.BitsPerPixel + 7) / 8;
            var bmp = BitmapSource.Create(width, height, dpiX, dpiY, pixelFormat, null, bgraBytes, stride);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            var ms = new MemoryStream();
            encoder.Save(ms);
            ms.Position = 0;
            return ms;
        }

        private BitmapImage CreatePlaceholderImage(string text, int width, int height)
        {
            using (var image = new System.Drawing.Bitmap(width, height))
            using (var g = System.Drawing.Graphics.FromImage(image))
            using (var font = new System.Drawing.Font("Arial", 24))
            {
                g.Clear(System.Drawing.Color.White);
                var size = g.MeasureString(text, font);
                var x = Math.Max(0, (width - size.Width) / 2);
                var y = Math.Max(0, (height - size.Height) / 2);
                g.DrawString(text, font, System.Drawing.Brushes.Gray, new System.Drawing.PointF(x, y));
                return BitmapToImageSource(image);
            }
        }

        // --- Cache y Pre-carga ---

        public void PreloadPages(int currentPageNumber)
        {
            if (SettingsManager.Settings?.EnableAsyncPrefetch == false)
            {
                return;
            }
            // La pre-carga ahora es más simple: solo dispara la tarea, el caché se encargará del resto.
            int window = _prefetchWindow > 0 ? _prefetchWindow : 4;
            for (int i = 1; i <= window; i++)
            {
                int prev = currentPageNumber - i;
                int next = currentPageNumber + i;
                if (prev >= 0) _ = GetPageImageAsync(prev);
                if (next < _pages.Count) _ = GetPageImageAsync(next);
            }
        }

        // Asegura que el índice esté dentro de rango; devuelve -1 si no hay páginas
        private int ClampPageIndex(int pageNumber)
        {
            int count = _pages?.Count ?? 0;
            if (count <= 0) return -1;
            if (pageNumber < 0) return 0;
            if (pageNumber >= count) return count - 1;
            return pageNumber;
        }

        // --- Disposición ---

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _imageCacheService.Clear();
                _pages.Clear();
                try { _openedArchive?.Dispose(); } catch { }
                _openedArchive = null;
                
#if SUPPORT_PDF
                try { _pdfDocument?.Dispose(); } catch {}
#endif
#if SUPPORT_DJVU
                try { _djvuDocument?.Dispose(); } catch {}
#endif
                Logger.Log("ComicPageLoader disposed.");
            }
        }

        // --- Miniaturas de Portada ---

        public async Task<BitmapImage> GetCoverThumbnailAsync(int width = 150, int height = 200)
        {
            if (_pages.Count == 0)
            {
                // Mostrar el icono de la app como portada por defecto
                try
                {
                    // 1) Intentar cargar desde recurso empaquetado (Build Action: Resource)
                    var packUri = new Uri("pack://application:,,,/icono.ico", UriKind.Absolute);
                    var sri = System.Windows.Application.GetResourceStream(packUri);
                    if (sri != null && sri.Stream != null)
                    {
                        using (var s = sri.Stream)
                        {
                            return CreateThumbnailImage(s, width, height);
                        }
                    }
                }
                catch { /* intentar con copia en disco */ }

                try
                {
                    // 2) Fallback: archivo copiado junto al ejecutable
                    var appDir = AppDomain.CurrentDomain.BaseDirectory;
                    var iconPath = System.IO.Path.Combine(appDir, "icono.ico");
                    if (File.Exists(iconPath))
                    {
                        using (var fs = File.OpenRead(iconPath))
                        {
                            return CreateThumbnailImage(fs, width, height);
                        }
                    }
                }
                catch { /* fallback abajo */ }

                return CreatePlaceholderImage("Sin portada", width, height);
            }

            BitmapImage? coverImage = null;
            try
            {
                coverImage = await Task.Run(() =>
                {
                    string fileName = _pages[0].FileName;
                    BitmapImage? img = null;
                    string ext = Path.GetExtension(_filePath).ToLowerInvariant();

                    if (Directory.Exists(_filePath))
                    {
                        // Elegir mejor candidato de portada en carpeta
                        var imageFiles = Directory.GetFiles(_filePath)
                            .Where(f => IsSupportedImageExtension(Path.GetExtension(f)))
                            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                            .ToList();
                        var best = SelectBestCover(imageFiles);
                        var path = string.IsNullOrEmpty(best) ? fileName : best;
                        using (var stream = File.OpenRead(path))
                        {
                            img = CreateThumbnailImage(stream, width, height, Path.GetExtension(path));
                        }
                    }
                    else
                    {
                        switch (ext)
                        {
                            case ".cbz":
                            case ".cbr":
                            case ".cbt":
                            case ".cb7":
                                // Usar el archivo e índice ya abiertos para elegir la mejor portada
                                if (_openedArchive != null)
                                {
                                    var names = (_entryIndex != null && _entryIndex.Count > 0)
                                        ? _entryIndex.Keys.ToList()
                                        : _openedArchive.Entries.Where(e => !e.IsDirectory && IsSupportedImageExtension(Path.GetExtension(e.Key))).Select(e => e.Key).ToList();
                                    names = names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
                                    var bestName = SelectBestCover(names) ?? names.FirstOrDefault();
                                    if (!string.IsNullOrEmpty(bestName))
                                    {
                                        var entryRef = TryGetArchiveEntry(bestName);
                                        // Si el índice aún no contiene la entrada (caso raro), buscarla desde el archivo
                                        if (entryRef == null)
                                        {
                                            entryRef = _openedArchive.Entries.FirstOrDefault(e => !e.IsDirectory && string.Equals(e.Key, bestName, StringComparison.OrdinalIgnoreCase));
                                        }
                                        if (entryRef != null)
                                        {
                                            lock (_archiveReadLock)
                                            {
                                                using (var es = entryRef.OpenEntryStream())
                                                using (var ms = new MemoryStream())
                                                {
                                                    es.CopyTo(ms);
                                                    ms.Position = 0;
                                                    img = CreateThumbnailImage(ms, width, height, Path.GetExtension(bestName));
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case ".pdf":
                                
#if SUPPORT_PDF
                                if (_pdfDocument != null)
                                {
                                    lock (_lock)
                                    {
                                        using (var pageReader = _pdfDocument.GetPageReader(0))
                                        {
                                            var w = pageReader.GetPageWidth();
                                            var h = pageReader.GetPageHeight();
                                            var bytes = pageReader.GetImage();
                                            img = RawBgraToBitmapImage(bytes, w, h);
                                        }
                                    }
                                }
                                else
#endif
                                {
                                    img = CreatePlaceholderImage("PDF", width, height);
                                }
                                break;
                            case ".epub":
                                // Deshabilitado: uso de EpubBook y imageContent para evitar errores de compilación.
                                break;
                            case ".djvu":
                                
#if SUPPORT_DJVU
                                if (_djvuDocument != null)
                                {
                                    lock (_lock)
                                    {
                                        int dpi = Math.Max(72, SettingsManager.Settings?.PdfRenderDpi ?? 150);
                                        using (var bitmap = _djvuDocument.Pages[0].RenderImage(dpi, dpi))
                                        {
                                            img = BitmapToImageSource(bitmap);
                                        }
                                    }
                                }
                                else
#endif
                                {
                                    img = CreatePlaceholderImage("DJVU", width, height);
                                }
                                break;
                            case ".jpg": case ".jpeg": case ".png": case ".gif": case ".bmp":
                            case ".webp": case ".heic":
                                using (var stream = File.OpenRead(_filePath))
                                {
                                    img = CreateThumbnailImage(stream, width, height, ext);
                                }
                                break;
                        }
                    }
                    return img;
                });
            }
            catch (Exception ex)
            {
                Logger.LogException($"Failed to get cover thumbnail for {_filePath}", ex);
            }
            return coverImage ?? CreatePlaceholderImage("Sin portada", width, height);
        }

        private BitmapImage CreateThumbnailImage(Stream stream, int width, int height, string extension)
        {
            try
            {
                // Reutilizar la lógica optimizada de CreateBitmapImage pero con las dimensiones de la miniatura.
                if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".gif" || extension == ".bmp")
                {
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.DecodePixelWidth = width; // Usar ancho de miniatura
                    img.StreamSource = stream;
                    img.EndInit();
                    img.Freeze();
                    return img;
                }

                // Fallback a ImageSharp para formatos no nativos (webp/heic)
                stream.Position = 0;
                var image = SixLabors.ImageSharp.Image.Load(stream);
                using (var ms = new MemoryStream())
                {
                    image.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                    ms.Position = 0;
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.StreamSource = ms;
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.DecodePixelWidth = width; // Usar ancho de miniatura
                    img.EndInit();
                    img.Freeze();
                    return img;
                }
            }
            catch
            {
                // Fallback final si todo lo demás falla
                var img = new BitmapImage();
                img.BeginInit();
                img.StreamSource = stream;
                img.DecodePixelWidth = width;
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                img.Freeze();
                return img;
            }
        }

        // Sobrecarga para mantener compatibilidad con llamadas existentes que no pasan la extensión.
        private BitmapImage CreateThumbnailImage(Stream stream, int width, int height)
        {
            return CreateThumbnailImage(stream, width, height, string.Empty);
        }

        private bool IsImageFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || 
                   ext == ".gif" || ext == ".bmp" || ext == ".webp" || 
                   ext == ".tiff" || ext == ".tif";
        }
    }
}

// Tipos de archivo comprimido soportados por detección de firma
internal enum ArchiveKind { None, Zip, Rar, SevenZip, Tar }

namespace ComicReader.Services
{
    public partial class ComicPageLoader
    {
        // Heurística de selección de portada: prioriza nombres comunes y primeras numeraciones, evita créditos/contraportada
        private static string SelectBestCover(IEnumerable<string> entries)
        {
            if (entries == null) return string.Empty;
            string best = string.Empty;
            int bestScore = int.MinValue;
            foreach (var e in entries)
            {
                int score = ScoreCoverName(e);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = e;
                }
                else if (score == bestScore && !string.IsNullOrEmpty(best))
                {
                    // Desempate: orden alfabético asc
                    if (string.Compare(e, best, StringComparison.OrdinalIgnoreCase) < 0)
                        best = e;
                }
            }
            return best;
        }

        private static int ScoreCoverName(string path)
        {
            if (string.IsNullOrEmpty(path)) return int.MinValue;
            var name = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            var full = path.Replace('\\', '/').ToLowerInvariant();
            int score = 0;

            // Palabras clave positivas (portada)
            if (name.Contains("cover")) score += 140;
            if (name.Contains("portada")) score += 140;
            if (name.Contains("front")) score += 100;
            if (name.Contains("caratula") || name.Contains("carátula") || name.Contains("cubierta") || name.Contains("tapa")) score += 100;
            if (name.Equals("folder") || name.Contains("folder")) score += 70;
            if (full.Contains("/cover") || full.Contains("/covers") || full.Contains("/portada") || full.Contains("/front")) score += 70;

            // Palabras clave negativas
            if (name.Contains("back") || name.Contains("contraport")) score -= 140;
            if (name.Contains("credit") || name.Contains("crédit")) score -= 120;
            if (name.Contains("indice") || name.Contains("índice") || name.Contains("index") || name.Contains("contenido") || name.Contains("sumario")) score -= 80;
            if (name.Contains("ads") || name.Contains("advert") || name.Contains("thanks") || name.Contains("agradec") || name.Contains("publicidad") || name.Contains("anuncio")) score -= 60;
            if (name.Contains("presenta") || name.Contains("prologo") || name.Contains("prólogo") || name.Contains("intro") || name.Contains("introduc")) score -= 50;
            if (name.Contains("preview") || name.Contains("sketch") || name.Contains("extra") || name.Contains("extras") || name.Contains("bonus")) score -= 60;
            if (full.Contains("/extras/") || full.Contains("/extra/") || full.Contains("/bonus/") || full.Contains("/preview/") || full.Contains("/sketch/")) score -= 60;

            // Preferir numeración baja (000, 001, 01)
            int leading = 0;
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsDigit(name[i])) leading = leading * 10 + (name[i] - '0'); else break;
            }
            // Penalizar explícitamente 000 (suele ser créditos) y favorecer 001
            var onlyDigits = new string(name.TakeWhile(char.IsDigit).ToArray());
            if (onlyDigits == "000") score -= 40;
            if (onlyDigits == "001") score += 35;
            if (leading == 0 && name.Length > 0 && char.IsDigit(name[0])) score += 30; // 000 puede ser portada, pero menor peso
            else if (leading == 1) score += 35;
            else if (leading <= 3) score += 20;
            else if (leading <= 5) score += 10;

            // Bonus leve si el nombre es corto
            if (name.Length <= 8) score += 5;

            // Preferir archivos en la raíz del zip (menos subcarpetas)
            int depth = full.Count(c => c == '/');
            score += Math.Max(0, 3 - depth) * 5; // más puntos si depth 0 o 1

            return score;
        }
        // Detección rápida por firma (cabecera) del tipo real de archivo
        private static ArchiveKind DetectArchiveKindFromFile(string path, string ext)
        {
            try
            {
                using var fs = File.OpenRead(path);
                Span<byte> header = stackalloc byte[8];
                int read = fs.Read(header);
                if (read >= 4)
                {
                    // ZIP: 50 4B 03 04 | 50 4B 05 06 | 50 4B 07 08
                    if (header[0] == 0x50 && header[1] == 0x4B &&
                        ((header[2] == 0x03 && header[3] == 0x04) ||
                         (header[2] == 0x05 && header[3] == 0x06) ||
                         (header[2] == 0x07 && header[3] == 0x08)))
                        return ArchiveKind.Zip;

                    // RAR 4.x: 52 61 72 21 1A 07 00 ; RAR5: 52 61 72 21 1A 07 01 00
                    if (read >= 7 && header[0] == 0x52 && header[1] == 0x61 && header[2] == 0x72 && header[3] == 0x21 && header[4] == 0x1A && header[5] == 0x07 && (header[6] == 0x00 || header[6] == 0x01))
                        return ArchiveKind.Rar;

                    // 7Z: 37 7A BC AF 27 1C
                    if (header[0] == 0x37 && header[1] == 0x7A && header[2] == 0xBC && header[3] == 0xAF && header[4] == 0x27 && header[5] == 0x1C)
                        return ArchiveKind.SevenZip;
                }
            }
            catch { }

            // TAR carece de una firma fuerte, asumimos por extensión
            if (string.Equals(ext, ".cbt", StringComparison.OrdinalIgnoreCase) || string.Equals(ext, ".tar", StringComparison.OrdinalIgnoreCase))
                return ArchiveKind.Tar;

            // Si nada coincide, devolvemos None
            return ArchiveKind.None;
        }

        // Carga genérica en función del tipo real detectado
        private async Task LoadArchiveByKindAsync(ArchiveKind kind)
        {
            switch (kind)
            {
                case ArchiveKind.Zip:
                    await LoadCBZAsync();
                    break;
                case ArchiveKind.Rar:
                    try { await LoadCBRAsync(); }
                    catch (SharpCompress.Common.InvalidFormatException)
                    {
                        // Fallback silencioso: algunos CBR son ZIP/7Z renombrados
                        var detected = DetectArchiveKindFromFile(_filePath, Path.GetExtension(_filePath));
                        if (detected == ArchiveKind.Zip) { await LoadCBZAsync(); return; }
                        if (detected == ArchiveKind.SevenZip) { await LoadCB7Async(); return; }
                        throw;
                    }
                    break;
                case ArchiveKind.SevenZip:
                    await LoadCB7Async();
                    break;
                case ArchiveKind.Tar:
                    await LoadCBTAsync();
                    break;
                default:
                    // Intentar con lector genérico como último recurso
                    await Task.Run(() =>
                    {
                        try
                        {
                            using var fs = File.OpenRead(_filePath);
                            using var reader = SharpCompress.Readers.ReaderFactory.Open(fs);
                            var list = new List<string>();
                            while (reader.MoveToNextEntry())
                            {
                                if (!reader.Entry.IsDirectory && IsSupportedImageExtension(Path.GetExtension(reader.Entry.Key)))
                                    list.Add(reader.Entry.Key);
                            }
                            list = list.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
                            for (int i = 0; i < list.Count; i++)
                                _pages.Add(new Models.ComicPage(i + 1, list[i], null));
                        }
                        catch (Exception ex)
                        {
                            Logger.LogException($"Fallback generic reader failed for {_filePath}", ex);
                            throw;
                        }
                    });
                    break;
            }
        }
    }
}