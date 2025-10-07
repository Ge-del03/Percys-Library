// FileName: /Views/SettingsView.xaml.cs
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ComicReader.Services;
using Microsoft.Win32;

namespace ComicReader.Views
{
    public partial class SettingsView : UserControl
    {
        public static readonly DependencyProperty AutoSaveEnabledProperty = DependencyProperty.Register(
            nameof(AutoSaveEnabled), typeof(bool), typeof(SettingsView), new PropertyMetadata(true));

        public static readonly DependencyProperty UseGlobalSettingsContextProperty = DependencyProperty.Register(
            nameof(UseGlobalSettingsContext), typeof(bool), typeof(SettingsView), new PropertyMetadata(true));

        private System.ComponentModel.INotifyPropertyChanged? _npc;
        private System.Windows.Threading.DispatcherTimer? _saveDebounce;
        private bool _usesGlobalSettings;
        private bool _syncingNav;
        private RadioButton?[]? _navButtons;
    private TabControl? _sectionTabs;
    private FrameworkElement? _summaryCard;
    
    // TERCERA ALTERNATIVA: Variables para preview de tema
    private string? _savedTheme;
    private bool _previewMode = false;

        public bool AutoSaveEnabled
        {
            get => (bool)GetValue(AutoSaveEnabledProperty);
            set => SetValue(AutoSaveEnabledProperty, value);
        }

        public bool UseGlobalSettingsContext
        {
            get => (bool)GetValue(UseGlobalSettingsContextProperty);
            set => SetValue(UseGlobalSettingsContextProperty, value);
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            _sectionTabs = FindName("SectionTabs") as TabControl;
            _summaryCard = FindName("SummaryCard") as FrameworkElement;
            _navButtons = new RadioButton?[]
            {
                FindName("NavSummary") as RadioButton,
                FindName("NavReading") as RadioButton,
                FindName("NavAppearance") as RadioButton,
                FindName("NavHomeBackgrounds") as RadioButton,
                FindName("NavAnimations") as RadioButton,
                FindName("NavFullscreen") as RadioButton,
                FindName("NavPerformance") as RadioButton,
                FindName("NavTools") as RadioButton,
                FindName("NavPdf") as RadioButton,
                FindName("NavStats") as RadioButton
            };
            if (_navButtons != null && _navButtons.Length > 0)
            {
                bool anyChecked = false;
                foreach (var btn in _navButtons)
                {
                    if (btn?.IsChecked == true)
                    {
                        anyChecked = true;
                        break;
                    }
                }
                if (!anyChecked)
                {
                    var first = _navButtons[0];
                    if (first != null)
                    {
                        first.IsChecked = true;
                    }
                }
            }
            if (_sectionTabs != null)
            {
                var idx = GetCurrentNavIndex();
                _sectionTabs.SelectedIndex = idx;
                UpdateSummaryVisibility(idx);
            }
            if (UseGlobalSettingsContext && this.DataContext == null)
            {
                this.DataContext = SettingsManager.Settings;
            }
            _usesGlobalSettings = ReferenceEquals(this.DataContext, SettingsManager.Settings);
            this.Loaded += SettingsView_Loaded;
            this.Unloaded += SettingsView_Unloaded;
        }

        private void SettingsView_Loaded(object? sender, RoutedEventArgs e)
        {
            // TERCERA ALTERNATIVA: Guardar el tema actualmente guardado
            _savedTheme = SettingsManager.Settings?.Theme;
            _previewMode = false;
            
            if (!AutoSaveEnabled || !_usesGlobalSettings)
            {
                return;
            }

            if (_saveDebounce == null)
            {
                _saveDebounce = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
                _saveDebounce.Tick += (_, __) => { try { _saveDebounce.Stop(); SettingsManager.SaveSettings(); } catch { } };
            }
            _npc = this.DataContext as INotifyPropertyChanged;
            if (_npc != null)
            {
                _npc.PropertyChanged += OnSettingsPropertyChanged;
            }
        }

        private void SettingsView_Unloaded(object? sender, RoutedEventArgs e)
        {
            // TERCERA ALTERNATIVA: No revertir automáticamente - usar botón "Revertir" explícito
            if (_npc != null)
            {
                _npc.PropertyChanged -= OnSettingsPropertyChanged;
                _npc = null;
            }
        }

