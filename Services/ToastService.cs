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
                    ToastManager.Instance.Enqueue(message, null, null, 2200, ComicReader.Views.ToastWindow.ToastKind.Comic);
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
                    ToastManager.Instance.Enqueue(message, actionLabel, action, 2200, ComicReader.Views.ToastWindow.ToastKind.Comic);
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
                    ToastManager.Instance.Enqueue(message, actionLabel, action, durationMs, ComicReader.Views.ToastWindow.ToastKind.Comic);
                }
                catch { }
            });
        }

        public static void Show(string message, ComicReader.Views.ToastWindow.ToastKind kind)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                try
                {
                    ToastManager.Instance.Enqueue(message, null, null, 2200, kind);
                }
                catch { }
            });
        }

        public static void Show(string message, string actionLabel, Action action, int durationMs, ComicReader.Views.ToastWindow.ToastKind kind)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                try
                {
                    ToastManager.Instance.Enqueue(message, actionLabel, action, durationMs, kind);
                }
                catch { }
            });
        }

        /// <summary>
        /// Try to register an undo action with the central IUndoService; if unavailable, fall back to showing a toast with an actionable button.
        /// </summary>
        public static void ShowWithUndo(string message, string undoLabel, Action undoAction, int durationMs = 2200)
        {
            try
            {
                // Try to resolve the central undo service from the core ServiceLocator
                var undoSvc = ComicReader.Core.Services.ServiceLocator.TryGet<ComicReader.Services.IUndoService>();
                if (undoSvc != null)
                {
                    undoSvc.Register(message, undoLabel, undoAction);
                    return;
                }
            }
            catch { }

            // Fallback to showing a toast with the provided action
            Show(message, undoLabel, undoAction, durationMs);
        }
    }
}
