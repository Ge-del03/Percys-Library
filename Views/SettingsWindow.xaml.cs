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

            _vm = this.Resources["SettingsVM"] as SettingsViewModel;
            if (this.DataContext == null) this.DataContext = _vm;

            // after load, sync visibility with SelectedSection
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                UpdateSectionVisibility(_vm?.SelectedSection);
                if (_vm != null)
                {
                    _vm.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(SettingsViewModel.SelectedSection))
                            UpdateSectionVisibility(_vm.SelectedSection);
                    };
                }
            }));
        }

        private void UpdateSectionVisibility(string section)
        {
            try
            {
                Panel_General.Visibility = section == "General" ? Visibility.Visible : Visibility.Collapsed;
                Panel_Apariencia.Visibility = section == "Apariencia" ? Visibility.Visible : Visibility.Collapsed;
                Panel_Lectura.Visibility = section == "Lectura" ? Visibility.Visible : Visibility.Collapsed;
                Panel_Controles.Visibility = section == "Controles" ? Visibility.Visible : Visibility.Collapsed;
                Panel_Rendimiento.Visibility = section == "Rendimiento" ? Visibility.Visible : Visibility.Collapsed;
                Panel_Seguridad.Visibility = section == "Seguridad" ? Visibility.Visible : Visibility.Collapsed;
                Panel_Personalizacion.Visibility = section == "Personalizacion" ? Visibility.Visible : Visibility.Collapsed;
                Panel_Acerca.Visibility = section == "Acerca" ? Visibility.Visible : Visibility.Collapsed;
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