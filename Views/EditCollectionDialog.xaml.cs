using System;
using System.Windows;
using Microsoft.Win32;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Input;

namespace ComicReader.Views
{
    public partial class EditCollectionDialog : Window
    {
        public string CollectionName { get => NameBox.Text.Trim(); set => NameBox.Text = value; }
        public string Description { get => DescBox.Text.Trim(); set => DescBox.Text = value; }
        public string CoverPath { get; set; }

        private System.Windows.Point _dragStartPoint;
        private readonly Stack<(List<Core.Abstractions.ComicItemDto> items, int index)> _undoStack = new Stack<(List<Core.Abstractions.ComicItemDto>, int)>();

        public EditCollectionDialog()
        {
            InitializeComponent();
        }

        public List<Core.Abstractions.ComicItemDto> Items
        {
            get => ItemsList.Items.Cast<Core.Abstractions.ComicItemDto>().ToList();
            set
            {
                ItemsList.Items.Clear();
                if (value == null) return;
                foreach (var it in value) ItemsList.Items.Add(it);
            }
        }

        private void AddComics_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Cómics|*.cbz;*.cbr;*.zip;*.rar|Todos los archivos|*.*";
            dlg.Multiselect = true;
            if (dlg.ShowDialog(this) == true)
            {
                foreach (var f in dlg.FileNames)
                {
                    var item = new Core.Abstractions.ComicItemDto { Path = f, Title = System.IO.Path.GetFileNameWithoutExtension(f), ThumbPath = string.Empty };
                    ItemsList.Items.Add(item);
                }
            }
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            var selected = ItemsList.SelectedItems.Cast<Core.Abstractions.ComicItemDto>().ToList();
            if (selected.Count == 0) return;
            int firstIndex = ItemsList.Items.IndexOf(selected.First());
            _undoStack.Push((selected, firstIndex));
            foreach (var s in selected) ItemsList.Items.Remove(s);
            UndoButton.IsEnabled = true;
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (_undoStack.Count == 0) return;
            var (items, index) = _undoStack.Pop();
            int insertAt = Math.Min(index, ItemsList.Items.Count);
            foreach (var it in items)
            {
                ItemsList.Items.Insert(insertAt++, it);
            }
            UndoButton.IsEnabled = _undoStack.Count > 0;
        }

        private void SelectCover_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.bmp|Todos los archivos|*.*";
            if (dlg.ShowDialog(this) == true)
            {
                CoverPath = dlg.FileName;
                CoverPathText.Text = CoverPath;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CollectionName))
            {
                MessageBox.Show(this, "El nombre no puede estar vacío.", "Nombre requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
            Close();
        }

        // Drag & drop handlers for ItemsList
        private void ItemsList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void ItemsList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            var pos = e.GetPosition(null);
            if (Math.Abs(pos.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(pos.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance) return;

            var list = ItemsList;
            var item = list.SelectedItem as Core.Abstractions.ComicItemDto;
            if (item == null) return;

            var data = new DataObject("ComicItem", item);
            DragDrop.DoDragDrop(list, data, DragDropEffects.Move | DragDropEffects.Copy);
        }

        private void ItemsList_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent("ComicItem"))
                e.Effects = DragDropEffects.Copy | DragDropEffects.Move;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void ItemsList_Drop(object sender, DragEventArgs e)
        {
            var list = ItemsList;
            var point = e.GetPosition(list);
            int index = GetCurrentIndex(point);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var f in files)
                {
                    var item = new Core.Abstractions.ComicItemDto { Path = f, Title = System.IO.Path.GetFileNameWithoutExtension(f), ThumbPath = string.Empty };
                    if (index >= 0 && index <= list.Items.Count)
                    {
                        list.Items.Insert(index++, item);
                    }
                    else
                    {
                        list.Items.Add(item);
                    }
                }
                e.Handled = true;
                return;
            }

            if (e.Data.GetDataPresent("ComicItem"))
            {
                var dragged = e.Data.GetData("ComicItem") as Core.Abstractions.ComicItemDto;
                if (dragged == null) return;
                int oldIndex = list.Items.IndexOf(dragged);
                if (oldIndex >= 0) list.Items.RemoveAt(oldIndex);
                if (index > list.Items.Count) index = list.Items.Count;
                list.Items.Insert(index, dragged);
                e.Handled = true;
            }
        }

        private int GetCurrentIndex(System.Windows.Point point)
        {
            for (int i = 0; i < ItemsList.Items.Count; i++)
            {
                var item = ItemsList.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (item == null) continue;
                var bounds = new Rect(item.TranslatePoint(new System.Windows.Point(0, 0), ItemsList), item.RenderSize);
                if (bounds.Contains(point)) return i;
            }
            return ItemsList.Items.Count;
        }
    }
}
