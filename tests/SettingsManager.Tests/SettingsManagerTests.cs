using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ComicReader.Services;
using Xunit;

namespace SettingsManager.Tests
{
    public class SettingsManagerTests
    {
        [Fact]
        public void SerializeRoundtrip_AppSettings_PreservesValues()
        {
            var s = new AppSettings
            {
                Theme = "UnitTestTheme",
                ThumbnailsVisible = false,
                Brightness = 1.23,
                Contrast = 0.89,
                DefaultComicsFolder = "C:\\tmp"
            };

            var txt = JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true });
            var obj = JsonSerializer.Deserialize<AppSettings>(txt);
            Assert.NotNull(obj);
            Assert.Equal(s.Theme, obj.Theme);
            Assert.Equal(s.ThumbnailsVisible, obj.ThumbnailsVisible);
            Assert.Equal(s.Brightness, obj.Brightness);
            Assert.Equal(s.Contrast, obj.Contrast);
            Assert.Equal(s.DefaultComicsFolder, obj.DefaultComicsFolder);
        }

        [Fact]
        public void SaveNow_WritesSettingsFile_And_RestoresBackup()
        {
            var path = ComicReader.Services.SettingsManager.GetSettingsFilePath();
            var bak = path + ".bak_test";
            bool hadBak = false;
            try
            {
                if (File.Exists(path))
                {
                    File.Copy(path, bak, true);
                    hadBak = true;
                }

                var tmp = new AppSettings { Theme = "FromTest", DefaultComicsFolder = Path.GetTempPath() };
                ComicReader.Services.SettingsManager.ReplaceSettings(tmp);
                ComicReader.Services.SettingsManager.SaveNow();

                Assert.True(File.Exists(path), "settings file should exist after SaveNow");

                // Quick sanity: file contains the theme
                var content = File.ReadAllText(path);
                Assert.Contains("FromTest", content);
            }
            finally
            {
                try
                {
                    if (File.Exists(path)) File.Delete(path);
                }
                catch { }

                try
                {
                    if (hadBak && File.Exists(bak)) File.Move(bak, path);
                    else if (File.Exists(bak)) File.Delete(bak);
                }
                catch { }
            }
        }
    }
}
