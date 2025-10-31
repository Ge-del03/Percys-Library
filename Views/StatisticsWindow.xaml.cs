using System;
using System.Windows;
using System.Windows.Input;
using ComicReader.ViewModels;

namespace ComicReader.Views
{
    public partial class StatisticsWindow : Window
    {
        public StatisticsWindow()
        {
            InitializeComponent();
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
    }
}


