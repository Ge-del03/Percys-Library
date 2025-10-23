using System;
using System.IO;
using Xunit;
using ComicReader.ViewModels;
using ComicReader;

namespace ComicReader.Tests
{
    public class SettingsViewModelTests
    {
        [Fact]
        public void PreviewTheme_DefaultsToSomething()
        {
            var vm = new SettingsViewModel();
            // PreviewTheme may be null until PreviewCommand sets it
            vm.PreviewCommand.Execute(null);
            Assert.False(string.IsNullOrEmpty(vm.PreviewTheme));
        }

        [Fact]
        public void ExportCommand_WritesFile()
        {
            var vm = new SettingsViewModel();
            var testFile = Path.Combine(Path.GetTempPath(), "percy_settings_export_test.json");
            try
            {
                if (File.Exists(testFile)) File.Delete(testFile);
                // Use ExportTo to write a portable temporary file for testing
                vm.ExportTo(testFile);
                Assert.True(File.Exists(testFile));
                // clean
                File.Delete(testFile);
            }
            catch (Exception ex)
            {
                // If environment prevents writing to Desktop, fail the test with info
                throw new Exception("Export command failed during test", ex);
            }
        }
    }
}
