using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ComicReader.Models
{
    /// <summary>
    /// Tipos de anotaciones soportadas
    /// </summary>
    public enum AnnotationType
    {
        Text,           // Nota de texto
        Highlight,      // Resaltado de área
        Arrow,          // Flecha señalando algo
        FreehandDraw,   // Dibujo libre
        Rectangle,      // Rectángulo
        Circle          // Círculo
    }

    /// <summary>
    /// Representa una anotación sobre una página de cómic
    /// </summary>
    public class Annotation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ComicFilePath { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public AnnotationType Type { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        // Contenido de texto (para notas)
        public string? TextContent { get; set; }

        // Posición y tamaño relativos (0.0 a 1.0 respecto al tamaño de página)
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        // Color de la anotación (hex string)
        public string Color { get; set; } = "#FFFF00";

        // Grosor de línea para dibujos
        public double StrokeThickness { get; set; } = 2.0;

        // Puntos para dibujo libre (formato: "x1,y1;x2,y2;...")
        public string? DrawingPoints { get; set; }

        // Opacidad
        public double Opacity { get; set; } = 0.7;

        // Tags para organización
        public List<string> Tags { get; set; } = new List<string>();

        // Convertir color string a Color
        public Color GetColor()
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(Color);
            }
            catch
            {
                return Colors.Yellow;
            }
        }

        // Convertir DrawingPoints a lista de puntos
        public List<Point> GetDrawingPoints()
        {
            var points = new List<Point>();
            if (string.IsNullOrEmpty(DrawingPoints)) return points;

            try
            {
                var pairs = DrawingPoints.Split(';');
                foreach (var pair in pairs)
                {
                    var coords = pair.Split(',');
                    if (coords.Length == 2 &&
                        double.TryParse(coords[0], out double x) &&
                        double.TryParse(coords[1], out double y))
                    {
                        points.Add(new Point(x, y));
                    }
                }
            }
            catch { }

            return points;
        }

        // Establecer puntos de dibujo
        public void SetDrawingPoints(List<Point> points)
        {
            DrawingPoints = string.Join(";", points.Select(p => $"{p.X},{p.Y}"));
            ModifiedDate = DateTime.Now;
        }
    }

    /// <summary>
    /// Gestiona las anotaciones de los cómics
    /// </summary>
    public class AnnotationManager
    {
        private readonly string _annotationsFile;
        private List<Annotation> _annotations = new List<Annotation>();

        public AnnotationManager(string dataFolder = "")
        {
            if (string.IsNullOrEmpty(dataFolder))
            {
                dataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PercysLibrary"
                );
            }
            Directory.CreateDirectory(dataFolder);
            _annotationsFile = Path.Combine(dataFolder, "annotations.json");
            LoadAnnotations();
        }

        public void LoadAnnotations()
        {
            try
            {
                if (File.Exists(_annotationsFile))
                {
                    var json = File.ReadAllText(_annotationsFile);
                    _annotations = System.Text.Json.JsonSerializer.Deserialize<List<Annotation>>(json) 
                        ?? new List<Annotation>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading annotations: {ex.Message}");
                _annotations = new List<Annotation>();
            }
        }

        public void SaveAnnotations()
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_annotations, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_annotationsFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving annotations: {ex.Message}");
            }
        }

        public void AddAnnotation(Annotation annotation)
        {
            _annotations.Add(annotation);
            SaveAnnotations();
        }

        public void UpdateAnnotation(Annotation annotation)
        {
            var existing = _annotations.FirstOrDefault(a => a.Id == annotation.Id);
            if (existing != null)
            {
                var index = _annotations.IndexOf(existing);
                annotation.ModifiedDate = DateTime.Now;
                _annotations[index] = annotation;
                SaveAnnotations();
            }
        }

        public void RemoveAnnotation(Guid annotationId)
        {
            _annotations.RemoveAll(a => a.Id == annotationId);
            SaveAnnotations();
        }

        public List<Annotation> GetAnnotations(string comicFilePath, int pageNumber)
        {
            return _annotations
                .Where(a => a.ComicFilePath == comicFilePath && a.PageNumber == pageNumber)
                .ToList();
        }

        public List<Annotation> GetAllAnnotations(string comicFilePath)
        {
            return _annotations
                .Where(a => a.ComicFilePath == comicFilePath)
                .OrderBy(a => a.PageNumber)
                .ToList();
        }

        public int GetAnnotationCount(string comicFilePath)
        {
            return _annotations.Count(a => a.ComicFilePath == comicFilePath);
        }

        public List<Annotation> SearchAnnotations(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText)) return new List<Annotation>();

            return _annotations
                .Where(a => 
                    (a.TextContent?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    a.Tags.Any(t => t.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
    }
}
