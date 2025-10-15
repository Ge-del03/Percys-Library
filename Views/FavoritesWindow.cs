using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ComicReader.Models;
using ComicReader.Services;

namespace ComicReader.Views
{
    public partial class FavoritesWindow : Window
    {
        public ObservableCollection<ComicCollection> Collections { get; set; }
        public ObservableCollection<FavoriteComic> CurrentCollectionItems { get; set; }
        public ObservableCollection<FavoriteComic> FilteredItems { get; set; }
        private ComicCollection _selectedCollection;

        public FavoritesWindow()
        {
            #pragma warning disable
            InitializeComponent();
            #pragma warning restore
            Collections = FavoritesStorage.Load();
            CurrentCollectionItems = new ObservableCollection<FavoriteComic>();
            FilteredItems = new ObservableCollection<FavoriteComic>();

            GetCollectionsListBox().ItemsSource = Collections;
            GetFavoritesListBox().ItemsSource = FilteredItems;

            // Permitir arrastrar y soltar archivos de cómic directamente a la lista
            var favList = GetFavoritesListBox();
            favList.AllowDrop = true;
            favList.DragOver += FavoritesListBox_DragOver;
            favList.Drop += FavoritesListBox_Drop;
        }

        // Guardar automáticamente cuando se cierre la ventana
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            FavoritesStorage.Save(Collections);
        }

        private void CollectionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var list = GetCollectionsListBox();
            if (list.SelectedItem is ComicCollection collection)
            {
                _selectedCollection = collection;
                SetCollectionTitle($"{collection.Name} ({collection.ItemCount} cómics)");
                
                CurrentCollectionItems.Clear();
                foreach (var item in collection.Items)
                {
                    CurrentCollectionItems.Add(item);
                }
                
                ApplyFilter();
            }
            else
            {
                _selectedCollection = null;
                SetCollectionTitle("Selecciona una colección");
                CurrentCollectionItems.Clear();
                ApplyFilter();
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            FilteredItems.Clear();
            var searchText = GetSearchTextBox().Text?.ToLower() ?? "";

            var filteredComics = CurrentCollectionItems.Where(c =>
            {
                if (string.IsNullOrEmpty(searchText)) return true;
                var title = c.Title ?? string.Empty;
                var author = c.Author ?? string.Empty;
                var tags = c.Tags ?? Array.Empty<string>();
                return title.ToLower().Contains(searchText)
                       || author.ToLower().Contains(searchText)
                       || tags.Any(tag => (tag ?? string.Empty).ToLower().Contains(searchText));
            });

            foreach (var comic in filteredComics)
            {
                FilteredItems.Add(comic);
            }
        }

        private void CreateCollection_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CreateCollectionDialog();
            dialog.Owner = this;
            
