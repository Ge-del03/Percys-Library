using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ComicReader.Views.Controls
{
    public partial class ProgressComicItem : UserControl
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, BitmapImage> _imageCache = new System.Collections.Concurrent.ConcurrentDictionary<string, BitmapImage>();

        public ProgressComicItem()
        {
            InitializeComponent();
            this.Loaded += ProgressComicItem_Loaded;
        }

        private void ProgressComicItem_Loaded(object sender, RoutedEventArgs e)
        {
            // Lazy-load thumbnail if a path is provided
            if (!string.IsNullOrEmpty(ThumbnailPath))
            {
                var bmp = _imageCache.GetOrAdd(ThumbnailPath, path =>
                {
                    try
                    {
                        var bi = new BitmapImage();
                        bi.BeginInit();
                        bi.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                        bi.DecodePixelWidth = 200; // optimize size
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.EndInit();
                        bi.Freeze();
                        return bi;
                    }
                    catch
                    {
                        return null;
                    }
                });

                if (bmp != null)
                    ThumbnailImage.Source = bmp;
            }

            // placeholder visibility and fade-in for thumbnail
            try
            {
                bool hasSource = false;
                try { hasSource = ThumbnailImage?.Source != null; } catch { }
                PlaceholderIcon.Visibility = hasSource ? Visibility.Collapsed : Visibility.Visible;
                if (hasSource && SystemParameters.ClientAreaAnimation)
                {
                    var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(240));
                    ThumbnailImage.BeginAnimation(OpacityProperty, fade);
                }
            }
            catch { }

            // Fade in if animations are enabled
            try
            {
                if (SystemParameters.ClientAreaAnimation)
                {
                    var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                    this.BeginAnimation(OpacityProperty, anim);
                }
                else
                {
                    this.Opacity = 1;
                }
            }
            catch { this.Opacity = 1; }
        }

        public static readonly DependencyProperty ThumbnailPathProperty = DependencyProperty.Register(
            nameof(ThumbnailPath), typeof(string), typeof(ProgressComicItem), new PropertyMetadata(string.Empty, OnThumbnailPathChanged));

        public static readonly DependencyProperty PagesTextProperty = DependencyProperty.Register(
            nameof(PagesText), typeof(string), typeof(ProgressComicItem), new PropertyMetadata(string.Empty, OnPagesTextChanged));

        public static readonly DependencyProperty AccessibilityHelpProperty = DependencyProperty.Register(
            nameof(AccessibilityHelp), typeof(string), typeof(ProgressComicItem), new PropertyMetadata(string.Empty));

        private static void OnThumbnailPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressComicItem ctrl)
            {
                ctrl.LoadThumbnailAsync(e.NewValue as string);
            }
        }

        public string ThumbnailPath
        {
            get => (string)GetValue(ThumbnailPathProperty);
            set => SetValue(ThumbnailPathProperty, value);
        }

        public string PagesText
        {
            get => (string)GetValue(PagesTextProperty);
            set => SetValue(PagesTextProperty, value);
        }

        public string AccessibilityHelp
        {
            get => (string)GetValue(AccessibilityHelpProperty);
            set => SetValue(AccessibilityHelpProperty, value);
        }

        private static void OnPagesTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressComicItem ctrl)
            {
                ctrl.UpdateAccessibilityHelp();
            }
        }

        private async void LoadThumbnailAsync(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    // clear image
                    ThumbnailImage.Source = null;
                    PlaceholderIcon.Visibility = Visibility.Visible;
                    return;
                }

                // load on threadpool to avoid UI freeze
                BitmapImage bmp = null;
                await Task.Run(() =>
                {
                    try
                    {
                        var bi = new BitmapImage();
                        bi.BeginInit();
                        bi.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                        bi.DecodePixelWidth = 200;
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.EndInit();
                        bi.Freeze();
                        bmp = bi;
                    }
                    catch { bmp = null; }
                }).ConfigureAwait(false);

                // assign on UI thread and animate row (slide + fade) when thumbnail appears
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        ThumbnailImage.Source = bmp;
                        PlaceholderIcon.Visibility = bmp == null ? Visibility.Visible : Visibility.Collapsed;

                        if (bmp != null && SystemParameters.ClientAreaAnimation)
                        {
                            // make sure Root has a TranslateTransform
                            try
                            {
                                if (!(Root.RenderTransform is TranslateTransform))
                                {
                                    Root.RenderTransform = new TranslateTransform(6, 0);
                                }

                                var tt = Root.RenderTransform as TranslateTransform;

                                // start from slightly below and transparent
                                tt.Y = 6;
                                Root.Opacity = 0;
                                ThumbnailImage.Opacity = 0;

                                var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                                var slide = new DoubleAnimation(6, 0, TimeSpan.FromMilliseconds(260)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

                                Root.BeginAnimation(UIElement.OpacityProperty, fade);
                                tt.BeginAnimation(TranslateTransform.YProperty, slide);
                                ThumbnailImage.BeginAnimation(UIElement.OpacityProperty, fade);
                            }
                            catch { }
                        }
                    }
                    catch { }
                });
            }
            catch { }
        }

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(ProgressComicItem), new PropertyMetadata(string.Empty, OnTitleChanged));

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressComicItem ctrl)
            {
                ctrl.UpdateAccessibilityHelp();
            }
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty PercentageProperty = DependencyProperty.Register(
            nameof(Percentage), typeof(double), typeof(ProgressComicItem), new PropertyMetadata(0.0, OnPercentageChanged));

        private static void OnPercentageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressComicItem ctrl)
            {
                try
                {
                    var newVal = (double)e.NewValue;
                    if (SystemParameters.ClientAreaAnimation)
                    {
                        var da = new DoubleAnimation(ctrl.InnerProgress.Value, newVal, TimeSpan.FromMilliseconds(350)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                        ctrl.InnerProgress.BeginAnimation(ProgressBar.ValueProperty, da);
                    }
                    else
                    {
                        ctrl.InnerProgress.Value = newVal;
                    }

                    // color the progress bar based on thresholds
                    try
                    {
                        var brush = (Brush)Application.Current.FindResource("RS_AccentBrush");
                        if (newVal >= 80) brush = (Brush)Application.Current.FindResource("RS_SuccessBrush");
                        else if (newVal >= 40) brush = (Brush)Application.Current.FindResource("RS_WarningBrush");
                        else brush = (Brush)Application.Current.FindResource("RS_DangerBrush");
                        ctrl.InnerProgress.Foreground = brush;
                    }
                    catch { }

                    // Update the accessibility help text when percentage changes
                    try { ctrl.UpdateAccessibilityHelp(); } catch { }
                }
                catch { try { ctrl.InnerProgress.Value = (double)e.NewValue; } catch { } }
            }
        }

        public double Percentage
        {
            get => (double)GetValue(PercentageProperty);
            set => SetValue(PercentageProperty, value);
        }

        private void UpdateAccessibilityHelp()
        {
            try
            {
                var pct = Percentage;
                var pages = PagesText ?? string.Empty;
                var title = Title ?? string.Empty;
                var help = $"{title}. {pages}. {pct:F0} por ciento leído.";
                AccessibilityHelp = help;
            }
            catch { }
        }
    }
}
