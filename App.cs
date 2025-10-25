using System.Windows;
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

            // If invoked with --validate-themes, run a headless theme validation pass and exit
            try
            {
                if (e?.Args != null && e.Args.Any(a => a.Equals("--validate-themes", StringComparison.OrdinalIgnoreCase)))
                {
                    RunThemeValidationAndExit();
                    return;
                }
            }
            catch { }

            // Crear y mostrar MainWindow manualmente ahora que removimos StartupUri
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
            // Abrir último cómic si el usuario lo solicita en la configuración
            try
            {
                if (SettingsManager.Settings != null && SettingsManager.Settings.OpenLastOnStartup && !string.IsNullOrWhiteSpace(SettingsManager.Settings.LastOpenedFilePath))
                {
                    var last = SettingsManager.Settings.LastOpenedFilePath;
                    if (System.IO.File.Exists(last) || System.IO.Directory.Exists(last))
                    {
                        var mi2 = typeof(MainWindow).GetMethod("OpenComicFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        mi2?.Invoke(main, new object[] { last });
                    }
                }
            }
            catch { }
        }

        private void RunThemeValidationAndExit()
        {
            var results = new List<string>();
            try
            {
                var themes = ComicReader.Themes.ThemeManager.GetAvailableThemes();
                foreach (var t in themes)
                {
                    try
                    {
                        // Apply and give the dispatcher a moment
                        ComicReader.Themes.ThemeManager.CurrentTheme = t.Mode;
                        this.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                        results.Add($"OK: {t.Name} ({t.Mode})");
                    }
                    catch (Exception ex)
                    {
                        results.Add($"ERROR: {t.Name} ({t.Mode}) -> {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                results.Add("Fatal error during theme validation: " + ex.Message);
            }

            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var dir = System.IO.Path.Combine(appData, "PercysLibrary", "validation");
                System.IO.Directory.CreateDirectory(dir);
                var outFile = System.IO.Path.Combine(dir, "theme-validation-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".log");
                System.IO.File.WriteAllLines(outFile, results);
                Logger.Log("Theme validation finished. Results written to " + outFile, LogLevel.Info);
            }
            catch { }

            // Show a simple message and exit
            try { MessageBox.Show("Theme validation completed. Revisa el log en %AppData%\\PercysLibrary\\validation.", "Validación de temas", MessageBoxButton.OK, MessageBoxImage.Information); } catch { }
            Environment.Exit(0);
        }
        public App()
        {
            try
            {
                ComicReader.Utils.DevLogger.Info("Iniciando aplicación...");
                
                Logger.Initialize();
                ComicReader.Utils.DevLogger.Info("Logger inicializado correctamente");
                
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
                ComicReader.Utils.DevLogger.Info("Manejador de excepciones configurado");
                
                SettingsManager.LoadSettings();
                ComicReader.Utils.DevLogger.Info("Configuraciones cargadas correctamente");
                
                ApplyTheme(SettingsManager.Settings.Theme);
                ComicReader.Utils.DevLogger.Info("Tema aplicado correctamente");

                // Registro de servicios básicos (fase inicial DI ligera)
                // Registrar el loader progresivo por defecto para mejorar la experiencia de carga
                ServiceLocator.RegisterSingleton<IComicPageLoader>(new ComicReader.Services.ProgressivePageLoader());
                ServiceLocator.RegisterSingleton<IBookmarkService>(new BookmarkServiceAdapter());
                ServiceLocator.RegisterSingleton<ISettingsService>(new SettingsServiceAdapter());
                ServiceLocator.RegisterSingleton<ILogService>(new LogServiceAdapter());
                ServiceLocator.RegisterSingleton<IImageCache>(new MultiLevelImageCache());
                ServiceLocator.RegisterSingleton<ComicReader.Core.Abstractions.IReadingStatsService>(new ReadingStatsService());
                ComicReader.Utils.DevLogger.Info("Servicios registrados en ServiceLocator");
                
                ComicReader.Utils.DevLogger.Info("Aplicación iniciada exitosamente");
            }
            catch (Exception ex)
            {
                ComicReader.Utils.DevLogger.Error($"Error crítico durante la inicialización: {ex.Message}");
                ComicReader.Utils.DevLogger.Error($"Stack trace: {ex.StackTrace}");
                
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
                ComicReader.Utils.DevLogger.Error($"Excepción no controlada: {e.Exception.Message}");
                ComicReader.Utils.DevLogger.Error($"Stack trace: {e.Exception.StackTrace}");
                
                Logger.LogException("Unhandled exception caught by App_DispatcherUnhandledException.", e.Exception);
                MessageBox.Show($"Error inesperado:\n{e.Exception.Message}\n\n{e.Exception.StackTrace}", "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            }
            catch (Exception logEx)
            {
                ComicReader.Utils.DevLogger.Error($"Error en el manejador de excepciones: {logEx.Message}");
                e.Handled = true;
                Environment.Exit(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SettingsManager.SaveSettings();
            base.OnExit(e);
        }

        public static void ApplyTheme(string themeName)
        {
            try
            {
                // If the theme maps to a ThemeMode we manage programmatically, prefer that
                try
                {
                    if (!string.IsNullOrWhiteSpace(themeName) && Enum.TryParse<ComicReader.Services.ThemeMode>(themeName, out var tm))
                    {
                        ComicReader.Themes.ThemeManager.ApplyTheme(tm);
                        Logger.Log($"Applied programmatic theme: {themeName}", LogLevel.Info);
                        return;
                    }
                }
                catch { }

                var oldThemeDictionaries = Application.Current.Resources.MergedDictionaries
                    .Where(rd => rd.Source != null && rd.Source.OriginalString.Contains("Theme.xaml"))
                    .ToList();

                foreach (var rd in oldThemeDictionaries)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(rd);
                }

                var themeUri = new Uri($"Themes/{themeName}Theme.xaml", UriKind.Relative);
                ResourceDictionary theme = new ResourceDictionary() { Source = themeUri };
                Application.Current.Resources.MergedDictionaries.Add(theme);

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
    }
}