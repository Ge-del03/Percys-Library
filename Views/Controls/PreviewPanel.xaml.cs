using System.Windows.Controls;
using System.Windows;

namespace ComicReader.Views.Controls
{
    public partial class PreviewPanel : UserControl
    {
        public PreviewPanel()
        {
            InitializeComponent();
        }

        // DependencyProperty to allow binding from the ViewModel
        public static readonly DependencyProperty PreviewThemeProperty = DependencyProperty.Register(
            "PreviewTheme", typeof(string), typeof(PreviewPanel), new PropertyMetadata(null, OnPreviewThemeChanged));

        public string PreviewTheme
        {
            get => (string)GetValue(PreviewThemeProperty);
            set => SetValue(PreviewThemeProperty, value);
        }

        private static void OnPreviewThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                var panel = d as PreviewPanel;
                var theme = e.NewValue as string;
                System.Console.WriteLine($"[Debug] PreviewPanel.PreviewTheme changed (DP) to '{theme}'");
                panel?.ApplyPreviewTheme(theme);
            }
            catch { }
        }

        // Apply a simple preview effect based on theme name (non-persistent)
        public void ApplyPreviewTheme(string themeName)
        {
            try
            {
                System.Console.WriteLine($"[Debug] PreviewPanel.ApplyPreviewTheme called with '{themeName}'");

                // Restore defaults if empty
                if (string.IsNullOrWhiteSpace(themeName))
                {
                    Resources["PreviewBg"] = TryFindResource("WindowBackgroundBrush") ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                    Resources["PreviewAccentBrush"] = TryFindResource("PrimaryBrush") ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xED, 0x6A, 0x00));
                    Resources["PreviewTextBrush"] = TryFindResource("TextBrush") ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                    Resources["PreviewCardBg"] = TryFindResource("CardBackgroundBrush") ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                    Resources["PreviewButtonTextBrush"] = TryFindResource("PrimaryBrush") ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                    return;
                }

                // Try loading ResourceDictionary from several candidate paths to be robust
                var candidates = new[] {
                    $"Themes/{themeName}Theme.xaml",
                    $"Themes/{themeName}.xaml",
                    $"Themes/{themeName.ToLowerInvariant()}Theme.xaml",
                    $"Themes/{themeName.ToLowerInvariant()}.xaml",
                };

                System.Windows.ResourceDictionary rd = null;
                foreach (var c in candidates)
                {
                    try
                    {
                        rd = new System.Windows.ResourceDictionary() { Source = new System.Uri(c, System.UriKind.Relative) };
                        if (rd != null) break;
                    }
                    catch { }
                }

                if (rd != null)
                {
                    if (rd.Contains("PrimaryBrush"))
                        Resources["PreviewAccentBrush"] = rd["PrimaryBrush"];
                    if (rd.Contains("CardBackgroundBrush"))
                        Resources["PreviewCardBg"] = rd["CardBackgroundBrush"];
                    if (rd.Contains("TextBrush"))
                        Resources["PreviewTextBrush"] = rd["TextBrush"];
                    if (rd.Contains("WindowBackgroundBrush"))
                        Resources["PreviewBg"] = rd["WindowBackgroundBrush"];
                    if (rd.Contains("PrimaryBrush"))
                        Resources["PreviewButtonTextBrush"] = rd["PrimaryBrush"];
                    System.Console.WriteLine($"[Debug] PreviewPanel.ApplyPreviewTheme applied resource dictionary for '{themeName}'");
                    // If themeName contains a variant (name:variant), apply a small variant transform
                    var parts = (themeName ?? "").Split(':');
                    if (parts.Length == 2)
                    {
                        var variant = parts[1];
                        ApplyVariantToPreview(variant);
                    }
                }
                else
                {
                    // fallback to simple mapping
                    // handle variant suffix if present
                    var parts = (themeName ?? "").Split(':');
                    if (parts.Length == 2)
                    {
                        ApplyFallbackPreview(parts[0]);
                        ApplyVariantToPreview(parts[1]);
                    }
                    else
                    {
                        ApplyFallbackPreview(themeName);
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[Debug] PreviewPanel.ApplyPreviewTheme failed: {ex.Message}");
            }
        }

        private void ApplyFallbackPreview(string themeName)
        {
            switch ((themeName ?? "").ToLowerInvariant())
            {
                case "comicclassic":
                case "comic classic":
                    this.Resources["PreviewBg"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(247, 239, 224));
                    this.Resources["PreviewAccentBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(221,85,68));
                    this.Resources["PreviewTextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(43,43,43));
                    this.Resources["PreviewCardBg"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255,248,240));
                    this.Resources["PreviewButtonTextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255,255,255));
                    break;
                case "light":
                    this.Resources["PreviewBg"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(250, 250, 250));
                    this.Resources["PreviewAccentBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0,86,255));
                    this.Resources["PreviewTextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(10,10,10));
                    this.Resources["PreviewCardBg"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255,255,255));
                    this.Resources["PreviewButtonTextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255,255,255));
                    break;
                default:
                    this.Resources["PreviewBg"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 18, 24));
                    this.Resources["PreviewAccentBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255,170,0));
                    this.Resources["PreviewTextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(237,237,237));
                    this.Resources["PreviewCardBg"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(21,26,40));
                    this.Resources["PreviewButtonTextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0,0,0));
                    break;
            }
        }

        private void ApplyVariantToPreview(string variant)
        {
            if (string.IsNullOrWhiteSpace(variant)) return;
            variant = variant.Trim().ToLowerInvariant();
            try
            {
                if (variant == "highcontrast")
                {
                    // make accent darker and text higher contrast
                    if (Resources["PreviewAccentBrush"] is System.Windows.Media.SolidColorBrush ab)
                    {
                        var c = ab.Color;
                        var darker = System.Windows.Media.Color.FromArgb(c.A, (byte)(c.R * 0.75), (byte)(c.G * 0.75), (byte)(c.B * 0.75));
                        Resources["PreviewAccentBrush"] = new System.Windows.Media.SolidColorBrush(darker);
                    }
                    if (Resources["PreviewTextBrush"] is System.Windows.Media.SolidColorBrush tb)
                    {
                        Resources["PreviewTextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                    }
                }
                else if (variant == "muted")
                {
                    // desaturate accent slightly
                    if (Resources["PreviewAccentBrush"] is System.Windows.Media.SolidColorBrush ab2)
                    {
                        var c = ab2.Color;
                        var avg = (c.R + c.G + c.B) / 3;
                        var muted = System.Windows.Media.Color.FromArgb(c.A, (byte)((c.R + avg) / 2), (byte)((c.G + avg) / 2), (byte)((c.B + avg) / 2));
                        Resources["PreviewAccentBrush"] = new System.Windows.Media.SolidColorBrush(muted);
                    }
                }
            }
            catch { }
        }
    }
}
