using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ComicReader.Views
{
    public partial class SettingsHub : UserControl
    {
        private ComicReader.ViewModels.SettingsViewModel _vm;

        public SettingsHub()
        {
            InitializeComponent();
            Console.WriteLine($"[Debug] SettingsHub constructed. DataContext set: { (DataContext != null) }");
            this.Loaded += SettingsHub_Loaded;
            this.DataContextChanged += SettingsHub_DataContextChanged;
        }

        private void SettingsHub_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var dc = this.DataContext;
                Console.WriteLine($"[Debug] SettingsHub Loaded. DataContext type: {dc?.GetType().FullName ?? "<null>"}");

                if (this.Content is Panel p)
                {
                    Console.WriteLine($"[Debug] SettingsHub content is Panel with children count: {p.Children.Count}");
                    try
                    {
                        var stack = this.FindName("HubStack") as Panel;
                        if (stack != null)
                        {
                            Console.WriteLine($"[Debug] HubStack children count: {stack.Children.Count}");
                            for (int i = 0; i < stack.Children.Count; i++)
                            {
                                var ch = stack.Children[i] as FrameworkElement;
                                Console.WriteLine($"[Debug] HubStack child[{i}] = {ch?.GetType().FullName ?? "<null>"} Name={ch?.Name}");
                                try
                                {
                                    int gc = VisualTreeHelper.GetChildrenCount(ch ?? (DependencyObject)stack.Children[i]);
                                    for (int j = 0; j < gc; j++)
                                    {
                                        var g = VisualTreeHelper.GetChild(ch ?? (DependencyObject)stack.Children[i], j) as FrameworkElement;
                                        Console.WriteLine($"[Debug]  child[{i}] grandchild[{j}] = {g?.GetType().FullName ?? "<null>"} Name={g?.Name}");
                                    }
                                }
                                catch { }
                            }
                        }

                        var scroll = this.FindName("HubScroll") as ScrollViewer;
                        if (scroll != null)
                        {
                            Console.WriteLine($"[Debug] HubScroll ViewportWidth={scroll.ViewportWidth} ViewportHeight={scroll.ViewportHeight} ActualWidth={scroll.ActualWidth} ActualHeight={scroll.ActualHeight}");
                        }

                        try
                        {
                            var live = this.FindName("LivePreview") as ComicReader.Views.Controls.PreviewPanel;
                            if (live == null)
                            {
                                live = FindChildOfType<ComicReader.Views.Controls.PreviewPanel>(this);
                                Console.WriteLine($"[Debug] FindName failed, FindChildOfType returned: { (live != null) }");
                            }
                            var vmLocal = _vm ?? (this.DataContext as ComicReader.ViewModels.SettingsViewModel);
                            if (live != null && vmLocal != null)
                            {
                                Console.WriteLine("[Debug] LivePreview control found during Loaded. Triggering PreviewCommand on DataContext VM (binding will update PreviewPanel.PreviewTheme).");
                                try { vmLocal.PreviewCommand?.Execute(null); } catch (Exception ex) { Console.WriteLine($"[Debug] Exception executing PreviewCommand: {ex}"); }
                            }
                            else
                            {
                                Console.WriteLine($"[Debug] LivePreview or VM missing: live={(live!=null)}, vm={(vmLocal!=null)}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Debug] Exception while triggering preview: {ex}");
                        }
                    }
                    catch (Exception inner)
                    {
                        Console.WriteLine($"[Debug] Exception inspecting Hub named elements: {inner}");
                    }
                }
                else
                {
                    int childCount = VisualTreeHelper.GetChildrenCount(this);
                    Console.WriteLine($"[Debug] SettingsHub VisualTree children count: {childCount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Debug] Exception in SettingsHub_Loaded: {ex}");
            }
        }

        private void SettingsHub_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (_vm != null) _vm.PropertyChanged -= Vm_PropertyChanged;
                _vm = this.DataContext as ComicReader.ViewModels.SettingsViewModel;
                if (_vm != null)
                {
                    _vm.PropertyChanged += Vm_PropertyChanged;
                    try
                    {
                        var live = this.FindName("LivePreview") as ComicReader.Views.Controls.PreviewPanel;
                        if (live == null) live = FindChildOfType<ComicReader.Views.Controls.PreviewPanel>(this);
                        if (live != null)
                        {
                            Console.WriteLine("[Debug] DataContextChanged: LivePreview found, executing PreviewCommand on VM.");
                            try { _vm.PreviewCommand?.Execute(null); } catch (Exception ex) { Console.WriteLine($"[Debug] Exception executing PreviewCommand during DataContextChanged: {ex}"); }
                        }
                        else
                        {
                            Console.WriteLine("[Debug] DataContextChanged: LivePreview not found yet.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Debug] Exception in DataContextChanged preview trigger: {ex}");
                    }
                }
            }
            catch { }
        }

        private void Vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ComicReader.ViewModels.SettingsViewModel.PreviewTheme) || e.PropertyName == "AppSettings")
            {
                try
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            var live = this.FindName("LivePreview") as ComicReader.Views.Controls.PreviewPanel;
                            if (live != null)
                            {
                                var theme = _vm?.PreviewTheme ?? _vm?.AppSettings?.Theme;
                                live.ApplyPreviewTheme(theme);
                            }
                        }
                        catch { }
                    });
                }
                catch { }
            }
            else if (e.PropertyName == nameof(ComicReader.ViewModels.SettingsViewModel.SelectedSection))
            {
                // When selection changes, try to scroll the corresponding ListBox item into view
                try
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            var vm = _vm;
                            if (vm == null) return;
                            var sidebar = FindChildOfType<System.Windows.Controls.ListBox>(this.Parent as DependencyObject);
                            if (sidebar != null)
                            {
                                var item = sidebar.ItemContainerGenerator.ContainerFromItem(vm.SelectedSection) as System.Windows.Controls.ListBoxItem;
                                if (item != null)
                                {
                                    item.BringIntoView();
                                }
                            }
                        }
                        catch { }
                    }));
                }
                catch { }
            }
        }

        // Quick-Find handlers
        private void QuickFindBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = string.Empty;
            var qb = ResolveQuickFindBox();
            if (qb != null) q = qb.Text ?? string.Empty;
            q = q.Trim();
            if (string.IsNullOrEmpty(q)) return;
            if (this.DataContext is ComicReader.ViewModels.SettingsViewModel vm)
            {
                var match = vm.Sections.FirstOrDefault(s => s?.ToLowerInvariant().Contains(q.ToLowerInvariant()) == true);
                if (!string.IsNullOrEmpty(match)) vm.SelectedSection = match;
            }
        }

        private void QuickFindClear_Click(object sender, RoutedEventArgs e)
        {
            var qb = ResolveQuickFindBox();
            if (qb != null) qb.Text = string.Empty;
        }

        // Resolve the QuickFind TextBox by name or by searching the visual tree
        private System.Windows.Controls.TextBox ResolveQuickFindBox()
        {
            try
            {
                var found = this.FindName("QuickFindBox") as System.Windows.Controls.TextBox;
                if (found != null) return found;
                return FindChildOfType<System.Windows.Controls.TextBox>(this);
            }
            catch { return null; }
        }

        // Helper: busca de forma recursiva un hijo de tipo T en el árbol visual
        private T FindChildOfType<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T found) return found;
                var deeper = FindChildOfType<T>(child);
                if (deeper != null) return deeper;
            }
            return null;
        }
    }
}
