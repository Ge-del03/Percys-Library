using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ComicReader.Core.Abstractions;
using ComicReader.Services;
using WinForms = System.Windows.Forms;

namespace ComicReader.Views
{
    public partial class ExportWindow : Window
    {
        private readonly IComicPageLoader _pageLoader;
        private readonly int _currentPage;
        private bool _isExporting = false;

        public ExportWindow(IComicPageLoader pageLoader, int currentPage)
        {
            InitializeComponent();
            _pageLoader = pageLoader ?? throw new ArgumentNullException(nameof(pageLoader));
            _currentPage = currentPage;

            // Configurar valores predeterminados
            EndPageTextBox.Text = _pageLoader.PageCount.ToString();
            OutputFolderTextBox.Text = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Percy's Library Exports"
            );

            UpdatePageCount();
        }

        private void Format_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (FormatComboBox.SelectedItem is ComboBoxItem item && item.Tag is string format)
            {
                // Mostrar control de calidad solo para JPEG y WebP
                QualityPanel.Visibility = (format == "JPEG" || format == "WebP") 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
        }

        private void Resize_Changed(object sender, RoutedEventArgs e)
        {
            if (ResizePanel != null)
            {
                ResizePanel.IsEnabled = ResizeCheckBox.IsChecked == true;
            }
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new WinForms.FolderBrowserDialog
            {
                Description = "Selecciona la carpeta de destino",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                OutputFolderTextBox.Text = dialog.SelectedPath;
            }
        }

        private void UpdatePageCount()
        {
            int count = 0;

            if (AllPagesRadio.IsChecked == true)
            {
                count = _pageLoader.PageCount;
            }
            else if (CurrentPageRadio.IsChecked == true)
            {
                count = 1;
            }
            else if (RangeRadio.IsChecked == true)
            {
                if (int.TryParse(StartPageTextBox.Text, out int start) &&
                    int.TryParse(EndPageTextBox.Text, out int end))
                {
                    count = Math.Max(0, end - start + 1);
                }
            }

            PageCountText.Text = count.ToString();
        }

        private async void Calculate_Click(object sender, RoutedEventArgs e)
        {
            EstimatedSizeText.Text = "Calculando...";

            try
            {
                var options = GetExportOptions();
                var pageIndices = GetPageIndices();
                var exporter = new PageExporter(_pageLoader);

                var estimatedSize = await exporter.EstimateExportSizeAsync(pageIndices, options);
                EstimatedSizeText.Text = FormatFileSize(estimatedSize);
            }
            catch (Exception ex)
            {
                EstimatedSizeText.Text = "Error al calcular";
                MessageBox.Show($"Error al calcular tamaño: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            if (_isExporting) return;

            // Validaciones
            if (string.IsNullOrWhiteSpace(OutputFolderTextBox.Text))
            {
                MessageBox.Show("Selecciona una carpeta de destino", "Validación", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _isExporting = true;
                ExportButton.IsEnabled = false;
                ProgressPanel.Visibility = Visibility.Visible;

                // Crear carpeta si no existe
                Directory.CreateDirectory(OutputFolderTextBox.Text);

                var options = GetExportOptions();
                var pageIndices = GetPageIndices();
                var exporter = new PageExporter(_pageLoader);

                var progress = new Progress<double>(value =>
                {
                    ProgressBar.Value = value * 100;
                    ProgressText.Text = $"Exportando... {value * 100:F0}%";
                });

                var exportedFiles = await exporter.ExportPageRangeAsync(
                    pageIndices.Min(),
                    pageIndices.Max(),
                    options,
                    progress
                );

                MessageBox.Show(
                    $"✓ Exportación completada\n\n{exportedFiles.Count} páginas exportadas a:\n{OutputFolderTextBox.Text}",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante la exportación: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isExporting = false;
                ExportButton.IsEnabled = true;
                ProgressPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private ExportOptions GetExportOptions()
        {
            var format = ExportFormat.PNG;
            if (FormatComboBox.SelectedItem is ComboBoxItem item && item.Tag is string formatStr)
            {
                Enum.TryParse(formatStr, out format);
            }

            var options = new ExportOptions
            {
                Format = format,
                Quality = (int)QualitySlider.Value,
                OutputFolder = OutputFolderTextBox.Text,
                FileNamePattern = "{comic}_{page}",
                MaintainAspectRatio = MaintainAspectCheckBox.IsChecked == true
            };

            if (ResizeCheckBox.IsChecked == true)
            {
                if (int.TryParse(MaxWidthTextBox.Text, out int maxWidth))
                    options.MaxWidth = maxWidth;
                if (int.TryParse(MaxHeightTextBox.Text, out int maxHeight))
                    options.MaxHeight = maxHeight;
            }

            return options;
        }

        private System.Collections.Generic.List<int> GetPageIndices()
        {
            var indices = new System.Collections.Generic.List<int>();

            if (AllPagesRadio.IsChecked == true)
            {
                indices.AddRange(Enumerable.Range(0, _pageLoader.PageCount));
            }
            else if (CurrentPageRadio.IsChecked == true)
            {
                indices.Add(_currentPage);
            }
            else if (RangeRadio.IsChecked == true)
            {
                if (int.TryParse(StartPageTextBox.Text, out int start) &&
                    int.TryParse(EndPageTextBox.Text, out int end))
                {
                    start = Math.Max(1, start);
                    end = Math.Min(_pageLoader.PageCount, end);
                    indices.AddRange(Enumerable.Range(start - 1, end - start + 1));
                }
            }

            return indices;
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
