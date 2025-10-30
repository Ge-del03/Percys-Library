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
                    MessageBox.Show(this, "Ajustes exportados correctamente.", "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch
            {
                MessageBox.Show(this, "No se pudo exportar ajustes.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show(this, "Ajustes importados y aplicados.", "Importar", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch
            {
                MessageBox.Show(this, "No se pudo importar ajustes.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }
}