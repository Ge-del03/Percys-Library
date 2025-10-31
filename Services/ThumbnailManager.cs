using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ComicReader.Services
{
    /// <summary>
    /// Simple background thumbnail generator with limited concurrency and disk cache.
    /// Designed to be safe to call from ViewModels and tests.
    /// </summary>
    public class ThumbnailManager : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentDictionary<string, Task> _tasks = new ConcurrentDictionary<string, Task>(StringComparer.OrdinalIgnoreCase);
        private readonly string _cacheDir;
        private bool _disposed;

        public ThumbnailManager(int maxConcurrency = 2)
        {
            _semaphore = new SemaphoreSlim(maxConcurrency);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _cacheDir = Path.Combine(appData, "PercysLibrary", "Thumbs");
            Directory.CreateDirectory(_cacheDir);
        }

        public string TryGetCached(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath)) return null;
                var cachePath = ComputePath(filePath);
                return File.Exists(cachePath) ? cachePath : null;
            }
            catch { return null; }
        }

        public string ComputePath(string filePath)
        {
            using (var sha1 = SHA1.Create())
            {
                var key = "v2|" + filePath;
                var hash = BitConverter.ToString(sha1.ComputeHash(Encoding.UTF8.GetBytes(key))).Replace("-", string.Empty);
                return Path.Combine(_cacheDir, hash + ".png");
            }
        }

        /// <summary>
        /// Enqueue thumbnail generation for a file if not already present. The callback is invoked on completion
        /// with the path to the generated thumbnail (or null if generation failed).
        /// </summary>
        public void EnqueueGenerate(string filePath, Func<string, Task> onComplete, int width = 300, int height = 400)
        {
            if (string.IsNullOrWhiteSpace(filePath) || onComplete == null) return;
            if (_tasks.ContainsKey(filePath)) return;

            var t = Task.Run(async () =>
            {
                await _semaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    var result = await GenerateThumbAsync(filePath, width, height).ConfigureAwait(false);
                    try { await onComplete(result).ConfigureAwait(false); } catch { }
                }
                finally
                {
                    _semaphore.Release();
                    _tasks.TryRemove(filePath, out _);
                }
            });

            _tasks.TryAdd(filePath, t);
        }

        private async Task<string> GenerateThumbAsync(string filePath, int width, int height)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath)) return null;
                if (!File.Exists(filePath) && !Directory.Exists(filePath)) return null;

                // Use ComicPageLoader if available; fall back to BitmapFrame attempt if not.
                BitmapSource cover = null;
                try
                {
                    using (var loader = new ComicReader.Services.ComicPageLoader(filePath))
                    {
                        await loader.LoadComicAsync().ConfigureAwait(false);
                        cover = await loader.GetCoverThumbnailAsync(width, height).ConfigureAwait(false);
                    }
                }
                catch { }

                if (cover == null)
                {
                    return null;
                }

                var path = ComputePath(filePath);
                try
                {
                    using (var fs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        var enc = new PngBitmapEncoder();
                        enc.Frames.Add(BitmapFrame.Create(cover));
                        enc.Save(fs);
                    }
                    return path;
                }
                catch { return null; }
            }
            catch { return null; }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _semaphore?.Dispose();
            _disposed = true;
        }
    }
}