        private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                _saveDebounce?.Stop();
                _saveDebounce?.Start();
            }
            catch { }
        }

        private static readonly Regex _svNumericRegex = new Regex("^[0-9]+$");

        private void SV_NumericOnly_PreviewTextInput(object? sender, TextCompositionEventArgs e)
        {
            e.Handled = !_svNumericRegex.IsMatch(e.Text);
        }

        private void SV_NumericOnly_Pasting(object? sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject?.GetDataPresent(DataFormats.Text) == true)
            {
                var text = e.DataObject.GetData(DataFormats.Text) as string;
                if (string.IsNullOrEmpty(text) || !_svNumericRegex.IsMatch(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void ResetFullscreenSettings_Click(object? sender, RoutedEventArgs e)
        {
            var settings = GetSettingsContext();
            settings.AutoEnterImmersiveOnOpen = false;
            settings.FadeOnFullscreenTransitions = true;
            PersistIfNeeded();
        }

        private void ApplyPdfRender_Click(object? sender, RoutedEventArgs e)
        {
            if (!_usesGlobalSettings || !AutoSaveEnabled)
            {
                MessageBox.Show("Aplica los cambios desde la ventana principal para re-renderizar el PDF.", "Acción pendiente", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                SettingsManager.SaveSettings();
                // Pedimos a la ventana principal que intente re-renderizar el PDF actual
                if (Application.Current?.MainWindow is MainWindow mw)
                {
                    mw.ReRenderCurrentPdfIfAny();
                }
            }
            catch { }
        }

        private void ThemeCard_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is string theme && !string.IsNullOrWhiteSpace(theme))
                {
                    // PRESERVAR ESTADÍSTICAS antes de cambiar tema
                    var currentStats = SettingsManager.Settings?.Statistics;
                    
                    // Convertir nombre del tema a formato correcto
                    string themeFile = theme;
                    if (!theme.EndsWith("Theme", StringComparison.OrdinalIgnoreCase))
                    {
                        themeFile = theme + "Theme";
                    }
                    
                    // Aplicar inmediatamente el tema
                    App.ApplyTheme(themeFile);
                    
                    // Actualizar configuración preservando estadísticas
                    if (SettingsManager.Settings != null)
                    {
                        SettingsManager.Settings.Theme = themeFile;
                        
                        // RESTAURAR ESTADÍSTICAS después del cambio
                        if (currentStats != null)
                        {
                            SettingsManager.Settings.Statistics = currentStats;
                        }
                        
                        // Guardar inmediatamente
                        SettingsManager.SaveSettings();
                    }
                    
                    // Actualizar configuración local
                    var localSettings = GetSettingsContext();
                    localSettings.Theme = themeFile;
                    
                    // Log para debugging
                    System.Diagnostics.Debug.WriteLine($"✅ [Theme] Tema aplicado correctamente: {themeFile}");
                    MessageBox.Show($"Tema '{themeFile}' aplicado correctamente", "Cambio de Tema", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ [Theme] Error aplicando tema: {ex.Message}");
                MessageBox.Show($"Error aplicando tema: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HomeBackgroundCard_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is string backgroundName && !string.IsNullOrWhiteSpace(backgroundName))
                {
                    // Debug: Log el cambio de fondo
                    System.Diagnostics.Debug.WriteLine($"[HomeBackground] Cambiando a fondo: {backgroundName}");
                    Console.WriteLine($"[HomeBackground] Cambiando a fondo: {backgroundName}");
                    
                    // SOLUCIÓN DIRECTA: Aplicar inmediatamente sin depender del setter
                    try
                    {
                        // Cargar el recurso directamente
                        var homeBackgroundsUri = new Uri("Themes/HomeBackgrounds.xaml", UriKind.Relative);
                        var homeBackgroundsDict = new ResourceDictionary() { Source = homeBackgroundsUri };
                        
                        if (homeBackgroundsDict.Contains(backgroundName))
                        {
                            var backgroundBrush = homeBackgroundsDict[backgroundName] as Brush;
                            if (backgroundBrush != null)
                            {
                                // Aplicar directamente a los recursos de la aplicación
                                Application.Current.Resources["DynamicHomeBackgroundBrush"] = backgroundBrush;
                                Console.WriteLine($"✅ Fondo aplicado directamente: {backgroundName}");
                                
                                // Mostrar confirmación visual
                                MessageBox.Show($"Fondo cambiado a: {backgroundName}", "Fondo Aplicado", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"❌ Fondo NO encontrado: {backgroundName}");
                            MessageBox.Show($"Fondo no encontrado: {backgroundName}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception directEx)
                    {
                        Console.WriteLine($"❌ Error aplicando fondo directamente: {directEx.Message}");
                        MessageBox.Show($"Error directo: {directEx.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    
                    // Actualizar la configuración
                    var settings = GetSettingsContext();
                    settings.HomeBackground = backgroundName;
                    PersistIfNeeded();
                    
                    System.Diagnostics.Debug.WriteLine($"[HomeBackground] Proceso completado para: {backgroundName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomeBackground] Error general: {ex.Message}");
                Console.WriteLine($"❌ Error general: {ex.Message}");
                MessageBox.Show($"Error aplicando fondo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetHomeBackground_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                const string defaultBackground = "SupermanHomeBackground";
                System.Diagnostics.Debug.WriteLine($"[HomeBackground] Restableciendo a fondo por defecto: {defaultBackground}");
                
                // Aplicar el fondo inmediatamente usando el método unificado
                SettingsManager.ApplyHomeBackgroundImmediate(defaultBackground);
                
                // Actualizar la configuración
                var settings = GetSettingsContext();
                settings.HomeBackground = defaultBackground;
                
                PersistIfNeeded();
                
                MessageBox.Show("Fondo restablecido al diseño Superman clásico.", "Fondo Restablecido", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomeBackground] Error restableciendo fondo: {ex.Message}");
                MessageBox.Show($"Error restableciendo fondo: {ex.Message}", "Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }





        private void ResetAnimations_Click(object? sender, RoutedEventArgs e)
        {
            var settings = GetSettingsContext();
            settings.EnableAnimations = true;
            settings.PreferReducedMotion = false;
            settings.EnablePageTurnAnimations = true;
            settings.PageTurnAnimationStyle = "Slide";
            settings.PageTurnAnimationDurationMs = 180;
            settings.FadeOnFullscreenTransitions = true;
            PersistIfNeeded();
        }

        // TERCERA ALTERNATIVA: Método para revertir tema
        private void RevertTheme_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_previewMode && !string.IsNullOrWhiteSpace(_savedTheme))
                {
                    // Revertir al tema guardado
                    App.ApplyTheme(_savedTheme);
                    
                    var localSettings = GetSettingsContext();
                    localSettings.Theme = _savedTheme;
                    
                    _previewMode = false;
                    

                    MessageBox.Show($"Tema revertido a: {_savedTheme}", "Tema Revertido", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("No hay cambios de tema que revertir.", "Sin Cambios", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show($"Error revirtiendo tema: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetAppearance_Click(object? sender, RoutedEventArgs e)
        {
            var settings = GetSettingsContext();
            settings.Theme = "DarkTheme";
            settings.IsNightMode = false;
            settings.Brightness = 1.0;
            settings.Contrast = 1.0;
            settings.ShowPageNumberOverlay = true;
            settings.ShowPageProgressIndicator = true;
            
            // Aplicar inmediatamente el tema por defecto
            App.ApplyTheme("DarkTheme");
            
            // Guardar si es configuración global
            if (_usesGlobalSettings)
            {
                SettingsManager.Settings.Theme = "DarkTheme";
                SettingsManager.SaveSettings();
            }
            
            PersistIfNeeded();
            
            MessageBox.Show("Apariencia restablecida al tema oscuro por defecto.", "Apariencia Restablecida", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ResetReadingBasics_Click(object? sender, RoutedEventArgs e)
        {
            var settings = GetSettingsContext();
            settings.IsReadingMode = false;
            settings.RememberLastSession = true;
            settings.ThumbnailsVisible = true;
            settings.AutoFitOnLoad = true;
            settings.DefaultFitMode = "Width";
            settings.SpacebarNextPage = true;
            settings.ShowPageProgressIndicator = true;
            settings.ShowLoadingIndicators = true;
            settings.EnableContinuousScroll = false;
            settings.SmoothScrolling = true;
            settings.AutoAdvancePages = false;
            settings.AutoAdvanceInterval = 6;
            settings.AutoAdvanceLoop = false;
            settings.InvertScrollWheel = false;
            settings.IsRecentListView = false;
            settings.ContinueCompactMode = false;
            settings.PageScrollStepRatio = 0.9;
            PersistIfNeeded();
        }

        private void ResetPerformance_Click(object? sender, RoutedEventArgs e)
        {
            var settings = GetSettingsContext();
            settings.EnableMemoryOptimization = true;
            settings.EnableAsyncPrefetch = true;
            settings.PageCacheLimit = 80;
            settings.PrefetchWindow = 4;
            settings.DecodeConcurrency = Math.Min(Environment.ProcessorCount, 4);
            settings.PreloadCacheSize = 6;
            PersistIfNeeded();
        }

        private void SaveNow_Click(object? sender, RoutedEventArgs e)
        {
            if (_usesGlobalSettings)
            {
                try
                {
                    // Guardar el tema actual como permanente
                    if (_previewMode && this.DataContext is UserAppSettings localSettings)
                    {
                        SettingsManager.Settings.Theme = localSettings.Theme;
                        _savedTheme = localSettings.Theme;
                        _previewMode = false;
                    }
                    
                    // Forzar guardado completo de toda la configuración
                    SettingsManager.SaveSettings();
                    
                    // Verificar que el tema se aplicó correctamente
                    if (!string.IsNullOrEmpty(SettingsManager.Settings?.Theme))
                    {
                        App.ApplyTheme(SettingsManager.Settings.Theme);
                    }
                    
                    MessageBox.Show("Configuración guardada y aplicada correctamente.", "Guardado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al guardar la configuración: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Aplica los cambios desde la ventana principal para guardarlos definitivamente.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OpenCustomizationGuide_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var guidePath = Path.Combine(baseDir, "MEJORAS_MODO_LECTURA.md");
                if (!File.Exists(guidePath))
                {
                    MessageBox.Show("No se encontró la guía de personalización en la carpeta de la aplicación.", "Archivo no encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = guidePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo abrir la guía: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetDefaults_Click(object? sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("¿Restaurar todos los ajustes a sus valores originales?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            var settings = GetSettingsContext();
            var defaults = new UserAppSettings();
            CopySettings(settings, defaults);
            PersistIfNeeded();
        }

        private void ClearHistory_Click(object? sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("¿Deseas limpiar la lista de 'Seguir leyendo'? Esta acción no se puede deshacer.", "Limpiar historial", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                ContinueReadingService.Instance.Clear();
                if (Application.Current?.MainWindow is MainWindow mw)
                {
                    mw.ApplySettingsRuntime();
                }
                MessageBox.Show("Historial eliminado.", "Listo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo limpiar el historial: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportSettings_Click(object? sender, RoutedEventArgs e)
        {
            if (!_usesGlobalSettings || !AutoSaveEnabled)
            {
                MessageBox.Show("Abre la configuración desde la vista principal para exportar directamente.", "Acción no disponible", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var dlg = new SaveFileDialog
                {
                    Title = "Exportar configuración",
                    Filter = "XML (*.xml)|*.xml",
                    FileName = "PercysLibrary.Settings.xml"
                };
                if (dlg.ShowDialog() == true)
                {
                    SettingsManager.SaveSettings();
                    var source = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PercysLibrary", "settings.xml");
                    File.Copy(source, dlg.FileName, overwrite: true);
                    MessageBox.Show("Configuración exportada correctamente.", "Exportado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo exportar la configuración: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportSettings_Click(object? sender, RoutedEventArgs e)
        {
            if (!_usesGlobalSettings || !AutoSaveEnabled)
            {
                MessageBox.Show("Importa la configuración desde la vista principal para aplicarla inmediatamente.", "Acción no disponible", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var dlg = new OpenFileDialog
                {
                    Title = "Importar configuración",
                    Filter = "XML (*.xml)|*.xml"
                };
                if (dlg.ShowDialog() == true)
                {
                    var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PercysLibrary");
                    Directory.CreateDirectory(appData);
                    var destination = Path.Combine(appData, "settings.xml");
                    File.Copy(dlg.FileName, destination, overwrite: true);
                    SettingsManager.LoadSettings();
                    PersistIfNeeded();
                    MessageBox.Show("Configuración importada. Se aplicará de inmediato.", "Importado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo importar la configuración: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private UserAppSettings GetSettingsContext()
        {
            return DataContext as UserAppSettings ?? SettingsManager.Settings;
        }

        private void PersistIfNeeded()
        {
            if (AutoSaveEnabled && _usesGlobalSettings)
            {
                SettingsManager.SaveSettings();
            }
        }

        private int GetCurrentNavIndex()
        {
            if (_navButtons == null) return 0;
            for (int i = 0; i < _navButtons.Length; i++)
            {
                if (_navButtons[i]?.IsChecked == true)
                {
                    return i;
                }
            }
            return 0;
        }

        private void SectionNav_Checked(object sender, RoutedEventArgs e)
        {
            if (_syncingNav) return;
            if (sender is RadioButton radio && radio.Tag is string tag && int.TryParse(tag, out int index))
            {
                try
                {
                    _syncingNav = true;
                    if (_sectionTabs != null && _sectionTabs.SelectedIndex != index)
                    {
                        _sectionTabs.SelectedIndex = index;
                    }
                    UpdateSummaryVisibility(index);
                }
                finally
                {
                    _syncingNav = false;
                }
            }
        }

        private void SectionTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncingNav) return;
            var tabs = sender as TabControl ?? _sectionTabs;
            if (tabs == null || _navButtons == null) return;
            var index = tabs.SelectedIndex;
            if (index < 0 || index >= _navButtons.Length) return;
            try
            {
                _syncingNav = true;
                var button = _navButtons[index];
                if (button != null && button.IsChecked != true)
                {
                    button.IsChecked = true;
                }
                UpdateSummaryVisibility(index);
            }
            finally
            {
                _syncingNav = false;
            }
        }

        private void UpdateSummaryVisibility(int selectedIndex)
        {
            if (_summaryCard == null) return;
            _summaryCard.Visibility = selectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void CopySettings(UserAppSettings target, UserAppSettings source)
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
    }
}
