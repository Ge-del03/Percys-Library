using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;

namespace ComicReader.Services
{
    /// <summary>
    /// Formatos de exportación soportados
    /// </summary>
    public enum ExportFormat
    {
        PNG,
        JPEG,
        WebP,
        PDF
    }

    /// <summary>
    /// Opciones para exportar páginas
    /// </summary>
    public class ExportOptions
    {
        public ExportFormat Format { get; set; } = ExportFormat.PNG;
        public int Quality { get; set; } = 90; // Para JPEG/WebP (0-100)
        public int MaxWidth { get; set; } = 0; // 0 = sin límite
        public int MaxHeight { get; set; } = 0; // 0 = sin límite
        public bool MaintainAspectRatio { get; set; } = true;
        public bool IncludeMetadata { get; set; } = true;
        public string OutputFolder { get; set; } = string.Empty;
        public string FileNamePattern { get; set; } = "{comic}_{page}"; // Patrón de nombre
    }

    /// <summary>
    /// Servicio para exportar páginas de cómics
    /// </summary>
    public class PageExporter
    {
        private readonly Core.Abstractions.IComicPageLoader _pageLoader;

        public PageExporter(Core.Abstractions.IComicPageLoader pageLoader)
        {
            _pageLoader = pageLoader ?? throw new ArgumentNullException(nameof(pageLoader));
        }

        /// <summary>
        /// Exporta una página individual
        /// </summary>
        public async Task<string> ExportPageAsync(int pageIndex, ExportOptions options)
        {
            if (pageIndex < 0 || pageIndex >= _pageLoader.PageCount)
                throw new ArgumentOutOfRangeException(nameof(pageIndex));

            var imageData = await _pageLoader.GetPageImageAsync(pageIndex);
            if (imageData == null)
                throw new Exception("No se pudo cargar la imagen de la página");

            // Convertir BitmapImage a byte[]
            var bytes = BitmapImageToBytes(imageData);
            return await ExportImageAsync(bytes, pageIndex, options);
        }

        private byte[] BitmapImageToBytes(BitmapImage bitmap)
        {
            using var stream = new MemoryStream();
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Exporta un rango de páginas
        /// </summary>
        public async Task<List<string>> ExportPageRangeAsync(int startPage, int endPage, ExportOptions options, IProgress<double>? progress = null)
        {
            if (startPage < 0 || endPage >= _pageLoader.PageCount || startPage > endPage)
                throw new ArgumentException("Rango de páginas inválido");

            var exportedFiles = new List<string>();
            int totalPages = endPage - startPage + 1;

            for (int i = startPage; i <= endPage; i++)
            {
                try
                {
                    var filePath = await ExportPageAsync(i, options);
                    exportedFiles.Add(filePath);

                    var currentProgress = (double)(i - startPage + 1) / totalPages;
                    progress?.Report(currentProgress);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error exportando página {i}: {ex.Message}");
                }
            }

            return exportedFiles;
        }

        /// <summary>
        /// Exporta todas las páginas del cómic
        /// </summary>
        public async Task<List<string>> ExportAllPagesAsync(ExportOptions options, IProgress<double>? progress = null)
        {
            return await ExportPageRangeAsync(0, _pageLoader.PageCount - 1, options, progress);
        }

        /// <summary>
        /// Exporta páginas seleccionadas
        /// </summary>
        public async Task<List<string>> ExportSelectedPagesAsync(List<int> pageIndices, ExportOptions options, IProgress<double>? progress = null)
        {
            var exportedFiles = new List<string>();
            int totalPages = pageIndices.Count;

            for (int i = 0; i < pageIndices.Count; i++)
            {
                try
                {
                    var filePath = await ExportPageAsync(pageIndices[i], options);
                    exportedFiles.Add(filePath);

                    var currentProgress = (double)(i + 1) / totalPages;
                    progress?.Report(currentProgress);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error exportando página {pageIndices[i]}: {ex.Message}");
                }
            }

            return exportedFiles;
        }

        private async Task<string> ExportImageAsync(byte[] imageData, int pageIndex, ExportOptions options)
        {
            options ??= new ExportOptions();
            using var image = SixLabors.ImageSharp.Image.Load(imageData);

            // Redimensionar si es necesario
            if (options.MaxWidth > 0 || options.MaxHeight > 0)
            {
                int targetWidth = options.MaxWidth > 0 ? options.MaxWidth : image.Width;
                int targetHeight = options.MaxHeight > 0 ? options.MaxHeight : image.Height;

                if (options.MaintainAspectRatio)
                {
                    double scale = Math.Min(
                        (double)targetWidth / image.Width,
                        (double)targetHeight / image.Height
                    );
                    targetWidth = (int)(image.Width * scale);
                    targetHeight = (int)(image.Height * scale);
                }

                image.Mutate(x => x.Resize(targetWidth, targetHeight));
            }

            // Generar nombre de archivo
            var comicName = Path.GetFileNameWithoutExtension(_pageLoader.FilePath ?? string.Empty);
            var pattern = options.FileNamePattern ?? "{comic}_{page}";
            var fileName = pattern
                .Replace("{comic}", SanitizeFileName(comicName ?? string.Empty))
                .Replace("{page}", (pageIndex + 1).ToString("D4"));

            string outputPath;
            var outFolder = !string.IsNullOrWhiteSpace(options.OutputFolder) ? options.OutputFolder : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            switch (options.Format)
            {
                case ExportFormat.JPEG:
                    fileName += ".jpg";
                    outputPath = Path.Combine(outFolder, fileName);
                    await image.SaveAsJpegAsync(outputPath, new JpegEncoder { Quality = options.Quality });
                    break;

                case ExportFormat.WebP:
                    fileName += ".webp";
                    outputPath = Path.Combine(outFolder, fileName);
                    await image.SaveAsWebpAsync(outputPath, new WebpEncoder { Quality = options.Quality });
                    break;

                case ExportFormat.PNG:
                default:
                    fileName += ".png";
            outputPath = Path.Combine(outFolder, fileName);
            await image.SaveAsPngAsync(outputPath);
                    break;
            }

            return outputPath;
        }

        /// <summary>
        /// Exporta páginas como PDF
        /// </summary>
        public async Task<string> ExportToPdfAsync(List<int> pageIndices, ExportOptions options, IProgress<double>? progress = null)
        {
            // Nota: Requeriría una biblioteca PDF adicional como iTextSharp o PdfSharp
            // Aquí está la estructura básica:
            
            var outputFolder = options?.OutputFolder ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var outputPath = Path.Combine(
                outputFolder,
                $"{SanitizeFileName(Path.GetFileNameWithoutExtension(_pageLoader.FilePath ?? string.Empty))}.pdf"
            );

            // TODO: Implementar generación de PDF
            // Por ahora, exportar como imágenes individuales
            options ??= new ExportOptions();
            await ExportSelectedPagesAsync(pageIndices, options, progress);

            return outputPath;
        }

        private string SanitizeFileName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        /// <summary>
        /// Crea un archivo ZIP con las páginas exportadas
        /// </summary>
        public async Task<string> ExportAsZipAsync(List<int> pageIndices, ExportOptions options, IProgress<double>? progress = null)
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);

