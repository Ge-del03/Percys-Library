using System;
using System.Collections.Generic;
using System.Linq;

namespace ComicReader.Services
{
    /// <summary>
    /// Tipos de recomendaciones
    /// </summary>
    public enum RecommendationType
    {
        SimilarContent,     // Contenido similar
        SameAuthor,         // Mismo autor
        SameSeries,         // Misma serie
        SameGenre,          // Mismo género
        PopularInLibrary,   // Popular en biblioteca
        RecentlyAdded,      // Agregados recientemente
        Trending,           // Tendencias
        BasedOnHistory      // Basado en historial
    }

    /// <summary>
    /// Recomendación de cómic
    /// </summary>
    public class ComicRecommendation
    {
        public string ComicPath { get; set; } = string.Empty;
        public string ComicName { get; set; } = string.Empty;
        public RecommendationType Type { get; set; }
        public double Score { get; set; } // 0.0 a 1.0
        public string Reason { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public DateTime AddedDate { get; set; }
        public int TimesRead { get; set; }
        public double AverageRating { get; set; }
    }

    /// <summary>
    /// Motor de recomendaciones inteligente
    /// </summary>
    public class RecommendationEngine
    {
        private readonly ReadingStatsService _statsService;
        private readonly Models.CollectionManager _collectionManager;
        private readonly string _libraryPath;

