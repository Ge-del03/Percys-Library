using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ComicReader.Core.Abstractions;
using System.Globalization;
using ComicReader.Services;
using ComicReader.Models;

namespace ComicReader.Services
{
    public class ReadingStatsService : IReadingStatsService
    {
        private readonly string _rootDir;
        private readonly string _dataFile;
        private Data _data = new();
    private ActiveSession? _active;

        public ReadingStatsService()
        {
            _rootDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PercysLibrary");
            _dataFile = Path.Combine(_rootDir, "readingStats.json");
            Load();
        }

        public void StartSession(string comicPath, string comicTitle, int totalPages)
        {
            EndSession(); // Cerrar si había una previa
            _active = new ActiveSession
            {
                ComicPath = comicPath,
                ComicTitle = comicTitle,
                TotalPages = totalPages,
                StartTime = DateTime.Now,
                LastPage = 0
            };
        }

        public void EndSession()
        {
            if (_active == null) return;
            var now = DateTime.Now;
            var duration = now - _active.StartTime;
            var pages = Math.Max(0, _active.LastPage - _active.StartPage);
            _data.Sessions.Add(new SessionRecord
            {
                ComicPath = _active.ComicPath,
                ComicTitle = _active.ComicTitle,
                StartTime = _active.StartTime,
                EndTime = now,
                PagesRead = pages
            });

            // Actualizar progreso del cómic
            var prog = _data.Progress.FirstOrDefault(p => string.Equals(p.ComicPath, _active.ComicPath, StringComparison.OrdinalIgnoreCase));
            if (prog == null)
            {
                prog = new ComicProgressRecord
                {
                    ComicPath = _active.ComicPath,
                    Title = _active.ComicTitle,
                    TotalPages = _active.TotalPages,
                    Progress = _active.LastPage,
                    LastRead = now
                };
                _data.Progress.Add(prog);
            }
            else
            {
                prog.Title = _active.ComicTitle;
                prog.TotalPages = _active.TotalPages;
                prog.Progress = Math.Max(prog.Progress, _active.LastPage);
                prog.LastRead = now;
            }

            Save();
            _active = null;
        }

        public void RecordPageViewed(int pageNumber)
        {
            if (_active == null)
                return;
            if (_active.StartPage == 0)
                _active.StartPage = pageNumber;
            _active.LastPage = Math.Max(_active.LastPage, pageNumber);
        }

