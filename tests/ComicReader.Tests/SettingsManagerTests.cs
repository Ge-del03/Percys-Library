using System;
using System.IO;
using System.Threading.Tasks;
using ComicReader.Services;
using Xunit;

namespace ComicReader.Tests
{
    public class SettingsManagerTests
    {
        [Fact]
        public async Task SaveNowAndFlush_WritesFile()
        {
            // Ensure settings directory exists and remove existing file for a deterministic test
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PercysLibrary", "settings.xml");
            try { if (File.Exists(path)) File.Delete(path); } catch { }

            // Modify settings and call SaveNow
            SettingsManager.Settings.LastOpenedFilePath = "test-path";
            SettingsManager.SaveNow();

            // Wait for flush (bounded)
            var cts = new System.Threading.CancellationTokenSource(5000);
            await SettingsManager.FlushPendingSavesAsync(cts.Token);

            Assert.True(File.Exists(path));
            var txt = File.ReadAllText(path);
            Assert.Contains("test-path", txt);
        }

        [Fact]
        public async Task DebouncedSave_CoalescesCalls()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PercysLibrary", "settings.xml");
            try { if (File.Exists(path)) File.Delete(path); } catch { }

            SettingsManager.Settings.LastOpenedFilePath = "t1";
            SettingsManager.SaveSettings();
            SettingsManager.Settings.LastOpenedFilePath = "t2";
            SettingsManager.SaveSettings();
            // Both calls within debounce window should coalesce to the latest value
            await SettingsManager.FlushPendingSavesAsync(new System.Threading.CancellationTokenSource(5000).Token);
            var txt = File.ReadAllText(path);
            Assert.Contains("t2", txt);
        }
    }
}
