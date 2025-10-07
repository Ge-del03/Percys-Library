using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace ComicReader.Models
{
    /// <summary>
    /// Tema personalizado del usuario
    /// </summary>
    public class CustomTheme
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "Mi Tema";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        // Colores principales
        public string PrimaryColor { get; set; } = "#3498db";
        public string SecondaryColor { get; set; } = "#2ecc71";
        public string AccentColor { get; set; } = "#e74c3c";
        public string BackgroundColor { get; set; } = "#1e1e1e";
        public string SurfaceColor { get; set; } = "#2d2d30";
        public string TextPrimaryColor { get; set; } = "#ffffff";
        public string TextSecondaryColor { get; set; } = "#cccccc";

        // Tipografía
        public string FontFamily { get; set; } = "Segoe UI";
        public double FontSize { get; set; } = 14;
        public bool BoldTitles { get; set; } = true;

        // Espaciado y bordes
        public double BorderRadius { get; set; } = 5;
        public double Padding { get; set; } = 10;
        public double Spacing { get; set; } = 5;

        // Efectos
        public bool UseAnimations { get; set; } = true;
        public bool UseShadows { get; set; } = true;
        public double Opacity { get; set; } = 1.0;
        public double BlurRadius { get; set; } = 10;

        // Configuración del lector
        public string ReaderBackground { get; set; } = "#000000";
        public double ReaderOpacity { get; set; } = 1.0;
        public bool ShowPageNumbers { get; set; } = true;
        public bool ShowThumbnails { get; set; } = true;

        // Conversión a Color
        public Color GetPrimaryColor() => (Color)ColorConverter.ConvertFromString(PrimaryColor);
        public Color GetSecondaryColor() => (Color)ColorConverter.ConvertFromString(SecondaryColor);
        public Color GetAccentColor() => (Color)ColorConverter.ConvertFromString(AccentColor);
        public Color GetBackgroundColor() => (Color)ColorConverter.ConvertFromString(BackgroundColor);
        public Color GetSurfaceColor() => (Color)ColorConverter.ConvertFromString(SurfaceColor);
        public Color GetTextPrimaryColor() => (Color)ColorConverter.ConvertFromString(TextPrimaryColor);
        public Color GetTextSecondaryColor() => (Color)ColorConverter.ConvertFromString(TextSecondaryColor);
        public Color GetReaderBackground() => (Color)ColorConverter.ConvertFromString(ReaderBackground);
    }

    /// <summary>
    /// Gestor de temas personalizados
    /// </summary>
    public class CustomThemeManager
    {
        private readonly string _themesFile;
        private List<CustomTheme> _themes = new List<CustomTheme>();
        private CustomTheme? _currentTheme;

        public event EventHandler<CustomTheme>? ThemeChanged;

        public CustomThemeManager(string dataFolder = "")
        {
            if (string.IsNullOrEmpty(dataFolder))
            {
                dataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PercysLibrary"
                );
            }
            Directory.CreateDirectory(dataFolder);
            _themesFile = Path.Combine(dataFolder, "custom_themes.json");
            LoadThemes();
            InitializeDefaultThemes();
        }

        private void InitializeDefaultThemes()
        {
            if (!_themes.Any())
            {
                // Tema Oscuro Clásico
                _themes.Add(new CustomTheme
                {
                    Name = "Oscuro Clásico",
                    PrimaryColor = "#3498db",
                    SecondaryColor = "#2ecc71",
                    AccentColor = "#e74c3c",
                    BackgroundColor = "#1e1e1e",
                    SurfaceColor = "#2d2d30"
                });

                // Tema Claro
                _themes.Add(new CustomTheme
                {
                    Name = "Claro",
                    PrimaryColor = "#2196f3",
                    SecondaryColor = "#4caf50",
                    AccentColor = "#ff5722",
                    BackgroundColor = "#ffffff",
                    SurfaceColor = "#f5f5f5",
                    TextPrimaryColor = "#000000",
                    TextSecondaryColor = "#666666"
                });

                // Tema Dracula
                _themes.Add(new CustomTheme
                {
                    Name = "Dracula",
                    PrimaryColor = "#bd93f9",
                    SecondaryColor = "#50fa7b",
                    AccentColor = "#ff79c6",
                    BackgroundColor = "#282a36",
                    SurfaceColor = "#44475a",
                    TextPrimaryColor = "#f8f8f2",
                    TextSecondaryColor = "#6272a4"
                });

                // Tema Nord
                _themes.Add(new CustomTheme
                {
                    Name = "Nord",
                    PrimaryColor = "#88c0d0",
                    SecondaryColor = "#a3be8c",
                    AccentColor = "#bf616a",
                    BackgroundColor = "#2e3440",
                    SurfaceColor = "#3b4252",
                    TextPrimaryColor = "#eceff4",
                    TextSecondaryColor = "#d8dee9"
                });

                // Tema Cyberpunk
                _themes.Add(new CustomTheme
                {
                    Name = "Cyberpunk",
                    PrimaryColor = "#00ffff",
                    SecondaryColor = "#ff00ff",
                    AccentColor = "#ffff00",
                    BackgroundColor = "#0a0a0a",
                    SurfaceColor = "#1a1a1a",
                    TextPrimaryColor = "#00ff00",
                    TextSecondaryColor = "#00cc00",
                    UseShadows = true,
                    BlurRadius = 15
                });

                // Tema Sepia (Lectura)
                _themes.Add(new CustomTheme
                {
                    Name = "Sepia",
                    PrimaryColor = "#8b4513",
                    SecondaryColor = "#cd853f",
                    AccentColor = "#d2691e",
                    BackgroundColor = "#f4ecd8",
                    SurfaceColor = "#e8dcc0",
                    TextPrimaryColor = "#3e2723",
                    TextSecondaryColor = "#5d4037",
                    ReaderBackground = "#f4ecd8"
                });

                SaveThemes();
            }
        }

        public void LoadThemes()
        {
            try
            {
                if (File.Exists(_themesFile))
                {
                    var json = File.ReadAllText(_themesFile);
                    _themes = System.Text.Json.JsonSerializer.Deserialize<List<CustomTheme>>(json)
                        ?? new List<CustomTheme>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading themes: {ex.Message}");
                _themes = new List<CustomTheme>();
            }
        }

        public void SaveThemes()
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_themes, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_themesFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving themes: {ex.Message}");
            }
        }

        public CustomTheme CreateTheme(string name)
        {
            var theme = new CustomTheme { Name = name };
            _themes.Add(theme);
            SaveThemes();
            return theme;
        }

        public void UpdateTheme(CustomTheme theme)
        {
            var existing = _themes.FirstOrDefault(t => t.Id == theme.Id);
            if (existing != null)
            {
                var index = _themes.IndexOf(existing);
                theme.ModifiedDate = DateTime.Now;
                _themes[index] = theme;
                SaveThemes();

                if (_currentTheme?.Id == theme.Id)
                {
                    ApplyTheme(theme);
                }
            }
        }

        public void DeleteTheme(Guid themeId)
        {
            _themes.RemoveAll(t => t.Id == themeId);
            SaveThemes();
        }

        public List<CustomTheme> GetAllThemes()
        {
            return _themes.ToList();
        }

        public CustomTheme? GetTheme(Guid themeId)
        {
            return _themes.FirstOrDefault(t => t.Id == themeId);
        }

        public CustomTheme? GetThemeByName(string name)
        {
            return _themes.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public void ApplyTheme(CustomTheme theme)
        {
            _currentTheme = theme;
            ThemeChanged?.Invoke(this, theme);
        }

        public CustomTheme? GetCurrentTheme()
        {
            return _currentTheme;
        }

        public CustomTheme DuplicateTheme(Guid themeId, string newName)
        {
            var original = GetTheme(themeId);
            if (original == null)
                throw new ArgumentException("Theme not found");

            var duplicate = new CustomTheme
            {
                Name = newName,
                PrimaryColor = original.PrimaryColor,
                SecondaryColor = original.SecondaryColor,
                AccentColor = original.AccentColor,
                BackgroundColor = original.BackgroundColor,
                SurfaceColor = original.SurfaceColor,
                TextPrimaryColor = original.TextPrimaryColor,
                TextSecondaryColor = original.TextSecondaryColor,
                FontFamily = original.FontFamily,
                FontSize = original.FontSize,
                BoldTitles = original.BoldTitles,
                BorderRadius = original.BorderRadius,
                Padding = original.Padding,
                Spacing = original.Spacing,
                UseAnimations = original.UseAnimations,
                UseShadows = original.UseShadows,
                Opacity = original.Opacity,
                BlurRadius = original.BlurRadius,
                ReaderBackground = original.ReaderBackground,
                ReaderOpacity = original.ReaderOpacity,
                ShowPageNumbers = original.ShowPageNumbers,
                ShowThumbnails = original.ShowThumbnails
            };

            _themes.Add(duplicate);
            SaveThemes();
            return duplicate;
        }

        public void ExportTheme(Guid themeId, string filePath)
        {
            var theme = GetTheme(themeId);
            if (theme != null)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(theme, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(filePath, json);
            }
        }

        public CustomTheme? ImportTheme(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var theme = System.Text.Json.JsonSerializer.Deserialize<CustomTheme>(json);
                if (theme != null)
                {
                    theme.Id = Guid.NewGuid(); // Nuevo ID para evitar conflictos
                    theme.CreatedDate = DateTime.Now;
                    theme.ModifiedDate = DateTime.Now;
                    _themes.Add(theme);
                    SaveThemes();
                    return theme;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error importing theme: {ex.Message}");
            }
            return null;
        }

        public List<string> GetColorSchemePresets()
        {
            return new List<string>
            {
                "Material Design",
                "Flat UI",
                "Solarized Dark",
                "Solarized Light",
                "Monokai",
                "Gruvbox",
                "One Dark",
                "Ayu",
                "Tokyo Night",
                "Catppuccin"
            };
        }

        public void ApplyColorScheme(CustomTheme theme, string schemeName)
        {
            switch (schemeName)
            {
                case "Solarized Dark":
                    theme.BackgroundColor = "#002b36";
                    theme.SurfaceColor = "#073642";
                    theme.PrimaryColor = "#268bd2";
                    theme.SecondaryColor = "#859900";
                    theme.AccentColor = "#dc322f";
                    theme.TextPrimaryColor = "#fdf6e3";
                    theme.TextSecondaryColor = "#93a1a1";
                    break;

                case "Monokai":
                    theme.BackgroundColor = "#272822";
                    theme.SurfaceColor = "#3e3d32";
                    theme.PrimaryColor = "#66d9ef";
                    theme.SecondaryColor = "#a6e22e";
                    theme.AccentColor = "#f92672";
                    theme.TextPrimaryColor = "#f8f8f2";
                    theme.TextSecondaryColor = "#75715e";
                    break;

                // Agregar más esquemas aquí
            }

            UpdateTheme(theme);
        }
    }
}
