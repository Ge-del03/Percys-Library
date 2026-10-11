using System.Windows;
using System.Windows.Controls;
using ComicReader.Models;
using ComicReader.Services;
using ComicReader.ViewModels;

namespace ComicReader.Views
{
    public partial class MetadataInspectorView : UserControl
    {
        public MetadataInspectorView()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.IsInspectorOpen = false;
        }

        private void ReadNowButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ComicItem comic && !string.IsNullOrEmpty(comic.FilePath))
            {
                NavigationService.Instance.NavigateTo(AppViewType.Reader, comic.FilePath);
            }
        }

        private void SaveMetadataButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ComicItem comic)
            {
                MessageBox.Show($"Metadatos actualizados para {comic.Title}.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}