using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ComicReader.Core.Abstractions;
using ComicReader.Services;
using ComicReader.Commands;
using System.Linq;

namespace ComicReader.ViewModels
{
    public class CollectionsViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        private readonly ICollectionService _service;

        public ObservableCollection<CollectionDto> Collections { get; } = new ObservableCollection<CollectionDto>();

        public ICommand NewCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand DuplicateCommand { get; }
        public ICommand DeleteCommand { get; }

        public CollectionsViewModel()
        {
            _service = new CollectionServiceJson();
            NewCommand = new RelayCommand(_ => NewCollection());
            RenameCommand = new RelayCommand(p => Rename(p as CollectionDto), p => p is CollectionDto);
            DuplicateCommand = new RelayCommand(p => Duplicate(p as CollectionDto), p => p is CollectionDto);
            DeleteCommand = new RelayCommand(p => Delete(p as CollectionDto), p => p is CollectionDto);
            Load();
        }

        private void Load()
        {
            Collections.Clear();
            foreach (var c in _service.GetAll()) Collections.Add(c);
        }

        private void NewCollection()
        {
            var req = new CollectionCreateRequest { Name = "Nueva Colección", Description = string.Empty };
            var created = _service.Create(req);
            if (created != null) Collections.Add(created);
        }

        // Public helper used by the view code-behind when creating via dialog
        public void CreateFromRequest(CollectionCreateRequest req)
        {
            if (req == null) return;
            var created = _service.Create(req);
            if (created != null) Collections.Add(created);
        }

        public void ImportFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                _service.ImportCollections(path);
                Load();
            }
            catch { }
        }

        public void ExportToFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                _service.ExportCollections(path);
            }
            catch { }
        }

        public void UpdateFromRequest(Guid id, CollectionCreateRequest req)
        {
            if (req == null) return;
            try
            {
                var updated = _service.Update(id, req);
                if (updated != null)
                {
                    var idx = Collections.IndexOf(Collections.First(x => x.Id == id));
                    Collections[idx] = updated;
                    OnPropertyChanged(nameof(Collections));
                }
            }
            catch { }
        }

        private void Rename(CollectionDto c)
        {
            if (c == null) return;
            var newName = c.Name + " (renombrada)";
            var updated = _service.Rename(c.Id, newName);
            if (updated != null)
            {
                var idx = Collections.IndexOf(Collections.First(x => x.Id == c.Id));
                Collections[idx] = updated;
                OnPropertyChanged(nameof(Collections));
            }
        }

        private void Duplicate(CollectionDto c)
        {
            if (c == null) return;
            var copy = _service.Duplicate(c.Id);
            if (copy != null) Collections.Add(copy);
        }

        private void Delete(CollectionDto c)
        {
            if (c == null) return;
            _service.Delete(c.Id);
            Collections.Remove(c);
        }

        // Public wrappers for view code-behind (distinct names to avoid conflict)
        public void DuplicateCollection(CollectionDto c)
        {
            Duplicate(c);
        }

        public void DeleteCollection(CollectionDto c)
        {
            Delete(c);
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}
