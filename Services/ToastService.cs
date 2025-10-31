using System;
using System.Windows;
using ComicReader.Views;

namespace ComicReader.Services
{
    public static class ToastService
    {
        public static void Show(string message)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                try
                {
                    ToastWindow.ShowToast(message);
                }
                catch { }
            });
        }
    }
}
