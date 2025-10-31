using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.IO;
using ComicReader.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using ComicReader.Core.Abstractions;
using System.Windows.Threading;

namespace ComicReader.ViewModels
{
    public class StatItem
    {
        public string Label { get; set; }
        public string Value { get; set; }
        public string Icon { get; set; }
    }

    public class ProgressItem : INotifyPropertyChanged
    {
        private string _title;
        private double _percentage;
        private string _thumbnailPath;

        public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
        public double Percentage { get => _percentage; set { _percentage = value; OnPropertyChanged(); } }
    public string ThumbnailPath { get => _thumbnailPath; set { _thumbnailPath = value; OnPropertyChanged(); } }
    public string PagesText { get; set; }
    public string LastReadText { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }

    public class ReadingStatsViewModel
    {
        private readonly IReadingStatsService _statsService = ComicReader.Core.Services.ServiceLocator.TryGet<IReadingStatsService>();
        private readonly DispatcherTimer _refreshTimer;
        private const string ThumbCacheVersion = "v2";
    private readonly SemaphoreSlim _thumbSemaphore;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Task> _thumbTasks = new System.Collections.Concurrent.ConcurrentDictionary<string, Task>(StringComparer.OrdinalIgnoreCase);

        public ObservableCollection<StatItem> Stats { get; } = new ObservableCollection<StatItem>();

        public ObservableCollection<ProgressItem> ProgressList { get; } = new ObservableCollection<ProgressItem>();

        public class SessionItem
        {
            public string StartText { get; set; }
            public string Title { get; set; }
            public string DurationText { get; set; }
            public string PagesText { get; set; }
            public string HelpText { get; set; }
            public string ComicPath { get; set; }
        }

        public ObservableCollection<SessionItem> TodaySessions { get; } = new ObservableCollection<SessionItem>();

        // LiveCharts series
        public ISeries[] Series { get; private set; }
        public string[] Labels { get; private set; } = new[] { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };

    public ReadingStatsViewModel()
        {
            // initialize with placeholder labels
            EnsureDefaultStats();

            // attempt to load saved order
            TryApplySavedOrder();

            // start periodic refresh to make stats dynamic
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _refreshTimer.Tick += (s, e) => Refresh();
            _refreshTimer.Start();

            // configure thumbnail generation concurrency from settings
            int concurrency = 2;
            try { concurrency = Math.Max(1, ComicReader.Services.SettingsManager.Settings.ReadingStatsThumbConcurrency); } catch { }
            _thumbSemaphore = new SemaphoreSlim(concurrency);

            // initial load
            Refresh();

            // Persist order when collection changes (reorder via drag & drop)
            Stats.CollectionChanged += (s, e) => SaveOrder();
        }

        private void EnsureDefaultStats()
        {
            if (Stats.Count == 0)
            {
                Stats.Add(new StatItem { Label = "Cómics Leídos", Value = "0" });
                Stats.Add(new StatItem { Label = "Páginas Leídas", Value = "0" });
                Stats.Add(new StatItem { Label = "Tiempo Total", Value = "0m" });
                Stats.Add(new StatItem { Label = "Días Consecutivos", Value = "0" });
                Stats.Add(new StatItem { Label = "Esta Semana", Value = "0" });
                Stats.Add(new StatItem { Label = "Este Mes", Value = "0" });
                Stats.Add(new StatItem { Label = "Promedio/Sesión", Value = "0m" });
                Stats.Add(new StatItem { Label = "Sesión Más Larga", Value = "0m" });
            }
        }

        private void TryApplySavedOrder()
        {
            try
            {
                var order = ComicReader.Services.SettingsManager.Settings.ReadingStatsModuleOrder;
                if (order != null && order.Length > 0 && Stats.Count > 0)
                {
                    var map = new System.Collections.Generic.Dictionary<string, StatItem>(System.StringComparer.OrdinalIgnoreCase);
                    foreach (var s in Stats) map[s.Label] = s;
                    var reordered = new System.Collections.ObjectModel.ObservableCollection<StatItem>();
                    foreach (var key in order)
                    {
                        if (map.TryGetValue(key, out var item))
                        {
                            reordered.Add(item);
                            map.Remove(key);
                        }
                    }
                    foreach (var kv in map) reordered.Add(kv.Value);
                    Stats.Clear();
                    foreach (var it in reordered) Stats.Add(it);
                }
            }
            catch { }
        }

