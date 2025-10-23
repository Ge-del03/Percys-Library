using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Media.Imaging;
using ComicReader.Core.Abstractions;

namespace ComicReader.Core.Services
{
    public class MultiLevelImageCache : IImageCache, IDisposable
    {
        private readonly ConcurrentDictionary<string, (BitmapImage image, DateTime ts)> _memory = new();
        private readonly int _memoryLimit;
        private readonly string _diskPath;
        private readonly object _diskLock = new();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ConcurrentBag<Task> _backgroundTasks = new ConcurrentBag<Task>();

        public MultiLevelImageCache(int memoryLimit = 200, string diskFolder = null)
        {
            _memoryLimit = memoryLimit;
            _diskPath = diskFolder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PercysLibrary", "Cache");
            Directory.CreateDirectory(_diskPath);
        }

        public async Task<BitmapImage> Get(string key)
        {
            if (_memory.TryGetValue(key, out var entry))
            {
                _memory[key] = (entry.image, DateTime.UtcNow);
                return entry.image;
            }
            var file = Path.Combine(_diskPath, SafeFileName(key) + ".png");
            if (File.Exists(file))
            {
                try
                {
                    // Load from disk on a background thread to avoid blocking UI callers
                    var bmp = await Task.Run(() =>
                    {
                        try
                        {
                            var local = new BitmapImage();
                            using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                            local.BeginInit();
                            local.CacheOption = BitmapCacheOption.OnLoad;
                            local.StreamSource = fs;
                            local.EndInit();
                            local.Freeze();
                            return local;
                        }
                        catch { return null; }
                    }).ConfigureAwait(false);

                    if (bmp != null)
                    {
                        _memory[key] = (bmp, DateTime.UtcNow);
                        EnforceMemoryLimit();
                        return bmp;
                    }
                }
                catch { }
            }
            return null;
        }

        public Task Set(string key, BitmapImage image)
        {
            if (image == null) return Task.CompletedTask;
            _memory[key] = (image, DateTime.UtcNow);
            EnforceMemoryLimit();
            try
            {
                var t = Task.Run(() => PersistToDisk(key, image), _cts.Token);
                _backgroundTasks.Add(t);
            }
            catch { }
            return Task.CompletedTask;
        }

        public void PurgeMemory()
        {
            _memory.Clear();
        }

        private void EnforceMemoryLimit()
        {
            if (_memory.Count <= _memoryLimit) return;
            foreach (var kv in _memory.OrderBy(k => k.Value.ts).Take(_memory.Count - _memoryLimit))
                _memory.TryRemove(kv.Key, out _);
        }

        private void PersistToDisk(string key, BitmapImage image)
        {
            try
            {
                var file = Path.Combine(_diskPath, SafeFileName(key) + ".png");
                if (File.Exists(file)) return;
                lock (_diskLock)
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    using var fs = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    encoder.Save(fs);
                }
            }
            catch { }
        }

        public void Dispose()
        {
            try { _cts.Cancel(); } catch { }
            try { Task.WaitAll(_backgroundTasks.ToArray(), 1000); } catch { }
            try { _cts.Dispose(); } catch { }
        }

        private string SafeFileName(string key)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                key = key.Replace(c, '_');
            return key.Length > 120 ? key.Substring(0, 120) : key;
        }
    }
}
