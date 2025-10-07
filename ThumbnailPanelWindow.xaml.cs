using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ComicReader.Models;

namespace ComicReader
{
    public partial class ThumbnailPanelWindow : Window
    {
        public event Action<int>? PageSelected;
        private readonly List<ComicPage> _pages;
        private int _currentPageIndex;

        // Constructor sin parámetros para compatibilidad de diseñador/XAML
        public ThumbnailPanelWindow() : this(new List<ComicPage>(), 0) { }

        public ThumbnailPanelWindow(List<ComicPage> pages, int currentPageIndex)
        {
            InitializeComponent();
            _pages = pages ?? new List<ComicPage>();
            _currentPageIndex = currentPageIndex;
            // Asegurar que los elementos con x:Name existen
            this.Loaded -= OnLoaded;
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            try { LoadThumbnails(); }
            catch { }
        }

        private void LoadThumbnails()
        {
            var thumbnailsGrid = (Grid?)FindName("ThumbnailsGrid");
            if (thumbnailsGrid == null) return; // diseño o XAML no cargado aún
            thumbnailsGrid.Children.Clear();
            
            int columns = 4;
            int rows = (int)Math.Ceiling((double)_pages.Count / columns);
            
            // Configurar grid
            thumbnailsGrid.ColumnDefinitions.Clear();
            thumbnailsGrid.RowDefinitions.Clear();
            
            for (int i = 0; i < columns; i++)
            {
                thumbnailsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }
            
            for (int i = 0; i < rows; i++)
            {
                thumbnailsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(200) });
            }

            // Añadir miniaturas
            for (int i = 0; i < _pages.Count; i++)
            {
                var page = _pages[i];
                int row = i / columns;
                int col = i % columns;

                var border = new Border
                {
                    BorderBrush = i == _currentPageIndex ? Brushes.Red : Brushes.LightGray,
                    BorderThickness = new Thickness(i == _currentPageIndex ? 3 : 1),
                    Margin = new Thickness(5),
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(5)
                };

                var stackPanel = new StackPanel();

                var image = new Image
                {
                    Source = page.Image ?? page.Thumbnail ?? new BitmapImage(),
                    Width = 120,
                    Height = 160,
                    Stretch = Stretch.Uniform,
                    Cursor = Cursors.Hand
                };

                var pageNumber = new TextBlock
                {
                    Text = $"Página {i + 1}",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 12,
                    Margin = new Thickness(0, 5, 0, 5)
                };

                stackPanel.Children.Add(image);
                stackPanel.Children.Add(pageNumber);
                border.Child = stackPanel;

                // Evento click
                int pageIndex = i; // Capturar el índice para el closure
                border.MouseLeftButtonUp += (s, e) =>
                {
                    PageSelected?.Invoke(pageIndex);
                    Close();
                };

                Grid.SetRow(border, row);
                Grid.SetColumn(border, col);
                thumbnailsGrid.Children.Add(border);
            }

            // Scroll a la página actual
            ScrollToCurrentPage();
        }

        private void ScrollToCurrentPage()
        {
            if (_currentPageIndex >= 0 && _currentPageIndex < _pages.Count)
            {
                int columns = 4;
                int row = _currentPageIndex / columns;
                
                // Calcular posición de scroll
                double scrollPosition = (double)row / Math.Ceiling((double)_pages.Count / columns);
                var scroller = (ScrollViewer?)FindName("ThumbnailScrollViewer");
                if (scroller != null)
                {
                    scroller.ScrollToVerticalOffset(scrollPosition * scroller.ScrollableHeight);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
