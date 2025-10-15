using System;
using System.Windows;
using System.Windows.Controls;

namespace ComicReader.Views
{
    public partial class ReadingCoreControl : UserControl
    {
        public event Action<string> ViewModeChanged;
        public event Action<int> SpacingChanged;
        public event Action CloseRequested;

        public ReadingCoreControl()
        {
            InitializeComponent();
        }

        private void ViewMode_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string tag)
            {
                ViewModeChanged?.Invoke(tag);
            }
        }

        private void SpacingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SpacingChanged?.Invoke((int)e.NewValue);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke();
        }

        // Helpers públicos para ser invocados desde código externo
        public void SetViewMode(string mode)
        {
            try
            {
                if (mode == "ContinuousScroll")
                    RbContinuous.IsChecked = true;
                else
                    RbSingle.IsChecked = true;
            }
            catch { }
        }

        public void SetSpacing(int px)
        {
            try
            {
                SpacingSlider.Value = Math.Max(0, Math.Min(128, px));
            }
            catch { }
        }
    }
}

