using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ComicReader.Models;

namespace ComicReader.Services
{
    public class FolderScannerService
    {
        private static readonly string[] SupportedExtensions = { ".cbz", ".cbr", ".pdf", ".epub" };

        public async Task<List<ComicItem>> ScanDirectoryAsync(string directoryPath, IProgress<int> progress = null)
        {
            return await Task.Run(() =>
            {
                var items = new List<ComicItem>();
                if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
                    return items;

                try
                {
                    var files = Directory.EnumerateFiles(directoryPath, "*.*", SearchOption.AllDirectories)
                        .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                        .ToList();

                    int total = files.Count;
                    int processed = 0;

                    foreach (var file in files)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            var ext = fileInfo.Extension.ToLowerInvariant();
                            string formatName = ext.Replace(".", "").ToUpper();

                            var item = new ComicItem
                            {
                                FilePath = file,
                                Title = Path.GetFileNameWithoutExtension(file),
                                FileSize = fileInfo.Length,
                                DateAdded = fileInfo.CreationTime,
                                Format = formatName
                            };

                            // Intentar extraer ComicInfo.xml si es CBZ/ZIP
                            if (ext == ".cbz")
                            {
                                var meta = ComicInfoParserService.ParseFromArchive(file);
                                if (meta != null)
                                {
                                    if (!string.IsNullOrEmpty(meta.Title)) item.Title = meta.Title;
                                    item.Series = meta.Series;
                                    item.IssueNumber = meta.Number;
                                    item.Volume = meta.Volume;
                                    item.Year = meta.Year;
                                    item.Publisher = meta.Publisher;
                                    item.Writer = meta.Writer;
                                    item.Penciller = meta.Penciller;
                                    item.Author = !string.IsNullOrEmpty(meta.Writer) ? meta.Writer : meta.Penciller;
                                    item.Genre = meta.Genre;
                                    item.Summary = meta.Summary;
                                    if (meta.PageCount > 0) item.PageCount = meta.PageCount;
                                }
                            }

                            // Sincronizar progreso si existe en ContinueReadingService
                            var progressItem = ContinueReadingService.Instance.Items
                                .FirstOrDefault(x => string.Equals(x.FilePath, file, StringComparison.OrdinalIgnoreCase));
                            if (progressItem != null)
                            {
                                item.CurrentPage = progressItem.LastPage;
                                item.PageCount = progressItem.PageCount > 0 ? progressItem.PageCount : item.PageCount;
                                item.LastRead = progressItem.LastOpened;
                                item.IsRead = item.PageCount > 0 && item.CurrentPage >= item.PageCount;
                            }

                            items.Add(item);
                        }
                        catch
                        {
                            // Ignorar archivos individuales con errores de acceso
                        }

                        processed++;
                        if (total > 0)
                        {
                            progress?.Report((int)((double)processed / total * 100));
                        }
                    }
                }
                catch
                {
                    // Manejar error general de lectura de directorio
                }

                return items;
            });
        }
    }
}