using System.Windows;
using Microsoft.Win32;
using ComicReader.ViewModels;
using ComicReader.Core.Abstractions;

namespace ComicReader.Views
{
    public partial class CollectionsWindow : Window
    {
        private CollectionsViewModel Vm => DataContext as CollectionsViewModel;

        public CollectionsWindow()
        {
            InitializeComponent();
            // DataContext is set in XAML
        }

        private void NewCollection_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new NewCollectionDialog() { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                var req = new CollectionCreateRequest { Name = dlg.CollectionName, Description = dlg.Description, CoverPath = dlg.CoverPath };
                Vm?.CreateFromRequest(req);
            }
        }

        private void ImportCollections_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "JSON Collections|*.json|All Files|*.*";
            if (dlg.ShowDialog(this) == true)
            {
                Vm?.ImportFromFile(dlg.FileName);
                MessageBox.Show(this, "Importación completada.", "Importar", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportCollections_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog();
            dlg.Filter = "JSON Collections|*.json|All Files|*.*";
            dlg.FileName = "collections-export.json";
            if (dlg.ShowDialog(this) == true)
            {
                Vm?.ExportToFile(dlg.FileName);
                MessageBox.Show(this, "Exportación guardada.", "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
