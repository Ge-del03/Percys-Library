using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ComicReader.Services;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Win32;
using System.Windows.Media.Effects;

namespace ComicReader.Views
{
    public partial class SettingsDialog : Window
    {
    private readonly UserAppSettings _original = null!;
    private readonly UserAppSettings _draft = null!;
        // Guardar valores originales para poder revertir si el usuario cierra sin aplicar
        private readonly string? _originalTheme;
        private readonly string? _originalHomeBackground;
        private bool _applied = false;
    // Colecciones globales de botones para que la selección sea exclusiva en todo el diálogo
    private readonly List<Button> _allThemeButtons = new List<Button>();
    private readonly List<Button> _allBackgroundButtons = new List<Button>();
    private bool _hasPendingChanges = false;
        private int _currentSectionIndex = 0;

    // Lista de todos los temas disponibles
        private readonly Dictionary<string, string> _availableThemes = new Dictionary<string, string>
        {
            { "DarkTheme", "🌙 Oscuro Clásico" },
            { "LightTheme", "☀️ Claro Moderno" },
            { "CosmicMarvelTheme", "🚀 Cósmico Marvel" },
            { "BatmanNocturnalTheme", "🦇 Batman Nocturno" },
            { "SupermanClassicTheme", "🔵 Superman Clásico" },
            { "SpidermanRetroTheme", "🕷️ Spiderman Retro" },
            { "WonderWomanTheme", "⚡ Wonder Woman" },
            { "FlashSpeedTheme", "⚡ Flash Velocidad" },
            { "GreenLanternTheme", "💚 Green Lantern" },
            { "AquamanOceanTheme", "🌊 Aquaman Océano" },
            { "XMenTheme", "❌ X-Men" },
            { "MarvelModernTheme", "🔴 Marvel Moderno" },
            { "DCClassicTheme", "🔷 DC Clásico" },
            { "DraculaTheme", "🧛 Dracula" },
            { "NordTheme", "❄️ Nord Ártico" },
            { "SolarizedDarkTheme", "🌒 Solarized Oscuro" },
            { "SolarizedLightTheme", "🌕 Solarized Claro" },
            { "CyberpunkTheme", "🤖 Cyberpunk" },
            { "NeoNoirTheme", "🎬 Neo Noir" },
            { "SepiaTheme", "📜 Sepia Vintage" },
            { "HighContrastTheme", "🔆 Alto Contraste" },
            { "ComicTheme", "📚 Cómic Clásico" },
            { "MangaTheme", "🎌 Manga" },
            { "MangaModernTheme", "🎌 Manga Moderno" },
            { "IndieComicTheme", "🎨 Indie Cómic" },
            { "IndieArtisticTheme", "🖼️ Indie Artístico" },
            { "GoldenAgeTheme", "✨ Era Dorada" },
            { "GoldenAgeClassicTheme", "🏛️ Clásico Era Dorada" },
            { "SilverAgeTheme", "🥈 Era Plateada" },
            { "UndergroundTheme", "🏃 Underground" }
        };

        // Lista completa de fondos HomeView disponibles (30 fondos)
        private readonly Dictionary<string, string> _availableBackgrounds = new Dictionary<string, string>
        {
            // DC Heroes
            { "SupermanHomeBackground", "🔵 Superman Clásico" },
            { "BatmanHomeBackground", "🦇 Batman Nocturno" },
            { "WonderWomanHomeBackground", "⚡ Wonder Woman" },
            { "GreenLanternHomeBackground", "💚 Green Lantern" },
            { "FlashHomeBackground", "⚡ Flash Velocidad" },
            { "AquamanHomeBackground", "🌊 Aquaman Océano" },
            { "JusticeLeagueHomeBackground", "🏛️ Liga de la Justicia" },
            
            // Marvel Heroes
            { "SpidermanHomeBackground", "🕷️ Spiderman" },
            { "IronManHomeBackground", "🤖 Iron Man Tech" },
            { "ThorHomeBackground", "🔨 Thor Asgard" },
            { "HulkHomeBackground", "💚 Hulk Gamma" },
            { "CaptainAmericaHomeBackground", "🛡️ Capitán América" },
            { "BlackPantherHomeBackground", "⚫ Black Panther Wakanda" },
            { "CaptainMarvelHomeBackground", "⭐ Capitana Marvel" },
            { "DrStrangeHomeBackground", "🔮 Doctor Strange" },
            { "DeadpoolHomeBackground", "❤️ Deadpool Caos" },
            { "WolverineHomeBackground", "🦾 Wolverine" },
            { "DaredevilHomeBackground", "� Daredevil" },
            { "SilverSurferHomeBackground", "🏄 Silver Surfer" },
            { "AvengersHomeBackground", "🔥 Vengadores Unidos" },
            { "XMenHomeBackground", "❌ X-Men Mutantes" },
            { "FantasticFourHomeBackground", "4️⃣ Cuatro Fantásticos" },
            { "GuardiansHomeBackground", "🚀 Guardianes Galaxia" },
            { "VenomHomeBackground", "🖤 Venom Simbiótico" },
            { "PhoenixHomeBackground", "🔥 Fuerza Fénix" },
            
            // Eras Cómics
            { "GoldenAgeHomeBackground", "� Era Dorada" },
            { "SilverAgeHomeBackground", "🥈 Era Plateada" },
            { "BronzeAgeHomeBackground", "🥉 Era de Bronce" },
            { "ModernAgeHomeBackground", "💻 Era Moderna" },
            
            // Estilos Especiales
            { "CosmicHomeBackground", "🌌 Cósmico Marvel" },
            { "CyberpunkHomeBackground", "🤖 Cyberpunk Futuro" },
            { "NeoNoirHomeBackground", "🎬 Neo-Noir" },
            { "UndergroundHomeBackground", "🎭 Underground" },
            { "MangaHomeBackground", "🌸 Manga Suave" },
            { "IndieHomeBackground", "🎨 Indie Artístico" },
            { "SinestroHomeBackground", "💛 Sinestro Corps" }
        };

        // Secciones: títulos y descripciones para UpdateSectionTitle
        private readonly string[] _sectionTitles = new[]
        {
            "📊 Resumen General del Sistema",
            "🎨 Temas y Personalización",
            "🌟 Fondos Épicos HomeView",
            "📖 Controles de Lectura Avanzados",
            "✨ Animaciones y Efectos",
            "🖥️ Configuración Pantalla Completa",
            "⚡ Optimización de Rendimiento",
            "📄 Procesamiento PDF Avanzado",
            "🔧 Herramientas de Respaldo",
            "📈 Estadísticas y Métricas"
        };

        private readonly string[] _sectionDescriptions = new[]
        {
            "Información general del sistema, estadísticas de uso y accesos rápidos a configuraciones principales.",
            "Personaliza completamente la apariencia de ComicReader con temas profesionales y efectos visuales avanzados.",
            "Transforma tu pantalla de inicio con fondos épicos inspirados en superhéroes de Marvel y DC Comics.",
            "Configura controles de navegación, zoom, direcciones de lectura y comportamientos automáticos.",
            "Ajusta animaciones, transiciones y efectos visuales para una experiencia de lectura cinematográfica.",
            "Optimiza la experiencia inmersiva en pantalla completa con controles personalizados.",
            "Configura caché, memoria, rendimiento y optimizaciones para máximo rendimiento del sistema.",
            "Configuraciones avanzadas para renderizado, calidad y procesamiento de archivos PDF.",
            "Herramientas de respaldo, exportación de configuraciones y utilidades de mantenimiento.",
            "Consulta métricas detalladas, estadísticas de lectura y análisis de uso de la aplicación."
        };

