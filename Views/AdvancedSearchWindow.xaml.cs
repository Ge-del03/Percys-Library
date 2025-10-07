using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ComicReader.Services;

namespace ComicReader.Views
{
    public partial class AdvancedSearchWindow : Window
    {
        private readonly AdvancedSearchEngine _searchEngine;
        private List<SearchResult> _currentResults = new List<SearchResult>();

        public event EventHandler<string>? ComicSelected;

        public AdvancedSearchWindow()
        {
            InitializeComponent();
            _searchEngine = new AdvancedSearchEngine();
            
            // Auto-complete en búsqueda
            SearchBox.TextChanged += SearchBox_TextChanged;
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var query = SearchBox.Text?.Trim();
            if (string.IsNullOrEmpty(query))
            {
                MessageBox.Show("Por favor ingresa un término de búsqueda", "Búsqueda", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Mostrar progreso
                SearchProgress.Visibility = Visibility.Visible;
                ResultsList.Visibility = Visibility.Collapsed;
                NoResultsPanel.Visibility = Visibility.Collapsed;
                StatusText.Text = "Buscando...";

                // Crear opciones de búsqueda
                var options = new SearchOptions
                {
                    Query = query,
                    SearchTypes = GetSelectedSearchTypes(),
                    CaseSensitive = CaseSensitiveBox.IsChecked == true,
                    UseRegex = UseRegexBox.IsChecked == true,
                    IncludeArchived = IncludeArchivedBox.IsChecked == true,
                    FromDate = FromDatePicker.SelectedDate,
                    ToDate = ToDatePicker.SelectedDate,
                    FileExtensions = GetSelectedFileExtensions(),
                    MaxResults = 100
                };

                // Aplicar filtros de tamaño
                if (!string.IsNullOrEmpty(MinSizeBox.Text) && double.TryParse(MinSizeBox.Text, out double minMB))
                {
                    options.MinFileSize = (long)(minMB * 1024 * 1024);
                }
                if (!string.IsNullOrEmpty(MaxSizeBox.Text) && double.TryParse(MaxSizeBox.Text, out double maxMB))
                {
                    options.MaxFileSize = (long)(maxMB * 1024 * 1024);
                }

                // Ejecutar búsqueda
                var progress = new Progress<double>(p =>
                {
                    StatusText.Text = $"Buscando... {p:P0}";
                });

                _currentResults = await _searchEngine.SearchAsync(options, progress);

                // Mostrar resultados
                DisplayResults(_currentResults);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en la búsqueda: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Error en búsqueda";
            }
            finally
            {
                SearchProgress.Visibility = Visibility.Collapsed;
            }
        }

        private void DisplayResults(List<SearchResult> results)
        {
            if (results.Count == 0)
            {
                ResultsList.Visibility = Visibility.Collapsed;
                NoResultsPanel.Visibility = Visibility.Visible;
                ResultsCountText.Text = "0 resultados encontrados";
                StatusText.Text = "No se encontraron resultados";
            }
            else
            {
                ResultsList.Visibility = Visibility.Visible;
                NoResultsPanel.Visibility = Visibility.Collapsed;
                ResultsList.ItemsSource = results;
                ResultsCountText.Text = $"{results.Count} resultado{(results.Count != 1 ? "s" : "")} encontrado{(results.Count != 1 ? "s" : "")}";
                StatusText.Text = $"Búsqueda completada - {results.Count} resultados";
            }
        }

        private List<SearchType> GetSelectedSearchTypes()
        {
            var types = new List<SearchType>();

            if (SearchInFileName.IsChecked == true)
                types.Add(SearchType.FileName);
            if (SearchInPath.IsChecked == true)
                types.Add(SearchType.Path);
            if (SearchInMetadata.IsChecked == true)
                types.Add(SearchType.Metadata);
            if (SearchInAnnotations.IsChecked == true)
                types.Add(SearchType.Annotations);
            if (SearchInTags.IsChecked == true)
                types.Add(SearchType.Tags);

            // Si no hay ninguno seleccionado, buscar al menos en nombre
            if (types.Count == 0)
                types.Add(SearchType.FileName);

            return types;
        }

        private List<string> GetSelectedFileExtensions()
        {
            var extensions = new List<string>();
            
            // Obtener todos los CheckBoxes de formatos
            var formatsPanel = FindName("OptionsPanel") as Expander;
            if (formatsPanel != null)
            {
                foreach (var child in LogicalTreeHelper.GetChildren(formatsPanel))
                {
                    if (child is Grid grid)
                    {
                        foreach (var item in LogicalTreeHelper.GetChildren(grid))
                        {
                            if (item is GroupBox groupBox && groupBox.Header.ToString() == "Formatos de archivo")
                            {
                                if (groupBox.Content is StackPanel panel)
                                {
                                    foreach (var cb in panel.Children.OfType<CheckBox>())
                                    {
                                        if (cb.IsChecked == true && cb.Content != null)
                                        {
                                            extensions.Add(cb.Content.ToString() ?? "");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return extensions;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Auto-complete simple
            var query = SearchBox.Text?.Trim();
            if (!string.IsNullOrEmpty(query) && query.Length >= 2)
            {
                // Aquí podrías implementar un auto-complete dropdown
            }
        }

        private void ToggleOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            OptionsPanel.IsExpanded = !OptionsPanel.IsExpanded;
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_currentResults.Count == 0)
                return;

            var sortedResults = new List<SearchResult>();

            switch (SortComboBox.SelectedIndex)
            {
                case 0: // Relevancia
                    sortedResults = _currentResults.OrderByDescending(r => r.Relevance).ToList();
                    break;
                case 1: // Nombre
                    sortedResults = _currentResults.OrderBy(r => r.ComicName).ToList();
                    break;
                case 2: // Fecha
                    sortedResults = _currentResults.OrderByDescending(r => r.LastAccessed).ToList();
                    break;
                case 3: // Tamaño
                    sortedResults = _currentResults.OrderByDescending(r => r.FileSize).ToList();
                    break;
            }

            ResultsList.ItemsSource = sortedResults;
        }

        private void ResultsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ResultsList.SelectedItem is SearchResult result)
            {
                ComicSelected?.Invoke(this, result.ComicPath);
                Close();
            }
        }

        private void OpenComicButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SearchResult result)
            {
                ComicSelected?.Invoke(this, result.ComicPath);
                Close();
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentResults.Count == 0)
            {
                MessageBox.Show("No hay resultados para exportar", "Exportar", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                DefaultExt = ".csv",
                FileName = $"busqueda_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _searchEngine.ExportResultsToCSV(_currentResults, dialog.FileName);
                    MessageBox.Show($"Resultados exportados correctamente a:\n{dialog.FileName}", 
                        "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
