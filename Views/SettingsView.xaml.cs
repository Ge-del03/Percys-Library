// FileName: /Views/SettingsView.xaml.cs
using System;
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
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
#if !DESIGN_TIME
            InitializeComponent();
#endif
            this.DataContext = SettingsManager.Settings;
            if (SettingsManager.Settings is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged += (_, __) => { SettingsManager.SaveSettings(); };
            }
        }

        private static readonly Regex _svNumericRegex = new Regex("^[0-9]+$");

        private void SV_NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !_svNumericRegex.IsMatch(e.Text);
        }

        private void SV_NumericOnly_Pasting(object sender, DataObjectPastingEventArgs e)
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

        private void ResetFullscreenSettings_Click(object sender, RoutedEventArgs e)
        {
            // Valores recomendados por defecto
            var s = SettingsManager.Settings;
            s.AutoEnterImmersiveOnOpen = false;    // No entrar automáticamente en inmersivo
            s.FadeOnFullscreenTransitions = true;  // Fundidos activados
            SettingsManager.SaveSettings();
        }

        private void ApplyPdfRender_Click(object sender, RoutedEventArgs e)
        {
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
    }
}
