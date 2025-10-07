using System.Windows;
using System.Windows.Media;
using ComicReader.Services;
using System;
using System.Linq;
using System.Collections.Generic;
using ComicReader.Core.Services;
using ComicReader.Core.Adapters;
using ComicReader.Core.Abstractions;

namespace ComicReader
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Aplicar tema antes de crear la ventana para que todos los StaticResource estén disponibles
            try
            {
                ApplyTheme(SettingsManager.Settings.Theme);
                Logger.Log("ApplyTheme inicial OK", LogLevel.Info);
            }
            catch (Exception exInitTheme)
            {
                try { Logger.LogException("Fallo ApplyTheme inicial", exInitTheme); } catch { }
                Console.WriteLine($"Fallo ApplyTheme inicial: {exInitTheme.Message}");
            }

            // Crear y mostrar MainWindow manualmente ahora que removimos StartupUri (después de aplicar tema)
            var main = new MainWindow();
            main.Show();

            try
            {
                // Si la app se invoca con un archivo asociado, abrirlo
                if (e?.Args != null && e.Args.Length > 0)
                {
                    var path = string.Join(" ", e.Args).Trim('"');
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        var mi = typeof(MainWindow).GetMethod("OpenComicFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        mi?.Invoke(main, new object[] { path });
                    }
                }
            }
            catch (Exception ex)
            {
                try { Logger.LogException("Error al abrir archivo por asociación", ex); } catch { }
            }
        }
        public App()
        {
            try
            {
                Console.WriteLine("Iniciando aplicación...");
                
                Logger.Initialize();
                Console.WriteLine("Logger inicializado correctamente");
                
                this.DispatcherUnhandledException += App_DispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    try
                    {
                        var ex = e.ExceptionObject as Exception;
                        Logger.LogException("UnhandledException (AppDomain.CurrentDomain)", ex ?? new Exception("Unknown domain exception"));
                    }
                    catch { }
                };
                System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
                {
                    try
                    {
                        Logger.LogException("UnobservedTaskException (TaskScheduler)", e.Exception);
                        e.SetObserved();
                    }
                    catch { }
                };
                Console.WriteLine("Manejador de excepciones configurado");
                // Diagnóstico extra de recursos faltantes
                AppDomain.CurrentDomain.FirstChanceException += (s, evt) =>
                {
                    if (evt.Exception is System.Windows.Markup.XamlParseException xpe)
                    {
                        var msg = $"FirstChance XamlParseException: {xpe.Message}";
                        Console.WriteLine(msg);
                        try { Logger.Log(msg, LogLevel.Warning); } catch { }
                    }
                };
                
                // Settings se cargan en el ctor estático de SettingsManager. Evitar doble carga.
                Console.WriteLine("Configuraciones listas");

                // Registro de servicios básicos (fase inicial DI ligera)
                // DEBE ocurrir ANTES de crear cualquier ventana o componente que los use.
                ServiceLocator.RegisterSingleton<IImageCacheService>(new ImageCacheService());
                ServiceLocator.RegisterSingleton<IComicPageLoader>(new ComicPageLoader());
                ServiceLocator.RegisterSingleton<IBookmarkService>(new BookmarkServiceAdapter());
                ServiceLocator.RegisterSingleton<ISettingsService>(new SettingsServiceAdapter());
                ServiceLocator.RegisterSingleton<ILogService>(new LogServiceAdapter());
                ServiceLocator.RegisterSingleton<IImageCache>(new MultiLevelImageCache());
                ServiceLocator.RegisterSingleton<ComicReader.Core.Abstractions.IReadingStatsService>(new ReadingStatsService());
                Console.WriteLine("Servicios registrados en ServiceLocator");

                // Ya se aplicó el tema en OnStartup antes de crear la ventana.
                Console.WriteLine("Tema ya aplicado en OnStartup");
                
                // Inicializar el fondo del HomeView
                var backgroundName = SettingsManager.Settings?.HomeBackground ?? "SupermanHomeBackground";
                Console.WriteLine($"Inicializando fondo del HomeView: {backgroundName}");
                SettingsManager.ApplyHomeBackgroundImmediate(backgroundName);
                Console.WriteLine("Fondo del HomeView inicializado correctamente");
                
                // Prueba adicional: Verificar que el recurso esté disponible
                if (Application.Current.Resources.Contains("DynamicHomeBackgroundBrush"))
                {
                    Console.WriteLine("✅ DynamicHomeBackgroundBrush está disponible en recursos");
                }
                else
                {
                    Console.WriteLine("❌ DynamicHomeBackgroundBrush NO está disponible en recursos");
                }
                
                Console.WriteLine("Aplicación iniciada exitosamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error crítico durante la inicialización: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                try { Logger.LogException("Fatal init error", ex); } catch { }

                try
                {
                    MessageBox.Show($"Error crítico durante la inicialización:\n{ex.Message}\n\n{ex.StackTrace}", 
                                  "Error de Inicialización", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch 
                {
                    // Si MessageBox falla, al menos tenemos el output de consola
                }
                
                Environment.Exit(1);
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                Console.WriteLine($"Excepción no controlada: {e.Exception.Message}");
                Console.WriteLine($"Stack trace: {e.Exception.StackTrace}");
                
                // Desglosar InnerExceptions para diagnosticar recursos XAML faltantes
                Exception? inner = e.Exception.InnerException;
                int depth = 1;
                while (inner != null && depth < 10)
                {
                    Console.WriteLine($"  Inner[{depth}]: {inner.GetType().FullName}: {inner.Message}");
                    try { Logger.LogException($"InnerException depth {depth}", inner); } catch { }
                    inner = inner.InnerException;
                    depth++;
                }

                try
                {
                    Logger.LogException("Unhandled exception caught by App_DispatcherUnhandledException.", e.Exception);
                }
                catch { }
                
                MessageBox.Show($"Error inesperado:\n{e.Exception.Message}\n\n{e.Exception.StackTrace}", "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"Error en el manejador de excepciones: {logEx.Message}");
                e.Handled = true;
                Environment.Exit(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Asegurar que cualquier guardado pendiente se escriba a disco antes de salir
            SettingsManager.FlushPendingSaves();
            base.OnExit(e);
        }

        public static void ApplyTheme(string themeName)
        {
            try
            {
                // Normalizar nombre
                if (string.IsNullOrWhiteSpace(themeName)) themeName = "DarkTheme";
                var candidateCheck = themeName.EndsWith("Theme", StringComparison.OrdinalIgnoreCase) || themeName.EndsWith("Theme.xaml", StringComparison.OrdinalIgnoreCase)
                    ? themeName
                    : themeName + "Theme";

                // Evitar reaplicar el mismo tema (si la última entrada ya coincide)
                var existingTheme = Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(rd => rd.Source != null && rd.Source.OriginalString.EndsWith("Theme.xaml", StringComparison.OrdinalIgnoreCase));
                if (existingTheme != null && existingTheme.Source != null && existingTheme.Source.OriginalString.Contains(candidateCheck, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Log($"Skip ApplyTheme: '{candidateCheck}' ya activo", LogLevel.Info);
                    return;
                }
                var oldThemeDictionaries = Application.Current.Resources.MergedDictionaries
                    .Where(rd => rd.Source != null && rd.Source.OriginalString.Contains("Theme.xaml"))
                    .ToList();

                foreach (var rd in oldThemeDictionaries)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(rd);
                }

                // Permitir nombres base (Dark, Light, Comic, Sepia, HighContrast) y nuevos (Dracula, Nord, SolarizedDark, SolarizedLight)
                var candidate = themeName.EndsWith("Theme", StringComparison.OrdinalIgnoreCase) || themeName.EndsWith("Theme.xaml", StringComparison.OrdinalIgnoreCase)
                    ? themeName
                    : themeName + "Theme";
                var themeUri = new Uri($"Themes/{candidate}.xaml", UriKind.Relative);
                ResourceDictionary theme = new ResourceDictionary() { Source = themeUri };
                Application.Current.Resources.MergedDictionaries.Add(theme);

                // Aplicar colores dinámicos a los botones según el tema
                ApplyDynamicButtonColors(themeName);

                Logger.Log($"Applied theme: {themeName}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.LogException($"Failed to apply theme: {themeName}. Attempting to load fallback theme.", ex);
                try
                {
                    var fallbackThemeUri = new Uri("Themes/DarkTheme.xaml", UriKind.Relative);
                    ResourceDictionary fallbackTheme = new ResourceDictionary() { Source = fallbackThemeUri };
                    Application.Current.Resources.MergedDictionaries.Add(fallbackTheme);
                    Logger.Log("Loaded fallback theme: DarkTheme.", LogLevel.Info);
                }
                catch (Exception fallbackEx)
                {
                    Logger.LogException("Failed to load fallback theme. Application may not display correctly.", fallbackEx);
                    MessageBox.Show("Error crítico al cargar el tema. La aplicación puede no mostrarse correctamente.", "Error de Tema", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private static void ApplyDynamicButtonColors(string themeName)
        {
            try
            {
                // Normalizar nombre del tema
                string normalizedTheme = themeName.Replace("Theme", "").ToLower();

                // Definir colores de botones para cada tema (Primary, Secondary, Danger, PrimaryText, SecondaryText)
                var buttonColors = new Dictionary<string, (string, string, string, string, string)>
                {
                    // Temas DC Comics
                    { "batman", ("#FFD700", "#1A1A1A", "#8B0000", "#000000", "#FFD700") },
                    { "superman", ("#E62020", "#0052A5", "#8B0000", "#FFFFFF", "#FFFFFF") },
                    { "greenlantern", ("#00FF00", "#0C4C0C", "#8B0000", "#000000", "#FFFFFF") },
                    { "wonderwoman", ("#DC143C", "#003087", "#8B0000", "#FFFFFF", "#FFD700") },
                    { "flash", ("#FF0000", "#FFD700", "#8B0000", "#FFFFFF", "#000000") },
                    { "aquaman", ("#FF8C00", "#006994", "#8B0000", "#000000", "#FFFFFF") },
                    { "joker", ("#9B59B6", "#2ECC71", "#8B0000", "#FFFFFF", "#000000") },
                    { "harleyquinn", ("#FF1493", "#00BFFF", "#8B0000", "#FFFFFF", "#000000") },
                    
                    // Temas Marvel
                    { "spiderman", ("#E62020", "#0052A5", "#8B0000", "#FFFFFF", "#FFFFFF") },
                    { "ironman", ("#FFD700", "#DC143C", "#8B0000", "#000000", "#FFFFFF") },
                    { "captainamerica", ("#0052A5", "#E62020", "#8B0000", "#FFFFFF", "#FFFFFF") },
                    { "hulk", ("#2ECC71", "#8B008B", "#8B0000", "#000000", "#FFFFFF") },
                    { "thor", ("#FFD700", "#0052A5", "#8B0000", "#000000", "#FFFFFF") },
                    { "blackpanther", ("#9B59B6", "#1A1A1A", "#8B0000", "#FFFFFF", "#9B59B6") },
                    { "deadpool", ("#E62020", "#1A1A1A", "#8B0000", "#FFFFFF", "#E62020") },
                    { "daredevil", ("#DC143C", "#8B0000", "#8B0000", "#FFFFFF", "#FFFFFF") },
                    
                    // Temas Especiales
                    { "cyberpunk", ("#FF00FF", "#00FFFF", "#FF0000", "#000000", "#000000") },
                    { "retro", ("#FF6B35", "#F7B801", "#8B0000", "#000000", "#000000") },
                    { "neon", ("#FF1493", "#00FF00", "#FF0000", "#000000", "#000000") },
                    { "dracula", ("#BD93F9", "#FF79C6", "#FF5555", "#282A36", "#282A36") },
                    { "nord", ("#88C0D0", "#5E81AC", "#BF616A", "#2E3440", "#ECEFF4") },
                    { "solarizeddark", ("#268BD2", "#2AA198", "#DC322F", "#FDF6E3", "#FDF6E3") },
                    { "solarizedlight", ("#268BD2", "#2AA198", "#DC322F", "#FDF6E3", "#002B36") },
                    
                    // Temas Clásicos
                    { "dark", ("#3B82F6", "#6B7280", "#EF4444", "#FFFFFF", "#FFFFFF") },
                    { "light", ("#2563EB", "#6B7280", "#DC2626", "#FFFFFF", "#1F2937") },
                    { "comic", ("#FF6B35", "#4ECDC4", "#E63946", "#000000", "#000000") },
                    { "sepia", ("#D2691E", "#8B4513", "#A0522D", "#F5DEB3", "#F5DEB3") },
                    { "highcontrast", ("#FFFF00", "#00FF00", "#FF0000", "#000000", "#000000") },
                };

                if (buttonColors.TryGetValue(normalizedTheme, out var colors))
                {
                    // Aplicar colores primarios
                    Application.Current.Resources["ButtonPrimaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.Item1));
                    Application.Current.Resources["ButtonPrimaryTextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.Item4));
                    Application.Current.Resources["ButtonPrimaryBorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.Item1));

                    // Aplicar colores secundarios
                    Application.Current.Resources["ButtonSecondaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.Item2));
                    Application.Current.Resources["ButtonSecondaryTextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.Item5));
                    Application.Current.Resources["ButtonSecondaryBorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.Item2));

                    // Aplicar colores de peligro
                    Application.Current.Resources["ButtonDangerBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.Item3));
                    Application.Current.Resources["ButtonDangerTextBrush"] = new SolidColorBrush(Colors.White);
                    Application.Current.Resources["ButtonDangerBorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.Item3));

                    Logger.Log($"Applied dynamic button colors for theme: {themeName}", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException($"Failed to apply dynamic button colors for theme: {themeName}", ex);
            }
        }
    }
}