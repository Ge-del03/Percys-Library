using System.Windows.Controls;
using System.Windows.Input;
using ComicReader.Models;
using ComicReader.ViewModels;

namespace ComicReader.Views
{
    public partial class LibraryCatalogView : UserControl
    {
        public CatalogViewModel ViewModel { get; }

        public LibraryCatalogView()
        {
            InitializeComponent();
            ViewModel = new CatalogViewModel();
            DataContext = ViewModel;
        }

        private void ComicCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement elem && elem.DataContext is ComicItem comic)
            {
                MainViewModel.Instance.SelectedComic = comic;
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView lv && lv.SelectedItem is ComicItem comic)
            {
                MainViewModel.Instance.SelectedComic = comic;
            }
        }
    }
}