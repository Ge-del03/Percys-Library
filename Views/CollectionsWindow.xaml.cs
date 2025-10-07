using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ComicReader.Models;
using Microsoft.Win32;

namespace ComicReader.Views
{
    public partial class CollectionsWindow : Window
    {
        private readonly CollectionManager _collectionManager;
        private ComicCollectionV2? _selectedCollection;

        public CollectionsWindow()
        {
            InitializeComponent();
            _collectionManager = new CollectionManager();
            LoadCollections();
        }

        private void LoadCollections()
        {
            var collections = _collectionManager.GetAllCollections();
            CollectionsList.ItemsSource = collections;
            
            TotalCollectionsText.Text = collections.Count.ToString();
            TotalComicsText.Text = collections.Sum(c => c.ComicCount).ToString();
        }

        private void LoadCollectionDetails(ComicCollectionV2 collection)
        {
            _selectedCollection = collection;
            
            CollectionNameText.Text = collection.Name;
            CollectionDescriptionText.Text = string.IsNullOrEmpty(collection.Description) 
                ? "Sin descripción" 
                : collection.Description;
            
            // Cargar cómics de la colección
            var comics = collection.ComicPaths.Select(path => new
            {
                Path = path,
                Name = System.IO.Path.GetFileNameWithoutExtension(path),
                CoverImage = LoadComicCover(path)
            }).ToList();
            
            ComicGrid.ItemsSource = comics;
            
            CollectionHeader.Visibility = Visibility.Visible;
            NoSelectionMessage.Visibility = Visibility.Collapsed;
        }

        private object? LoadComicCover(string path)
        {
            try
            {
                if (!System.IO.File.Exists(path))
                    return null;

                // Usar ComicFileLoader para obtener la primera página como portada
                var pages = Services.ComicFileLoader.LoadComic(path);
                if (pages != null && pages.Count > 0)
                {
                    return pages[0];
                }
            }
            catch (Exception ex)
            {
                Services.Logger.Log($"Error cargando portada de {path}: {ex.Message}");
            }
            return null;
        }

        private void CollectionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CollectionsList.SelectedItem is ComicCollectionV2 collection)
            {
                LoadCollectionDetails(collection);
            }
        }

        private void NewCollection_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CollectionEditorDialog();
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.CollectionName))
            {
                _collectionManager.CreateCollection(dialog.CollectionName, dialog.CollectionDescription);
                LoadCollections();
            }
        }

        private void EditCollection_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCollection != null)
            {
                var dialog = new CollectionEditorDialog(_selectedCollection);
                if (dialog.ShowDialog() == true)
                {
                    _selectedCollection.Name = dialog.CollectionName;
                    _selectedCollection.Description = dialog.CollectionDescription;
                    _collectionManager.UpdateCollection(_selectedCollection);
                    LoadCollections();
                    LoadCollectionDetails(_selectedCollection);
                }
            }
        }

        private void DeleteCollection_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCollection != null)
            {
                var result = MessageBox.Show(
                    $"¿Estás seguro de eliminar la colección '{_selectedCollection.Name}'?",
                    "Confirmar eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    _collectionManager.DeleteCollection(_selectedCollection.Id);
                    LoadCollections();
                    CollectionHeader.Visibility = Visibility.Collapsed;
                    NoSelectionMessage.Visibility = Visibility.Visible;
                }
            }
        }

        private void AddComics_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCollection == null) return;

            var dialog = new OpenFileDialog
            {
                Filter = "Archivos de Cómic|*.cbz;*.cbr;*.cb7;*.cbt;*.zip;*.rar;*.7z;*.pdf;*.epub|Todos los archivos|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    _collectionManager.AddComicToCollection(_selectedCollection.Id, file);
                }
                LoadCollections();
                LoadCollectionDetails(_selectedCollection);
            }
        }

        private void ReadFirst_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCollection?.ComicPaths.Any() == true)
            {
                // TODO: Abrir el primer cómic de la colección
                MessageBox.Show($"Abriendo: {_selectedCollection.ComicPaths.First()}", "Información");
            }
        }

        private void RemoveComic_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string comicPath && _selectedCollection != null)
            {
                _collectionManager.RemoveComicFromCollection(_selectedCollection.Id, comicPath);
                LoadCollections();
                LoadCollectionDetails(_selectedCollection);
            }
        }

        private void Comic_Click(object sender, MouseButtonEventArgs e)
        {
            // TODO: Abrir cómic seleccionado
        }
    }

    // Diálogo simple para crear/editar colecciones
    public class CollectionEditorDialog : Window
    {
        private TextBox _nameTextBox;
        private TextBox _descriptionTextBox;

        public string CollectionName => _nameTextBox?.Text ?? string.Empty;
        public string CollectionDescription => _descriptionTextBox?.Text ?? string.Empty;

        public CollectionEditorDialog(ComicCollectionV2? collection = null)
        {
            Title = collection == null ? "Nueva Colección" : "Editar Colección";
            Width = 550;
            Height = 350;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
            
            // Aplicar el ícono de la aplicación si existe
            try
            {
                var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icono.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri(iconPath));
                }
            }
            catch { }

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var nameLabel = new TextBlock { Text = "Nombre:", Foreground = System.Windows.Media.Brushes.White, Margin = new Thickness(0, 0, 0, 5) };
            Grid.SetRow(nameLabel, 0);
            grid.Children.Add(nameLabel);

            _nameTextBox = new TextBox 
            { 
                Text = collection?.Name ?? string.Empty, 
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(8, 5, 8, 5),
                FontSize = 14,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60))
            };
            Grid.SetRow(_nameTextBox, 1);
            grid.Children.Add(_nameTextBox);

            var descLabel = new TextBlock 
            { 
                Text = "Descripción:", 
                Foreground = System.Windows.Media.Brushes.White, 
                Margin = new Thickness(0, 0, 0, 5),
                FontSize = 13
            };
            Grid.SetRow(descLabel, 2);
            grid.Children.Add(descLabel);

            _descriptionTextBox = new TextBox 
            { 
                Text = collection?.Description ?? string.Empty, 
                AcceptsReturn = true, 
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(8, 5, 8, 5),
                FontSize = 13,
                MinHeight = 100,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60))
            };
            Grid.SetRow(_descriptionTextBox, 3);
            grid.Children.Add(_descriptionTextBox);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetRow(buttonPanel, 4);

            var okButton = new Button 
            { 
                Content = "✅ Aceptar", 
                Width = 120, 
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                FontSize = 13,
                Cursor = Cursors.Hand,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };
            okButton.Click += (s, e) => 
            { 
                if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
                {
                    MessageBox.Show("El nombre de la colección no puede estar vacío.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                DialogResult = true; 
                Close(); 
            };
            buttonPanel.Children.Add(okButton);

            var cancelButton = new Button 
            { 
                Content = "❌ Cancelar", 
                Width = 120,
                Height = 35,
                FontSize = 13,
                Cursor = Cursors.Hand,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };
            buttonPanel.Children.Add(cancelButton);

            grid.Children.Add(buttonPanel);
            Content = grid;
        }
    }
}
