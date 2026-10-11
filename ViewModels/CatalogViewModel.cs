using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using ComicReader.Commands;
using ComicReader.Models;

namespace ComicReader.ViewModels
{
    public enum ViewMode
    {
        Grid,
        List
    }

    public enum SmartCollectionType
    {
        All,
        Favorites,
        Reading,
        Unread,
        Completed,
        TopRated,
        Manga,
        WesternComics,
        RecentlyAdded
    }

    public class CatalogViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ComicItem> _allComics = new ObservableCollection<ComicItem>();
        private ICollectionView _comicsView;
        private string _searchFilter;
        private string _selectedPublisherFilter = "Todas";
        private string _selectedStatusFilter = "Todos";
        private string _selectedSortOption = "Título";
        private SmartCollectionType _selectedSmartCollection = SmartCollectionType.All;
        private ViewMode _currentViewMode = ViewMode.Grid;
        private double _cardWidth = 160;

        public CatalogViewModel()
        {
            _comicsView = CollectionViewSource.GetDefaultView(AllComics);
            _comicsView.Filter = FilterComicItem;

            SetViewModeGridCommand = new RelayCommand(() => CurrentViewMode = ViewMode.Grid);
            SetViewModeListCommand = new RelayCommand(() => CurrentViewMode = ViewMode.List);
            SetSmartCollectionCommand = new RelayCommand(param =>
            {
                if (param is SmartCollectionType st)
                {
                    SelectedSmartCollection = st;
                }
                else if (param is string s && Enum.TryParse<SmartCollectionType>(s, out var parsed))
                {
                    SelectedSmartCollection = parsed;
                }
            });
        }

        public ObservableCollection<ComicItem> AllComics
        {
            get => _allComics;
            set
            {
                _allComics = value;
                OnPropertyChanged();
                _comicsView = CollectionViewSource.GetDefaultView(AllComics);
                _comicsView.Filter = FilterComicItem;
                ApplySorting();
                OnPropertyChanged(nameof(ComicsView));
                OnPropertyChanged(nameof(TotalComicsCount));
            }
        }