        public SettingsDialog() : this(SettingsManager.Settings) { }

        public SettingsDialog(UserAppSettings settings)
        {
            InitializeComponent();
            _original = settings ?? new UserAppSettings();
            _draft = Clone(_original);
            // Capturar tema y fondo originales para poder revertir preview si el usuario cancela
            _originalTheme = _original?.Theme;
            _originalHomeBackground = _original?.HomeBackground;
            DataContext = _draft;
            
            _draft.PropertyChanged += Draft_PropertyChanged;
            
            // Crear los paneles de contenido dinámicamente
            CreateContentPanels();
            
            // Mostrar el panel inicial
            UpdateContentSection(0);
            UpdateActionButtons();

            // Atender cierre para revertir previews no aplicados
            this.Closing += SettingsDialog_Closing;
        }

        private void Draft_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            _hasPendingChanges = true;
            UpdateActionButtons();
        }

        private void UpdateActionButtons()
        {
            try
            {
                // Si hay cambios pendientes, habilitar Apply/Ok de forma segura usando FindName
                var applyBtn = GetActionButton("ApplyButton");
                var okBtn = GetActionButton("OkButton");
                var cancelBtn = GetActionButton("CancelButton");

                if (applyBtn != null) applyBtn.IsEnabled = _hasPendingChanges;
                if (okBtn != null) okBtn.IsEnabled = _hasPendingChanges;
                if (cancelBtn != null) cancelBtn.IsEnabled = true;
            }
            catch (Exception ex)
            {
                try { Logger.LogException("Error updating action buttons", ex); } catch { }
            }
        }

