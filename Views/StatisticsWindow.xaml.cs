using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ComicReader.Services;

namespace ComicReader.Views
{
    public partial class StatisticsWindow : Window
    {
        private readonly ReadingStatsService _statsService;
        private readonly Models.AchievementManager _achievementManager;
        private readonly Models.CollectionManager _collectionManager;

        public StatisticsWindow()
        {
            InitializeComponent();
            
            _statsService = new ReadingStatsService();
            _achievementManager = new Models.AchievementManager();
            _collectionManager = new Models.CollectionManager();

            LoadStatistics();
        }

        private void LoadStatistics()
        {
            try
            {
                // Cargar estadísticas generales
                LoadGeneralStats();
                LoadTopComics();
                LoadHistory();
                LoadActivityData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar estadísticas: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadGeneralStats()
        {
            // Total de cómics leídos
            var totalComics = _statsService.GetTotalComicsRead();
            TotalComicsText.Text = totalComics.ToString();

            // Total de páginas leídas
            var totalPages = _statsService.GetTotalPagesRead();
            TotalPagesText.Text = totalPages.ToString("N0");

            // Tiempo total de lectura
            var totalMinutes = _statsService.GetTotalReadingTime();
            var hours = totalMinutes / 60;
            TotalTimeText.Text = hours > 0 ? $"{hours}h" : $"{totalMinutes}m";

            // Racha actual
            var streak = _statsService.GetCurrentStreak();
            StreakText.Text = streak.ToString();

            // Promedio diario
            var dailyAvg = _statsService.GetDailyAverage();
            DailyAvgText.Text = dailyAvg.ToString("F1");

            // Favoritos
            FavoritesText.Text = "0"; // Implementar GetFavoritesCount()

            // Logros
            var achievements = _achievementManager.GetAllAchievements();
            var unlockedCount = achievements.Count(a => a.IsUnlocked);
            AchievementsText.Text = $"{unlockedCount}/{achievements.Count}";

            // Colecciones
            var collections = _collectionManager.GetAllCollections();
            CollectionsText.Text = collections.Count.ToString();

            // Resumen
            SummaryText.Text = $"Has leído {totalComics} cómics con un total de {totalPages:N0} páginas";
        }

        private void LoadTopComics()
        {
            var topComics = _statsService.GetMostReadComics(10);
            var rank = 1;

            var displayList = topComics.Select(comic => new
            {
                Rank = rank++,
                ComicName = System.IO.Path.GetFileNameWithoutExtension(comic.FilePath),
                ReadCount = comic.ReadCount,
                LastRead = comic.LastReadDate
            }).ToList();

            TopComicsGrid.ItemsSource = displayList;
        }

        private void LoadHistory()
        {
            var recentComics = _statsService.GetRecentComics(50);
            
            var historyList = recentComics.Select(comic => new
            {
                Date = comic.LastReadDate,
                ComicName = System.IO.Path.GetFileNameWithoutExtension(comic.FilePath),
                PageNumber = comic.CurrentPage,
                Duration = FormatDuration(comic.TotalReadingTime)
            }).ToList();

            HistoryGrid.ItemsSource = historyList;
        }

        private void LoadActivityData()
        {
            // Actividad semanal
            var weeklyData = _statsService.GetWeeklyActivity();
            var weeklyList = new List<string>();
            
            foreach (var day in weeklyData)
            {
                var bar = new string('█', Math.Min(day.Value / 5, 20));
                weeklyList.Add($"{day.Key}: {bar} ({day.Value} páginas)");
            }
            
            WeeklyActivityList.ItemsSource = weeklyList;

            // Formatos preferidos
            var formats = _statsService.GetFormatDistribution();
            var formatsList = formats.Select(f => $"{f.Key}: {f.Value} cómics ({f.Value * 100.0 / _statsService.GetTotalComicsRead():F1}%)").ToList();
            FormatsList.ItemsSource = formatsList;
        }

        private string FormatDuration(int minutes)
        {
            if (minutes < 60)
                return $"{minutes}m";
            
            var hours = minutes / 60;
            var mins = minutes % 60;
            return mins > 0 ? $"{hours}h {mins}m" : $"{hours}h";
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadStatistics();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                DefaultExt = ".csv",
                FileName = $"estadisticas_{DateTime.Now:yyyyMMdd}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Exportar estadísticas a CSV
                    var csv = new System.Text.StringBuilder();
                    csv.AppendLine("Tipo,Valor");
                    csv.AppendLine($"Total Cómics,{TotalComicsText.Text}");
                    csv.AppendLine($"Total Páginas,{TotalPagesText.Text}");
                    csv.AppendLine($"Tiempo Total,{TotalTimeText.Text}");
                    csv.AppendLine($"Racha Actual,{StreakText.Text}");

                    System.IO.File.WriteAllText(dialog.FileName, csv.ToString());
                    
                    MessageBox.Show($"Estadísticas exportadas correctamente a:\n{dialog.FileName}",
                        "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void FilterHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var fromDate = HistoryFromDate.SelectedDate;
            var toDate = HistoryToDate.SelectedDate;

            if (fromDate.HasValue && toDate.HasValue)
            {
                var recentComics = _statsService.GetRecentComics(1000);
                var filtered = recentComics.Where(c => 
                    c.LastReadDate >= fromDate.Value && 
                    c.LastReadDate <= toDate.Value);

                var historyList = filtered.Select(comic => new
                {
                    Date = comic.LastReadDate,
                    ComicName = System.IO.Path.GetFileNameWithoutExtension(comic.FilePath),
                    PageNumber = comic.CurrentPage,
                    Duration = FormatDuration(comic.TotalReadingTime)
                }).ToList();

                HistoryGrid.ItemsSource = historyList;
            }
        }
    }
}
