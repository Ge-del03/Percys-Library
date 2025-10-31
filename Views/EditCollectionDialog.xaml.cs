using System.Windows;
using Microsoft.Win32;

namespace ComicReader.Views
{
    public partial class EditCollectionDialog : Window
    {
        public string CollectionName { get => NameBox.Text.Trim(); set => NameBox.Text = value; }
        public string Description { get => DescBox.Text.Trim(); set => DescBox.Text = value; }
    public string CoverPath { get; set; }

        public EditCollectionDialog()
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

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CollectionName))
            {
                MessageBox.Show(this, "El nombre no puede estar vacío.", "Nombre requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
            Close();
        }
    }
}
