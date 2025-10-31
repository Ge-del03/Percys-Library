using System.Windows;
using System.Windows.Controls;
using ComicReader.Views;

namespace ComicReader.Views.Controls
{
    public partial class CollectionCard : UserControl
    {
        public CollectionCard()
        {
            InitializeComponent();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var win = Window.GetWindow(this);
            var vm = win?.DataContext as ViewModels.CollectionsViewModel;
            var dto = DataContext as Core.Abstractions.CollectionDto;
            if (dto == null || vm == null) return;

            var dlg = new EditCollectionDialog() { Owner = win };
            dlg.CollectionName = dto.Name;
            dlg.Description = dto.Description;
            dlg.CoverPath = dto.CoverPath;
            dlg.CoverPathText.Text = dto.CoverPath;
            if (dlg.ShowDialog() == true)
            {
                var req = new Core.Abstractions.CollectionCreateRequest { Name = dlg.CollectionName, Description = dlg.Description, CoverPath = dlg.CoverPath };
                vm.UpdateFromRequest(dto.Id, req);
                MessageBox.Show(win, "Colección actualizada.", "Editar", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Duplicate_Click(object sender, RoutedEventArgs e)
        {
            var win = Window.GetWindow(this);
            var vm = win?.DataContext as ViewModels.CollectionsViewModel;
            var dto = DataContext as Core.Abstractions.CollectionDto;
            if (dto == null || vm == null) return;
            vm.DuplicateCollection(dto);
            MessageBox.Show(win, "Colección duplicada.", "Duplicar", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var win = Window.GetWindow(this);
            var vm = win?.DataContext as ViewModels.CollectionsViewModel;
            var dto = DataContext as Core.Abstractions.CollectionDto;
            if (dto == null || vm == null) return;

            var res = MessageBox.Show(win, $"¿Eliminar la colección '{dto.Name}'? Esta acción no se puede deshacer.", "Eliminar colección", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes)
            {
                vm.DeleteCollection(dto);
            }
        }
    }
}
