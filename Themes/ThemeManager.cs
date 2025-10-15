using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using ComicReader.Services;

namespace ComicReader.Themes
{
    public static class ThemeManager
    {
        private static readonly Dictionary<ThemeMode, ResourceDictionary> _themes = 
            new Dictionary<ThemeMode, ResourceDictionary>();
        
        private static ThemeMode _currentTheme = ThemeMode.Comic;
        public static event Action<ThemeMode> ThemeChanged;

        static ThemeManager()
        {
            LoadAllThemes();
        }

        public static ThemeMode CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    ApplyTheme(value);
                    ThemeChanged?.Invoke(value);
                }
            }
        }

        private static void LoadAllThemes()
        {
            _themes[ThemeMode.Light] = CreateLightTheme();
            _themes[ThemeMode.Dark] = CreateDarkTheme();
            _themes[ThemeMode.Comic] = CreateComicTheme();
            _themes[ThemeMode.Sepia] = CreateSepiaTheme();
            _themes[ThemeMode.HighContrast] = CreateHighContrastTheme();
        }

        public static void ApplyTheme(ThemeMode theme)
        {
            if (_themes.TryGetValue(theme, out var themeResource))
            {
                var app = Application.Current;
                if (app?.Resources != null)
                {
                    // Limpiar recursos de tema anteriores
                    var resourcesToRemove = new List<object>();
                    foreach (var key in app.Resources.Keys)
                    {
                        if (key.ToString().Contains("Brush") || key.ToString().Contains("Color"))
                        {
                            resourcesToRemove.Add(key);
                        }
                    }
                    
                    foreach (var key in resourcesToRemove)
                    {
                        app.Resources.Remove(key);
                    }

                    // Aplicar nuevo tema
                    foreach (var key in themeResource.Keys)
                    {
                        app.Resources[key] = themeResource[key];
                    }
                }
            }
        }

        private static ResourceDictionary CreateLightTheme()
        {
            var theme = new ResourceDictionary();

            // Colores primarios
            theme["PrimaryColor"] = Color.FromRgb(33, 150, 243);
            theme["SecondaryColor"] = Color.FromRgb(255, 193, 7);
            theme["AccentColor"] = Color.FromRgb(76, 175, 80);
            theme["ErrorColor"] = Color.FromRgb(244, 67, 54);

            // Colores de fondo
            theme["WindowBackgroundColor"] = Color.FromRgb(250, 250, 250);
            theme["PanelBackgroundColor"] = Color.FromRgb(255, 255, 255);
            theme["HeaderBackgroundColor"] = Color.FromRgb(240, 240, 240);
            theme["InputBackgroundColor"] = Color.FromRgb(248, 248, 248);

            // Colores de texto
            theme["TextColor"] = Color.FromRgb(33, 33, 33);
            theme["SecondaryTextColor"] = Color.FromRgb(117, 117, 117);
            theme["DisabledTextColor"] = Color.FromRgb(189, 189, 189);

            // Colores de interacción
            theme["BorderColor"] = Color.FromRgb(224, 224, 224);
            theme["ItemHoverColor"] = Color.FromRgb(245, 245, 245);
            theme["ItemSelectedColor"] = Color.FromRgb(227, 242, 253);
            theme["CurrentPageColor"] = Color.FromRgb(255, 249, 196);

            // Crear pinceles
            CreateBrushesFromColors(theme);
            return theme;
        }

        private static ResourceDictionary CreateDarkTheme()
        {
            var theme = new ResourceDictionary();

            // Colores primarios
            theme["PrimaryColor"] = Color.FromRgb(100, 181, 246);
            theme["SecondaryColor"] = Color.FromRgb(255, 213, 79);
            theme["AccentColor"] = Color.FromRgb(129, 199, 132);
            theme["ErrorColor"] = Color.FromRgb(239, 83, 80);

            // Colores de fondo
            theme["WindowBackgroundColor"] = Color.FromRgb(18, 18, 18);
            theme["PanelBackgroundColor"] = Color.FromRgb(32, 32, 32);
            theme["HeaderBackgroundColor"] = Color.FromRgb(48, 48, 48);
            theme["InputBackgroundColor"] = Color.FromRgb(42, 42, 42);

            // Colores de texto
            theme["TextColor"] = Color.FromRgb(255, 255, 255);
            theme["SecondaryTextColor"] = Color.FromRgb(170, 170, 170);
            theme["DisabledTextColor"] = Color.FromRgb(120, 120, 120);

            // Colores de interacción
            theme["BorderColor"] = Color.FromRgb(80, 80, 80);
            theme["ItemHoverColor"] = Color.FromRgb(55, 55, 55);
            theme["ItemSelectedColor"] = Color.FromRgb(25, 118, 210);
            theme["CurrentPageColor"] = Color.FromRgb(102, 187, 106);

            CreateBrushesFromColors(theme);
            return theme;
        }

        private static ResourceDictionary CreateComicTheme()
        {
            var theme = new ResourceDictionary();

            // Colores vibrantes inspirados en cómics
            theme["PrimaryColor"] = Color.FromRgb(255, 87, 34);  // Naranja vibrante
            theme["SecondaryColor"] = Color.FromRgb(156, 39, 176); // Púrpura
            theme["AccentColor"] = Color.FromRgb(0, 188, 212);   // Cyan
            theme["ErrorColor"] = Color.FromRgb(244, 67, 54);

            // Gradientes de fondo (simulando páginas de cómic)
            var windowBrush = new LinearGradientBrush();
            windowBrush.GradientStops.Add(new GradientStop(Color.FromRgb(63, 81, 181), 0.0));
            windowBrush.GradientStops.Add(new GradientStop(Color.FromRgb(156, 39, 176), 1.0));
            theme["WindowBackgroundBrush"] = windowBrush;

            var panelBrush = new LinearGradientBrush();
            panelBrush.GradientStops.Add(new GradientStop(Color.FromRgb(240, 240, 255), 0.0));
            panelBrush.GradientStops.Add(new GradientStop(Color.FromRgb(250, 240, 255), 1.0));
            theme["PanelBackgroundBrush"] = panelBrush;

            theme["HeaderBackgroundColor"] = Color.FromRgb(33, 33, 33);
            theme["InputBackgroundColor"] = Color.FromRgb(255, 255, 255);

            // Colores de texto con estilo cómic
            theme["TextColor"] = Color.FromRgb(33, 33, 33);
            theme["SecondaryTextColor"] = Color.FromRgb(96, 125, 139);
            theme["DisabledTextColor"] = Color.FromRgb(176, 190, 197);

            // Colores de interacción con estilo pop
            theme["BorderColor"] = Color.FromRgb(255, 87, 34);
            theme["ItemHoverColor"] = Color.FromRgb(255, 241, 118);
            theme["ItemSelectedColor"] = Color.FromRgb(255, 213, 79);
            theme["CurrentPageColor"] = Color.FromRgb(129, 199, 132);

            // Colores especiales para el tema cómic
            theme["BookmarkColor"] = Color.FromRgb(255, 152, 0);
            theme["CurrentPageBorderColor"] = Color.FromRgb(255, 235, 59);

            CreateBrushesFromColors(theme);
            return theme;
        }

        private static ResourceDictionary CreateSepiaTheme()
        {
            var theme = new ResourceDictionary();

            // Paleta sepia clásica
            theme["PrimaryColor"] = Color.FromRgb(139, 69, 19);   // Sepia oscuro
            theme["SecondaryColor"] = Color.FromRgb(210, 180, 140); // Tan
            theme["AccentColor"] = Color.FromRgb(160, 82, 45);    // Marrón rojizo
            theme["ErrorColor"] = Color.FromRgb(178, 34, 34);

            // Fondos en tonos sepia
            theme["WindowBackgroundColor"] = Color.FromRgb(245, 235, 220);
            theme["PanelBackgroundColor"] = Color.FromRgb(250, 240, 230);
            theme["HeaderBackgroundColor"] = Color.FromRgb(222, 184, 135);
            theme["InputBackgroundColor"] = Color.FromRgb(255, 248, 240);

            // Textos en sepia
            theme["TextColor"] = Color.FromRgb(101, 67, 33);
            theme["SecondaryTextColor"] = Color.FromRgb(139, 119, 101);
            theme["DisabledTextColor"] = Color.FromRgb(188, 143, 143);

            // Interacciones en sepia
            theme["BorderColor"] = Color.FromRgb(205, 133, 63);
            theme["ItemHoverColor"] = Color.FromRgb(240, 230, 215);
            theme["ItemSelectedColor"] = Color.FromRgb(222, 184, 135);
            theme["CurrentPageColor"] = Color.FromRgb(255, 228, 181);

            CreateBrushesFromColors(theme);
            return theme;
        }

        private static ResourceDictionary CreateHighContrastTheme()
        {
            var theme = new ResourceDictionary();

            // Alto contraste para accesibilidad
            theme["PrimaryColor"] = Color.FromRgb(255, 255, 0);    // Amarillo brillante
            theme["SecondaryColor"] = Color.FromRgb(0, 255, 255);  // Cyan brillante
            theme["AccentColor"] = Color.FromRgb(255, 0, 255);     // Magenta
            theme["ErrorColor"] = Color.FromRgb(255, 0, 0);

            // Fondos con máximo contraste
            theme["WindowBackgroundColor"] = Color.FromRgb(0, 0, 0);
            theme["PanelBackgroundColor"] = Color.FromRgb(16, 16, 16);
            theme["HeaderBackgroundColor"] = Color.FromRgb(32, 32, 32);
            theme["InputBackgroundColor"] = Color.FromRgb(48, 48, 48);

            // Textos con máximo contraste
            theme["TextColor"] = Color.FromRgb(255, 255, 255);
            theme["SecondaryTextColor"] = Color.FromRgb(255, 255, 0);
            theme["DisabledTextColor"] = Color.FromRgb(128, 128, 128);

            // Interacciones con alto contraste
            theme["BorderColor"] = Color.FromRgb(255, 255, 255);
            theme["ItemHoverColor"] = Color.FromRgb(64, 64, 64);
            theme["ItemSelectedColor"] = Color.FromRgb(0, 0, 255);
            theme["CurrentPageColor"] = Color.FromRgb(255, 255, 0);

            CreateBrushesFromColors(theme);
            return theme;
        }

        private static void CreateBrushesFromColors(ResourceDictionary theme)
        {
            var colorsToProcess = new List<string>();
            
            foreach (var key in theme.Keys)
            {
                if (key.ToString().EndsWith("Color"))
                {
                    colorsToProcess.Add(key.ToString());
                }
            }

            foreach (var colorKey in colorsToProcess)
            {
                if (theme[colorKey] is Color color)
                {
                    var brushKey = colorKey.Replace("Color", "Brush");
                    theme[brushKey] = new SolidColorBrush(color);

                    // Crear versiones con transparencia
                    var hoverBrushKey = colorKey.Replace("Color", "HoverBrush");
                    var hoverColor = Color.FromArgb(200, color.R, color.G, color.B);
                    theme[hoverBrushKey] = new SolidColorBrush(hoverColor);

                    var pressedBrushKey = colorKey.Replace("Color", "PressedBrush");
                    var pressedColor = Color.FromArgb(150, color.R, color.G, color.B);
                    theme[pressedBrushKey] = new SolidColorBrush(pressedColor);
                }
            }

            // Crear pincel especial para overlay
            var overlayColor = Color.FromArgb(128, 0, 0, 0);
            theme["OverlayBackgroundBrush"] = new SolidColorBrush(overlayColor);

            // Pincel para elementos deshabilitados
            var disabledColor = Color.FromArgb(100, 128, 128, 128);
            theme["DisabledBrush"] = new SolidColorBrush(disabledColor);
        }

        public static void SaveCurrentTheme()
        {
            try
            {
                var settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PercysLibrary");
                Directory.CreateDirectory(settingsDir);
                
                var themePath = Path.Combine(settingsDir, "CurrentTheme.txt");
                File.WriteAllText(themePath, CurrentTheme.ToString());
            }
            catch (Exception ex)
            {
                Logger.Log($"Error saving theme: {ex.Message}");
            }
        }

        public static void LoadSavedTheme()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var newDir = Path.Combine(appData, "PercysLibrary");
                var newPath = Path.Combine(newDir, "CurrentTheme.txt");
                var oldPath = Path.Combine(appData, "ComicReader", "CurrentTheme.txt");

                string themeText = null;
                if (File.Exists(newPath))
                {
                    themeText = File.ReadAllText(newPath);
                }
                else if (File.Exists(oldPath))
                {
                    themeText = File.ReadAllText(oldPath);
                    try { Directory.CreateDirectory(newDir); File.Copy(oldPath, newPath, overwrite: true); } catch { }
                }

                if (!string.IsNullOrWhiteSpace(themeText) && Enum.TryParse<ThemeMode>(themeText, out var savedTheme))
                {
                    CurrentTheme = savedTheme;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading saved theme: {ex.Message}");
            }
        }

        public static List<ThemeInfo> GetAvailableThemes()
        {
            return new List<ThemeInfo>
            {
                new ThemeInfo(ThemeMode.Light, "Claro", "Tema claro y limpio para lectura diurna"),
                new ThemeInfo(ThemeMode.Dark, "Oscuro", "Tema oscuro para lectura nocturna"),
                new ThemeInfo(ThemeMode.Comic, "Cómic", "Tema colorido inspirado en cómics clásicos"),
                new ThemeInfo(ThemeMode.Sepia, "Sepia", "Tema vintage con tonos sepia"),
                new ThemeInfo(ThemeMode.HighContrast, "Alto Contraste", "Tema de alto contraste para accesibilidad")
            };
        }
    }

    public class ThemeInfo
    {
        public ThemeMode Mode { get; }
        public string Name { get; }
        public string Description { get; }

        public ThemeInfo(ThemeMode mode, string name, string description)
        {
            Mode = mode;
            Name = name;
            Description = description;
        }
    }
}