        public RecommendationEngine(string libraryPath = "")
        {
            _libraryPath = string.IsNullOrEmpty(libraryPath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : libraryPath;

            _statsService = new ReadingStatsService();
            _collectionManager = new Models.CollectionManager();
        }

        /// <summary>
        /// Obtiene recomendaciones personalizadas
        /// </summary>
        public List<ComicRecommendation> GetRecommendations(string currentComicPath = "", int maxRecommendations = 10)
        {
            var recommendations = new List<ComicRecommendation>();

            // Recomendaciones basadas en el cómic actual
            if (!string.IsNullOrEmpty(currentComicPath))
            {
                recommendations.AddRange(GetSimilarComics(currentComicPath, 3));
                recommendations.AddRange(GetComicsInSameCollection(currentComicPath, 3));
            }

            // Recomendaciones basadas en historial
            recommendations.AddRange(GetBasedOnHistory(5));

            // Recomendaciones populares
            recommendations.AddRange(GetPopularComics(3));

            // Recomendaciones recientes
            recommendations.AddRange(GetRecentlyAdded(3));

            // Eliminar duplicados y ordenar por score
            recommendations = recommendations
                .GroupBy(r => r.ComicPath)
                .Select(g => g.OrderByDescending(r => r.Score).First())
                .OrderByDescending(r => r.Score)
                .Take(maxRecommendations)
                .ToList();

            return recommendations;
        }

        /// <summary>
        /// Obtiene cómics similares al especificado
        /// </summary>
        public List<ComicRecommendation> GetSimilarComics(string comicPath, int count = 5)
        {
            var recommendations = new List<ComicRecommendation>();

            try
            {
                // Buscar en la misma carpeta
                var directory = System.IO.Path.GetDirectoryName(comicPath);
                if (!string.IsNullOrEmpty(directory) && System.IO.Directory.Exists(directory))
                {
                    var similarFiles = System.IO.Directory.GetFiles(directory, "*.*")
                        .Where(f => IsSupportedFormat(f) && f != comicPath)
                        .Take(count);

                    foreach (var file in similarFiles)
                    {
                        recommendations.Add(new ComicRecommendation
                        {
                            ComicPath = file,
                            ComicName = System.IO.Path.GetFileNameWithoutExtension(file),
                            Type = RecommendationType.SimilarContent,
                            Score = 0.8,
                            Reason = "En la misma carpeta"
                        });
                    }
                }
            }
            catch { }

            return recommendations;
        }

        /// <summary>
        /// Obtiene cómics en las mismas colecciones
        /// </summary>
        public List<ComicRecommendation> GetComicsInSameCollection(string comicPath, int count = 5)
        {
            var recommendations = new List<ComicRecommendation>();
            var collections = _collectionManager.GetCollectionsForComic(comicPath);

            foreach (var collection in collections)
            {
                foreach (var path in collection.ComicPaths.Where(p => p != comicPath).Take(count))
                {
                    recommendations.Add(new ComicRecommendation
                    {
                        ComicPath = path,
                        ComicName = System.IO.Path.GetFileNameWithoutExtension(path),
                        Type = RecommendationType.SameSeries,
                        Score = 0.9,
                        Reason = $"En la colección '{collection.Name}'",
                        Tags = collection.Tags
                    });
                }
            }

            return recommendations;
        }

        /// <summary>
        /// Obtiene recomendaciones basadas en historial de lectura
        /// </summary>
        public List<ComicRecommendation> GetBasedOnHistory(int count = 10)
        {
            var recommendations = new List<ComicRecommendation>();
            var recentlyRead = _statsService.GetRecentComics(20);

            // Analizar patrones de lectura
            var genreFrequency = new Dictionary<string, int>();
            var authorFrequency = new Dictionary<string, int>();

            foreach (var comic in recentlyRead)
            {
                // Aquí podrías analizar metadatos para encontrar patrones
                // Por ahora, usamos una heurística simple basada en nombres de carpetas
                var directory = System.IO.Path.GetDirectoryName(comic.FilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    var folderName = System.IO.Path.GetFileName(directory);
                    if (!genreFrequency.ContainsKey(folderName))
                        genreFrequency[folderName] = 0;
                    genreFrequency[folderName]++;
                }
            }

            // Buscar cómics en carpetas más leídas
            foreach (var genre in genreFrequency.OrderByDescending(kvp => kvp.Value).Take(3))
            {
                try
                {
                    var genrePath = System.IO.Path.Combine(_libraryPath, genre.Key);
                    if (System.IO.Directory.Exists(genrePath))
                    {
                        var files = System.IO.Directory.GetFiles(genrePath, "*.*")
                            .Where(f => IsSupportedFormat(f) && 
                                   !recentlyRead.Any(r => r.FilePath == f))
                            .Take(count);

                        foreach (var file in files)
                        {
                            recommendations.Add(new ComicRecommendation
                            {
                                ComicPath = file,
                                ComicName = System.IO.Path.GetFileNameWithoutExtension(file),
                                Type = RecommendationType.BasedOnHistory,
                                Score = 0.85,
                                Reason = $"Basado en tu interés en '{genre.Key}'"
                            });
                        }
                    }
                }
                catch { }
            }

            return recommendations;
        }

        /// <summary>
        /// Obtiene los cómics más populares
        /// </summary>
        public List<ComicRecommendation> GetPopularComics(int count = 5)
        {
            var recommendations = new List<ComicRecommendation>();
            var mostRead = _statsService.GetMostReadComics(count);

            foreach (var comic in mostRead)
            {
                recommendations.Add(new ComicRecommendation
                {
                    ComicPath = comic.FilePath,
                    ComicName = System.IO.Path.GetFileNameWithoutExtension(comic.FilePath),
                    Type = RecommendationType.PopularInLibrary,
                    Score = 0.75,
                    Reason = "Popular en tu biblioteca",
                    TimesRead = comic.ReadCount
                });
            }

            return recommendations;
        }

        /// <summary>
        /// Obtiene cómics agregados recientemente
        /// </summary>
        public List<ComicRecommendation> GetRecentlyAdded(int count = 5)
        {
            var recommendations = new List<ComicRecommendation>();

            try
            {
                if (System.IO.Directory.Exists(_libraryPath))
                {
                    var recentFiles = System.IO.Directory.GetFiles(_libraryPath, "*.*", System.IO.SearchOption.AllDirectories)
                        .Where(f => IsSupportedFormat(f))
                        .Select(f => new System.IO.FileInfo(f))
                        .OrderByDescending(fi => fi.CreationTime)
                        .Take(count);

                    foreach (var file in recentFiles)
                    {
                        recommendations.Add(new ComicRecommendation
                        {
                            ComicPath = file.FullName,
                            ComicName = System.IO.Path.GetFileNameWithoutExtension(file.Name),
                            Type = RecommendationType.RecentlyAdded,
                            Score = 0.7,
                            Reason = "Agregado recientemente",
                            AddedDate = file.CreationTime
                        });
                    }
                }
            }
            catch { }

            return recommendations;
        }

        /// <summary>
        /// Obtiene sugerencias para continuar leyendo
        /// </summary>
        public List<ComicRecommendation> GetContinueReading(int count = 5)
        {
            var recommendations = new List<ComicRecommendation>();
            var unfinished = _statsService.GetUnfinishedComics(count);

            foreach (var comic in unfinished)
            {
                recommendations.Add(new ComicRecommendation
                {
                    ComicPath = comic.FilePath,
                    ComicName = System.IO.Path.GetFileNameWithoutExtension(comic.FilePath),
                    Type = RecommendationType.BasedOnHistory,
                    Score = 0.95,
                    Reason = $"Continuar desde página {comic.CurrentPage}"
                });
            }

            return recommendations;
        }

        /// <summary>
        /// Registra interacción del usuario (para mejorar recomendaciones)
        /// </summary>
        public void RegisterInteraction(string comicPath, string interactionType, double weight = 1.0)
        {
            // Aquí podrías implementar un sistema de aprendizaje
            // Por ahora, solo registramos en stats
            try
            {
                // Registrar la interacción
                System.Diagnostics.Debug.WriteLine($"Interaction: {interactionType} on {comicPath} (weight: {weight})");
            }
            catch { }
        }

        private bool IsSupportedFormat(string filePath)
        {
            var supportedExtensions = new[] { ".cbz", ".cbr", ".cb7", ".cbt", ".zip", ".rar", ".7z", ".pdf", ".epub" };
            var ext = System.IO.Path.GetExtension(filePath).ToLower();
            return supportedExtensions.Contains(ext);
        }
    }
}
