using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Windows.Media.Imaging;

namespace ComicReader.Services
{
    public interface IImageCacheService
    {
        BitmapImage? Get(string key);
        void Set(string key, BitmapImage image);
        void Clear();
        void ClearThumbnails();
    }

    public class ImageCacheService : IImageCacheService
    {
        private class CacheItem
        {
            public BitmapImage Image { get; }
            public DateTime LastAccess { get; set; }

            public CacheItem(BitmapImage image)
            {
                Image = image;
                LastAccess = DateTime.UtcNow;
            }
        }

        private readonly ConcurrentDictionary<string, CacheItem> _cache = new ConcurrentDictionary<string, CacheItem>();
        private readonly int _maxCacheSize;
        private readonly object _trimLock = new object();

        public ImageCacheService(int maxCacheSize = 100) // e.g., 100 full-size images
        {
            _maxCacheSize = Math.Max(10, maxCacheSize);
        }

        public BitmapImage? Get(string key)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                item.LastAccess = DateTime.UtcNow;
                return item.Image;
            }
            return null;
        }

        public void Set(string key, BitmapImage image)
        {
            if (image == null) return;

            var item = new CacheItem(image);
            _cache[key] = item;

            if (_cache.Count > _maxCacheSize)
            {
                TrimCache();
            }
        }

        public void Clear()
        {
            _cache.Clear();
            GC.Collect(); // Be a bit more aggressive when clearing everything
        }

        public void ClearThumbnails()
        {
            var thumbKeys = _cache.Keys.Where(k => k.Contains("thumb")).ToList();
            foreach (var key in thumbKeys)
            {
                _cache.TryRemove(key, out _);
            }
        }

        private void TrimCache()
        {
            lock (_trimLock)
            {
                if (_cache.Count <= _maxCacheSize) return;

                var itemsToRemove = _cache.Count - _maxCacheSize;
                if (itemsToRemove <= 0) return;

                var keysToRemove = _cache.OrderBy(kvp => kvp.Value.LastAccess)
                                         .Take(itemsToRemove)
                                         .Select(kvp => kvp.Key)
                                         .ToList();

                foreach (var key in keysToRemove)
                {
                    _cache.TryRemove(key, out _);
                }
            }
        }
    }
}
