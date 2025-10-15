#if false
// FileName: /Views/SettingsView.xaml.cs
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ComicReader.Services;
using System.ComponentModel;

namespace ComicReader.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            this.DataContext = SettingsManager.Settings;
            if (SettingsManager.Settings is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged += (_, __) =>
                {
                    SettingsManager.SaveSettings();
                };
            }
        }

        private static readonly Regex _numericRegex = new Regex("^[0-9]+$");

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !_numericRegex.IsMatch(e.Text);
        }

        private void NumericOnly_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject?.GetDataPresent(DataFormats.Text) == true)
            {
                var text = e.DataObject.GetData(DataFormats.Text) as string;
                if (string.IsNullOrEmpty(text) || !_numericRegex.IsMatch(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}
#endif