using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ComicReader.Services;

namespace Collections.Tests
{
    public class ThumbnailManagerSizeTests
    {
        [Fact]
        public async Task EnforceCacheLimit_BySize_DeletesUntilUnderLimit()
        {
            // Arrange
            var tempCacheDir = Path.Combine(Path.GetTempPath(), "thumbs_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempCacheDir);
            var mgr = new ThumbnailManager(maxConcurrency:1, maxCacheFiles:1000, maxCacheBytes: 1024 * 10, cacheDirectory: tempCacheDir); // 10KB

            // Create files: 6 files of 4KB -> total 24KB
            for (int i = 0; i < 6; i++)
            {
                var path = Path.Combine(tempCacheDir, $"tst_{Guid.NewGuid():N}_{i}.png");
                using (var s = File.Create(path))
                {
                    var data = new byte[4 * 1024];
                    new Random().NextBytes(data);
                    await s.WriteAsync(data, 0, data.Length);
                }
                // touch last write time to simulate older/newer
                File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(i));
            }

            // Act
            mgr.EnforceCacheLimit();

            // Assert: total size should be <= 10KB
            var remFiles = Directory.GetFiles(tempCacheDir, "*.png").Select(p => new FileInfo(p)).ToList();
            long total = remFiles.Sum(f => f.Length);
            Assert.True(total <= 1024 * 10, $"Total cache size is {total} bytes, expected <= 10KB");

            // Cleanup
            try { mgr.ClearCache(); } catch { }
            try { if (Directory.Exists(tempCacheDir)) Directory.Delete(tempCacheDir, true); } catch { }
        }
    }
}
