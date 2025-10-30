using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using ComicReader.ViewModels;
using ComicReader.Services;

namespace ComicReader.Tests
{
    internal class WritingThumbnailGenerator : IThumbnailGenerator
    {
        private readonly string _outDir;
        private readonly TaskCompletionSource<string> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public WritingThumbnailGenerator(string outDir)
        {
            _outDir = outDir;
            Directory.CreateDirectory(_outDir);
        }

        public Task<string> GeneratePersistedCoverForPath(string filePath)
        {
            try
            {
                var fileName = Guid.NewGuid().ToString("N") + ".png";
                var outPath = Path.Combine(_outDir, fileName);
                // simple 1x1 PNG (base64)
                var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR4nGNgYAAAAAMAASsJTYQAAAAASUVORK5CYII=");
                File.WriteAllBytes(outPath, png);
                _tcs.TrySetResult(outPath);
                return Task.FromResult(outPath as string);
            }
            catch (Exception ex)
            {
                _tcs.TrySetException(ex);
                return Task.FromResult<string>(null);
            }
        }

        public async Task<string> WaitForPathAsync(TimeSpan timeout)
        {
            var completed = await Task.WhenAny(_tcs.Task, Task.Delay(timeout)).ConfigureAwait(false);
            if (completed == _tcs.Task)
            {
                try { return await _tcs.Task.ConfigureAwait(false); } catch { return null; }
            }
            return null;
        }
    }

    public class FavoritesViewModelIntegrationThumbnailTest
    {
        [Fact]
        public async Task AddingFiles_WritesThumbnailFile_And_ExportContainsPath()
        {
            var dlg = new FakeDialogService();
            var nav = new WpfAppNavigationService();
            var tempDir = Path.Combine(Path.GetTempPath(), "fav_int_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var thumbOut = Path.Combine(tempDir, "covers");
            Directory.CreateDirectory(thumbOut);

            var thumbGen = new WritingThumbnailGenerator(thumbOut);
            var vm = new FavoritesViewModel(dlg, nav, thumbGen);

            var col = new Models.ComicCollection { Name = "Int" };
            vm.AddCollection(col);
            vm.SelectedCollection = col;

            // create a dummy comic file that exists on disk
            var f = Path.Combine(tempDir, "comic.cbz");
            File.WriteAllText(f, "dummy");

            vm.AddFilePathsToSelected(new[] { f });

            // wait for generator to write the file (up to 3s)
            var written = await thumbGen.WaitForPathAsync(TimeSpan.FromSeconds(3));
            Assert.False(string.IsNullOrWhiteSpace(written));
            Assert.True(File.Exists(written));

            // Export and verify json contains the generated path
            var outJson = Path.Combine(tempDir, "export.json");
            try
            {
                vm.ExportToFile(outJson);
                Assert.True(File.Exists(outJson));
                var txt = File.ReadAllText(outJson);
                Assert.Contains(written.Replace("\\", "\\\\"), txt);
            }
            finally
            {
                try { File.Delete(f); } catch { }
                try { File.Delete(outJson); } catch { }
                try { Directory.Delete(thumbOut, true); } catch { }
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }
}