        public void Refresh()
        {
            try
            {
                if (_statsService == null)
                    return; // no service registered — keep placeholders

                var dash = _statsService.GetDashboard();
                if (dash != null)
                {
                    UpdateStat("Cómics Leídos", dash.TotalComicsRead.ToString());
                    UpdateStat("Páginas Leídas", dash.TotalPagesRead.ToString());
                    UpdateStat("Tiempo Total", FormatTimeSpan(dash.TotalReadingTime));
                    UpdateStat("Días Consecutivos", dash.CurrentStreak.ToString());
                    UpdateStat("Esta Semana", dash.ComicsThisWeek.ToString());
                    UpdateStat("Este Mes", dash.ComicsThisMonth.ToString());
                    UpdateStat("Promedio/Sesión", FormatTimeSpan(dash.AverageReadingTime));
                    UpdateStat("Sesión Más Larga", FormatTimeSpan(dash.LongestReadingSession));

                    // series remain a simple column series showing comics this week as sample
                    Series = new ISeries[] { new ColumnSeries<double> { Values = new double[] { dash.ComicsThisWeek, dash.ComicsThisMonth }, Name = "Lecturas" } };
                    Labels = new[] { "Semana", "Mes" };
                }

                // progress list: try to attach a cached thumbnail path if available
                ProgressList.Clear();
                foreach (var p in _statsService.GetRecentProgress(10))
                {
                    var percent = p.TotalPages > 0 ? (double)p.Progress * 100.0 / p.TotalPages : 0.0;
                    var thumb = TryGetCachedThumbPath(p.ComicPath);
                    var item = new ProgressItem { Title = p.Title, Percentage = percent, ThumbnailPath = thumb, PagesText = (p.TotalPages>0? $"{p.Progress} / {p.TotalPages}" : "-"), LastReadText = p.LastRead.ToString("g") };
                    ProgressList.Add(item);

                    // If no cached thumb, try to generate one in background (limited concurrency)
                    if (string.IsNullOrWhiteSpace(thumb) && !string.IsNullOrWhiteSpace(p.ComicPath) && (File.Exists(p.ComicPath) || Directory.Exists(p.ComicPath)))
                    {
                        if (!_thumbTasks.ContainsKey(p.ComicPath))
                        {
                            var t = GenerateAndSaveThumbAsync(p.ComicPath, item);
                            _thumbTasks.TryAdd(p.ComicPath, t);
                            // when complete, remove from dictionary
                            _ = t.ContinueWith(_ => { _thumbTasks.TryRemove(p.ComicPath, out _); });
                        }
                    }
                }

                // today sessions
                TodaySessions.Clear();
                foreach (var s in _statsService.GetTodaySessions())
                {
                    var help = $"Hora: {s.StartTime:HH:mm}; Duración: {((s.Duration.TotalMinutes>=60)? string.Format("{0}h {1}m", (int)s.Duration.TotalHours, s.Duration.Minutes) : string.Format("{0}m", (int)s.Duration.TotalMinutes))};" + (s.PagesRead>0? $" Páginas: {s.PagesRead};" : string.Empty);
                    TodaySessions.Add(new SessionItem
                    {
                        StartText = s.StartTime.ToString("HH:mm"),
                        Title = s.ComicTitle,
                        ComicPath = (s is ComicReader.Core.Abstractions.ReadingSessionInfo rsi) ? rsi.ComicPath : null,
                        DurationText = (s.Duration.TotalMinutes >= 60) ? string.Format("{0}h {1}m", (int)s.Duration.TotalHours, s.Duration.Minutes) : string.Format("{0}m", (int)s.Duration.TotalMinutes),
                        PagesText = s.PagesRead > 0 ? s.PagesRead + "p" : string.Empty,
                        HelpText = help
                    });
                }
            }
            catch { }
        }

        private static string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalHours >= 1) return string.Format("{0}h {1}m", (int)ts.TotalHours, ts.Minutes);
            return string.Format("{0}m", (int)ts.TotalMinutes);
        }

        private void UpdateStat(string label, string value)
        {
            var item = System.Linq.Enumerable.FirstOrDefault(Stats, s => string.Equals(s.Label, label, StringComparison.OrdinalIgnoreCase));
            if (item != null) item.Value = value;
        }

        private void SaveOrder()
        {
            try
            {
                var labels = new System.Collections.Generic.List<string>();
                foreach (var it in Stats) labels.Add(it.Label);
                ComicReader.Services.SettingsManager.Settings.ReadingStatsModuleOrder = labels.ToArray();
                ComicReader.Services.SettingsManager.SaveSettings();
            }
            catch { }
        }

        // Try to compute the same thumbnail cache path used across the app (HomeView)
        private string TryGetCachedThumbPath(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath)) return null;
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var dir = System.IO.Path.Combine(appData, "PercysLibrary", "Thumbs");
                System.IO.Directory.CreateDirectory(dir);
                using (var sha1 = System.Security.Cryptography.SHA1.Create())
                {
                    var key = ThumbCacheVersion + "|" + filePath;
                    var hash = BitConverter.ToString(sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key))).Replace("-", string.Empty);
                    var p = System.IO.Path.Combine(dir, hash + ".png");
                    return System.IO.File.Exists(p) ? p : null;
                }
            }
            catch { return null; }
        }

        private string ComputeThumbCachePath(string filePath)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = System.IO.Path.Combine(appData, "PercysLibrary", "Thumbs");
            System.IO.Directory.CreateDirectory(dir);
            using (var sha1 = System.Security.Cryptography.SHA1.Create())
            {
                var key = ThumbCacheVersion + "|" + filePath;
                var hash = BitConverter.ToString(sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key))).Replace("-", string.Empty);
                return System.IO.Path.Combine(dir, hash + ".png");
            }
        }

        private async Task GenerateAndSaveThumbAsync(string filePath, ProgressItem item)
        {
            await _thumbSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                BitmapSource cover = null;
                try
                {
                    using (var loader = new ComicPageLoader(filePath))
                    {
                        await loader.LoadComicAsync().ConfigureAwait(false);
                        cover = await loader.GetCoverThumbnailAsync(300, 400).ConfigureAwait(false);
                    }
                }
                catch { }

                if (cover != null)
                {
                    // Save to cache
                    try
                    {
                        var path = ComputeThumbCachePath(filePath);
                        // encode as PNG
                        using (var fs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            var enc = new PngBitmapEncoder();
                            enc.Frames.Add(BitmapFrame.Create(cover));
                            enc.Save(fs);
                        }

                        // update the item on UI thread
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            try { item.ThumbnailPath = path; } catch { }
                        });
                    }
                    catch { }
                }
            }
            finally
            {
                _thumbSemaphore.Release();
            }
        }
    }
}