        public ICollectionView ComicsView => _comicsView;
        public int TotalComicsCount => AllComics?.Count ?? 0;

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                _searchFilter = value;
                OnPropertyChanged();
                _comicsView?.Refresh();
            }
        }

        public string SelectedPublisherFilter
        {
            get => _selectedPublisherFilter;
            set
            {
                _selectedPublisherFilter = value;
                OnPropertyChanged();
                _comicsView?.Refresh();
            }
        }

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                _selectedStatusFilter = value;
                OnPropertyChanged();
                _comicsView?.Refresh();
            }
        }

        public string SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                _selectedSortOption = value;
                OnPropertyChanged();
                ApplySorting();
            }
        }

        public SmartCollectionType SelectedSmartCollection
        {
            get => _selectedSmartCollection;
            set
            {
                _selectedSmartCollection = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CollectionTitle));
                _comicsView?.Refresh();
            }
        }

        public string CollectionTitle
        {
            get
            {
                return SelectedSmartCollection switch
                {
                    SmartCollectionType.Favorites => "★ Favoritos",
                    SmartCollectionType.Reading => "▶ En Curso",
                    SmartCollectionType.Unread => "• Sin Empezar",
                    SmartCollectionType.Completed => "✓ Finalizados",
                    SmartCollectionType.TopRated => "⭐⭐⭐⭐⭐ Mejor Valorados",
                    SmartCollectionType.Manga => "⛩️ Manga & Manhwa",
                    SmartCollectionType.WesternComics => "🦸 Cómic Americano & Europeo",
                    SmartCollectionType.RecentlyAdded => "⚡ Añadidos Recientemente",
                    _ => "Catálogo Completo"
                };
            }
        }

        public ViewMode CurrentViewMode
        {
            get => _currentViewMode;
            set
            {
                _currentViewMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsGridView));
                OnPropertyChanged(nameof(IsListView));
            }
        }

        public bool IsGridView => CurrentViewMode == ViewMode.Grid;
        public bool IsListView => CurrentViewMode == ViewMode.List;

        public double CardWidth
        {
            get => _cardWidth;
            set
            {
                _cardWidth = Math.Max(120, Math.Min(300, value));
                OnPropertyChanged();
                OnPropertyChanged(nameof(CardHeight));
            }
        }

        public double CardHeight => CardWidth * 1.53;

        public ICommand SetViewModeGridCommand { get; }
        public ICommand SetViewModeListCommand { get; }
        public ICommand SetSmartCollectionCommand { get; }

        private bool FilterComicItem(object obj)
        {
            if (obj is not ComicItem item) return false;

            // Smart Collections
            switch (SelectedSmartCollection)
            {
                case SmartCollectionType.Favorites:
                    if (item.Rating < 4) return false;
                    break;
                case SmartCollectionType.Reading:
                    if (item.CurrentPage <= 0 || item.IsRead) return false;
                    break;
                case SmartCollectionType.Unread:
                    if (item.CurrentPage > 0 || item.IsRead) return false;
                    break;
                case SmartCollectionType.Completed:
                    if (!item.IsRead) return false;
                    break;
                case SmartCollectionType.TopRated:
                    if (item.Rating < 5) return false;
                    break;
                case SmartCollectionType.Manga:
                    if (item.Genre?.ToLower().Contains("manga") != true && item.Publisher?.ToLower().Contains("shueisha") != true && item.Publisher?.ToLower().Contains("kodansha") != true)
                        return false;
                    break;
                case SmartCollectionType.WesternComics:
                    if (item.Genre?.ToLower().Contains("manga") == true) return false;
                    break;
            }

            // Filtro por búsqueda
            if (!string.IsNullOrWhiteSpace(SearchFilter))
            {
                var q = SearchFilter.Trim().ToLower();
                bool matchTitle = item.Title?.ToLower().Contains(q) == true;
                bool matchSeries = item.Series?.ToLower().Contains(q) == true;
                bool matchAuthor = item.Author?.ToLower().Contains(q) == true;
                bool matchPublisher = item.Publisher?.ToLower().Contains(q) == true;
                if (!matchTitle && !matchSeries && !matchAuthor && !matchPublisher) return false;
            }

            // Filtro por editorial
            if (SelectedPublisherFilter != "Todas" && !string.IsNullOrEmpty(SelectedPublisherFilter))
            {
                if (item.Publisher?.Equals(SelectedPublisherFilter, StringComparison.OrdinalIgnoreCase) != true)
                    return false;
            }

            // Filtro por estado manual
            if (SelectedStatusFilter == "Leídos" && !item.IsRead) return false;
            if (SelectedStatusFilter == "En curso" && (item.CurrentPage <= 0 || item.IsRead)) return false;
            if (SelectedStatusFilter == "Pendientes" && (item.CurrentPage > 0 || item.IsRead)) return false;

            return true;
        }

        private void ApplySorting()
        {
            if (_comicsView == null) return;
            _comicsView.SortDescriptions.Clear();

            switch (SelectedSortOption)
            {
                case "Año":
                    _comicsView.SortDescriptions.Add(new SortDescription(nameof(ComicItem.Year), ListSortDirection.Descending));
                    break;
                case "Recientes":
                    _comicsView.SortDescriptions.Add(new SortDescription(nameof(ComicItem.DateAdded), ListSortDirection.Descending));
                    break;
                case "Rating":
                    _comicsView.SortDescriptions.Add(new SortDescription(nameof(ComicItem.Rating), ListSortDirection.Descending));
                    break;
                case "Serie":
                    _comicsView.SortDescriptions.Add(new SortDescription(nameof(ComicItem.Series), ListSortDirection.Ascending));
                    _comicsView.SortDescriptions.Add(new SortDescription(nameof(ComicItem.IssueNumber), ListSortDirection.Ascending));
                    break;
                case "Título":
                default:
                    _comicsView.SortDescriptions.Add(new SortDescription(nameof(ComicItem.Title), ListSortDirection.Ascending));
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}