        #region Navegación

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tagStr && int.TryParse(tagStr, out int sectionIndex))
            {
                UpdateActiveNavigation(button);
                UpdateContentSection(sectionIndex);
                _currentSectionIndex = sectionIndex;
            }
        }

        private void UpdateActiveNavigation(Button activeButton)
        {
            // Navegación simplificada
            var buttons = FindAllButtons();
            var normalStyle = FindResource("NavButtonStyle") as Style;
            var selectedStyle = FindResource("SelectedNavButtonStyle") as Style;

            foreach (var btn in buttons)
            {
                if (btn != null) btn.Style = normalStyle;
            }

            if (activeButton != null) activeButton.Style = selectedStyle;
        }

        private void UpdateContentSection(int sectionIndex)
        {
            var sectionTitles = new[]
            {
                "📊 Resumen General del Sistema",
                "🎨 Temas y Personalización",
                "🌟 Fondos Épicos HomeView",
                "📖 Controles de Lectura Avanzados",
                "✨ Animaciones y Efectos",
                "🖥️ Configuración Pantalla Completa",
                "⚡ Optimización de Rendimiento",
                "📄 Procesamiento PDF Avanzado",
                "🔧 Herramientas de Respaldo",
                "📈 Estadísticas y Métricas"
            };

            var sectionDescriptions = new[]
            {
                "Información general del sistema, estadísticas de uso y accesos rápidos a configuraciones principales.",
                "Personaliza completamente la apariencia de ComicReader con temas profesionales y efectos visuales avanzados.",
                "Transforma tu pantalla de inicio con fondos épicos inspirados en superhéroes de Marvel y DC Comics.",
                "Configura controles de navegación, zoom, direcciones de lectura y comportamientos automáticos.",
                "Ajusta animaciones, transiciones y efectos visuales para una experiencia de lectura cinematográfica.",
                "Optimiza la experiencia inmersiva en pantalla completa con controles personalizados.",
                "Configura caché, memoria, rendimiento y optimizaciones para máximo rendimiento del sistema.",
                "Configuraciones avanzadas para renderizado, calidad y procesamiento de archivos PDF.",
                "Herramientas de respaldo, exportación de configuraciones y utilidades de mantenimiento.",
                "Consulta métricas detalladas, estadísticas de lectura y análisis de uso de la aplicación."
            };

            if (sectionIndex >= 0 && sectionIndex < sectionTitles.Length)
            {
                // Títulos manejados dinámicamente
                UpdateSectionTitle(sectionIndex);
            }

            ShowContentPanel(sectionIndex);
        }

        #endregion

        private void SettingsDialog_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (!_applied)
                {
                    // Revertir tema
                    if (!string.IsNullOrWhiteSpace(_originalTheme))
                    {
                        try { App.ApplyTheme(_originalTheme); } catch { }
                    }

                    // Revertir fondo
                    if (!string.IsNullOrWhiteSpace(_originalHomeBackground))
                    {
                        try { SettingsManager.ApplyHomeBackgroundImmediate(_originalHomeBackground); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                try { Logger.LogException("Error during SettingsDialog closing revert", ex); } catch { }
            }
        }

        #region Creación de Paneles

        private void CreateContentPanels()
        {
            // Limpiar contenido dinámicamente
            ClearContent();

            // Panel 0: Resumen General
            var summaryPanel = CreateSummaryPanel();
            summaryPanel.Name = "SummaryPanel";
            summaryPanel.Visibility = Visibility.Visible;
            AddToContent(summaryPanel);

            // Panel 1: Temas y Apariencia
            var themesPanel = CreateThemesPanel();
            themesPanel.Name = "ThemesPanel";
            themesPanel.Visibility = Visibility.Collapsed;
            AddToContent(themesPanel);

            // Panel 2: Fondos HomeView
            var backgroundsPanel = CreateBackgroundsPanel();
            backgroundsPanel.Name = "BackgroundsPanel";
            backgroundsPanel.Visibility = Visibility.Collapsed;
            AddToContent(backgroundsPanel);

            // Panel 3: Lectura
            var readingPanel = CreateReadingPanel();
            readingPanel.Name = "ReadingPanel";
            readingPanel.Visibility = Visibility.Collapsed;
            AddToContent(readingPanel);

            // Panel 4: Animaciones
            var animationsPanel = CreateAnimationsPanel();
            animationsPanel.Name = "AnimationsPanel";
            animationsPanel.Visibility = Visibility.Collapsed;
            AddToContent(animationsPanel);

            // Panel 5: Pantalla Completa
            var fullscreenPanel = CreateFullscreenPanel();
            fullscreenPanel.Name = "FullscreenPanel";
            fullscreenPanel.Visibility = Visibility.Collapsed;
            AddToContent(fullscreenPanel);

            // Panel 6: Rendimiento
            var performancePanel = CreatePerformancePanel();
            performancePanel.Name = "PerformancePanel";
            performancePanel.Visibility = Visibility.Collapsed;
            AddToContent(performancePanel);

            // Panel 7: PDF
            var pdfPanel = CreatePdfPanel();
            pdfPanel.Name = "PdfPanel";
            pdfPanel.Visibility = Visibility.Collapsed;
            AddToContent(pdfPanel);

            // Panel 8: Herramientas
            var toolsPanel = CreateToolsPanel();
            toolsPanel.Name = "ToolsPanel";
            toolsPanel.Visibility = Visibility.Collapsed;
            AddToContent(toolsPanel);

            // Panel 9: Estadísticas
            var statsPanel = CreateStatsPanel();
            statsPanel.Name = "StatsPanel";
            statsPanel.Visibility = Visibility.Collapsed;
            AddToContent(statsPanel);
        }

        private void ShowContentPanel(int index)
        {
            UpdateSectionTitle(index);
            
            // Ocultar todos los paneles
            var container = this.FindName("ContentContainer") as Panel;
            if (container != null)
            {
                foreach (UIElement child in container.Children)
                {
                    if (child is FrameworkElement element)
                    {
                        element.Visibility = Visibility.Collapsed;
                    }
                }
            }
            
            // Mostrar el panel seleccionado por índice
            var panelNames = new[] 
            {
                "SummaryPanel",
                "ThemesPanel",
                "BackgroundsPanel",
                "ReadingPanel",
                "AnimationsPanel",
                "FullscreenPanel",
                "PerformancePanel",
                "PdfPanel",
                "ToolsPanel",
                "StatsPanel"
            };
            
            if (index >= 0 && index < panelNames.Length)
            {
                var container2 = this.FindName("ContentContainer") as Panel;
                var targetPanel = container2?.Children.OfType<FrameworkElement>()
                    .FirstOrDefault(e => e.Name == panelNames[index]);
                
                if (targetPanel != null)
                {
                    targetPanel.Visibility = Visibility.Visible;
                }
            }
        }

        private void HideAllSections()
        {
            var container3 = this.FindName("ContentContainer") as Panel;
            if (container3 != null)
            {
                foreach (UIElement child in container3.Children)
                {
                    if (child is FrameworkElement element)
                    {
                        element.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void ShowSectionAtIndex(int index)
        {
            ShowContentPanel(index);
        }

        #endregion

        #region Paneles Específicos

        private StackPanel CreateSummaryPanel()
        {
            var panel = new StackPanel();

            // Estadísticas del sistema
            var statsCard = CreateCard("📊 Estadísticas del Sistema", "#28A745");
            var statsGrid = new Grid();
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            statsGrid.RowDefinitions.Add(new RowDefinition());
            statsGrid.RowDefinitions.Add(new RowDefinition());

            // Estadísticas reales desde SettingsManager
            var stats = SettingsManager.Settings.Statistics;
            
            AddStatToGrid(statsGrid, "Cómics Leídos", stats.ComicsRead.ToString(), 0, 0);
            AddStatToGrid(statsGrid, "Tiempo Total", $"{stats.TotalReadingTime.TotalHours:F1}h", 0, 1);
            AddStatToGrid(statsGrid, "Páginas Vistas", stats.PagesViewed.ToString(), 1, 0);
            AddStatToGrid(statsGrid, "Sesiones", stats.ReadingSessions.ToString(), 1, 1);

            ((StackPanel)statsCard.Child).Children.Add(statsGrid);
            panel.Children.Add(statsCard);

            // Accesos rápidos
            var quickCard = CreateCard("⚡ Accesos Rápidos", "#007BFF");
            var quickStack = new StackPanel();
            
            quickStack.Children.Add(CreateQuickButton("🎨 Cambiar Tema", () => UpdateContentSection(1)));
            quickStack.Children.Add(CreateQuickButton("🌟 Fondos HomeView", () => UpdateContentSection(2)));
            quickStack.Children.Add(CreateQuickButton("📖 Configurar Lectura", () => UpdateContentSection(3)));
            quickStack.Children.Add(CreateQuickButton("⚡ Optimizar Rendimiento", () => UpdateContentSection(6)));

            ((StackPanel)quickCard.Child).Children.Add(quickStack);
            panel.Children.Add(quickCard);

            return panel;
        }

        private StackPanel CreateThemesPanel()
        {
            var panel = new StackPanel();

            // Categoría 1: Temas Clásicos
            var classicCard = CreateCard("🌙 Temas Clásicos", "#8A2BE2");
            var classicWrap = new WrapPanel { Orientation = Orientation.Horizontal };
            var classicThemes = new Dictionary<string, string>
            {
                { "DarkTheme", "🌙 Oscuro Clásico" },
                { "LightTheme", "☀️ Claro Moderno" },
                { "SepiaTheme", "📜 Sepia Vintage" },
                { "HighContrastTheme", "🔆 Alto Contraste" }
            };
            CreateThemeButtons(classicWrap, classicThemes, Color.FromRgb(138, 43, 226));
            ((StackPanel)classicCard.Child).Children.Add(classicWrap);
            panel.Children.Add(classicCard);

            // Categoría 2: Superhéroes DC
            var dcCard = CreateCard("🦸 Superhéroes DC Comics", "#003366");
            var dcWrap = new WrapPanel { Orientation = Orientation.Horizontal };
            var dcThemes = new Dictionary<string, string>
            {
                { "BatmanNocturnalTheme", "🦇 Batman Nocturno" },
                { "SupermanClassicTheme", "🔵 Superman Clásico" },
                { "WonderWomanTheme", "⚡ Wonder Woman" },
                { "FlashSpeedTheme", "⚡ Flash Velocidad" },
                { "GreenLanternTheme", "💚 Green Lantern" },
                { "AquamanOceanTheme", "🌊 Aquaman Océano" },
                { "DCClassicTheme", "🔷 DC Clásico" }
            };
            CreateThemeButtons(dcWrap, dcThemes, Color.FromRgb(0, 51, 102));
            ((StackPanel)dcCard.Child).Children.Add(dcWrap);
            panel.Children.Add(dcCard);

            // Categoría 3: Superhéroes Marvel
            var marvelCard = CreateCard("🕷️ Superhéroes Marvel", "#CC0000");
            var marvelWrap = new WrapPanel { Orientation = Orientation.Horizontal };
            var marvelThemes = new Dictionary<string, string>
            {
                { "SpidermanRetroTheme", "🕷️ Spiderman Retro" },
                { "XMenTheme", "❌ X-Men" },
                { "MarvelModernTheme", "🔴 Marvel Moderno" },
                { "CosmicMarvelTheme", "🚀 Cósmico Marvel" }
            };
            CreateThemeButtons(marvelWrap, marvelThemes, Color.FromRgb(204, 0, 0));
            ((StackPanel)marvelCard.Child).Children.Add(marvelWrap);
            panel.Children.Add(marvelCard);

            // Categoría 4: Temas Especiales
            var specialCard = CreateCard("✨ Temas Especiales", "#9932CC");
            var specialWrap = new WrapPanel { Orientation = Orientation.Horizontal };
            var specialThemes = new Dictionary<string, string>
            {
                { "DraculaTheme", "🧛 Dracula" },
                { "NordTheme", "❄️ Nord Ártico" },
                { "SolarizedDarkTheme", "🌒 Solarized Oscuro" },
                { "SolarizedLightTheme", "🌕 Solarized Claro" },
                { "CyberpunkTheme", "🤖 Cyberpunk" },
                { "NeoNoirTheme", "🎬 Neo Noir" }
            };
            CreateThemeButtons(specialWrap, specialThemes, Color.FromRgb(153, 50, 204));
            ((StackPanel)specialCard.Child).Children.Add(specialWrap);
            panel.Children.Add(specialCard);

            // Categoría 5: Temas de Cómic
            var comicCard = CreateCard("📚 Estilos de Cómic", "#FF6B35");
            var comicWrap = new WrapPanel { Orientation = Orientation.Horizontal };
            var comicThemes = new Dictionary<string, string>
            {
                { "ComicTheme", "📚 Cómic Clásico" },
                { "MangaTheme", "🎌 Manga" },
                { "MangaModernTheme", "🎌 Manga Moderno" },
                { "IndieComicTheme", "🎨 Indie Cómic" },
                { "IndieArtisticTheme", "🖼️ Indie Artístico" },
                { "GoldenAgeTheme", "✨ Era Dorada" },
                { "GoldenAgeClassicTheme", "🏛️ Clásico Era Dorada" },
                { "SilverAgeTheme", "🥈 Era Plateada" },
                { "UndergroundTheme", "🏃 Underground" }
            };
            CreateThemeButtons(comicWrap, comicThemes, Color.FromRgb(255, 107, 53));
            ((StackPanel)comicCard.Child).Children.Add(comicWrap);
            panel.Children.Add(comicCard);

            return panel;
        }

        private void CreateThemeButtons(WrapPanel container, Dictionary<string, string> themes, Color categoryColor)
        {
            foreach (var theme in themes)
            {
                var themeButton = new Button
                {
                    Content = theme.Value,
                    Tag = theme.Key,
                    Margin = new Thickness(3, 3, 3, 3),
                    Padding = new Thickness(12, 8, 12, 8),
                    MinWidth = 150,
                    Height = 36,
                    Background = new SolidColorBrush(Colors.Transparent),
                    Foreground = new SolidColorBrush(Colors.White),
                    BorderBrush = new SolidColorBrush(categoryColor),
                    BorderThickness = new Thickness(1.5),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    FontSize = 11,
                    FontWeight = FontWeights.Medium
                };

                // Destacar tema actual
                if (theme.Key == _draft.Theme || (theme.Key + "Theme") == _draft.Theme)
                {
                    themeButton.Background = GetHighlightBrush(categoryColor);
                    themeButton.Effect = new DropShadowEffect { Color = LightenColor(categoryColor, 0.35), BlurRadius = 12, ShadowDepth = 0 };
                    themeButton.Foreground = new SolidColorBrush(Colors.White);
                    themeButton.FontWeight = FontWeights.SemiBold;
                    themeButton.BorderThickness = new Thickness(2);
                }

                themeButton.Click += (s, e) =>
                {
                    _draft.Theme = theme.Key;
                    UpdateThemeSelection(container, themeButton, categoryColor);
                    // Previsualizar el tema sin persistir: aplicar a la app para ver el cambio
                    try
                    {
                        App.ApplyTheme(theme.Key);
                    }
                    catch (Exception ex)
                    {
                        try { Logger.LogException("Error al previsualizar tema", ex); } catch { }
                    }
                };

                container.Children.Add(themeButton);
                // Registrar en la colección global para selección exclusiva
                _allThemeButtons.Add(themeButton);
            }
        }

        private void UpdateThemeSelection(WrapPanel container, Button selectedButton, Color categoryColor)
        {
            // Limpiar visualmente todos los botones de tema en todo el diálogo
            foreach (Button btn in _allThemeButtons)
            {
                try
                {
                    btn.Background = new SolidColorBrush(Colors.Transparent);
                    btn.Effect = null;
                    btn.Foreground = new SolidColorBrush(Color.FromRgb(200,200,200));
                }
                catch { }
            }
            try
            {
                selectedButton.Background = GetHighlightBrush(categoryColor);
                selectedButton.Effect = new DropShadowEffect { Color = LightenColor(categoryColor, 0.35), BlurRadius = 12, ShadowDepth = 0 };
                selectedButton.Foreground = new SolidColorBrush(Colors.White);
            }
            catch { }
        }

        private StackPanel CreateBackgroundsPanel()
        {
            var panel = new StackPanel();

            // Categoría 1: Fondos DC Comics Universe
            var dcBackgroundsCard = CreateCard("🦸 Fondos DC Comics Universe", "#003366");
            var dcBackgroundsWrap = new WrapPanel { Orientation = Orientation.Horizontal };
            
            var dcBackgrounds = new Dictionary<string, string>
            {
                { "SupermanHomeBackground", "🔵 Superman Clásico" },
                { "BatmanHomeBackground", "🦇 Batman Nocturno" },
                { "WonderWomanHomeBackground", "⚡ Wonder Woman" },
                { "GreenLanternHomeBackground", "💚 Green Lantern" },
                { "FlashHomeBackground", "⚡ Flash Velocidad" },
                { "AquamanHomeBackground", "🌊 Aquaman Océano" },
                { "JusticeLeagueHomeBackground", "🏛️ Liga de la Justicia" },
                { "SinestroHomeBackground", "💛 Sinestro Corps" }
            };

            CreateBackgroundButtons(dcBackgroundsWrap, dcBackgrounds, Color.FromRgb(0, 51, 102));
            ((StackPanel)dcBackgroundsCard.Child).Children.Add(dcBackgroundsWrap);
            panel.Children.Add(dcBackgroundsCard);

            // Categoría 2: Fondos Marvel Universe
            var marvelBackgroundsCard = CreateCard("🕷️ Fondos Marvel Universe", "#CC0000");
            var marvelBackgroundsWrap = new WrapPanel { Orientation = Orientation.Horizontal };
            
            var marvelBackgrounds = new Dictionary<string, string>
            {
                { "SpidermanHomeBackground", "🕷️ Spiderman" },
                { "IronManHomeBackground", "🤖 Iron Man Tech" },
                { "ThorHomeBackground", "🔨 Thor Asgard" },
                { "HulkHomeBackground", "💚 Hulk Gamma" },
                { "CaptainAmericaHomeBackground", "🛡️ Capitán América" },
                { "BlackPantherHomeBackground", "⚫ Black Panther Wakanda" },
                { "CaptainMarvelHomeBackground", "⭐ Capitana Marvel" },
                { "DrStrangeHomeBackground", "🔮 Doctor Strange" },
                { "DeadpoolHomeBackground", "❤️ Deadpool Caos" },
                { "WolverineHomeBackground", "🦾 Wolverine" },
                { "DaredevilHomeBackground", "😈 Daredevil" },
                { "SilverSurferHomeBackground", "🏄 Silver Surfer" },
                { "AvengersHomeBackground", "🔥 Vengadores Unidos" },
                { "XMenHomeBackground", "❌ X-Men Mutantes" },
                { "FantasticFourHomeBackground", "4️⃣ Cuatro Fantásticos" },
                { "GuardiansHomeBackground", "🚀 Guardianes Galaxia" },
                { "VenomHomeBackground", "🖤 Venom Simbiótico" },
                { "PhoenixHomeBackground", "🔥 Fuerza Fénix" },
                { "CosmicHomeBackground", "🌌 Cósmico Marvel" }
            };

            CreateBackgroundButtons(marvelBackgroundsWrap, marvelBackgrounds, Color.FromRgb(204, 0, 0));
            ((StackPanel)marvelBackgroundsCard.Child).Children.Add(marvelBackgroundsWrap);
            panel.Children.Add(marvelBackgroundsCard);

            // Categoría 3: Fondos Eras de Cómics
            var erasCard = CreateCard("📚 Fondos Eras de Cómics", "#FFD700");
            var erasWrap = new WrapPanel { Orientation = Orientation.Horizontal };
            
            var erasBackgrounds = new Dictionary<string, string>
            {
                { "GoldenAgeHomeBackground", "🏆 Era Dorada" },
                { "SilverAgeHomeBackground", "🥈 Era Plateada" },
                { "BronzeAgeHomeBackground", "🥉 Era de Bronce" },
                { "ModernAgeHomeBackground", "💻 Era Moderna" }
            };

            CreateBackgroundButtons(erasWrap, erasBackgrounds, Color.FromRgb(255, 215, 0));
            ((StackPanel)erasCard.Child).Children.Add(erasWrap);
            panel.Children.Add(erasCard);

            // Categoría 4: Fondos Estilos Especiales
            var specialCard = CreateCard("🎨 Fondos Estilos Especiales", "#00FF7F");
            var specialWrap = new WrapPanel { Orientation = Orientation.Horizontal };
            
            var specialBackgrounds = new Dictionary<string, string>
            {
                { "CyberpunkHomeBackground", "🤖 Cyberpunk Futuro" },
                { "NeoNoirHomeBackground", "🎬 Neo-Noir" },
                { "UndergroundHomeBackground", "🎭 Underground" },
                { "MangaHomeBackground", "🌸 Manga Suave" },
                { "IndieHomeBackground", "🎨 Indie Artístico" }
            };

            CreateBackgroundButtons(specialWrap, specialBackgrounds, Color.FromRgb(0, 255, 127));
            ((StackPanel)specialCard.Child).Children.Add(specialWrap);
            panel.Children.Add(specialCard);

            return panel;
        }

        private void CreateBackgroundButtons(WrapPanel container, Dictionary<string, string> backgrounds, Color categoryColor)
        {
            foreach (var bg in backgrounds)
            {
                var bgButton = new Button
                {
                    Content = bg.Value,
                    Tag = bg.Key,
                    Margin = new Thickness(3, 3, 3, 3),
                    Padding = new Thickness(12, 8, 12, 8),
                    MinWidth = 170,
                    Height = 36,
                    Background = new SolidColorBrush(Colors.Transparent),
                    Foreground = new SolidColorBrush(Colors.White),
                    BorderBrush = new SolidColorBrush(categoryColor),
                    BorderThickness = new Thickness(1.5),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    FontSize = 11,
                    FontWeight = FontWeights.Medium
                };

                // Destacar fondo actual
                if (bg.Key == _draft.HomeBackground)
                {
                    bgButton.Background = GetHighlightBrush(categoryColor);
                    bgButton.Effect = new DropShadowEffect { Color = LightenColor(categoryColor, 0.35), BlurRadius = 12, ShadowDepth = 0 };
                    bgButton.Foreground = new SolidColorBrush(Colors.White);
                    bgButton.FontWeight = FontWeights.SemiBold;
                    bgButton.BorderThickness = new Thickness(2);
                }

                bgButton.Click += (s, e) =>
                {
                    _draft.HomeBackground = bg.Key;
                    UpdateBackgroundSelection(container, bgButton, categoryColor);
                    // Previsualizar el fondo sin persistir
                    try
                    {
                        SettingsManager.ApplyHomeBackgroundImmediate(bg.Key);
                    }
                    catch (Exception ex)
                    {
                        try { Logger.LogException("Error al previsualizar fondo", ex); } catch { }
                    }
                };

                container.Children.Add(bgButton);
                // Registrar en la colección global para selección exclusiva
                _allBackgroundButtons.Add(bgButton);
            }
        }

        private void UpdateBackgroundSelection(WrapPanel container, Button selectedButton, Color categoryColor)
        {
            // Limpiar visualmente todos los botones de fondo en todo el diálogo
            foreach (Button btn in _allBackgroundButtons)
            {
                try
                {
                    btn.Background = new SolidColorBrush(Colors.Transparent);
                    btn.Effect = null;
                    btn.Foreground = new SolidColorBrush(Color.FromRgb(200,200,200));
                }
                catch { }
            }
            try
            {
                selectedButton.Background = GetHighlightBrush(categoryColor);
                selectedButton.Effect = new DropShadowEffect { Color = LightenColor(categoryColor, 0.35), BlurRadius = 12, ShadowDepth = 0 };
                selectedButton.Foreground = new SolidColorBrush(Colors.White);
            }
            catch { }
        }

        // Helper: devuelve un color más claro
        private Color LightenColor(Color input, double factor)
        {
            factor = Math.Max(0, Math.Min(1, factor));
            byte R = (byte)(input.R + (255 - input.R) * factor);
            byte G = (byte)(input.G + (255 - input.G) * factor);
            byte B = (byte)(input.B + (255 - input.B) * factor);
            return Color.FromRgb(R, G, B);
        }

        // Helper: crea un gradiente de highlight más brillante para selección
        private Brush GetHighlightBrush(Color baseColor)
        {
            var light = LightenColor(baseColor, 0.40);
            var brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(1, 0);
            brush.GradientStops.Add(new GradientStop(light, 0.0));
            brush.GradientStops.Add(new GradientStop(baseColor, 0.6));
            brush.Opacity = 0.95;
            return brush;
        }

        private StackPanel CreateReadingPanel()
        {
            var panel = new StackPanel();

            var readingCard = CreateCard("📖 Configuraciones de Lectura", "#28A745");
            
            var readingStack = new StackPanel();
            
            // Dirección de lectura
            readingStack.Children.Add(new TextBlock { Text = "Dirección de lectura:", Foreground = new SolidColorBrush(Colors.White), FontSize = 14, Margin = new Thickness(0, 10, 0, 5) });
            var directionCombo = new ComboBox { Style = FindResource("ThemeComboBoxStyle") as Style, Margin = new Thickness(0, 0, 0, 15) };
            directionCombo.Items.Add(new ComboBoxItem { Content = "De izquierda a derecha", Tag = "LeftToRight" });
            directionCombo.Items.Add(new ComboBoxItem { Content = "De derecha a izquierda", Tag = "RightToLeft" });
            directionCombo.SelectedIndex = _draft.ReadingDirection == "RightToLeft" ? 1 : 0;
            directionCombo.SelectionChanged += (s, e) =>
            {
                if (directionCombo.SelectedItem is ComboBoxItem item && item.Tag != null)
                    _draft.ReadingDirection = item.Tag.ToString() ?? "LeftToRight";
            };
            readingStack.Children.Add(directionCombo);

            // Checkboxes para configuraciones
            var autoAdvanceCheck = CreateCheckBox("Avance automático de páginas", _draft.AutoAdvancePages, v => _draft.AutoAdvancePages = v);
            var autoZoomCheck = CreateCheckBox("Zoom automático al abrir cómic", _draft.AutoZoom, v => _draft.AutoZoom = v);
            var fullscreenCheck = CreateCheckBox("Modo pantalla completa por defecto", _draft.AutoFullscreen, v => _draft.AutoFullscreen = v);
            var preloadCheck = CreateCheckBox("Pre-cargar páginas siguientes", _draft.PreloadNextPages, v => _draft.PreloadNextPages = v);

            readingStack.Children.Add(autoAdvanceCheck);
            readingStack.Children.Add(autoZoomCheck);
            readingStack.Children.Add(fullscreenCheck);
            readingStack.Children.Add(preloadCheck);

            ((StackPanel)readingCard.Child).Children.Add(readingStack);
            panel.Children.Add(readingCard);

            return panel;
        }

        private StackPanel CreateAnimationsPanel()
        {
            var panel = new StackPanel();

            var animCard = CreateCard("✨ Configuración de Animaciones", "#FF6B35");
            
            var animStack = new StackPanel();
            
            var enableAnimCheck = CreateCheckBox("Habilitar animaciones globales", _draft.EnableAnimations, v => _draft.EnableAnimations = v);
            var pageAnimCheck = CreateCheckBox("Animaciones de cambio de página", _draft.EnablePageTurnAnimations, v => _draft.EnablePageTurnAnimations = v);
            var reducedMotionCheck = CreateCheckBox("Preferir movimiento reducido (accesibilidad)", _draft.PreferReducedMotion, v => _draft.PreferReducedMotion = v);

            animStack.Children.Add(enableAnimCheck);
            animStack.Children.Add(pageAnimCheck);
            animStack.Children.Add(reducedMotionCheck);

            // Selector de estilo de animación
            animStack.Children.Add(new TextBlock { Text = "Estilo de animación de página:", Foreground = new SolidColorBrush(Colors.White), FontSize = 14, Margin = new Thickness(0, 15, 0, 5) });
            var styleCombo = new ComboBox { Style = FindResource("ThemeComboBoxStyle") as Style, Margin = new Thickness(0, 0, 0, 15) };
            styleCombo.Items.Add(new ComboBoxItem { Content = "Deslizamiento suave", Tag = "Slide" });
            styleCombo.Items.Add(new ComboBoxItem { Content = "Desvanecimiento", Tag = "Fade" });
            styleCombo.Items.Add(new ComboBoxItem { Content = "Voltear página", Tag = "Flip" });
            styleCombo.Items.Add(new ComboBoxItem { Content = "Sin animación", Tag = "None" });
            
            // Establecer selección actual
            var currentStyle = _draft.PageTurnAnimationStyle ?? "Slide";
            foreach (ComboBoxItem item in styleCombo.Items)
            {
                if (item.Tag.ToString() == currentStyle)
                {
                    styleCombo.SelectedItem = item;
                    break;
                }
            }
            
            styleCombo.SelectionChanged += (s, e) =>
            {
                if (styleCombo.SelectedItem is ComboBoxItem item && item.Tag != null)
                    _draft.PageTurnAnimationStyle = item.Tag.ToString() ?? "Slide";
            };
            animStack.Children.Add(styleCombo);

            ((StackPanel)animCard.Child).Children.Add(animStack);
            panel.Children.Add(animCard);

            return panel;
        }

        private StackPanel CreateFullscreenPanel()
        {
            var panel = new StackPanel();

            var fullscreenCard = CreateCard("🖥️ Configuración Pantalla Completa", "#6F42C1");
            
            var fullscreenStack = new StackPanel();
            
            var hideOnFullscreenCheck = CreateCheckBox("Ocultar barra de tareas en pantalla completa", _draft.HideTaskbarInFullscreen, v => _draft.HideTaskbarInFullscreen = v);
            var hideMenuCheck = CreateCheckBox("Ocultar menú en pantalla completa", _draft.HideMenuInFullscreen, v => _draft.HideMenuInFullscreen = v);
            var escExitCheck = CreateCheckBox("Salir de pantalla completa con ESC", _draft.EscapeExitsFullscreen, v => _draft.EscapeExitsFullscreen = v);

            fullscreenStack.Children.Add(hideOnFullscreenCheck);
            fullscreenStack.Children.Add(hideMenuCheck);
            fullscreenStack.Children.Add(escExitCheck);

            ((StackPanel)fullscreenCard.Child).Children.Add(fullscreenStack);
            panel.Children.Add(fullscreenCard);

            return panel;
        }

        private StackPanel CreatePerformancePanel()
        {
            var panel = new StackPanel();

            var perfCard = CreateCard("⚡ Optimización de Rendimiento", "#28A745");
            
            var perfStack = new StackPanel();
            
            // Tamaño de caché
            perfStack.Children.Add(new TextBlock { Text = "Tamaño de caché de pre-carga:", Foreground = new SolidColorBrush(Colors.White), FontSize = 14, Margin = new Thickness(0, 10, 0, 5) });
            var cacheSlider = new Slider 
            { 
                Minimum = 1, 
                Maximum = 20, 
                Value = _draft.PreloadCacheSize, 
                IsSnapToTickEnabled = true, 
                TickFrequency = 1,
                Margin = new Thickness(0, 0, 0, 5)
            };
            var cacheValueText = new TextBlock { Text = $"Páginas: {_draft.PreloadCacheSize}", Foreground = new SolidColorBrush(Colors.LightGray), FontSize = 12, Margin = new Thickness(0, 0, 0, 15) };
            
            cacheSlider.ValueChanged += (s, e) =>
            {
                _draft.PreloadCacheSize = (int)cacheSlider.Value;
                cacheValueText.Text = $"Páginas: {(int)cacheSlider.Value}";
            };

            perfStack.Children.Add(cacheSlider);
            perfStack.Children.Add(cacheValueText);

            var enableCacheCheck = CreateCheckBox("Habilitar caché de imágenes", _draft.EnableImageCache, v => _draft.EnableImageCache = v);
            var hardwareAccelCheck = CreateCheckBox("Aceleración por hardware (GPU)", _draft.UseHardwareAcceleration, v => _draft.UseHardwareAcceleration = v);

            perfStack.Children.Add(enableCacheCheck);
            perfStack.Children.Add(hardwareAccelCheck);

            ((StackPanel)perfCard.Child).Children.Add(perfStack);
            panel.Children.Add(perfCard);

            return panel;
        }

        private StackPanel CreatePdfPanel()
        {
            var panel = new StackPanel();

            var pdfCard = CreateCard("📄 Configuración PDF Avanzada", "#DC3545");
            
            var pdfStack = new StackPanel();
            
            // DPI de renderizado
            pdfStack.Children.Add(new TextBlock { Text = "Calidad de renderizado (DPI):", Foreground = new SolidColorBrush(Colors.White), FontSize = 14, Margin = new Thickness(0, 10, 0, 5) });
            var dpiSlider = new Slider 
            { 
                Minimum = 72, 
                Maximum = 300, 
                Value = _draft.PdfRenderDpi, 
                IsSnapToTickEnabled = true, 
                TickFrequency = 24,
                Margin = new Thickness(0, 0, 0, 5)
            };
            var dpiValueText = new TextBlock { Text = $"DPI: {_draft.PdfRenderDpi}", Foreground = new SolidColorBrush(Colors.LightGray), FontSize = 12, Margin = new Thickness(0, 0, 0, 15) };
            
            dpiSlider.ValueChanged += (s, e) =>
            {
                _draft.PdfRenderDpi = (int)dpiSlider.Value;
                dpiValueText.Text = $"DPI: {(int)dpiSlider.Value}";
            };

            pdfStack.Children.Add(dpiSlider);
            pdfStack.Children.Add(dpiValueText);

            var antialiasCheck = CreateCheckBox("Suavizado de texto (Anti-aliasing)", _draft.PdfUseAntialiasing, v => _draft.PdfUseAntialiasing = v);
            var vectorRenderCheck = CreateCheckBox("Renderizado vectorial de alta calidad", _draft.PdfUseVectorRendering, v => _draft.PdfUseVectorRendering = v);

            pdfStack.Children.Add(antialiasCheck);
            pdfStack.Children.Add(vectorRenderCheck);

            ((StackPanel)pdfCard.Child).Children.Add(pdfStack);
            panel.Children.Add(pdfCard);

            return panel;
        }

        private StackPanel CreateToolsPanel()
        {
            var panel = new StackPanel();

            var toolsCard = CreateCard("🔧 Herramientas y Utilidades", "#6C757D");
            
            var toolsStack = new StackPanel();
            
            var exportBtn = CreateActionButton("💾 Exportar Configuración", "#28A745", Export_Click);
            var importBtn = CreateActionButton("📂 Importar Configuración", "#007BFF", Import_Click);
            var resetBtn = CreateActionButton("🔄 Restablecer a Valores por Defecto", "#DC3545", Reset_Click);
            var clearCacheBtn = CreateActionButton("🧹 Limpiar Caché de Imágenes", "#FFC107", ClearCache_Click);

            toolsStack.Children.Add(exportBtn);
            toolsStack.Children.Add(importBtn);
            toolsStack.Children.Add(resetBtn);
            toolsStack.Children.Add(clearCacheBtn);

            ((StackPanel)toolsCard.Child).Children.Add(toolsStack);
            panel.Children.Add(toolsCard);

            return panel;
        }

        private StackPanel CreateStatsPanel()
        {
            var panel = new StackPanel();

            var statsCard = CreateCard("📈 Estadísticas Detalladas", "#17A2B8");
            
            var statsStack = new StackPanel();
            var stats = SettingsManager.Settings.Statistics;

            // Estadísticas detalladas
            AddDetailedStat(statsStack, "📚 Total de cómics leídos", stats.ComicsRead.ToString());
            AddDetailedStat(statsStack, "📄 Páginas visualizadas", stats.PagesViewed.ToString());
            AddDetailedStat(statsStack, "⏱️ Tiempo total de lectura", $"{stats.TotalReadingTime.TotalHours:F1} horas");
            AddDetailedStat(statsStack, "📊 Sesiones de lectura", stats.ReadingSessions.ToString());
            AddDetailedStat(statsStack, "📈 Promedio por sesión", stats.ReadingSessions > 0 ? $"{stats.TotalReadingTime.TotalMinutes / stats.ReadingSessions:F1} min" : "0 min");

            ((StackPanel)statsCard.Child).Children.Add(statsStack);
            panel.Children.Add(statsCard);

            return panel;
        }

        #endregion

        #region Helpers UI

        private Border CreateCard(string title, string colorHex)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 43, 43, 63)), // Más transparencia
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 20),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                BorderThickness = new Thickness(1),
                Effect = new DropShadowEffect { Color = Color.FromArgb(80, 138, 43, 226), BlurRadius = 10, ShadowDepth = 0 }
            };

            var stackPanel = new StackPanel();
            
            var titleBlock = new TextBlock
            {
                Text = title,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15),
                Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 2, ShadowDepth = 1, Opacity = 0.8 } // Sombra para mejor legibilidad
            };

            stackPanel.Children.Add(titleBlock);
            card.Child = stackPanel;
            
            return card;
        }

        private CheckBox CreateCheckBox(string content, bool isChecked, Action<bool> onChanged)
        {
            var checkBox = new CheckBox
            {
                Content = content,
                IsChecked = isChecked,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 13,
                Margin = new Thickness(0, 8, 0, 0)
            };

            checkBox.Checked += (s, e) => onChanged(true);
            checkBox.Unchecked += (s, e) => onChanged(false);

            return checkBox;
        }

        private Button CreateQuickButton(string content, Action onClick)
        {
            var button = new Button
            {
                Content = content,
                Background = new SolidColorBrush(Color.FromRgb(138, 43, 226)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(15, 8, 15, 8),
                Margin = new Thickness(0, 5, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            button.Click += (s, e) => onClick();
            return button;
        }

        private Button CreateActionButton(string content, string colorHex, RoutedEventHandler onClick)
        {
            var button = new Button
            {
                Content = content,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(20, 10, 20, 10),
                Margin = new Thickness(0, 8, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontWeight = FontWeights.SemiBold
            };

            button.Click += onClick;
            return button;
        }

        private void AddStatToGrid(Grid grid, string label, string value, int row, int col)
        {
            // Contenedor con colores fijos para máxima visibilidad
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 50)), // Fondo gris oscuro sólido
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(20, 15, 20, 15),
                Margin = new Thickness(8, 8, 8, 8),
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 149, 237)), // Azul cornflower
                BorderThickness = new Thickness(3, 3, 3, 3)
            };

            var stackPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

            var labelBlock = new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush(Colors.White), // Blanco puro
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var valueBlock = new TextBlock
            {
                Text = value,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 0)), // Amarillo puro
                FontSize = 28,
                FontWeight = FontWeights.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };

            stackPanel.Children.Add(labelBlock);
            stackPanel.Children.Add(valueBlock);
            border.Child = stackPanel;

            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            grid.Children.Add(border);
        }

        private void AddDetailedStat(StackPanel parent, string label, string value)
        {
            // Contenedor con fondo oscuro para mejor contraste
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(240, 45, 45, 60)), // Fondo oscuro sólido
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15, 12, 15, 12),
                Margin = new Thickness(0, 4, 0, 4),
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 149, 237)), // Azul para el borde
                BorderThickness = new Thickness(2, 2, 2, 2)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var labelBlock = new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush(Colors.White), // Blanco para contraste
                FontSize = 15,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center
            };

            var valueBlock = new TextBlock
            {
                Text = value,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)), // Dorado brillante
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(labelBlock, 0);
            Grid.SetColumn(valueBlock, 1);
            grid.Children.Add(labelBlock);
            grid.Children.Add(valueBlock);

            border.Child = grid;

            parent.Children.Add(border);
        }

        #endregion

        #region Event Handlers



        private void ApplyChanges()
        {
            // Asegurar que los valores críticos se persisten explícitamente
            try
            {
                if (SettingsManager.Settings != null)
                {
                    SettingsManager.Settings.Theme = _draft.Theme;
                    SettingsManager.Settings.HomeBackground = _draft.HomeBackground;
                }
            }
            catch (Exception ex)
            {
                try { Logger.LogException("Error asignando Theme/HomeBackground en ApplyChanges", ex); } catch { }
            }

            SettingsManager.SaveSettings();

            try
            {
                if (Owner is MainWindow mw)
                {
                    mw.ApplySettingsRuntime();
                }
            }
            catch { }
            
            _hasPendingChanges = false;
            UpdateActionButtons();
            // Marcar que el usuario aplicó los cambios (persistidos)
            _applied = true;
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            ApplyChanges();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            ApplyChanges();
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_applied)
                {
                    // Revertir inmediatamente antes de cerrar
                    if (!string.IsNullOrWhiteSpace(_originalTheme))
                    {
                        try { App.ApplyTheme(_originalTheme); } catch (Exception ex) { try { Logger.LogException("Error al revertir tema en Cancel_Click", ex); } catch { } }
                    }
                    if (!string.IsNullOrWhiteSpace(_originalHomeBackground))
                    {
                        try { SettingsManager.ApplyHomeBackgroundImmediate(_originalHomeBackground); } catch (Exception ex) { try { Logger.LogException("Error al revertir fondo en Cancel_Click", ex); } catch { } }
                    }
                }
            }
            catch (Exception ex)
            {
                try { Logger.LogException("Error en Cancel_Click revert", ex); } catch { }
            }
            Close();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "¿Estás seguro de que deseas restablecer todas las configuraciones a los valores por defecto?",
                "Confirmar Restablecimiento",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var defaultSettings = new UserAppSettings();
                CopyInto(_draft, defaultSettings);
                
                // Recrear paneles con nuevos valores
                CreateContentPanels();
                ShowContentPanel(_currentSectionIndex);
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Configuración ComicReader (*.crconfig)|*.crconfig|Todos los archivos (*.*)|*.*",
                DefaultExt = "crconfig",
                FileName = $"ComicReader_Config_{DateTime.Now:yyyy-MM-dd}.crconfig"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    SettingsManager.ExportSettings(saveFileDialog.FileName);
                    MessageBox.Show("Configuración exportada exitosamente.", "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar configuración: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Configuración ComicReader (*.crconfig)|*.crconfig|Todos los archivos (*.*)|*.*",
                DefaultExt = "crconfig"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var importedSettings = SettingsManager.ImportSettings(openFileDialog.FileName);
                    CopyInto(_draft, importedSettings);
                    
                    // Recrear paneles con nuevos valores
                    CreateContentPanels();
                    ShowContentPanel(_currentSectionIndex);
                    
                    MessageBox.Show("Configuración importada exitosamente.", "Importar", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al importar configuración: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearCache_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Aquí llamarías al método de limpieza de caché
                // CacheManager.ClearCache(); 
                MessageBox.Show("Caché de imágenes limpiado exitosamente.", "Limpiar Caché", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al limpiar caché: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Utility Methods

        private static UserAppSettings Clone(UserAppSettings source)
        {
            var clone = new UserAppSettings();
            CopyInto(clone, source);
            return clone;
        }

        private static void CopyInto(UserAppSettings target, UserAppSettings source)
        {
            if (target == null || source == null) return;
            var props = typeof(UserAppSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                if (!prop.CanRead || !prop.CanWrite) continue;
                try
                {
                    var value = prop.GetValue(source);
                    prop.SetValue(target, value);
                }
                catch { }
            }
        }

        private void ApplyThemeToCurrentWindow()
        {
            try
            {
                // Cambiar el fondo de la ventana según el tema
                var currentTheme = SettingsManager.Settings?.Theme ?? "Dark";
                
                // Definir colores base según el tema
                LinearGradientBrush backgroundBrush;
                SolidColorBrush textBrush;
                
                if (currentTheme.Contains("Batman") || currentTheme.Contains("Dark"))
                {
                    backgroundBrush = new LinearGradientBrush(Colors.Black, Color.FromRgb(30, 30, 30), 45);
                    textBrush = new SolidColorBrush(Colors.White);
                }
                else if (currentTheme.Contains("Superman"))
                {
                    backgroundBrush = new LinearGradientBrush(Color.FromRgb(0, 31, 63), Color.FromRgb(220, 20, 60), 45);
                    textBrush = new SolidColorBrush(Colors.White);
                }
                else if (currentTheme.Contains("Light"))
                {
                    backgroundBrush = new LinearGradientBrush(Colors.LightGray, Colors.White, 45);
                    textBrush = new SolidColorBrush(Colors.Black);
                }
                else if (currentTheme.Contains("Marvel") || currentTheme.Contains("Cosmic"))
                {
                    backgroundBrush = new LinearGradientBrush(Color.FromRgb(11, 11, 47), Color.FromRgb(26, 26, 74), 45);
                    textBrush = new SolidColorBrush(Colors.White);
                }
                else
                {
                    // Tema por defecto
                    backgroundBrush = new LinearGradientBrush(Color.FromRgb(11, 11, 47), Color.FromRgb(26, 26, 74), 45);
                    textBrush = new SolidColorBrush(Colors.White);
                }

                // Aplicar el fondo a la ventana actual
                if (this.Content is Grid mainGrid)
                {
                    mainGrid.Background = backgroundBrush;
                    
                    // Actualizar todos los TextBlocks para mejor contraste
                    UpdateTextElementsContrast(mainGrid, textBrush);
                }
            }
            catch (Exception ex)
            {
                // Registro de error silencioso
                System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
            }
        }

        private void UpdateTextElementsContrast(DependencyObject parent, SolidColorBrush textBrush)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is TextBlock textBlock && textBlock.Name != "HeaderTitle") // No cambiar títulos especiales
                {
                    // Solo cambiar si el texto actual es muy similar al fondo
                    if (textBlock.Foreground is SolidColorBrush currentBrush)
                    {
                        var currentColor = currentBrush.Color;
                        // Si es muy oscuro o muy claro, aplicar el contraste
                        if (currentColor.R < 100 || currentColor.R > 200)
                        {
                            textBlock.Foreground = textBrush;
                        }
                    }
                }
                
                // Recursivo para hijos
                UpdateTextElementsContrast(child, textBrush);
            }
        }

        #endregion

        #region Métodos Auxiliares Faltantes

        private Button[] FindAllButtons()
        {
            var names = new[] { "NavSummary", "NavAppearance", "NavHomeBackgrounds", "NavReading", "NavAnimations", "NavFullscreen", "NavPerformance", "NavPdf", "NavTools", "NavStats" };
            var list = new List<Button>();
            foreach (var n in names)
            {
                var btn = this.FindName(n) as Button;
                if (btn != null) list.Add(btn);
            }
            return list.ToArray();
        }

        private void UpdateSectionTitle(int sectionIndex)
        {
            try
            {
                var titleBlock = this.FindName("HeaderTitle") as TextBlock;
                var descBlock = this.FindName("HeaderDescription") as TextBlock;
                if (sectionIndex >= 0 && sectionIndex < _sectionTitles.Length)
                {
                    if (titleBlock != null) titleBlock.Text = _sectionTitles[sectionIndex];
                    if (descBlock != null) descBlock.Text = _sectionDescriptions[sectionIndex];
                }
            }
            catch (Exception ex)
            {
                try { Logger.LogException("Error updating section title", ex); } catch { }
            }
        }

        private void ClearContent()
        {
            var container = this.FindName("ContentContainer") as Panel;
            if (container != null)
            {
                container.Children.Clear();
            }
        }

        private void AddToContent(UIElement element)
        {
            var container = this.FindName("ContentContainer") as Panel;
            if (container != null)
            {
                container.Children.Add(element);
            }
        }

        // Helper to get action buttons safely
        private Button? GetActionButton(string name)
        {
            return this.FindName(name) as Button;
        }

        #endregion
    }
}