using System.Linq;
using Xunit;
using ComicReader.Themes;

namespace SettingsManager.Tests
{
    public class ThemeResourceKeysTests
    {
        [Fact]
        public void EachTheme_ShouldContain_ImportantBrushKeys()
        {
            var issues = new System.Collections.Generic.List<string>();
            foreach (var info in ThemeManager.GetAvailableThemes())
            {
                var dict = ThemeManager.GetThemeDictionary(info.Mode);
                if (dict == null)
                {
                    issues.Add($"Theme {info.Mode} dictionary is null");
                    continue;
                }

                string[] required = new[] { "AccentBrush", "WindowBackgroundBrush", "TextBrush" };
                foreach (var key in required)
                {
                    if (!dict.Contains(key)) issues.Add($"Theme {info.Mode}: missing {key}");
                }
            }

            if (issues.Any())
            {
                var msg = string.Join("\n", issues);
                Assert.True(false, "Theme resource keys missing:\n" + msg);
            }
            Assert.True(true);
        }
    }
}
