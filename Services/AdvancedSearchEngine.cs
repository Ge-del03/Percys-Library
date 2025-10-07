using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ComicReader.Services
{
    /// <summary>
    /// Tipos de búsqueda disponibles
    /// </summary>
    public enum SearchType
    {
        FileName,       // Buscar por nombre de archivo
        Path,           // Buscar por ruta
        Metadata,       // Buscar en metadatos
        Annotations,    // Buscar en anotaciones
        Tags,           // Buscar por tags
        Content         // Buscar en contenido (OCR)
    }

    /// <summary>
    /// Resultado de búsqueda
    /// </summary>
    public class SearchResult
    {
        public string ComicPath { get; set; } = string.Empty;
        public string ComicName { get; set; } = string.Empty;
        public int? PageNumber { get; set; }
        public SearchType MatchType { get; set; }
        public string MatchText { get; set; } = string.Empty;
        public double Relevance { get; set; } // 0.0 a 1.0
        public DateTime LastAccessed { get; set; }
        public long FileSize { get; set; }
        public int PageCount { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }

    /// <summary>
    /// Opciones de búsqueda
    /// </summary>
    public class SearchOptions
    {
        public string Query { get; set; } = string.Empty;
        public List<SearchType> SearchTypes { get; set; } = new List<SearchType>();
        public bool CaseSensitive { get; set; } = false;
        public bool UseRegex { get; set; } = false;
        public bool IncludeArchived { get; set; } = false;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public long? MinFileSize { get; set; }
        public long? MaxFileSize { get; set; }
        public List<string> FileExtensions { get; set; } = new List<string>();
        public int MaxResults { get; set; } = 100;
    }

    /// <summary>
    /// Motor de búsqueda avanzada para cómics
    /// </summary>
    public class AdvancedSearchEngine
    {
        private readonly string _libraryPath;
        private readonly Models.AnnotationManager _annotationManager;
        private readonly Models.CollectionManager _collectionManager;
        private List<SearchResult> _cachedResults = new List<SearchResult>();

        public AdvancedSearchEngine(string libraryPath = "")
        {
            _libraryPath = string.IsNullOrEmpty(libraryPath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : libraryPath;

            _annotationManager = new Models.AnnotationManager();
            _collectionManager = new Models.CollectionManager();
        }

        /// <summary>
        /// Busca cómics según los criterios especificados
        /// </summary>
        public async Task<List<SearchResult>> SearchAsync(SearchOptions options, IProgress<double>? progress = null)
        {
            var results = new List<SearchResult>();
            var allComics = GetAllComics();
            
            int processed = 0;
            foreach (var comic in allComics)
            {
                if (await MatchesSearchCriteria(comic, options))
                {
                    results.Add(comic);
                }

                processed++;
                progress?.Report((double)processed / allComics.Count);

                if (results.Count >= options.MaxResults)
                    break;
            }

            // Ordenar por relevancia
            results = results.OrderByDescending(r => r.Relevance).ToList();

            _cachedResults = results;
            return results;
        }

        /// <summary>
        /// Búsqueda rápida por nombre
        /// </summary>
        public List<SearchResult> QuickSearch(string query, int maxResults = 20)
        {
            var options = new SearchOptions
            {
                Query = query,
                SearchTypes = new List<SearchType> { SearchType.FileName, SearchType.Path },
                MaxResults = maxResults
            };

            return SearchAsync(options).Result;
        }

        /// <summary>
        /// Búsqueda en anotaciones
        /// </summary>
        public List<SearchResult> SearchInAnnotations(string query)
        {
            var results = new List<SearchResult>();
            var annotations = _annotationManager.SearchAnnotations(query);

            foreach (var annotation in annotations)
            {
                var result = new SearchResult
                {
                    ComicPath = annotation.ComicFilePath,
                    ComicName = System.IO.Path.GetFileNameWithoutExtension(annotation.ComicFilePath),
                    PageNumber = annotation.PageNumber,
                    MatchType = SearchType.Annotations,
                    MatchText = annotation.TextContent ?? "",
                    Relevance = CalculateRelevance(annotation.TextContent ?? "", query)
                };

                results.Add(result);
            }

            return results.OrderByDescending(r => r.Relevance).ToList();
        }

        /// <summary>
        /// Búsqueda en colecciones
        /// </summary>
        public List<SearchResult> SearchInCollections(string query)
        {
            var results = new List<SearchResult>();
            var collections = _collectionManager.SearchCollections(query);

            foreach (var collection in collections)
            {
                foreach (var comicPath in collection.ComicPaths)
                {
                    var result = new SearchResult
                    {
                        ComicPath = comicPath,
                        ComicName = System.IO.Path.GetFileNameWithoutExtension(comicPath),
                        MatchType = SearchType.Tags,
                        MatchText = collection.Name,
                        Relevance = 0.8,
                        Tags = collection.Tags
                    };

                    results.Add(result);
                }
            }

            return results;
        }

        /// <summary>
        /// Búsqueda similar (encuentra cómics relacionados)
        /// </summary>
        public List<SearchResult> FindSimilar(string comicPath, int maxResults = 10)
        {
            var results = new List<SearchResult>();
            
            // Buscar cómics en la misma carpeta
            var directory = System.IO.Path.GetDirectoryName(comicPath);
            if (!string.IsNullOrEmpty(directory))
            {
                var similarComics = System.IO.Directory.GetFiles(directory, "*.*")
                    .Where(f => IsSupportedFormat(f) && f != comicPath)
                    .Take(maxResults);

                foreach (var similar in similarComics)
                {
                    results.Add(new SearchResult
                    {
                        ComicPath = similar,
                        ComicName = System.IO.Path.GetFileNameWithoutExtension(similar),
                        MatchType = SearchType.Path,
                        Relevance = 0.7
                    });
                }
            }

            // Buscar en mismas colecciones
            var collections = _collectionManager.GetCollectionsForComic(comicPath);
            foreach (var collection in collections)
            {
                foreach (var path in collection.ComicPaths.Where(p => p != comicPath).Take(maxResults))
                {
                    if (!results.Any(r => r.ComicPath == path))
                    {
                        results.Add(new SearchResult
                        {
                            ComicPath = path,
                            ComicName = System.IO.Path.GetFileNameWithoutExtension(path),
                            MatchType = SearchType.Tags,
                            Relevance = 0.9
                        });
                    }
                }
            }

            return results.OrderByDescending(r => r.Relevance).Take(maxResults).ToList();
        }

        /// <summary>
        /// Autocompletar búsqueda
        /// </summary>
        public List<string> AutoComplete(string partialQuery, int maxSuggestions = 10)
        {
            var suggestions = new HashSet<string>();
            var allComics = GetAllComics();

            foreach (var comic in allComics)
            {
                if (comic.ComicName.Contains(partialQuery, StringComparison.OrdinalIgnoreCase))
                {
                    suggestions.Add(comic.ComicName);
                }

                if (suggestions.Count >= maxSuggestions)
                    break;
            }

            return suggestions.OrderBy(s => s).ToList();
        }

        /// <summary>
        /// Filtrar resultados existentes
        /// </summary>
        public List<SearchResult> FilterResults(List<SearchResult> results, Func<SearchResult, bool> predicate)
        {
            return results.Where(predicate).ToList();
        }

        /// <summary>
        /// Exportar resultados a CSV
        /// </summary>
        public async Task ExportResultsToCSV(List<SearchResult> results, string outputPath)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Comic Name,Path,Page,Match Type,Relevance,File Size,Page Count");

            foreach (var result in results)
            {
                csv.AppendLine($"\"{result.ComicName}\",\"{result.ComicPath}\",{result.PageNumber},{result.MatchType},{result.Relevance},{result.FileSize},{result.PageCount}");
            }

            await System.IO.File.WriteAllTextAsync(outputPath, csv.ToString());
        }

        // Métodos privados auxiliares

        private List<SearchResult> GetAllComics()
        {
            var results = new List<SearchResult>();

            if (!System.IO.Directory.Exists(_libraryPath))
                return results;

            try
            {
                var files = System.IO.Directory.GetFiles(_libraryPath, "*.*", System.IO.SearchOption.AllDirectories)
                    .Where(f => IsSupportedFormat(f));

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new System.IO.FileInfo(file);
                        results.Add(new SearchResult
                        {
                            ComicPath = file,
                            ComicName = System.IO.Path.GetFileNameWithoutExtension(file),
                            LastAccessed = fileInfo.LastAccessTime,
                            FileSize = fileInfo.Length,
                            Relevance = 0.5
                        });
                    }
                    catch { }
                }
            }
            catch { }

            return results;
        }

        private async Task<bool> MatchesSearchCriteria(SearchResult comic, SearchOptions options)
        {
            bool matches = false;

            foreach (var searchType in options.SearchTypes)
            {
                switch (searchType)
                {
                    case SearchType.FileName:
                        if (MatchesQuery(comic.ComicName, options.Query, options.CaseSensitive))
                        {
                            matches = true;
                            comic.Relevance = Math.Max(comic.Relevance, CalculateRelevance(comic.ComicName, options.Query));
                        }
                        break;

                    case SearchType.Path:
                        if (MatchesQuery(comic.ComicPath, options.Query, options.CaseSensitive))
                        {
                            matches = true;
                            comic.Relevance = Math.Max(comic.Relevance, 0.6);
                        }
                        break;

                    case SearchType.Annotations:
                        var annotations = _annotationManager.GetAllAnnotations(comic.ComicPath);
                        if (annotations.Any(a => MatchesQuery(a.TextContent ?? "", options.Query, options.CaseSensitive)))
                        {
                            matches = true;
                            comic.Relevance = Math.Max(comic.Relevance, 0.9);
                        }
                        break;
                }
            }

            // Aplicar filtros adicionales
            if (matches)
            {
                if (options.FromDate.HasValue && comic.LastAccessed < options.FromDate.Value)
                    return false;

                if (options.ToDate.HasValue && comic.LastAccessed > options.ToDate.Value)
                    return false;

                if (options.MinFileSize.HasValue && comic.FileSize < options.MinFileSize.Value)
                    return false;

                if (options.MaxFileSize.HasValue && comic.FileSize > options.MaxFileSize.Value)
                    return false;

                if (options.FileExtensions.Any())
                {
                    var ext = System.IO.Path.GetExtension(comic.ComicPath).ToLower();
                    if (!options.FileExtensions.Contains(ext))
                        return false;
                }
            }

            return matches;
        }

        private bool MatchesQuery(string text, string query, bool caseSensitive)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query))
                return false;

            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            return text.Contains(query, comparison);
        }

        private double CalculateRelevance(string text, string query)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query))
                return 0.0;

            // Coincidencia exacta
            if (text.Equals(query, StringComparison.OrdinalIgnoreCase))
                return 1.0;

            // Comienza con la query
            if (text.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                return 0.9;

            // Contiene la query completa
            if (text.Contains(query, StringComparison.OrdinalIgnoreCase))
                return 0.8;

            // Coincidencia parcial de palabras
            var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var textWords = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var matches = queryWords.Count(qw => textWords.Any(tw => tw.Contains(qw, StringComparison.OrdinalIgnoreCase)));
            
            return (double)matches / queryWords.Length * 0.7;
        }

        private bool IsSupportedFormat(string filePath)
        {
            var supportedExtensions = new[] { ".cbz", ".cbr", ".cb7", ".cbt", ".zip", ".rar", ".7z", ".pdf", ".epub" };
            var ext = System.IO.Path.GetExtension(filePath).ToLower();
            return supportedExtensions.Contains(ext);
        }
    }
}
