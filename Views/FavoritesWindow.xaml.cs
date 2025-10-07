using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using ComicReader.Models;
using ComicReader.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using SharpCompress.Archives;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ComicReader.Views
{
    public partial class FavoritesWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<ComicCollection> _collections = new();
        private ObservableCollection<FavoriteComic> _individualFavorites = new();
        private ComicCollection? _selectedCollection;
        private ComicCollection? _favoritesCollection; // Referencia a la colección real

        public ObservableCollection<ComicCollection> Collections
        {
            get => _collections;
            set { _collections = value; OnPropertyChanged(); }
        }

        public ObservableCollection<FavoriteComic> IndividualFavorites
        {
            get => _individualFavorites;
            set { _individualFavorites = value; OnPropertyChanged(); }
        }

        public ComicCollection? SelectedCollection
        {
            get => _selectedCollection;
            set { _selectedCollection = value; OnPropertyChanged(); }
        }

        public FavoritesWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                Collections = FavoritesStorage.Load();
                
                // Buscar colección de favoritos individuales
                _favoritesCollection = Collections.FirstOrDefault(c => c.Name == "Favoritos Individuales");
                if (_favoritesCollection == null)
                {
                    _favoritesCollection = new ComicCollection
                    {
                        Name = "Favoritos Individuales",
                        Description = "Cómics favoritos sin organizar en colecciones específicas",
                        Color = "#FF6B6B"
                    };
                    Collections.Insert(0, _favoritesCollection);
                }
                
                // IMPORTANTE: Usar la misma referencia, no crear una nueva
                IndividualFavorites = _favoritesCollection.Items;
                
                // Cargar miniaturas para favoritos existentes que no las tengan
                RefreshThumbnails();
            }
            catch (Exception)
            {
                Collections = new ObservableCollection<ComicCollection>();
                IndividualFavorites = new ObservableCollection<FavoriteComic>();
                
                // Crear colección por defecto
                _favoritesCollection = new ComicCollection
                {
                    Name = "Favoritos Individuales",
                    Description = "Cómics favoritos sin organizar en colecciones específicas",
                    Color = "#FF6B6B"
                };
                Collections.Add(_favoritesCollection);
                IndividualFavorites = _favoritesCollection.Items;
            }
        }

        private string GenerateThumbnail(string comicPath)
        {
            try
            {
                if (!File.Exists(comicPath))
                    return string.Empty;
                    
                var extension = Path.GetExtension(comicPath).ToLower();
                
                // Para archivos ZIP/RAR/CBZ/CBR, intentar extraer primera imagen
                if (extension == ".cbz" || extension == ".zip" || extension == ".cbr" || extension == ".rar")
                {
                    var thumbnail = ExtractThumbnailFromArchive(comicPath);
                    return !string.IsNullOrEmpty(thumbnail) ? thumbnail : string.Empty;
                }
                // Para PDFs
                else if (extension == ".pdf")
                {
                    var thumbnail = ExtractThumbnailFromPdf(comicPath);
                    return !string.IsNullOrEmpty(thumbnail) ? thumbnail : string.Empty;
                }
                // Para imágenes directas
                else if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".bmp" || extension == ".gif")
                {
                    return comicPath; // Usar la imagen directamente
                }
            }
            catch (Exception)
            {
                // Silenciar errores en producción
            }
            
            return string.Empty;
        }

        private string ExtractThumbnailFromArchive(string archivePath)
        {
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), "ComicReader", "Thumbnails");
                Directory.CreateDirectory(tempPath);
                
                var thumbnailPath = Path.Combine(tempPath, $"{Path.GetFileNameWithoutExtension(archivePath)}_thumb.jpg");
                
                // Si ya existe, usar el existente
                if (File.Exists(thumbnailPath))
                {
                    return thumbnailPath;
                }
                
                // Usar SharpCompress para extraer primera imagen
                using (var archive = ArchiveFactory.Open(archivePath))
                {
                    var imageEntry = archive.Entries
                        .Where(entry => !entry.IsDirectory && !string.IsNullOrEmpty(entry.Key))
                        .Where(entry => 
                        {
                            var ext = Path.GetExtension(entry.Key ?? "").ToLower();
                            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif";
                        })
                        .OrderBy(entry => entry.Key ?? "")
                        .FirstOrDefault();

                    if (imageEntry != null)
                    {
                        // Extraer y guardar thumbnail
                        using (var entryStream = imageEntry.OpenEntryStream())
                        using (var fileStream = File.Create(thumbnailPath))
                        {
                            entryStream.CopyTo(fileStream);
                        }
                        
                        return thumbnailPath;
                    }
                }
            }
            catch (Exception)
            {
                // Silenciar errores en producción
            }
            
            return string.Empty;
        }

        private string ExtractThumbnailFromPdf(string pdfPath)
        {
            try
            {
                // Para PDFs, necesitaríamos implementar con Docnet.Core
                // Por ahora, retornar placeholder
                return string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private void RefreshThumbnails()
        {
            foreach (var favorite in IndividualFavorites)
            {
                if (string.IsNullOrEmpty(favorite.Thumbnail))
                {
                    var thumbnail = GenerateThumbnail(favorite.FilePath);
                    if (!string.IsNullOrEmpty(thumbnail))
                    {
                        favorite.Thumbnail = thumbnail;
                    }
                }
            }
        }

        private void SaveData()
        {
            try
            {
                FavoritesStorage.Save(Collections);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error guardando favoritos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // FUNCIONALIDAD PRINCIPAL DE FAVORITOS
        private void AddFavorite_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar cómic para agregar a favoritos",
                Filter = "Archivos de cómic|*.cbz;*.cbr;*.zip;*.rar;*.pdf;*.epub|" +
                        "Archivos CBZ|*.cbz|" +
                        "Archivos CBR|*.cbr|" +
                        "Archivos ZIP|*.zip|" +
                        "Archivos RAR|*.rar|" +
                        "Archivos PDF|*.pdf|" +
                        "Archivos EPUB|*.epub|" +
                        "Todos los archivos|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (string filePath in dialog.FileNames)
                {
                    AddComicToFavorites(filePath);
                }
                SaveData();
            }
        }

        private void AddComicToFavorites(string filePath)
        {
            try
            {

                
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    MessageBox.Show("El archivo no existe o la ruta es inválida.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Asegurar que tenemos la colección de favoritos
                if (_favoritesCollection == null)
                {

                    LoadData();
                }

                // Verificar si ya existe
                var existing = _favoritesCollection!.Items.FirstOrDefault(f => string.Equals(f.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    MessageBox.Show($"'{Path.GetFileName(filePath)}' ya está en favoritos.", "Ya existe", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var favorite = new FavoriteComic
                {
                    FilePath = filePath,
                    Title = Path.GetFileNameWithoutExtension(filePath),
                    DateAdded = DateTime.Now,
                    Rating = 0,
                    Thumbnail = GenerateThumbnail(filePath)
                };

                // Agregar tanto a la colección real como a IndividualFavorites
                _favoritesCollection.Items.Add(favorite);

                
                // Forzar actualización de la UI
                OnPropertyChanged(nameof(IndividualFavorites));
                
                MessageBox.Show($"'{favorite.Title}' agregado a favoritos.", "Agregado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error agregando a favoritos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveFavorite_Click(object sender, RoutedEventArgs e)
        {
            // Intentar obtener favorito desde el DataContext del botón o desde la selección del ListBox
            FavoriteComic? favorite = null;
            
            if (sender is Button button && button.DataContext is FavoriteComic buttonFavorite)
            {
                favorite = buttonFavorite;
            }
            else if (sender is FrameworkElement element && 
                     element.FindName("FavoritesListBox") is ListBox listBox && 
                     listBox.SelectedItem is FavoriteComic selectedFavorite)
            {
                favorite = selectedFavorite;
            }
            
            if (favorite != null)
            {
                var result = MessageBox.Show($"¿Quitar '{favorite.Title}' de favoritos?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _favoritesCollection?.Items.Remove(favorite);
                    SaveData();
                    MessageBox.Show($"'{favorite.Title}' eliminado de favoritos.", "Eliminado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Selecciona un favorito para quitar.", "Sin selección", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RemoveFromFavorites_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is FavoriteComic favorite)
            {
                var result = MessageBox.Show($"¿Quitar '{favorite.Title}' de favoritos?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _favoritesCollection?.Items.Remove(favorite);
                    SaveData();
                }
            }
        }

        private void OpenComic_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is FavoriteComic favorite)
            {
                try
                {
                    if (File.Exists(favorite.FilePath))
                    {
                        // Aquí integrar con el sistema de apertura de cómics existente
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = favorite.FilePath,
                            UseShellExecute = true
                        });
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("El archivo no existe en la ubicación especificada.", "Archivo no encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error abriendo cómic: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ShowInExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is FavoriteComic favorite)
            {
                try
                {
                    if (File.Exists(favorite.FilePath))
                    {
                        Process.Start("explorer.exe", $"/select,\"{favorite.FilePath}\"");
                    }
                    else
                    {
                        MessageBox.Show("El archivo no existe en la ubicación especificada.", "Archivo no encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error mostrando en explorador: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RefreshThumbnails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                button.IsEnabled = false;
                button.Content = "🔄 Cargando...";
            }

            // Refrescar miniaturas en un hilo de fondo
            Task.Run(() =>
            {
                foreach (var favorite in IndividualFavorites)
                {
                    // Regenerar miniatura siempre
                    var thumbnail = GenerateThumbnail(favorite.FilePath);
                    
                    // Actualizar en el hilo principal
                    Dispatcher.Invoke(() =>
                    {
                        favorite.Thumbnail = thumbnail;
                    });
                }

                // Restaurar botón en el hilo principal
                Dispatcher.Invoke(() =>
                {
                    if (button != null)
                    {
                        button.IsEnabled = true;
                        var stack = new StackPanel { Orientation = Orientation.Horizontal };
                        stack.Children.Add(new TextBlock { Text = "🔄", FontSize = 16, Margin = new Thickness(0, 0, 8, 0) });
                        stack.Children.Add(new TextBlock { Text = "Refrescar Portadas" });
                        button.Content = stack;
                    }
                    SaveData();
                    MessageBox.Show("Portadas actualizadas correctamente.", "Completado", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            });
        }

        private void SearchFavorites_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                var searchText = textBox.Text.ToLower();
                
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    // Mostrar todos los favoritos
                    foreach (var favorite in _favoritesCollection?.Items ?? new ObservableCollection<FavoriteComic>())
                    {
                        if (!IndividualFavorites.Contains(favorite))
                        {
                            IndividualFavorites.Add(favorite);
                        }
                    }
                }
                else
                {
                    // Filtrar por término de búsqueda
                    var filtered = _favoritesCollection?.Items
                        .Where(f => f.Title.ToLower().Contains(searchText) || 
                                   Path.GetFileNameWithoutExtension(f.FilePath).ToLower().Contains(searchText))
                        .ToList() ?? new List<FavoriteComic>();
                    
                    IndividualFavorites.Clear();
                    foreach (var item in filtered)
                    {
                        IndividualFavorites.Add(item);
                    }
                }
            }
        }

        private void FavoritesSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && _favoritesCollection != null)
            {
                var sortedList = comboBox.SelectedIndex switch
                {
                    0 => _favoritesCollection.Items.OrderByDescending(f => f.DateAdded).ToList(), // Reciente
                    1 => _favoritesCollection.Items.OrderBy(f => f.DateAdded).ToList(),           // Antiguo
                    2 => _favoritesCollection.Items.OrderBy(f => f.Title).ToList(),               // A-Z
                    3 => _favoritesCollection.Items.OrderByDescending(f => f.Title).ToList(),     // Z-A
                    4 => _favoritesCollection.Items.OrderByDescending(f => f.LastRead ?? DateTime.MinValue).ToList(), // Última lectura
                    _ => _favoritesCollection.Items.ToList()
                };

                IndividualFavorites.Clear();
                foreach (var item in sortedList)
                {
                    IndividualFavorites.Add(item);
                }
            }
        }

        private void CreateCollection_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CollectionDialog("Nueva Colección");
            if (dialog.ShowDialog() == true)
            {
                var collection = new ComicCollection
                {
                    Name = dialog.CollectionName,
                    Description = dialog.Description,
                    Color = dialog.SelectedColor
                };
                
                Collections.Add(collection);
                SaveData();
                MessageBox.Show($"Colección '{collection.Name}' creada.", "Creado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RenameCollection_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCollection != null && SelectedCollection.Name != "Favoritos Individuales")
            {
                var dialog = new CollectionDialog("Renombrar Colección", SelectedCollection);
                if (dialog.ShowDialog() == true)
                {
                    SelectedCollection.Name = dialog.CollectionName;
                    SelectedCollection.Description = dialog.Description;
                    SelectedCollection.Color = dialog.SelectedColor;
                    SaveData();
                }
            }
        }

        private void DuplicateCollection_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCollection != null)
            {
                var duplicate = new ComicCollection
                {
                    Name = $"{SelectedCollection.Name} (Copia)",
                    Description = SelectedCollection.Description,
                    Color = SelectedCollection.Color
                };
                
                foreach (var item in SelectedCollection.Items)
                {
                    duplicate.Items.Add(new FavoriteComic
                    {
                        FilePath = item.FilePath,
                        Title = item.Title,
                        Author = item.Author,
                        Rating = item.Rating,
                        Tags = item.Tags?.ToArray() ?? Array.Empty<string>(),
                        Notes = item.Notes,
                        DateAdded = DateTime.Now
                    });
                }
                
                Collections.Add(duplicate);
                SaveData();
                MessageBox.Show($"Colección duplicada como '{duplicate.Name}'.", "Duplicado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteCollection_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCollection != null && SelectedCollection.Name != "Favoritos Individuales")
            {
                var result = MessageBox.Show($"¿Eliminar la colección '{SelectedCollection.Name}'?\\n\\nEsta acción no se puede deshacer.", 
                                           "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    Collections.Remove(SelectedCollection);
                    SelectedCollection = null;
                    SaveData();
                }
            }
        }

        private void ChangeCollectionColor_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCollection != null)
            {
                var dialog = new CollectionDialog("Cambiar Color", SelectedCollection);
                if (dialog.ShowDialog() == true)
                {
                    SelectedCollection.Color = dialog.SelectedColor;
                    SaveData();
                }
            }
        }

        private void CollectionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is ComicCollection collection)
            {
                SelectedCollection = collection;
            }
        }

        private void AddToCollection_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCollection == null)
            {
                MessageBox.Show("Selecciona una colección primero.", "Sin selección", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar cómics para agregar a la colección",
                Filter = "Archivos de cómic|*.cbz;*.cbr;*.zip;*.rar;*.pdf;*.epub|Todos los archivos|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (string filePath in dialog.FileNames)
                {
                    AddComicToCollection(filePath, SelectedCollection);
                }
                SaveData();
            }
        }

        private void AddComicToCollection(string filePath, ComicCollection collection)
        {
            try
            {
                if (collection.Items.Any(f => string.Equals(f.FilePath, filePath, StringComparison.OrdinalIgnoreCase)))
                {
                    return; // Ya existe
                }

                var comic = new FavoriteComic
                {
                    FilePath = filePath,
                    Title = Path.GetFileNameWithoutExtension(filePath),
                    DateAdded = DateTime.Now
                };

                collection.Items.Add(comic);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error agregando cómic a colección: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddFolderToCollection_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidad de agregar carpeta - En desarrollo", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidad de quitar seleccionados - En desarrollo", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SearchCollection_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Implementar búsqueda en colección
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Exportar favoritos y colecciones",
                    Filter = "JSON|*.json|Todos los archivos|*.*",
                    FileName = "favoritos_backup.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(Collections, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(dialog.FileName, json);
                    MessageBox.Show("Favoritos exportados correctamente.", "Exportado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exportando: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Importar favoritos y colecciones",
                    Filter = "JSON|*.json|Todos los archivos|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    var json = File.ReadAllText(dialog.FileName);
                    var imported = System.Text.Json.JsonSerializer.Deserialize<ObservableCollection<ComicCollection>>(json);
                    
                    if (imported != null)
                    {
                        foreach (var collection in imported)
                        {
                            Collections.Add(collection);
                        }
                        SaveData();
                        MessageBox.Show($"Importadas {imported.Count} colecciones.", "Importado", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importando: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            SaveData();
            Close();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }
    }
}