using System.Windows.Controls;
using System.Windows.Input;
using ComicReader.Models;
using ComicReader.Services;
using ComicReader.ViewModels;

namespace ComicReader.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            this.Loaded += DashboardView_Loaded;
        }

        private void DashboardView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            RefreshData();
        }

        public void RefreshData()
        {
            var continueItems = ContinueReadingService.Instance.Items;
            InCourseItemsControl.ItemsSource = continueItems;

            if (MainViewModel.Instance != null && MainViewModel.Instance.SelectedComic != null)
            {
                RecentItemsControl.ItemsSource = new[] { MainViewModel.Instance.SelectedComic };
            }

            var hero = System.Linq.Enumerable.FirstOrDefault(continueItems);
            if (hero != null)
            {
                HeroSection.Visibility = System.Windows.Visibility.Visible;
                HeroTitleText.Text = hero.DisplayName;
                HeroSubtitleText.Text = $"Página {hero.LastPage} de {hero.PageCount}";
                HeroProgressBar.Value = hero.ProgressPercent;
                HeroProgressText.Text = $"Progreso: {System.Math.Round(hero.ProgressPercent)}%";
                HeroCoverImage.Source = hero.CoverThumbnail;
            }
            else
            {
                HeroSection.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void ContinueReadingHero_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var hero = System.Linq.Enumerable.FirstOrDefault(ContinueReadingService.Instance.Items);
            if (hero != null)
            {
                NavigationService.Instance.NavigateTo(AppViewType.Reader, hero.FilePath);
            }
        }

        private void SeeAllInCourse_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            NavigationService.Instance.NavigateTo(AppViewType.Catalog);
        }

        private void ExploreCatalog_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            NavigationService.Instance.NavigateTo(AppViewType.Catalog);
        }

        private void Card_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement elem && elem.DataContext is ContinueItem item)
            {
                NavigationService.Instance.NavigateTo(AppViewType.Reader, item.FilePath);
            }
        }

        private void ComicCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement elem && elem.DataContext is ComicItem comic)
            {
                MainViewModel.Instance.SelectedComic = comic;
            }
        }
    }
}