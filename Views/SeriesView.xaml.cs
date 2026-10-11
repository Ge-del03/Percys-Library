using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using ComicReader.Models;
using ComicReader.ViewModels;

namespace ComicReader.Views
{
    public partial class SeriesView : UserControl
    {
        public SeriesView()
        {
            InitializeComponent();
        }

        public void LoadSeriesData(IEnumerable<ComicItem> comics)
        {
            if (comics == null) return;

            var grouped = comics
                .Where(c => !string.IsNullOrEmpty(c.Series))
                .GroupBy(c => c.Series)
                .Select(g => new { Key = g.Key, Count = g.Count(), Items = g.ToList() })
                .OrderBy(g => g.Key)
                .ToList();

            SeriesListBox.ItemsSource = grouped;
            if (grouped.Any())
            {
                SeriesListBox.SelectedIndex = 0;
            }
        }

        private void SeriesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SeriesListBox.SelectedItem != null)
            {
                dynamic selected = SeriesListBox.SelectedItem;
                SeriesTitleText.Text = selected.Key;
                SeriesComicsItemsControl.ItemsSource = selected.Items;
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