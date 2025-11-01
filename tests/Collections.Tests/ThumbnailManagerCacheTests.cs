using System;
using System.IO;
using System.Linq;
using ComicReader.Services;
using Xunit;

namespace Collections.Tests
{
    public class ThumbnailManagerCacheTests : IDisposable
    {
        private readonly ThumbnailManager _mgr;
        private readonly string[] _fileKeys;
        private readonly string _tempCacheDir;

        public ThumbnailManagerCacheTests()
        {
            // small cache to test eviction
            _tempCacheDir = Path.Combine(Path.GetTempPath(), "thumbs_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempCacheDir);
            _mgr = new ThumbnailManager(1, maxCacheFiles: 3, cacheDirectory: _tempCacheDir);
            _fileKeys = new[] { "a.cbz", "b.cbz", "c.cbz", "d.cbz", "e.cbz" };
        }

        [Fact]
        public void EnforceCacheLimit_removes_oldest_files()
        {
            // create fake cache files with different last write times
            var paths = _fileKeys.Select(k => _mgr.ComputePath(Path.Combine(Path.GetTempPath(), k))).ToArray();
            // ensure directory exists
            var dir = Path.GetDirectoryName(paths[0]);
            Directory.CreateDirectory(dir);
            // clean any leftover cache files to avoid permission/leftover artifacts from other tests
            try
            {
                foreach (var f in Directory.GetFiles(dir, "*.png"))
                {
                    try { File.SetAttributes(f, FileAttributes.Normal); } catch { }
                    try { File.Delete(f); } catch { }
                }
            }
            catch { }

            // create files and set increasing modification times
            for (int i = 0; i < paths.Length; i++)
            {
                File.WriteAllText(paths[i], "x");
                File.SetLastWriteTimeUtc(paths[i], DateTime.UtcNow.AddMinutes(-paths.Length + i));
            }

            // sanity: all files exist
            Assert.Equal(paths.Length, paths.Count(p => File.Exists(p)));

            // Enforce limit
            _mgr.EnforceCacheLimit();

            var remaining = Directory.GetFiles(Path.GetDirectoryName(paths[0]), "*.png");
            // should be <= 3
            Assert.True(remaining.Length <= 3);

            // Ensure the newest 3 remain (by LastWriteTime)
            var ordered = paths.OrderBy(p => File.GetLastWriteTimeUtc(p)).ToArray();
            var expectedKeep = ordered.Skip(ordered.Length - 3).ToArray();
            foreach (var k in expectedKeep)
            {
                Assert.True(File.Exists(k));
            }
        }

        public void Dispose()
        {
            try
            {
                var dir = Path.GetDirectoryName(_mgr.ComputePath(Path.Combine(Path.GetTempPath(), _fileKeys[0])));
                foreach (var f in Directory.GetFiles(dir, "*.png"))
                    try { File.Delete(f); } catch { }
                try { if (Directory.Exists(_tempCacheDir)) Directory.Delete(_tempCacheDir, true); } catch { }
            }
            catch { }
        }
    }
}
