using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;

namespace ComicReader.Services
{
    /// <summary>
    /// Resultado de comparación de dos imágenes
    /// </summary>
    public class ImageComparisonResult
    {
        public double SimilarityPercentage { get; set; }
        public int DifferentPixels { get; set; }
        public int TotalPixels { get; set; }
        public bool AreIdentical { get; set; }
        public TimeSpan ComparisonTime { get; set; }
    }

    /// <summary>
    /// Resultado de comparación de cómics completos
    /// </summary>
    public class ComicComparisonResult
    {
        public string Comic1Path { get; set; } = string.Empty;
        public string Comic2Path { get; set; } = string.Empty;
        public int Comic1PageCount { get; set; }
        public int Comic2PageCount { get; set; }
        public List<PageComparisonResult> PageComparisons { get; set; } = new List<PageComparisonResult>();
        public double OverallSimilarity { get; set; }
        public List<int> MissingPagesInComic1 { get; set; } = new List<int>();
        public List<int> MissingPagesInComic2 { get; set; } = new List<int>();
        public List<int> DifferentPages { get; set; } = new List<int>();
        public TimeSpan TotalComparisonTime { get; set; }
    }

    /// <summary>
    /// Resultado de comparación de una página específica
    /// </summary>
    public class PageComparisonResult
    {
        public int PageIndex { get; set; }
        public ImageComparisonResult? ComparisonResult { get; set; }
        public bool ExistsInBoth { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Servicio para comparar diferentes versiones de cómics
    /// </summary>
    public class ComicComparator
    {
        private const double SIMILARITY_THRESHOLD = 95.0; // Porcentaje de similitud para considerar imágenes idénticas

        private byte[] BitmapImageToBytes(System.Windows.Media.Imaging.BitmapImage bitmap)
        {
            using var stream = new MemoryStream();
            System.Windows.Media.Imaging.BitmapEncoder encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            encoder.Save(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Compara dos imágenes y devuelve el porcentaje de similitud
        /// </summary>
        public async Task<ImageComparisonResult> CompareImagesAsync(byte[] image1Data, byte[] image2Data)
        {
            var startTime = DateTime.Now;
            var result = new ImageComparisonResult();

            try
            {
                using var img1 = SixLabors.ImageSharp.Image.Load<Rgba32>(image1Data);
                using var img2 = SixLabors.ImageSharp.Image.Load<Rgba32>(image2Data);

                // Redimensionar la segunda imagen si tienen diferentes tamaños
                if (img1.Width != img2.Width || img1.Height != img2.Height)
                {
                    img2.Mutate(x => x.Resize(img1.Width, img1.Height));
                }

                result.TotalPixels = img1.Width * img1.Height;
                int differentPixels = 0;

                // Comparar pixel por pixel
                await Task.Run(() =>
                {
                    for (int y = 0; y < img1.Height; y++)
                    {
                        for (int x = 0; x < img1.Width; x++)
                        {
                            var pixel1 = img1[x, y];
                            var pixel2 = img2[x, y];

                            // Calcular diferencia de color
                            int rDiff = Math.Abs(pixel1.R - pixel2.R);
                            int gDiff = Math.Abs(pixel1.G - pixel2.G);
                            int bDiff = Math.Abs(pixel1.B - pixel2.B);

                            // Si la diferencia total excede un umbral, contar como diferente
                            if (rDiff + gDiff + bDiff > 30) // Umbral ajustable
                            {
                                differentPixels++;
                            }
                        }
                    }
                });

                result.DifferentPixels = differentPixels;
                result.SimilarityPercentage = 100.0 - ((double)differentPixels / result.TotalPixels * 100.0);
                result.AreIdentical = result.SimilarityPercentage >= SIMILARITY_THRESHOLD;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error comparando imágenes: {ex.Message}");
                result.SimilarityPercentage = 0;
            }

            result.ComparisonTime = DateTime.Now - startTime;
            return result;
        }

        /// <summary>
        /// Compara dos cómics página por página
        /// </summary>
        public async Task<ComicComparisonResult> CompareComicsAsync(
            Core.Abstractions.IComicPageLoader comic1,
            Core.Abstractions.IComicPageLoader comic2,
            IProgress<double>? progress = null)
        {
            var startTime = DateTime.Now;
            var result = new ComicComparisonResult
            {
                Comic1Path = comic1.FilePath,
                Comic2Path = comic2.FilePath,
                Comic1PageCount = comic1.PageCount,
                Comic2PageCount = comic2.PageCount
            };

            int maxPages = Math.Max(comic1.PageCount, comic2.PageCount);
            double totalSimilarity = 0;
            int comparedPages = 0;

            for (int i = 0; i < maxPages; i++)
            {
                var pageResult = new PageComparisonResult
                {
                    PageIndex = i
                };

                bool existsIn1 = i < comic1.PageCount;
                bool existsIn2 = i < comic2.PageCount;

                pageResult.ExistsInBoth = existsIn1 && existsIn2;

                if (!existsIn1)
                {
                    result.MissingPagesInComic1.Add(i);
                }
                else if (!existsIn2)
                {
                    result.MissingPagesInComic2.Add(i);
                }
                else
                {
                    try
                    {
                        var image1 = await comic1.GetPageImageAsync(i);
                        var image2 = await comic2.GetPageImageAsync(i);

                        if (image1 != null && image2 != null)
                        {
                            var bytes1 = BitmapImageToBytes(image1);
                            var bytes2 = BitmapImageToBytes(image2);
                            var comparison = await CompareImagesAsync(bytes1, bytes2);
                            pageResult.ComparisonResult = comparison;

                            totalSimilarity += comparison.SimilarityPercentage;
                            comparedPages++;

                            if (!comparison.AreIdentical)
                            {
                                result.DifferentPages.Add(i);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        pageResult.ErrorMessage = ex.Message;
                    }
                }

                result.PageComparisons.Add(pageResult);

                // Reportar progreso
                progress?.Report((double)(i + 1) / maxPages);
            }

            result.OverallSimilarity = comparedPages > 0 ? totalSimilarity / comparedPages : 0;
            result.TotalComparisonTime = DateTime.Now - startTime;

            return result;
        }

        /// <summary>
        /// Compara solo un rango de páginas
        /// </summary>
        public async Task<ComicComparisonResult> ComparePageRangeAsync(
            Core.Abstractions.IComicPageLoader comic1,
            Core.Abstractions.IComicPageLoader comic2,
            int startPage,
            int endPage,
            IProgress<double>? progress = null)
        {
            var startTime = DateTime.Now;
            var result = new ComicComparisonResult
            {
                Comic1Path = comic1.FilePath,
                Comic2Path = comic2.FilePath,
                Comic1PageCount = comic1.PageCount,
                Comic2PageCount = comic2.PageCount
            };

            int totalPages = endPage - startPage + 1;
            double totalSimilarity = 0;
            int comparedPages = 0;

            for (int i = startPage; i <= endPage; i++)
            {
                if (i >= comic1.PageCount || i >= comic2.PageCount)
                    break;

                var pageResult = new PageComparisonResult
                {
                    PageIndex = i,
                    ExistsInBoth = true
                };

                try
                {
                    var image1 = await comic1.GetPageImageAsync(i);
                    var image2 = await comic2.GetPageImageAsync(i);

                    if (image1 != null && image2 != null)
                    {
                        var bytes1 = BitmapImageToBytes(image1);
                        var bytes2 = BitmapImageToBytes(image2);
                        var comparison = await CompareImagesAsync(bytes1, bytes2);
                        pageResult.ComparisonResult = comparison;

                        totalSimilarity += comparison.SimilarityPercentage;
                        comparedPages++;

                        if (!comparison.AreIdentical)
                        {
                            result.DifferentPages.Add(i);
                        }
                    }
                }
                catch (Exception ex)
                {
                    pageResult.ErrorMessage = ex.Message;
                }

                result.PageComparisons.Add(pageResult);
                progress?.Report((double)(i - startPage + 1) / totalPages);
            }

            result.OverallSimilarity = comparedPages > 0 ? totalSimilarity / comparedPages : 0;
            result.TotalComparisonTime = DateTime.Now - startTime;

            return result;
        }

        /// <summary>
        /// Genera un reporte de comparación en formato de texto
        /// </summary>
        public string GenerateComparisonReport(ComicComparisonResult result)
        {
            var report = new System.Text.StringBuilder();

            report.AppendLine("═══════════════════════════════════════════════════════");
            report.AppendLine("        REPORTE DE COMPARACIÓN DE CÓMICS");
            report.AppendLine("═══════════════════════════════════════════════════════");
            report.AppendLine();

            report.AppendLine($"Cómic 1: {Path.GetFileName(result.Comic1Path)}");
            report.AppendLine($"  Páginas: {result.Comic1PageCount}");
            report.AppendLine();

            report.AppendLine($"Cómic 2: {Path.GetFileName(result.Comic2Path)}");
            report.AppendLine($"  Páginas: {result.Comic2PageCount}");
            report.AppendLine();

            report.AppendLine("───────────────────────────────────────────────────────");
            report.AppendLine("RESULTADOS:");
            report.AppendLine("───────────────────────────────────────────────────────");
            report.AppendLine($"Similitud General: {result.OverallSimilarity:F2}%");
            report.AppendLine($"Tiempo de Comparación: {result.TotalComparisonTime.TotalSeconds:F2} segundos");
            report.AppendLine();

            if (result.MissingPagesInComic1.Any())
            {
                report.AppendLine($"Páginas faltantes en Cómic 1: {result.MissingPagesInComic1.Count}");
                report.AppendLine($"  Páginas: {string.Join(", ", result.MissingPagesInComic1.Select(p => p + 1))}");
                report.AppendLine();
            }

            if (result.MissingPagesInComic2.Any())
            {
                report.AppendLine($"Páginas faltantes en Cómic 2: {result.MissingPagesInComic2.Count}");
                report.AppendLine($"  Páginas: {string.Join(", ", result.MissingPagesInComic2.Select(p => p + 1))}");
                report.AppendLine();
            }

            if (result.DifferentPages.Any())
            {
                report.AppendLine($"Páginas diferentes: {result.DifferentPages.Count}");
                report.AppendLine($"  Páginas: {string.Join(", ", result.DifferentPages.Select(p => p + 1))}");
                report.AppendLine();
            }

            if (!result.DifferentPages.Any() && !result.MissingPagesInComic1.Any() && !result.MissingPagesInComic2.Any())
            {
                report.AppendLine("✓ Los cómics son idénticos");
            }

            report.AppendLine("═══════════════════════════════════════════════════════");

            return report.ToString();
        }

        /// <summary>
        /// Guarda el reporte de comparación en un archivo
        /// </summary>
        public async Task SaveComparisonReportAsync(ComicComparisonResult result, string outputPath)
        {
            var report = GenerateComparisonReport(result);
            await File.WriteAllTextAsync(outputPath, report);
        }
    }
}
