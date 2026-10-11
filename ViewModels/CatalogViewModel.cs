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

    public class CatalogViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ComicItem> _allComics = new ObservableCollection<ComicItem>();
        private ICollectionView _comicsView;
        private string _searchFilter;
        private string _selectedPublisherFilter = "Todas";
        private string _selectedStatusFilter = "Todos";
        private string _selectedSortOption = "Título";
        private ViewMode _currentViewMode = ViewMode.Grid;
        private double _cardWidth = 160;

        public CatalogViewModel()
        {
            _comicsView = CollectionViewSource.GetDefaultView(AllComics);
            _comicsView.Filter = FilterComicItem;

            SetViewModeGridCommand = new RelayCommand(() => CurrentViewMode = ViewMode.Grid);
            SetViewModeListCommand = new RelayCommand(() => CurrentViewMode = ViewMode.List);
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
            }
        }

        public ICollectionView ComicsView => _comicsView;

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

        private bool FilterComicItem(object obj)
        {
            if (obj is not ComicItem item) return false;

            // Filtro por búsqueda
            if (!string.IsNullOrWhiteSpace(SearchFilter))
            {
                var q = SearchFilter.Trim().ToLower();
                bool matchTitle = item.Title?.ToLower().Contains(q) == true;
                bool matchSeries = item.Series?.ToLower().Contains(q) == true;
                bool matchAuthor = item.Author?.ToLower().Contains(q) == true;
                if (!matchTitle && !matchSeries && !matchAuthor) return false;
            }

            // Filtro por editorial
            if (SelectedPublisherFilter != "Todas" && !string.IsNullOrEmpty(SelectedPublisherFilter))
            {
                if (item.Publisher?.Equals(SelectedPublisherFilter, StringComparison.OrdinalIgnoreCase) != true)
                    return false;
            }

            // Filtro por estado
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