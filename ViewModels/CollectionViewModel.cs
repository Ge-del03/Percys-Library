using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using ComicReader.Commands;
using ComicReader.Models;

#nullable enable

namespace ComicReader.ViewModels
{
    public class CollectionViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ComicCollection> Collections { get; } = new ObservableCollection<ComicCollection>();

        private ComicCollection? _selected;
        public ComicCollection? SelectedCollection
        {
            get => _selected;
            set
            {
                if (_selected == value) return;
                _selected = value;
                OnPropertyChanged(nameof(SelectedCollection));
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand AddCollectionCommand { get; }
        public ICommand RemoveCollectionCommand { get; }
        public ICommand AddFavoriteCommand { get; }

        public CollectionViewModel()
        {
            AddCollectionCommand = new RelayCommand(_ => AddCollection());
            RemoveCollectionCommand = new RelayCommand(_ => RemoveSelectedCollection(), _ => SelectedCollection != null);
            AddFavoriteCommand = new RelayCommand(_ => AddSampleFavorite(), _ => SelectedCollection != null);
        }

        private void AddCollection()
        {
            var c = new ComicCollection { Name = "Nueva colección" };
            Collections.Add(c);
            SelectedCollection = c;
        }

        private void RemoveSelectedCollection()
        {
            if (SelectedCollection != null)
                Collections.Remove(SelectedCollection);
        }

        private void AddSampleFavorite()
        {
            if (SelectedCollection == null) return;
            var fav = new FavoriteComic { Title = "Nuevo Favorito", Author = "Desconocido", Thumbnail = null, TotalPages = 100 };
            SelectedCollection.Add(fav);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
