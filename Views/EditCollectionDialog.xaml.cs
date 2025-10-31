using System.Windows;
using Microsoft.Win32;
using System.Linq;
using System.Collections.Generic;

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

        public System.Collections.Generic.List<Core.Abstractions.ComicItemDto> Items
        {
            get => ItemsList.Items.Cast<Core.Abstractions.ComicItemDto>().ToList();
            set
            {
                ItemsList.Items.Clear();
                if (value == null) return;
                foreach (var it in value) ItemsList.Items.Add(it);
            }
        }

        private void AddComics_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Cómics|*.cbz;*.cbr;*.zip;*.rar|Todos los archivos|*.*";
            dlg.Multiselect = true;
            if (dlg.ShowDialog(this) == true)
            {
                foreach (var f in dlg.FileNames)
                {
                    var item = new Core.Abstractions.ComicItemDto { Path = f, Title = System.IO.Path.GetFileNameWithoutExtension(f), ThumbPath = string.Empty };
                    ItemsList.Items.Add(item);
                }
            }
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            var sel = ItemsList.SelectedItems.Cast<object>().ToList();
            foreach (var s in sel) ItemsList.Items.Remove(s);
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
