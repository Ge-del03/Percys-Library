using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ComicReader.Core.Abstractions;

namespace ComicReader.Core.Services
{
    public class MultiLevelImageCache : IImageCache
    {
        private readonly ConcurrentDictionary<string, (BitmapImage image, DateTime ts)> _memory = new();
        private readonly int _memoryLimit;
        private readonly string _diskPath;
        private readonly object _diskLock = new();

        public MultiLevelImageCache(int memoryLimit = 200, string diskFolder = null)
        {
            _memoryLimit = memoryLimit;
            _diskPath = diskFolder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PercysLibrary", "Cache");
            Directory.CreateDirectory(_diskPath);
        }

        public Task<BitmapImage> Get(string key)
        {
            if (_memory.TryGetValue(key, out var entry))
            {
                _memory[key] = (entry.image, DateTime.UtcNow);
                return Task.FromResult(entry.image);
            }
            var file = Path.Combine(_diskPath, SafeFileName(key) + ".png");
            if (File.Exists(file))
            {
                try
                {
                    var bmp = new BitmapImage();
                    using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = fs;
                    bmp.EndInit();
                    bmp.Freeze();
                    _memory[key] = (bmp, DateTime.UtcNow);
                    EnforceMemoryLimit();
                    return Task.FromResult(bmp);
                }
                catch { }
            }
            return Task.FromResult<BitmapImage>(null);
        }

        public Task Set(string key, BitmapImage image)
        {
            if (image == null) return Task.CompletedTask;
            _memory[key] = (image, DateTime.UtcNow);
            EnforceMemoryLimit();
            Task.Run(() => PersistToDisk(key, image));
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

        private string SafeFileName(string key)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                key = key.Replace(c, '_');
            return key.Length > 120 ? key.Substring(0, 120) : key;
        }
    }
}