            try
            {
                // Exportar páginas a carpeta temporal
                var tempOptions = new ExportOptions
                {
                    Format = options.Format,
                    Quality = options.Quality,
                    MaxWidth = options.MaxWidth,
                    MaxHeight = options.MaxHeight,
                    MaintainAspectRatio = options.MaintainAspectRatio,
                    OutputFolder = tempFolder,
                    FileNamePattern = options.FileNamePattern
                };

                await ExportSelectedPagesAsync(pageIndices, tempOptions, progress);

                // Crear archivo ZIP
                var comicName = Path.GetFileNameWithoutExtension(_pageLoader.FilePath ?? string.Empty);
                var outFolder = options?.OutputFolder ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var zipPath = Path.Combine(outFolder, $"{SanitizeFileName(comicName)}_exported.zip");

                System.IO.Compression.ZipFile.CreateFromDirectory(tempFolder, zipPath);

                return zipPath;
            }
            finally
            {
                // Limpiar archivos temporales
                try
                {
                    if (Directory.Exists(tempFolder))
                    {
                        Directory.Delete(tempFolder, true);
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Obtiene el tamaño estimado de la exportación
        /// </summary>
        public async Task<long> EstimateExportSizeAsync(List<int> pageIndices, ExportOptions options)
        {
            // Tomar muestra de 3 páginas para estimar
            var sampleSize = Math.Min(3, pageIndices.Count);
            var sampleIndices = pageIndices.Take(sampleSize).ToList();

            long totalSize = 0;
            var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);

            try
            {
                var tempOptions = new ExportOptions
                {
                    Format = options.Format,
                    Quality = options.Quality,
                    MaxWidth = options.MaxWidth,
                    MaxHeight = options.MaxHeight,
                    MaintainAspectRatio = options.MaintainAspectRatio,
                    OutputFolder = tempFolder,
                    FileNamePattern = options.FileNamePattern
                };

                var sampleFiles = await ExportSelectedPagesAsync(sampleIndices, tempOptions);

                foreach (var file in sampleFiles)
                {
                    if (File.Exists(file))
                    {
                        totalSize += new FileInfo(file).Length;
                    }
                }

                // Estimar tamaño total
                long estimatedSize = (totalSize / sampleSize) * pageIndices.Count;
                return estimatedSize;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempFolder))
                    {
                        Directory.Delete(tempFolder, true);
                    }
                }
                catch { }
            }
        }
    }
}
