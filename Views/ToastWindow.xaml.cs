using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Threading.Tasks;

namespace ComicReader.Views
{
    public partial class ToastWindow : Window
    {
        public ToastWindow()
        {
            InitializeComponent();
            Loaded += ToastWindow_Loaded;
        }

        private async void ToastWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // position bottom-right of the primary screen's work area
            var wa = SystemParameters.WorkArea;
            Left = wa.Right - Width - 16;
            Top = wa.Bottom - Height - 16;

            var fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(200)));
            this.BeginAnimation(OpacityProperty, fadeIn);

            await Task.Delay(2200);

            var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(300)));
            fadeOut.Completed += (s, a) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }

        public static void ShowToast(string message)
        {
            var w = new ToastWindow();
            w.MessageText.Text = message;
            w.WindowStartupLocation = WindowStartupLocation.Manual;
            var desktop = SystemParameters.WorkArea;
            w.Left = desktop.Right - w.Width - 20;
            w.Top = desktop.Bottom - w.Height - 20;
            w.Show();
        }
    }
}
