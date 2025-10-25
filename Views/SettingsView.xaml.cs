using System;
using System.Windows;
using System.Windows.Controls;
using ComicReader.ViewModels;

namespace ComicReader.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            // Ensure DataContext
            if (DataContext == null) DataContext = new SettingsViewModel();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (DataContext is SettingsViewModel vm && sender is TextBox tb)
                {
                    vm.FilterSections(tb.Text);
                }
            }
            catch { }
        }
    }
}
