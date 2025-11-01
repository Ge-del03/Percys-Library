using System;
using System.Windows;
using System.Windows.Input;
using ComicReader.ViewModels;
using Microsoft.Win32;
using ComicReader.Core.Abstractions;
using ComicReader.Core.Services;
using System.IO;

namespace ComicReader.Views
{
    public partial class StatisticsWindow : Window
    {
        public StatisticsWindow()
        {
            try
            {
                var mi = this.GetType().GetMethod("InitializeComponent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                mi?.Invoke(this, null);
            }
            catch { }
            this.DataContext = new ReadingStatsViewModel();
        }

        private void SessionItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // detect double-click using ClickCount since Border doesn't expose MouseDoubleClick
                if (e.ClickCount == 2 && sender is FrameworkElement fe && fe.DataContext is ReadingStatsViewModel.SessionItem item)
                {
                    OpenComicPath(item.ComicPath);
                }
            }
            catch { }
        }

        private void SessionItem_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter && sender is FrameworkElement fe && fe.DataContext is ReadingStatsViewModel.SessionItem item)
                {
                    OpenComicPath(item.ComicPath);
                }
            }
            catch { }
        }

        private void OpenComicPath(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return;
                if (!System.IO.File.Exists(path) && !System.IO.Directory.Exists(path))
                {
                    MessageBox.Show("El archivo no existe o ha sido movido.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var mainWindow = Window.GetWindow(this) as global::ComicReader.MainWindow;
                if (mainWindow != null)
                {
                    var method = mainWindow.GetType().GetMethod("OpenComicFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    method?.Invoke(mainWindow, new object[] { path });
                }
            }
            catch { }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog()
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    FileName = $"reading-sessions-{DateTime.Now:yyyyMMdd}.csv",
                    DefaultExt = ".csv",
                    Title = "Exportar sesiones a CSV"
                };

                if (dlg.ShowDialog(this) == true)
                {
                    var svc = ServiceLocator.TryGet<IReadingStatsService>();
                    if (svc != null)
                    {
                        svc.ExportSessionsToCsv(dlg.FileName);
                        MessageBox.Show(this, "Exportación completada.", "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(this, "Servicio de estadísticas no disponible.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error al exportar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var res = MessageBox.Show(this, "¿Deseas resetear todas las estadísticas y la configuración relacionada? Esta acción no se puede deshacer.", "Confirmar reset", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res != MessageBoxResult.Yes) return;

                var svc = ServiceLocator.TryGet<IReadingStatsService>();
                if (svc == null)
                {
                    MessageBox.Show(this, "Servicio de estadísticas no disponible.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Reset stored stats
                svc.ResetAll();

                // Clear thumbnail cache
                try
                {
                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var dir = System.IO.Path.Combine(appData, "PercysLibrary", "Thumbs");
                    if (Directory.Exists(dir))
                    {
                        foreach (var f in Directory.GetFiles(dir))
                        {
                            try { File.Delete(f); } catch { }
                        }
                    }
                }
                catch { }

                // Reset settings to defaults and persist immediately
                try
                {
                    ComicReader.Services.SettingsManager.ResetToDefaults();
                    ComicReader.Services.SettingsManager.SaveNow();
                }
                catch { }

                // Refresh VM/UI
                try { if (this.DataContext is ReadingStatsViewModel vm) vm.Refresh(); } catch { }

                MessageBox.Show(this, "Reinicio completo. La aplicación reflejará los valores por defecto.", "Reset", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error al resetear: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}