        public StatsDashboard GetDashboard()
        {
            var now = DateTime.Now;
            var weekAgo = now.AddDays(-7);
            var monthAgo = now.AddDays(-30);

            var totalTime = _data.Sessions.Aggregate(TimeSpan.Zero, (acc, s) => acc + (s.EndTime - s.StartTime));
            var totalPages = _data.Sessions.Sum(s => s.PagesRead);
            var comicsFinished = _data.Progress.Count(p => p.TotalPages > 0 && p.Progress >= p.TotalPages);

            var thisWeekComics = _data.Sessions.Where(s => s.StartTime >= weekAgo).Select(s => s.ComicPath).Distinct().Count();
            var thisMonthComics = _data.Sessions.Where(s => s.StartTime >= monthAgo).Select(s => s.ComicPath).Distinct().Count();
            var longest = _data.Sessions.Any() ? _data.Sessions.Max(s => (s.EndTime - s.StartTime)) : TimeSpan.Zero;

            // streak: días consecutivos con al menos una sesión
            int streak = 0;
            var days = _data.Sessions
                .Select(s => s.StartTime.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();
            var dcur = now.Date;
            foreach (var d in days)
            {
                if (d == dcur) { streak++; dcur = dcur.AddDays(-1); }
                else if (d == dcur.AddDays(-1)) { streak++; dcur = dcur.AddDays(-1); }
                else break;
            }

            var avg = _data.Sessions.Any() ? TimeSpan.FromMinutes(_data.Sessions.Average(s => (s.EndTime - s.StartTime).TotalMinutes)) : TimeSpan.Zero;

            // Día favorito (por número de sesiones)
            string favoriteDay = "—";
            if (_data.Sessions.Any())
            {
                var day = _data.Sessions
                    .GroupBy(s => s.StartTime.DayOfWeek)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault();
                // Localizar nombre del día según cultura actual
                var culture = CultureInfo.CurrentUICulture;
                favoriteDay = culture.DateTimeFormat.GetDayName(day);
                if (!string.IsNullOrEmpty(favoriteDay))
                {
                    // Capitalizar primera letra si la cultura lo devuelve en minúsculas
                    favoriteDay = char.ToUpper(favoriteDay[0], culture) + favoriteDay.Substring(1);
                }
            }

            // Formato preferido (por sesiones únicas por archivo)
            string preferredFormat = "—";
            if (_data.Sessions.Any())
            {
                preferredFormat = _data.Sessions
                    .Select(s => Path.GetExtension(s.ComicPath) ?? string.Empty)
                    .Select(ext => ext.TrimStart('.').ToUpperInvariant())
                    .Where(ext => !string.IsNullOrWhiteSpace(ext))
                    .GroupBy(ext => ext)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault() ?? "—";
            }

            // Género favorito (por tags de colecciones de favoritos para los cómics con actividad)
            string favoriteGenre = "—";
            try
            {
                var readPaths = _data.Progress.Select(p => p.ComicPath)
                    .Concat(_data.Sessions.Select(s => s.ComicPath))
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (readPaths.Count > 0)
                {
                    var collections = FavoritesStorage.Load();
                    var tags = collections
                        .SelectMany(c => c.Items)
                        .Where(item => !string.IsNullOrWhiteSpace(item.FilePath) && readPaths.Contains(item.FilePath))
                        .SelectMany(item => item.Tags ?? Array.Empty<string>())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .Select(t => t.Trim())
                        .ToList();

                    if (tags.Count > 0)
                    {
                        favoriteGenre = tags
                            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
                            .OrderByDescending(g => g.Count())
                            .Select(g => g.Key)
                            .FirstOrDefault() ?? "—";
                    }
                }
            }
            catch { }

            return new StatsDashboard
            {
                TotalComicsRead = comicsFinished,
                TotalPagesRead = totalPages,
                TotalReadingTime = totalTime,
                AverageReadingTime = avg,
                ComicsThisWeek = thisWeekComics,
                ComicsThisMonth = thisMonthComics,
                FavoriteGenre = favoriteGenre,
                FavoriteDay = favoriteDay,
                PreferredFormat = preferredFormat,
                LongestReadingSession = longest,
                CurrentStreak = streak
            };
        }

        public IEnumerable<ComicProgressInfo> GetRecentProgress(int count = 10)
        {
            return _data.Progress
                .OrderByDescending(p => p.LastRead)
                .Take(count)
                .Select(p => new ComicProgressInfo
                {
                    Title = p.Title,
                    ComicPath = p.ComicPath,
                    Progress = p.Progress,
                    TotalPages = p.TotalPages,
                    LastRead = p.LastRead
                });
        }

        public IEnumerable<ReadingSessionInfo> GetTodaySessions()
        {
            var today = DateTime.Today;
            return _data.Sessions
                .Where(s => s.StartTime.Date == today)
                .OrderByDescending(s => s.StartTime)
                .Select(s => new ReadingSessionInfo
                {
                    ComicTitle = s.ComicTitle,
                    StartTime = s.StartTime,
                    Duration = s.EndTime - s.StartTime,
                    PagesRead = s.PagesRead
                });
        }

        public void ResetAll()
        {
            _data = new Data();
            Save();
        }

        public void ExportSessionsToCsv(string filePath)
        {
            var sep = ",";
            var lines = new List<string> { "ComicTitle,StartTime,EndTime,DurationMinutes,PagesRead" };
            lines.AddRange(_data.Sessions.Select(s => string.Join(sep, new[]
            {
                Escape(s.ComicTitle),
                s.StartTime.ToString("s"),
                s.EndTime.ToString("s"),
                ((int)(s.EndTime - s.StartTime).TotalMinutes).ToString(),
                s.PagesRead.ToString()
            })));
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllLines(filePath, lines);

            static string Escape(string v) => '"' + (v?.Replace("\"", "\"\"") ?? string.Empty) + '"';
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_dataFile))
                {
                    var json = File.ReadAllText(_dataFile);
                    _data = JsonSerializer.Deserialize<Data>(json) ?? new Data();
                }
                else
                {
                    // Migrar desde ruta antigua si existe
                    var oldRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ComicReader");
                    var oldFile = Path.Combine(oldRoot, "readingStats.json");
                    if (File.Exists(oldFile))
                    {
                        var json = File.ReadAllText(oldFile);
                        _data = JsonSerializer.Deserialize<Data>(json) ?? new Data();
                        try
                        {
                            Directory.CreateDirectory(_rootDir);
                            File.Copy(oldFile, _dataFile, overwrite: true);
                        }
                        catch { }
                    }
                }
            }
            catch
            {
                _data = new Data();
            }
        }

        private void Save()
        {
            try
            {
                Directory.CreateDirectory(_rootDir);
                var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_dataFile, json);
            }
            catch { }
        }

        private class Data
        {
            public List<SessionRecord> Sessions { get; set; } = new();
            public List<ComicProgressRecord> Progress { get; set; } = new();
        }

        private class SessionRecord
        {
            public string ComicPath { get; set; } = string.Empty;
            public string ComicTitle { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public int PagesRead { get; set; }
        }

        private class ComicProgressRecord
        {
            public string ComicPath { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public int Progress { get; set; }
            public int TotalPages { get; set; }
            public DateTime LastRead { get; set; }
        }

        private class ActiveSession
        {
            public string ComicPath { get; set; } = string.Empty;
            public string ComicTitle { get; set; } = string.Empty;
            public int TotalPages { get; set; }
            public DateTime StartTime { get; set; }
            public int StartPage { get; set; }
            public int LastPage { get; set; }
        }

        // Métodos adicionales para estadísticas
        public int GetTotalComicsRead()
        {
            return _data.Progress.Count;
        }

        public int GetTotalPagesRead()
        {
            return _data.Sessions.Sum(s => s.PagesRead);
        }

        public int GetTotalReadingTime()
        {
            var totalTime = _data.Sessions.Aggregate(TimeSpan.Zero, (acc, s) => acc + (s.EndTime - s.StartTime));
            return (int)totalTime.TotalMinutes;
        }

        public int GetCurrentStreak()
        {
            if (_data.Sessions.Count == 0) return 0;
            
            var sessions = _data.Sessions.OrderByDescending(s => s.StartTime).ToList();
            var streak = 0;
            var currentDate = DateTime.Now.Date;
            
            while (sessions.Any(s => s.StartTime.Date == currentDate))
            {
                streak++;
                currentDate = currentDate.AddDays(-1);
            }
            
            return streak;
        }

        public double GetDailyAverage()
        {
            if (_data.Progress.Count == 0) return 0;
            
            var firstRead = _data.Sessions.Min(s => s.StartTime);
            var days = Math.Max(1, (DateTime.Now - firstRead).Days);
            
            return (double)GetTotalPagesRead() / days;
        }

        public class ComicStats
        {
            public string FilePath { get; set; } = string.Empty;
            public int ReadCount { get; set; }
            public int CurrentPage { get; set; }
            public int TotalPages { get; set; }
            public DateTime LastReadDate { get; set; }
            public int TotalReadingTime { get; set; }
        }

        public List<ComicStats> GetMostReadComics(int count)
        {
            var comicGroups = _data.Sessions
                .GroupBy(s => s.ComicPath)
                .Select(g => new 
                {
                    Path = g.Key,
                    Count = g.Count(),
                    LastDate = g.Max(s => s.EndTime),
                    TotalTime = (int)g.Sum(s => (s.EndTime - s.StartTime).TotalMinutes)
                })
                .OrderByDescending(x => x.Count)
                .Take(count);
            
            return comicGroups.Select(g =>
            {
                var prog = _data.Progress.FirstOrDefault(p => p.ComicPath == g.Path);
                return new ComicStats
                {
                    FilePath = g.Path,
                    ReadCount = g.Count,
                    CurrentPage = prog?.Progress ?? 0,
                    TotalPages = prog?.TotalPages ?? 0,
                    LastReadDate = g.LastDate,
                    TotalReadingTime = g.TotalTime
                };
            }).ToList();
        }

        public List<ComicStats> GetRecentComics(int count)
        {
            return _data.Progress
                .OrderByDescending(p => p.LastRead)
                .Take(count)
                .Select(p => new ComicStats
                {
                    FilePath = p.ComicPath,
                    ReadCount = _data.Sessions.Count(s => s.ComicPath == p.ComicPath),
                    CurrentPage = p.Progress,
                    TotalPages = p.TotalPages,
                    LastReadDate = p.LastRead,
                    TotalReadingTime = (int)_data.Sessions
                        .Where(s => s.ComicPath == p.ComicPath)
                        .Sum(s => (s.EndTime - s.StartTime).TotalMinutes)
                })
                .ToList();
        }

        public List<ComicStats> GetUnfinishedComics(int count)
        {
            return _data.Progress
                .Where(p => p.Progress < p.TotalPages && p.TotalPages > 0)
                .OrderByDescending(p => p.LastRead)
                .Take(count)
                .Select(p => new ComicStats
                {
                    FilePath = p.ComicPath,
                    ReadCount = _data.Sessions.Count(s => s.ComicPath == p.ComicPath),
                    CurrentPage = p.Progress,
                    TotalPages = p.TotalPages,
                    LastReadDate = p.LastRead,
                    TotalReadingTime = (int)_data.Sessions
                        .Where(s => s.ComicPath == p.ComicPath)
                        .Sum(s => (s.EndTime - s.StartTime).TotalMinutes)
                })
                .ToList();
        }

        public Dictionary<string, int> GetWeeklyActivity()
        {
            var weekAgo = DateTime.Now.AddDays(-7);
            var activity = new Dictionary<string, int>();
            
            for (int i = 0; i < 7; i++)
            {
                var date = DateTime.Now.Date.AddDays(-i);
                var dayName = date.ToString("dddd", new CultureInfo("es-ES"));
                var pages = _data.Sessions
                    .Where(s => s.StartTime.Date == date)
                    .Sum(s => s.PagesRead);
                activity[dayName] = pages;
            }
            
            return activity;
        }

        public Dictionary<string, int> GetFormatDistribution()
        {
            return _data.Progress
                .GroupBy(p => Path.GetExtension(p.ComicPath).ToLower())
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}
