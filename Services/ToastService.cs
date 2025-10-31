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
                    ToastWindow.ShowToast(message, null, null, 2200);
                }
                catch { }
            });
        }

        public static void Show(string message, string actionLabel, Action action)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                try
                {
                    ToastWindow.ShowToast(message, actionLabel, action, 2200);
                }
                catch { }
            });
        }

        // Expose overload that allows specifying duration in milliseconds
        public static void Show(string message, string actionLabel, Action action, int durationMs)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                try
                {
                    ToastWindow.ShowToast(message, actionLabel, action, durationMs);
                }
                catch { }
            });
        }
    }
}
