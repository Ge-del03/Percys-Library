using System;
using System.IO;
using System.Threading.Tasks;
using ComicReader.Services;
using Xunit;

namespace Collections.Tests
{
    public class ThumbnailManagerTests : IDisposable
    {
        private readonly ThumbnailManager _mgr;
        private readonly string _tempFile;

        public ThumbnailManagerTests()
        {
            _mgr = new ThumbnailManager(1);
            _tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".cbz");
            // create an empty file to simulate a comic container
            File.WriteAllText(_tempFile, string.Empty);
        }

        [Fact]
        public async Task EnqueueGenerate_calls_onComplete_even_when_generation_fails()
        {
            var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

            _mgr.EnqueueGenerate(_tempFile, async (result) =>
            {
                // set result (may be null if generation failed)
                tcs.TrySetResult(result);
                await Task.CompletedTask;
            });

            var res = await Task.WhenAny(tcs.Task, Task.Delay(5000));
            Assert.True(res == tcs.Task, "onComplete was not called within timeout");
            // result may be null; just assert the callback was invoked
            Assert.True(tcs.Task.IsCompleted);
        }

        [Fact]
        public void ComputePath_and_TryGetCached_behave_consistently()
        {
            var path = _mgr.ComputePath(_tempFile);
            Assert.False(string.IsNullOrWhiteSpace(path));
            // cached file should not exist yet
            var cached = _mgr.TryGetCached(_tempFile);
            Assert.Null(cached);

            // create an empty cache file and verify TryGetCached finds it
            File.WriteAllText(path, "x");
            var cached2 = _mgr.TryGetCached(_tempFile);
            Assert.Equal(path, cached2);
        }

        public void Dispose()
        {
            try { if (File.Exists(_tempFile)) File.Delete(_tempFile); } catch { }
            try { _mgr.Dispose(); } catch { }
        }
    }
}
