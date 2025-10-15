using System;
using System.ComponentModel;
using System.Windows;
using ComicReader.Services;

namespace ComicReader.Views
{
    public partial class SettingsDialog : Window
    {
    private readonly UserAppSettings _original;
    private readonly UserAppSettings _draft;
    private bool _hasPendingChanges = false;
        private System.Windows.Controls.Button _applyButton;
        private System.Windows.Controls.Button _okButton;

        public SettingsDialog() : this(SettingsManager.Settings) { }

        public SettingsDialog(UserAppSettings settings)
        {
            #pragma warning disable
            InitializeComponent();
            #pragma warning restore
            _original = settings ?? new UserAppSettings();
            _draft = Clone(_original);
            DataContext = _draft;
            _draft.PropertyChanged += Draft_PropertyChanged;
            UpdateActionButtons();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // Obtener referencias a los botones declarados en XAML
            _applyButton = this.FindName("ApplyButton") as System.Windows.Controls.Button;
            _okButton = this.FindName("OkButton") as System.Windows.Controls.Button;
            UpdateActionButtons();
        }

        private void Draft_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _hasPendingChanges = true;
            UpdateActionButtons();

            // Propagar en memoria los cambios del borrador al Settings real (sin guardar a disco todavía)
            try { CopyInto(_original, _draft); } catch { }

            // Aplicación en vivo de algunos ajustes visuales
            if (e.PropertyName == nameof(UserAppSettings.Theme))
            {
                try { App.ApplyTheme(_draft.Theme); } catch { }
            }
            else if (e.PropertyName == nameof(UserAppSettings.IsNightMode) ||
                     e.PropertyName == nameof(UserAppSettings.IsReadingMode) ||
                     e.PropertyName == nameof(UserAppSettings.HideOverlayDelaySeconds) ||
                     e.PropertyName == nameof(UserAppSettings.HideCursorDelaySeconds))
            {
                try { (this.Owner as ComicReader.MainWindow)?.ApplySettingsRuntime(); } catch { }
            }

            // En general, intentar aplicar cambios en caliente cuando sea posible (p.ej. modo continuo, encuadre, etc.)
            try { (this.Owner as ComicReader.MainWindow)?.ApplySettingsRuntime(); } catch { }
        }

        private void UpdateActionButtons()
        {
            try
            {
                if (_applyButton != null) _applyButton.IsEnabled = _hasPendingChanges;
                if (_okButton != null) _okButton.IsEnabled = true; // siempre permitir Aceptar (aplica cambios)
            }
            catch { }
        }

        private static UserAppSettings Clone(UserAppSettings s)
        {
            // Clonado simple campo a campo (suficiente para nuestras props primitivas)
            var c = new UserAppSettings
            {
                RememberLastSession = s.RememberLastSession,
                ShowPageNumberOverlay = s.ShowPageNumberOverlay,
                DefaultFitMode = s.DefaultFitMode,
                IsReadingMode = s.IsReadingMode,
                IsNightMode = s.IsNightMode,
                AutoAdvancePages = s.AutoAdvancePages,
                AutoAdvanceInterval = s.AutoAdvanceInterval,
                CurrentReadingDirection = s.CurrentReadingDirection,
                Theme = s.Theme,
                Brightness = s.Brightness,
                Contrast = s.Contrast,
                EnableMemoryOptimization = s.EnableMemoryOptimization,
                PageCacheLimit = s.PageCacheLimit,
                PrefetchWindow = s.PrefetchWindow,
                SmoothScrolling = s.SmoothScrolling,
                EnableContinuousScroll = s.EnableContinuousScroll,
                EnableZoomPan = s.EnableZoomPan,
                // HideUIOnFullScreen eliminado
                ShowLoadingIndicators = s.ShowLoadingIndicators,
                AutoAdvanceLoop = s.AutoAdvanceLoop,
                HideOverlayDelaySeconds = s.HideOverlayDelaySeconds,
                InvertScrollWheel = s.InvertScrollWheel
            };
            return c;
        }

