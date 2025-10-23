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
            this.DataContext = new SettingsViewModel();
        }
    }
}