using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ComicReader.ViewModels;

namespace ComicReader.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _vm;

        public SettingsWindow()
        {
            InitializeComponent();
                // Try to obtain the VM from resources or DataContext; fallback to a new instance so UI code is safe
                _vm = this.Resources["SettingsVM"] as SettingsViewModel ?? this.DataContext as SettingsViewModel;
                if (_vm == null)
                {
                    _vm = new SettingsViewModel();
                    // register in resources so XAML bindings that reference StaticResource will still work
                    try { this.Resources["SettingsVM"] = _vm; } catch { }
                    this.DataContext = _vm;
                }

                // after load, sync visibility with SelectedSection (defensive: guard nulls)
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                {
                    try { UpdateSectionVisibility(_vm?.SelectedSection); } catch { }
                    try { InitializeComboBoxValues(); } catch { }
                    if (_vm != null)
                    {
                        try
                        {
                            _vm.PropertyChanged += (s, e) =>
                            {
                                try
                                {
                                    if (e.PropertyName == nameof(SettingsViewModel.SelectedSection))
                                        UpdateSectionVisibility(_vm.SelectedSection);
                                }
                                catch { }
                            };
                        }
                        catch { }
                    }
                }));
        }

        private void UpdateSectionVisibility(string section)
        {
            try
            {
                // Use FindName to avoid relying on generated fields which may be out-of-sync in some build states
                void SetVis(string name, string key)
                {
                    try
                    {
                        var el = this.FindName(name) as FrameworkElement;
                        if (el != null)
                            el.Visibility = (section == key) ? Visibility.Visible : Visibility.Collapsed;
                    }
                    catch { }
                }

                SetVis("Panel_General", "General");
                SetVis("Panel_Apariencia", "Apariencia");
                SetVis("Panel_Lectura", "Lectura");
                SetVis("Panel_Controles", "Controles");
                SetVis("Panel_Rendimiento", "Rendimiento");
                SetVis("Panel_Seguridad", "Seguridad");
                SetVis("Panel_Personalizacion", "Personalizacion");
                SetVis("Panel_Acerca", "Acerca");
            }
            catch { /* safe-ignore UI update errors */ }
        }

        private void InitializeComboBoxValues()
        {
            try
            {
                var settings = ComicReader.Services.SettingsManager.Settings;
                
                // ReadingDirection
                var dirCombo = this.FindName("ReadingDirectionCombo") as ComboBox;
                if (dirCombo != null)
                {
                    var dirTag = settings.CurrentReadingDirection.ToString();
                    foreach (ComboBoxItem item in dirCombo.Items)
                    {
                        if (item.Tag?.ToString() == dirTag)
                        {
                            dirCombo.SelectedItem = item;
                            break;
                        }
                    }
                }

                // ReadingMode
                var modeCombo = this.FindName("ReadingModeCombo") as ComboBox;
                if (modeCombo != null)
                {
                    foreach (ComboBoxItem item in modeCombo.Items)
                    {
                        if (item.Tag?.ToString() == settings.ReadingMode)
                        {
                            modeCombo.SelectedItem = item;
                            break;
                        }
                    }
                }

                // PageTurnAnimation
                var animCombo = this.FindName("PageTurnAnimationCombo") as ComboBox;
                if (animCombo != null)
                {
                    foreach (ComboBoxItem item in animCombo.Items)
                    {
                        if (item.Tag?.ToString() == settings.PageTurnAnimation)
                        {
                            animCombo.SelectedItem = item;
                            break;
                        }
                    }
                }

                // UIScale
                var scaleCombo = this.FindName("UIScaleCombo") as ComboBox;
                if (scaleCombo != null)
                {
                    foreach (ComboBoxItem item in scaleCombo.Items)
                    {
                        if (item.Tag?.ToString() == settings.UIScale)
                        {
                            scaleCombo.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var vm = this.DataContext as SettingsViewModel;
                try { vm?.SaveCommand?.Execute(null); } catch { }
                this.Close();
            }
            catch { this.Close(); }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var vm = this.DataContext as SettingsViewModel;
                var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "JSON settings|*.json", FileName = "percys-settings.json", Title = "Exportar ajustes" };
                if (dlg.ShowDialog(this) == true)
                {
                    vm?.ExportTo(dlg.FileName);
                    ComicReader.Services.Notifications.NotificationService.Instance.Success("Ajustes exportados correctamente", "Exportación completada");
                }
            }
            catch (Exception ex)
            {
                ComicReader.Services.ErrorHandling.ErrorHandler.Instance.HandleException(ex, "Exportar ajustes", ComicReader.Services.ErrorHandling.ErrorRecoveryStrategy.Notify);
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var vm = this.DataContext as SettingsViewModel;
                var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "JSON settings|*.json", Title = "Importar ajustes" };
                if (dlg.ShowDialog(this) == true)
                {
                    vm?.ImportFrom(dlg.FileName);
                    ComicReader.Services.Notifications.NotificationService.Instance.Success("Ajustes importados y aplicados", "Importación completada");
                }
            }
            catch (Exception ex)
            {
                ComicReader.Services.ErrorHandling.ErrorHandler.Instance.HandleException(ex, "Importar ajustes", ComicReader.Services.ErrorHandling.ErrorRecoveryStrategy.Notify);
            }
        }

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var vm = this.DataContext as SettingsViewModel;
                vm?.PreviewCommand?.Execute(null);
            }
            catch { }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var vm = this.DataContext as SettingsViewModel;
                vm?.FilterSections(((TextBox)sender).Text);
            }
            catch { }
        }

        private void ReadingDirectionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var combo = sender as ComboBox;
                var item = combo?.SelectedItem as ComboBoxItem;
                if (item?.Tag is string tag)
                {
                    if (Enum.TryParse<ComicReader.Services.ReadingDirection>(tag, out var direction))
                    {
                        ComicReader.Services.SettingsManager.Settings.CurrentReadingDirection = direction;
                        ComicReader.Services.SettingsManager.SaveSettings();
                    }
                }
            }
            catch { }
        }

        private void ReadingModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var combo = sender as ComboBox;
                var item = combo?.SelectedItem as ComboBoxItem;
                if (item?.Tag is string mode)
                {
                    ComicReader.Services.SettingsManager.Settings.ReadingMode = mode;
                    ComicReader.Services.SettingsManager.SaveSettings();
                }
            }
            catch { }
        }

        private void PageTurnAnimationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var combo = sender as ComboBox;
                var item = combo?.SelectedItem as ComboBoxItem;
                if (item?.Tag is string animation)
                {
                    ComicReader.Services.SettingsManager.Settings.PageTurnAnimation = animation;
                    ComicReader.Services.SettingsManager.SaveSettings();
                }
            }
            catch { }
        }

        private void UIScaleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var combo = sender as ComboBox;
                var item = combo?.SelectedItem as ComboBoxItem;
                if (item?.Tag is string scale)
                {
                    ComicReader.Services.SettingsManager.Settings.UIScale = scale;
                    ComicReader.Services.SettingsManager.SaveSettings();
                }
            }
            catch { }
        }
    }
}