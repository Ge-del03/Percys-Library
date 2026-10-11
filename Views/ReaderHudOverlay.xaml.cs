using System.Windows;
using System.Windows.Controls;
using ComicReader.Services;

namespace ComicReader.Views
{
    public partial class ReaderHudOverlay : UserControl
    {
        public ReaderHudOverlay()
        {
            InitializeComponent();
        }

        private void BackToLibrary_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Instance.NavigateTo(AppViewType.Dashboard);
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow main)
            {
                main.PrevPage_Click(sender, e);
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow main)
            {
                main.NextPage_Click(sender, e);
            }
        }

        private void PageHudSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Paginación vinculada si aplica
        }
    }
}