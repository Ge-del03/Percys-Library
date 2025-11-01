using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ComicReader.Services;
using Xunit;

namespace Collections.Tests
{
    public class ThumbnailManagerReconfigureTests : IDisposable
    {
        private readonly ThumbnailManager _mgr;
        private readonly string _tempCacheDir;

        public ThumbnailManagerReconfigureTests()
        {
            _tempCacheDir = Path.Combine(Path.GetTempPath(), "thumbs_reconfig_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempCacheDir);
            // start with higher limits
            _mgr = new ThumbnailManager(2, maxCacheFiles: 10, maxCacheBytes: 10 * 1024 * 1024, cacheDirectory: _tempCacheDir);
        }

        [Fact]
        public void Reconfigure_reduces_cache_files_when_limits_lowered()
        {
            // create 8 dummy files
            for (int i = 0; i < 8; i++) File.WriteAllText(Path.Combine(_tempCacheDir, $"f{i}.png"), "x");
            var filesBefore = Directory.GetFiles(_tempCacheDir, "*.png").Length;
            Assert.Equal(8, filesBefore);

            // lower max files to 3
            _mgr.Reconfigure(2, 3, 1024 * 1024);
            // EnforceCacheLimit should have run; allow a short pause in case
            Task.Delay(200).Wait();
            var filesAfter = Directory.GetFiles(_tempCacheDir, "*.png").Length;
            Assert.InRange(filesAfter, 0, 3);
        }

        [Fact]
        public void Reconfigure_changes_concurrency_without_throwing()
        {
            _mgr.Reconfigure(4, 10, 1024 * 1024 * 1024);
            // no assert other than not throwing; ensure we can still call ComputePath
            var p = _mgr.ComputePath("somepath.cbz");
            Assert.False(string.IsNullOrWhiteSpace(p));
        }

        public void Dispose()
        {
            try { _mgr.Dispose(); } catch { }
            try { if (Directory.Exists(_tempCacheDir)) Directory.Delete(_tempCacheDir, true); } catch { }
        }
    }
}