            if (dialog.ShowDialog() == true)
            {
                var newCollection = new ComicCollection
                {
                    Id = Guid.NewGuid(),
                    Name = dialog.CollectionName,
                    Description = dialog.CollectionDescription,
                    DateCreated = DateTime.Now,
                    Color = dialog.SelectedColor
                };
                
                Collections.Add(newCollection);
                GetCollectionsListBox().SelectedItem = newCollection;
                FavoritesStorage.Save(Collections);
            }
        }

        private void AddToCollection_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCollection == null)
            {
                MessageBox.Show("Primero selecciona una colección.", "Información", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var openDialog = new OpenFileDialog
            {
                Title = "Agregar Cómic a Colección",
                Filter = "Archivos de Comic|*.cbz;*.cbr;*.cb7;*.cbt;*.zip;*.rar;*.7z;*.tar;*.pdf;*.epub;*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp;*.heic;*.tif;*.tiff;*.avif|Todos los archivos|*.*",
                Multiselect = true
            };

            if (openDialog.ShowDialog() == true)
            {
                foreach (var filename in openDialog.FileNames)
                {
                    var comic = new FavoriteComic
                    {
                        Id = Guid.NewGuid(),
                        Title = Path.GetFileNameWithoutExtension(filename),
                        FilePath = filename,
                        DateAdded = DateTime.Now,
                        Tags = new[] { "Sin categorizar" }
                    };

                    _selectedCollection.Items.Add(comic);
                    CurrentCollectionItems.Add(comic);
                }
                
                ApplyFilter();
                FavoritesStorage.Save(Collections);
                MessageBox.Show($"Se agregaron {openDialog.FileNames.Length} cómics a la colección.", 
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddFolderToCollection_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCollection == null)
            {
                MessageBox.Show("Primero selecciona una colección.", "Información",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                dlg.Description = "Seleccionar carpeta con cómics";
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var exts = new[] { ".cbz", ".cbr", ".cb7", ".cbt", ".zip", ".rar", ".7z", ".tar", ".pdf", ".epub",
                                        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".heic", ".tif", ".tiff", ".avif" };
                    var files = Directory.GetFiles(dlg.SelectedPath, "*", SearchOption.AllDirectories)
                                          .Where(f => exts.Contains(Path.GetExtension(f).ToLowerInvariant()))
                                          .Take(300) // límite razonable para evitar bloqueos
                                          .ToList();
                    foreach (var filename in files)
                    {
                        var comic = new FavoriteComic
                        {
                            Id = Guid.NewGuid(),
                            Title = Path.GetFileNameWithoutExtension(filename),
                            FilePath = filename,
                            DateAdded = DateTime.Now,
                            Tags = new[] { "Sin categorizar" }
                        };
                        _selectedCollection.Items.Add(comic);
                        CurrentCollectionItems.Add(comic);
                    }
                    ApplyFilter();
                    FavoritesStorage.Save(Collections);
                    MessageBox.Show($"Se agregaron {files.Count} elementos.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void OpenFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is FavoriteComic comic)
            {
                if (File.Exists(comic.FilePath))
                {
                    // Intentar abrir el cómic en la ventana principal
                    if (Owner is global::ComicReader.MainWindow mainWindow)
                    {
                        var method = mainWindow.GetType().GetMethod("OpenComicFile", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        method?.Invoke(mainWindow, new object[] { comic.FilePath });
                        mainWindow.ShowComicView();
                        Close();
                    }
                }
                else
                {
                    MessageBox.Show("El archivo no existe o ha sido movido.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void RemoveFromCollection_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is FavoriteComic comic)
            {
                var result = MessageBox.Show(
                    $"¿Quitar '{comic.Title}' de la colección?",
                    "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _selectedCollection?.Items.Remove(comic);
                    CurrentCollectionItems.Remove(comic);
                    FilteredItems.Remove(comic);
                    SetCollectionTitle(_selectedCollection != null 
                        ? $"{_selectedCollection.Name} ({_selectedCollection.ItemCount} cómics)" 
                        : "Selecciona una colección");
                    FavoritesStorage.Save(Collections);
                }
            }
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCollection == null) return;
            var list = GetFavoritesListBox();
            var selected = list.SelectedItems.Cast<FavoriteComic>().ToList();
            if (selected.Count == 0) return;
            if (MessageBox.Show($"¿Quitar {selected.Count} elemento(s) de la colección?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                foreach (var item in selected)
                {
                    _selectedCollection.Items.Remove(item);
                    CurrentCollectionItems.Remove(item);
                    FilteredItems.Remove(item);
                }
                SetCollectionTitle(_selectedCollection != null
                    ? $"{_selectedCollection.Name} ({_selectedCollection.ItemCount} cómics)"
                    : "Selecciona una colección");
                FavoritesStorage.Save(Collections);
            }
        }

        // Punto de extensión futuro: edición de metadatos del cómic

        private void ExportCollections_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Title = "Exportar Colecciones",
                Filter = "Archivo JSON|*.json",
                DefaultExt = "json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(Collections, new System.Text.Json.JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    File.WriteAllText(saveDialog.FileName, json);
                    FavoritesStorage.Save(Collections);
                    MessageBox.Show("Colecciones exportadas exitosamente.", "Éxito", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ImportCollections_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Title = "Importar Colecciones",
                Filter = "Archivo JSON|*.json"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(openDialog.FileName);
                    var importedCollections = System.Text.Json.JsonSerializer.Deserialize<ComicCollection[]>(json);
                    
                    foreach (var collection in importedCollections)
                    {
                        Collections.Add(collection);
                    }
                    
                    MessageBox.Show($"Se importaron {importedCollections.Length} colecciones.", 
                        "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al importar: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DeleteCollection_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCollection == null)
            {
                MessageBox.Show("Primero selecciona una colección.", "Información", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"¿Eliminar la colección '{_selectedCollection.Name}'?\nEsta acción no elimina los archivos físicos.",
                "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                var idx = Collections.IndexOf(_selectedCollection);
                Collections.Remove(_selectedCollection);
                _selectedCollection = null;
                CurrentCollectionItems.Clear();
                FilteredItems.Clear();
                SetCollectionTitle("Selecciona una colección");
                FavoritesStorage.Save(Collections);
                if (Collections.Count > 0)
                {
                    GetCollectionsListBox().SelectedIndex = Math.Min(idx, Collections.Count - 1);
                }
            }
        }

        private void RenameCollection_Click(object sender, RoutedEventArgs e)
        {
            var list = GetCollectionsListBox();
            var col = (ComicCollection) (list.SelectedItem ?? _selectedCollection);
            if (col == null)
            {
                MessageBox.Show("Selecciona una colección.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var input = Microsoft.VisualBasic.Interaction.InputBox("Nuevo nombre de la colección:", "Renombrar", col.Name ?? "");
            if (!string.IsNullOrWhiteSpace(input))
            {
                col.Name = input.Trim();
                SetCollectionTitle($"{col.Name} ({col.ItemCount} cómics)");
                FavoritesStorage.Save(Collections);
            }
        }

        private void DuplicateCollection_Click(object sender, RoutedEventArgs e)
        {
            var col = _selectedCollection ?? (GetCollectionsListBox().SelectedItem as ComicCollection);
            if (col == null)
            {
                MessageBox.Show("Selecciona una colección.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var copy = new ComicCollection
            {
                Id = Guid.NewGuid(),
                Name = col.Name + " (Copia)",
                Description = col.Description,
                Color = col.Color,
                DateCreated = DateTime.Now,
                Items = new ObservableCollection<FavoriteComic>(col.Items.Select(i => new FavoriteComic
                {
                    Id = Guid.NewGuid(),
                    Title = i.Title,
                    Author = i.Author,
                    FilePath = i.FilePath,
                    DateAdded = DateTime.Now,
                    Rating = i.Rating,
                    Tags = i.Tags?.ToArray() ?? new string[0],
                    Notes = i.Notes
                }))
            };
            Collections.Add(copy);
            FavoritesStorage.Save(Collections);
            GetCollectionsListBox().SelectedItem = copy;
        }

        private void FavoritesListBox_DragOver(object sender, DragEventArgs e)
        {
            if (_selectedCollection == null) { e.Effects = DragDropEffects.None; return; }
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Copy; else e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void FavoritesListBox_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (_selectedCollection == null) return;
                if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
                var files = ((string[])e.Data.GetData(DataFormats.FileDrop)) ?? Array.Empty<string>();
                var exts = new[] { ".cbz", ".cbr", ".cb7", ".cbt", ".zip", ".rar", ".7z", ".tar", ".pdf", ".epub",
                                    ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".heic", ".tif", ".tiff", ".avif" };
                foreach (var path in files.SelectMany(p => Directory.Exists(p)
                                                    ? Directory.GetFiles(p, "*", SearchOption.AllDirectories)
                                                    : new[] { p }))
                {
                    var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
                    if (!exts.Contains(ext)) continue;
                    var item = new FavoriteComic
                    {
                        Id = Guid.NewGuid(),
                        Title = System.IO.Path.GetFileNameWithoutExtension(path),
                        FilePath = path,
                        DateAdded = DateTime.Now,
                        Tags = new[] { "Sin categorizar" }
                    };
                    _selectedCollection.Items.Add(item);
                    CurrentCollectionItems.Add(item);
                }
                ApplyFilter();
                FavoritesStorage.Save(Collections);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudieron agregar los elementos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowInFolderFromItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement el && el.DataContext is FavoriteComic comic && !string.IsNullOrWhiteSpace(comic.FilePath))
            {
                try
                {
                    if (File.Exists(comic.FilePath))
                    {
                        var argument = "/select, \"" + comic.FilePath + "\"";
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = argument,
                            UseShellExecute = true
                        });
                    }
                    else if (Directory.Exists(comic.FilePath))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = comic.FilePath,
                            UseShellExecute = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo abrir el Explorador: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    // Helpers para resolver los elementos del XAML de forma segura
    public partial class FavoritesWindow
    {
        private ListBox GetCollectionsListBox() => (ListBox)FindName("CollectionsListBox");
        private ListBox GetFavoritesListBox() => (ListBox)FindName("FavoritesListBox");
        private TextBox GetSearchTextBox() => (TextBox)FindName("SearchTextBox");
        private void SetCollectionTitle(string text)
        {
            if (FindName("CollectionTitleText") is TextBlock tb) tb.Text = text;
        }
    }

    // Diálogo para crear nueva colección
    public partial class CreateCollectionDialog : Window
    {
        public string CollectionName { get; private set; }
        public string CollectionDescription { get; private set; }
        public string SelectedColor { get; private set; } = "#FF3B82F6";

        public CreateCollectionDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Title = "Nueva Colección";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var stackPanel = new StackPanel { Margin = new Thickness(20) };
            
            stackPanel.Children.Add(new TextBlock { Text = "Nombre:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
            var nameTextBox = new TextBox { Name = "NameTextBox", Padding = new Thickness(8), FontSize = 14 };
            stackPanel.Children.Add(nameTextBox);
            
            stackPanel.Children.Add(new TextBlock { Text = "Descripción:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 15, 0, 5) });
            var descTextBox = new TextBox { Name = "DescriptionTextBox", Padding = new Thickness(8), FontSize = 14, Height = 80, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true };
            stackPanel.Children.Add(descTextBox);

            Grid.SetRow(stackPanel, 0);
            grid.Children.Add(stackPanel);

            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20)
            };

            var okButton = new Button 
            { 
                Content = "Crear", 
                Width = 80, 
                Height = 35, 
                Margin = new Thickness(5, 0, 5, 0),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };
            okButton.Click += (s, e) => 
            {
                CollectionName = nameTextBox.Text?.Trim();
                CollectionDescription = descTextBox.Text?.Trim();
                
                if (string.IsNullOrEmpty(CollectionName))
                {
                    MessageBox.Show("El nombre es obligatorio.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                DialogResult = true;
            };

            var cancelButton = new Button 
            { 
                Content = "Cancelar", 
                Width = 80, 
                Height = 35, 
                Margin = new Thickness(5, 0, 5, 0),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };
            cancelButton.Click += (s, e) => DialogResult = false;

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            Grid.SetRow(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }
    }
}