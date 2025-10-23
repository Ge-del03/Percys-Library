using System;
using System.Windows;

namespace ComicReader.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            // Log basic construction
            Console.WriteLine($"[Debug] SettingsWindow constructed. DataContext set: { (DataContext != null) }");
            this.Loaded += SettingsWindow_Loaded;
            // Try to ensure the dialog appears above other windows (debug aide)
            try
            {
                this.Activated += (s, e) => { try { this.Activate(); } catch { } };
                // briefly set Topmost to bring to front then clear it
                this.Topmost = true;
                this.Dispatcher.BeginInvoke(new Action(() => { try { this.Topmost = false; this.Focus(); } catch { } }));
            }
            catch { }
        }

    private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var dc = this.DataContext;
                Console.WriteLine($"[Debug] SettingsWindow Loaded. DataContext type: {dc?.GetType().FullName ?? "<null>"}");

                // Inspect visual tree lightly: check content presenter or children count
                if (this.Content is FrameworkElement fe)
                {
                    int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(fe);
                    Console.WriteLine($"[Debug] SettingsWindow content is FrameworkElement with visual children count: {childCount}");

                    try
                    {
                        var root = this.FindName("RootGrid") as FrameworkElement;
                        if (root != null)
                        {
                            Console.WriteLine($"[Debug] RootGrid ActualWidth={root.ActualWidth} ActualHeight={root.ActualHeight} Visibility={root.Visibility}");
                        }

                        var sidebar = this.FindName("SidebarBorder") as FrameworkElement;
                        if (sidebar != null)
                        {
                            Console.WriteLine($"[Debug] SidebarBorder Visibility={sidebar.Visibility} Background={sidebar.GetValue(System.Windows.Controls.Control.BackgroundProperty)}");
                        }

                        var content = this.FindName("ContentBorder") as FrameworkElement;
                        if (content != null)
                        {
                            Console.WriteLine($"[Debug] ContentBorder Visibility={content.Visibility} Background={content.GetValue(System.Windows.Controls.Control.BackgroundProperty)}");
                        }

                        var hub = this.FindName("SettingsHubControl") as FrameworkElement;
                        if (hub != null)
                        {
                            Console.WriteLine($"[Debug] SettingsHubControl visibility={hub.Visibility}");
                        }
                    }
                    catch (Exception innerEx)
                    {
                        Console.WriteLine($"[Debug] Exception while inspecting named elements: {innerEx}");
                    }
                }
                else
                {
                    Console.WriteLine("[Debug] SettingsWindow Content is not a FrameworkElement or is null.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Debug] Exception in SettingsWindow_Loaded: {ex}");
            }
        }
    }
}