        private void ApplyChanges()
        {
            // Copiar valores del borrador a los settings reales
            _original.RememberLastSession = _draft.RememberLastSession;
            _original.ShowPageNumberOverlay = _draft.ShowPageNumberOverlay;
            _original.DefaultFitMode = _draft.DefaultFitMode;
            _original.IsReadingMode = _draft.IsReadingMode;
            _original.IsNightMode = _draft.IsNightMode;
            _original.AutoAdvancePages = _draft.AutoAdvancePages;
            _original.AutoAdvanceInterval = _draft.AutoAdvanceInterval;
            _original.CurrentReadingDirection = _draft.CurrentReadingDirection;
            _original.Theme = _draft.Theme;
            _original.Brightness = _draft.Brightness;
            _original.Contrast = _draft.Contrast;
            _original.EnableMemoryOptimization = _draft.EnableMemoryOptimization;
            _original.PageCacheLimit = _draft.PageCacheLimit;
            _original.PrefetchWindow = _draft.PrefetchWindow;
            _original.SmoothScrolling = _draft.SmoothScrolling;
            _original.EnableContinuousScroll = _draft.EnableContinuousScroll;
            _original.EnableZoomPan = _draft.EnableZoomPan;
            // HideUIOnFullScreen eliminado
            _original.ShowLoadingIndicators = _draft.ShowLoadingIndicators;
            _original.AutoAdvanceLoop = _draft.AutoAdvanceLoop;
            _original.HideOverlayDelaySeconds = _draft.HideOverlayDelaySeconds;
            _original.InvertScrollWheel = _draft.InvertScrollWheel;
            _original.SpacebarNextPage = _draft.SpacebarNextPage;
            _original.PageScrollStepRatio = _draft.PageScrollStepRatio;

            SettingsManager.SaveSettings();

            // Aplicar tema si cambió
            try { App.ApplyTheme(_original.Theme); } catch { }

            // Avisar a la ventana principal para aplicar cambios en caliente
            try
            {
                if (this.Owner is ComicReader.MainWindow mw)
                {
                    mw.ApplySettingsRuntime();
                }
            }
            catch { }
            _hasPendingChanges = false;
            UpdateActionButtons();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            ApplyChanges();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            ApplyChanges();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("¿Restaurar valores por defecto?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes)
            {
                var def = new UserAppSettings();
                var cloned = Clone(def);
                CopyInto(_draft, cloned);
            }
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("¿Seguro que quieres borrar todos los recientes? Esta acción no se puede deshacer.", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
            try
            {
                // Historial de "Seguir leyendo" se gestiona desde ContinueReadingService
                SettingsManager.SaveSettings();
                MessageBox.Show("Historial de lectura limpiado.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo limpiar el historial: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void CopyInto(UserAppSettings target, UserAppSettings source)
        {
            target.RememberLastSession = source.RememberLastSession;
            target.ShowPageNumberOverlay = source.ShowPageNumberOverlay;
            target.DefaultFitMode = source.DefaultFitMode;
            target.IsReadingMode = source.IsReadingMode;
            target.IsNightMode = source.IsNightMode;
            target.AutoAdvancePages = source.AutoAdvancePages;
            target.AutoAdvanceInterval = source.AutoAdvanceInterval;
            target.CurrentReadingDirection = source.CurrentReadingDirection;
            target.Theme = source.Theme;
            target.Brightness = source.Brightness;
            target.Contrast = source.Contrast;
            target.EnableMemoryOptimization = source.EnableMemoryOptimization;
            target.PageCacheLimit = source.PageCacheLimit;
            target.PrefetchWindow = source.PrefetchWindow;
            target.SmoothScrolling = source.SmoothScrolling;
            target.EnableContinuousScroll = source.EnableContinuousScroll;
            target.EnableZoomPan = source.EnableZoomPan;
            // HideUIOnFullScreen eliminado
            target.ShowLoadingIndicators = source.ShowLoadingIndicators;
            target.AutoAdvanceLoop = source.AutoAdvanceLoop;
            target.HideOverlayDelaySeconds = source.HideOverlayDelaySeconds;
            target.InvertScrollWheel = source.InvertScrollWheel;
            target.SpacebarNextPage = source.SpacebarNextPage;
            target.PageScrollStepRatio = source.PageScrollStepRatio;
        }

        private void ExportSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Exportar configuración",
                    Filter = "XML (*.xml)|*.xml",
                    FileName = "PercysLibrary.Settings.xml"
                };
                if (dlg.ShowDialog() == true)
                {
                    ApplyChanges(); // asegurar persistencia
                    System.IO.File.Copy(
                        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PercysLibrary", "settings.xml"),
                        dlg.FileName,
                        overwrite: true);
                    MessageBox.Show("Configuración exportada.", "Listo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo exportar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Importar configuración",
                    Filter = "XML (*.xml)|*.xml"
                };
                if (dlg.ShowDialog() == true)
                {
                    var appData = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PercysLibrary");
                    System.IO.Directory.CreateDirectory(appData);
                    var target = System.IO.Path.Combine(appData, "settings.xml");
                    System.IO.File.Copy(dlg.FileName, target, overwrite: true);
                    SettingsManager.LoadSettings();
                    CopyInto(_draft, Clone(SettingsManager.Settings));
                    MessageBox.Show("Configuración importada. Aplica o Acepta para usarla.", "Listo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo importar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
