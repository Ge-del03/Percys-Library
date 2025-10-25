using System.Linq;
using Xunit;
using ComicReader.Themes;

namespace SettingsManager.Tests
{
    public class ThemeManagerTests
    {
        [Fact]
        public void AllThemes_ShouldHaveRequiredKeysAndBrushes()
        {
            var issues = ThemeManager.ValidateAllThemes();
            if (issues != null && issues.Any())
            {
                var msg = string.Join("\n", issues);
                Assert.True(false, "Theme validation failed:\n" + msg);
            }
            Assert.True(true);
        }
    }
}
