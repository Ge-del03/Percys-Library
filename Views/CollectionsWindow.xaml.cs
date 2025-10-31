using System.Windows;
using Microsoft.Win32;
using ComicReader.ViewModels;
using ComicReader.Core.Abstractions;

namespace ComicReader.Views
{
    public partial class CollectionsWindow : Window
    {
        private CollectionsViewModel Vm => DataContext as CollectionsViewModel;

        public CollectionsWindow()
        {
            InitializeComponent();
            // DataContext is set in XAML
        }

        private void NewCollection_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new NewCollectionDialog() { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                var req = new CollectionCreateRequest { Name = dlg.CollectionName, Description = dlg.Description, CoverPath = dlg.CoverPath };
                Vm?.CreateFromRequest(req);
            }
        }

        private void ImportCollections_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "JSON Collections|*.json|All Files|*.*";
            if (dlg.ShowDialog(this) == true)
            {
                Vm?.ImportFromFile(dlg.FileName);
                ComicReader.Services.ToastService.Show("Importación completada.");
            }
        }

        private void ExportCollections_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog();
            dlg.Filter = "JSON Collections|*.json|All Files|*.*";
            dlg.FileName = "collections-export.json";
            if (dlg.ShowDialog(this) == true)
            {
                Vm?.ExportToFile(dlg.FileName);
                ComicReader.Services.ToastService.Show("Exportación guardada.");
            }
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Ctrl+I => Import
            if (e.Key == System.Windows.Input.Key.I && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                ImportCollections_Click(this, new RoutedEventArgs());
                e.Handled = true;
                return;
            }

            // Ctrl+E => Export
            if (e.Key == System.Windows.Input.Key.E && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                ExportCollections_Click(this, new RoutedEventArgs());
                e.Handled = true;
                return;
            }

            // Delete => delete focused collection
            if (e.Key == System.Windows.Input.Key.Delete)
            {
                var focused = System.Windows.Input.FocusManager.GetFocusedElement(this) as System.Windows.DependencyObject;
                var card = FindParent<Controls.CollectionCard>(focused);
                if (card != null)
                {
                    // call viewmodel delete
                    var dto = card.DataContext as Core.Abstractions.CollectionDto;
                    if (dto != null) Vm?.DeleteCollection(dto);
                    e.Handled = true;
                }
            }

            // Enter => open edit when a CollectionCard is focused
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                var focused = System.Windows.Input.FocusManager.GetFocusedElement(this) as System.Windows.DependencyObject;
                var card = FindParent<Controls.CollectionCard>(focused);
                if (card != null)
                {
                    // invoke edit
                    card.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                    e.Handled = true;
                }
            }
        }

        private static T FindParent<T>(System.Windows.DependencyObject child) where T : class
        {
            if (child == null) return null;
            var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T t) return t;
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }
            return null;
        }
    }
}
