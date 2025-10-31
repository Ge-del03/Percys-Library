using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Threading.Tasks;

namespace ComicReader.Views
{
    public partial class ToastWindow : Window
    {
        private Action _action;
        private System.Windows.Threading.DispatcherTimer _timer;
        private int _durationMs = 2200;
        private int _elapsedMs = 0;
        private const int TICK_MS = 50;

        public ToastWindow()
        {
            InitializeComponent();
            Loaded += ToastWindow_Loaded;
        }

        private void ToastWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // position bottom-right of the primary screen's work area
            var wa = SystemParameters.WorkArea;
            Left = wa.Right - Width - 16;
            Top = wa.Bottom - Height - 16;
            var fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(200)));
            this.BeginAnimation(OpacityProperty, fadeIn);

            // start TTL timer to update progress and close when elapsed
            _elapsedMs = 0;
            try
            {
                ProgressFill.Width = 0;
            }
            catch { }

            _timer = new System.Windows.Threading.DispatcherTimer(System.TimeSpan.FromMilliseconds(TICK_MS), System.Windows.Threading.DispatcherPriority.Background, (s, a) =>
            {
                _elapsedMs += TICK_MS;
                try
                {
                    double fraction = Math.Min(1.0, (double)_elapsedMs / Math.Max(1, _durationMs));
                    var totalWidth = (this.ActualWidth - 24); // approximate inner width (padding)
                    ProgressFill.Width = Math.Max(0, totalWidth * fraction);
                }
                catch { }

                if (_elapsedMs >= _durationMs)
                {
                    // stop and fade out
                    _timer.Stop();
                    var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(300)));
                    fadeOut.Completed += (ss, aa) => this.Close();
                    this.BeginAnimation(OpacityProperty, fadeOut);
                }
            }, System.Windows.Threading.Dispatcher.CurrentDispatcher);
            _timer.Start();
        }

        public static void ShowToast(string message)
        {
            ShowToast(message, null, null, 2200);
        }

        public static void ShowToast(string message, string actionLabel, Action action, int durationMs)
        {
            var w = new ToastWindow();
            w.MessageText.Text = message;
            w._action = action;
            w._durationMs = durationMs <= 0 ? 2200 : durationMs;
            if (!string.IsNullOrWhiteSpace(actionLabel) && action != null)
            {
                w.ActionButton.Content = actionLabel;
                w.ActionButton.Visibility = Visibility.Visible;
            }
            w.WindowStartupLocation = WindowStartupLocation.Manual;
            var desktop = SystemParameters.WorkArea;
            w.Left = desktop.Right - w.Width - 20;
            w.Top = desktop.Bottom - w.Height - 20;
            w.Show();
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            try { _action?.Invoke(); } catch { }
            this.Close();
        }
    }
}
