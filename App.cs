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
                
                SettingsManager.LoadSettings();
                Console.WriteLine("Configuraciones cargadas correctamente");
                
                ApplyTheme(SettingsManager.Settings.Theme);
                Console.WriteLine("Tema aplicado correctamente");

                // Registro de servicios básicos (fase inicial DI ligera)
                ServiceLocator.RegisterSingleton<IComicPageLoader>(new ComicPageLoader());
                ServiceLocator.RegisterSingleton<IBookmarkService>(new BookmarkServiceAdapter());
                ServiceLocator.RegisterSingleton<ISettingsService>(new SettingsServiceAdapter());
                ServiceLocator.RegisterSingleton<ILogService>(new LogServiceAdapter());
                ServiceLocator.RegisterSingleton<IImageCache>(new MultiLevelImageCache());
                ServiceLocator.RegisterSingleton<ComicReader.Core.Abstractions.IReadingStatsService>(new ReadingStatsService());
                Console.WriteLine("Servicios registrados en ServiceLocator");
                
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
                
                Logger.LogException("Unhandled exception caught by App_DispatcherUnhandledException.", e.Exception);
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
            SettingsManager.SaveSettings();
            base.OnExit(e);
        }

        public static void ApplyTheme(string themeName)
        {
            try
            {
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