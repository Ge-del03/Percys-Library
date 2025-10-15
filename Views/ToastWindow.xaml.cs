using System.Threading.Tasks;
using System.Windows;

namespace ComicReader.Views
{
    public partial class ToastWindow : Window
    {
        public ToastWindow()
        {
            InitializeComponent();
            this.Loaded += ToastWindow_Loaded;
        }

        private async void ToastWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(2500);
            this.Close();
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
