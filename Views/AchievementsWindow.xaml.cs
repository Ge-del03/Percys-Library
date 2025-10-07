using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ComicReader.Models;

namespace ComicReader.Views
{
    public partial class AchievementsWindow : Window
    {
        private readonly AchievementManager _achievementManager;
        private string _currentFilter = "All";
        private string _currentCategory = "";

        public AchievementsWindow()
        {
            InitializeComponent();
            _achievementManager = new AchievementManager();
            LoadAchievements();
            UpdateStatistics();
        }

        private void LoadAchievements()
        {
            var achievements = _achievementManager.GetAllAchievements();

            // Aplicar filtros
            if (_currentFilter == "Unlocked")
            {
                achievements = achievements.Where(a => a.IsUnlocked).ToList();
            }
            else if (_currentFilter == "Locked")
            {
                achievements = achievements.Where(a => !a.IsUnlocked).ToList();
            }

            if (!string.IsNullOrEmpty(_currentCategory))
            {
                achievements = achievements.Where(a => a.Category.ToString() == _currentCategory).ToList();
            }

            // Ordenar: desbloqueados primero, luego por progreso
            achievements = achievements
                .OrderByDescending(a => a.IsUnlocked)
                .ThenByDescending(a => a.Progress)
                .ToList();

            AchievementsList.ItemsSource = achievements;
        }

        private void UpdateStatistics()
        {
            var allAchievements = _achievementManager.GetAllAchievements();
            var unlockedCount = _achievementManager.GetUnlockedAchievements().Count;
            var totalPoints = _achievementManager.GetTotalPoints();
            var completion = _achievementManager.GetCompletionPercentage();

            TotalPointsText.Text = totalPoints.ToString();
            UnlockedCountText.Text = $"{unlockedCount}/{allAchievements.Count}";
            CompletionText.Text = $"{completion:F1}%";
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filter)
            {
                _currentFilter = filter;
                LoadAchievements();
            }
        }

        private void CategoryFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryFilter.SelectedItem is ComboBoxItem item && item.Tag is string category)
            {
                _currentCategory = category;
                LoadAchievements();
            }
        }
    }
}
