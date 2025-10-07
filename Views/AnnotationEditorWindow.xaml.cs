using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ComicReader.Models;

namespace ComicReader.Views
{
    public partial class AnnotationEditorWindow : Window
    {
        private AnnotationType _currentTool = AnnotationType.Text;
        private bool _isDrawing = false;
        private Point _startPoint;
        private Shape? _currentShape = null;
        private AnnotationManager _annotationManager;
        private string _comicPath = string.Empty;
        private int _pageNumber = 0;

        public AnnotationEditorWindow()
        {
            InitializeComponent();
            _annotationManager = new AnnotationManager();
            
            AnnotationCanvas.MouseDown += Canvas_MouseDown;
            AnnotationCanvas.MouseMove += Canvas_MouseMove;
            AnnotationCanvas.MouseUp += Canvas_MouseUp;
        }

        public void LoadPage(string comicPath, int pageNumber, byte[] imageData)
        {
            _comicPath = comicPath;
            _pageNumber = pageNumber;

            // Cargar imagen
            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            using (var stream = new System.IO.MemoryStream(imageData))
            {
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();
            }
            
            PageImage.Source = bitmap;
            AnnotationCanvas.Width = bitmap.PixelWidth;
            AnnotationCanvas.Height = bitmap.PixelHeight;

            // Cargar anotaciones existentes
            LoadAnnotations();
        }

        private void LoadAnnotations()
        {
            var annotations = _annotationManager.GetAnnotations(_comicPath, _pageNumber);
            AnnotationsList.ItemsSource = annotations;

            // Dibujar anotaciones en el canvas
            foreach (var annotation in annotations)
            {
                DrawAnnotation(annotation);
            }
        }

        private void DrawAnnotation(Annotation annotation)
        {
            // TODO: Implementar dibujo de cada tipo de anotación
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDrawing = true;
                _startPoint = e.GetPosition(AnnotationCanvas);
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawing)
            {
                var currentPoint = e.GetPosition(AnnotationCanvas);
                
                if (_currentShape != null)
                {
                    AnnotationCanvas.Children.Remove(_currentShape);
                }

                switch (_currentTool)
                {
                    case AnnotationType.Rectangle:
                        _currentShape = CreateRectangle(_startPoint, currentPoint);
                        break;
                    case AnnotationType.Circle:
                        _currentShape = CreateCircle(_startPoint, currentPoint);
                        break;
                }

                if (_currentShape != null)
                {
                    AnnotationCanvas.Children.Add(_currentShape);
                }
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawing)
            {
                _isDrawing = false;
                
                if (_currentTool == AnnotationType.Text)
                {
                    // Mostrar diálogo para entrada de texto
                    ShowTextInputDialog(_startPoint);
                }
                
                _currentShape = null;
            }
        }

        private Rectangle CreateRectangle(Point start, Point end)
        {
            var rect = new Rectangle
            {
                Stroke = GetCurrentColor(),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(GetCurrentColor().Color) { Opacity = 0.3 }
            };

            var x = Math.Min(start.X, end.X);
            var y = Math.Min(start.Y, end.Y);
            var width = Math.Abs(end.X - start.X);
            var height = Math.Abs(end.Y - start.Y);

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            rect.Width = width;
            rect.Height = height;

            return rect;
        }

        private Ellipse CreateCircle(Point start, Point end)
        {
            var ellipse = new Ellipse
            {
                Stroke = GetCurrentColor(),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(GetCurrentColor().Color) { Opacity = 0.3 }
            };

            var radius = Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));
            
            Canvas.SetLeft(ellipse, start.X - radius);
            Canvas.SetTop(ellipse, start.Y - radius);
            ellipse.Width = radius * 2;
            ellipse.Height = radius * 2;

            return ellipse;
        }

        private SolidColorBrush GetCurrentColor()
        {
            var selectedItem = ColorPicker.SelectedItem as ComboBoxItem;
            var colorHex = selectedItem?.Tag as string ?? "#FFFF00";
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
        }

        private void ShowTextInputDialog(Point position)
        {
            var dialog = new Window
            {
                Title = "Agregar Nota",
                Width = 400,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var textBox = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(textBox, 0);
            grid.Children.Add(textBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };
            Grid.SetRow(buttonPanel, 1);

            var okButton = new Button { Content = "Aceptar", Width = 80, Margin = new Thickness(5, 0, 5, 0) };
            okButton.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    var annotation = new Annotation
                    {
                        ComicFilePath = _comicPath,
                        PageNumber = _pageNumber,
                        Type = AnnotationType.Text,
                        TextContent = textBox.Text,
                        X = position.X / AnnotationCanvas.Width,
                        Y = position.Y / AnnotationCanvas.Height,
                        Color = (ColorPicker.SelectedItem as ComboBoxItem)?.Tag as string ?? "#FFFF00"
                    };
                    _annotationManager.AddAnnotation(annotation);
                    LoadAnnotations();
                }
                dialog.Close();
            };

            var cancelButton = new Button { Content = "Cancelar", Width = 80, Margin = new Thickness(5, 0, 5, 0) };
            cancelButton.Click += (s, e) => dialog.Close();

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;
            dialog.ShowDialog();
        }

        // Event Handlers
        private void TextAnnotation_Click(object sender, RoutedEventArgs e)
        {
            _currentTool = AnnotationType.Text;
            StatusText.Text = "Haz clic en la página para agregar una nota de texto";
        }

        private void Highlight_Click(object sender, RoutedEventArgs e)
        {
            _currentTool = AnnotationType.Highlight;
            StatusText.Text = "Arrastra para crear un área resaltada";
        }

        private void Arrow_Click(object sender, RoutedEventArgs e)
        {
            _currentTool = AnnotationType.Arrow;
            StatusText.Text = "Arrastra para dibujar una flecha";
        }

        private void Draw_Click(object sender, RoutedEventArgs e)
        {
            _currentTool = AnnotationType.FreehandDraw;
            StatusText.Text = "Dibuja libremente sobre la página";
        }

        private void Rectangle_Click(object sender, RoutedEventArgs e)
        {
            _currentTool = AnnotationType.Rectangle;
            StatusText.Text = "Arrastra para dibujar un rectángulo";
        }

        private void Circle_Click(object sender, RoutedEventArgs e)
        {
            _currentTool = AnnotationType.Circle;
            StatusText.Text = "Arrastra para dibujar un círculo";
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (AnnotationsList.SelectedItem is Annotation annotation)
            {
                _annotationManager.RemoveAnnotation(annotation.Id);
                LoadAnnotations();
            }
        }

        private void AnnotationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Resaltar anotación seleccionada en el canvas
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
