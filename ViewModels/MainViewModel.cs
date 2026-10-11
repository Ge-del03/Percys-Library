using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ComicReader.Commands;
using ComicReader.Models;
using ComicReader.Services;

namespace ComicReader.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ComicItem _selectedComic;
        private bool _isInspectorOpen;
        private string _searchQuery;
        private AppViewType _currentViewType = AppViewType.Dashboard;
        private int _totalComicsCount;
        private string _totalStorageText = "0 GB";

        public static MainViewModel Instance { get; private set; }

        public MainViewModel()
        {
            Instance = this;
            NavigationService.Instance.Navigated += (viewType, param) =>
            {
                CurrentViewType = viewType;
            };

            NavigateCommand = new RelayCommand(param =>
            {
                if (param is string s && Enum.TryParse<AppViewType>(s, out var target))
                {
                    NavigationService.Instance.NavigateTo(target);
                }
            });

            ToggleInspectorCommand = new RelayCommand(() => IsInspectorOpen = !IsInspectorOpen);
            OpenReaderCommand = new RelayCommand(param =>
            {
                var item = param as ComicItem ?? SelectedComic;
                if (item != null && !string.IsNullOrEmpty(item.FilePath))
                {
                    NavigationService.Instance.NavigateTo(AppViewType.Reader, item);
                }
            });
        }

        public ComicItem SelectedComic
        {
            get => _selectedComic;
            set
            {
                _selectedComic = value;
                OnPropertyChanged();
                if (_selectedComic != null)
                {
                    IsInspectorOpen = true;
                }
            }
        }

        public bool IsInspectorOpen
        {
            get => _isInspectorOpen;
            set { _isInspectorOpen = value; OnPropertyChanged(); }
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set { _searchQuery = value; OnPropertyChanged(); }
        }

        public AppViewType CurrentViewType
        {
            get => _currentViewType;
            set { _currentViewType = value; OnPropertyChanged(); }
        }

        public int TotalComicsCount
        {
            get => _totalComicsCount;
            set { _totalComicsCount = value; OnPropertyChanged(); }
        }

        public string TotalStorageText
        {
            get => _totalStorageText;
            set { _totalStorageText = value; OnPropertyChanged(); }
        }

        public ICommand NavigateCommand { get; }
        public ICommand ToggleInspectorCommand { get; }
        public ICommand OpenReaderCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}