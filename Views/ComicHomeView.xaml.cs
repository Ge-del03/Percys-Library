using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using ComicReader.Models;
using ComicReader.Services;
using Microsoft.Win32;

namespace ComicReader.Views
{
    public partial class ComicHomeView : UserControl
    {
        private readonly ContinueReadingService? _continueService;
        private MainWindow? _mainWindow;

        public ComicHomeView()
        {
            InitializeComponent();
            _continueService = ContinueReadingService.Instance;
            
            Loaded += ComicHomeView_Loaded;
        }

        private void ComicHomeView_Loaded(object sender, RoutedEventArgs e)
        {
            // Obtener referencia a MainWindow
            _mainWindow = Window.GetWindow(this) as MainWindow;
            
            // Cargar datos
            LoadContinueReading();
            LoadRecentComics();
            
            // Animación de entrada
            var storyboard = (Storyboard)Resources["FadeInAnimation"];
            storyboard.Begin(this);
        }

        private void LoadContinueReading()
        {
            try
            {
                if (_continueService == null) return;
                var continueItems = _continueService.Items.Take(5);
                
                if (continueItems.Any())
                {
                var displayItems = continueItems.Select(item => new
                {
                    Path = item.FilePath,
                    Title = item.DisplayName,
                    Progress = $"Página {item.LastPage} de {item.PageCount}",
                    CoverImage = LoadCover(item.FilePath)
                }).ToList();                    ContinueReadingList.ItemsSource = displayItems;
                    NoContinueMessage.Visibility = Visibility.Collapsed;
                }
                else
                {
                    NoContinueMessage.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error cargando continuar leyendo: {ex.Message}");
            }
        }

        private void LoadRecentComics()
        {
            try
            {
                // Por ahora, lista vacía - se puede implementar después
                var displayItems = new List<object>();
                RecentComicsList.ItemsSource = displayItems;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error cargando recientes: {ex.Message}");
            }
        }

        private BitmapImage? LoadCover(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                    return null;

                var pages = ComicFileLoader.LoadComic(filePath);
                if (pages != null && pages.Count > 0)
                {
                    return pages[0];
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error cargando portada de {filePath}: {ex.Message}");
            }
            return null;
        }

        // Event handlers
        private void ContinueReading_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string path)
            {
                OpenComicFile(path);
            }
        }

        private void OpenRecent_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string path)
            {
                OpenComicFile(path);
            }
        }

        private void ViewAllRecent_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Mostrar ventana con todos los recientes
            MessageBox.Show("Función 'Ver Todos' en desarrollo", "Percy's Library", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenComic_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Archivos de Cómic|*.cbz;*.cbr;*.cb7;*.cbt;*.zip;*.rar;*.7z;*.pdf;*.epub|Todos los archivos|*.*",
                Title = "Abrir Cómic"
            };

            if (dialog.ShowDialog() == true)
            {
                OpenComicFile(dialog.FileName);
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Selecciona una carpeta con cómics",
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OpenComicFile(dialog.SelectedPath);
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implementar búsqueda simple
            MessageBox.Show("Función de búsqueda en desarrollo", "Percy's Library", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            // Abrir diálogo de configuración
            var settingsDialog = new Views.SettingsDialog();
            settingsDialog.Owner = Window.GetWindow(this);
            settingsDialog.ShowDialog();
        }

        private void OpenComicFile(string path)
        {
            try
            {
                if (_mainWindow != null)
                {
                    // Llamar al método de MainWindow para abrir el cómic
                    var method = _mainWindow.GetType().GetMethod("LoadComic", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (method != null)
                    {
                        method.Invoke(_mainWindow, new object[] { path });
                    }
                    else
                    {
                        Logger.Log("No se encontró el método LoadComic en MainWindow");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error abriendo cómic: {ex.Message}");
                MessageBox.Show($"Error al abrir el cómic: {ex.Message}", "Error", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método público para refrescar la vista
        public void Refresh()
        {
            LoadContinueReading();
            LoadRecentComics();
        }
    }
}
