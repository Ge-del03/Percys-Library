using System.Windows;
using Microsoft.Win32;

namespace ComicReader.Views
{
    public partial class NewCollectionDialog : Window
    {
        public string CollectionName => NameBox.Text.Trim();
        public string Description => DescBox.Text.Trim();
        public string CoverPath { get; private set; }

        public NewCollectionDialog()
        {
            InitializeComponent();
        }

        private void SelectCover_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.bmp|Todos los archivos|*.*";
            if (dlg.ShowDialog(this) == true)
            {
                CoverPath = dlg.FileName;
                CoverPathText.Text = CoverPath;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CollectionName))
            {
                MessageBox.Show(this, "Debes indicar un nombre para la colección.", "Nombre requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}
