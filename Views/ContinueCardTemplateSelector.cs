using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ComicReader.Views
{
    public class ContinueCardTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? FullTemplate { get; set; }
        public DataTemplate? CompactTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement element)
            {
                var homeView = FindAncestor<HomeView>(element);
                if (homeView?.ContinueCompactMode == true && CompactTemplate != null)
                {
                    return CompactTemplate;
                }
            }

            return FullTemplate ?? base.SelectTemplate(item, container);
        }

        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match)
                {
                    return match;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}
