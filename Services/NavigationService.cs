using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ComicReader.Services
{
    public enum AppViewType
    {
        Dashboard,
        Catalog,
        Series,
        Stats,
        Settings,
        Reader
    }

    public class NavigationService : INotifyPropertyChanged
    {
        private static NavigationService _instance;
        public static NavigationService Instance => _instance ??= new NavigationService();

        private AppViewType _currentViewType = AppViewType.Dashboard;
        private object _currentViewParameter;

        public event Action<AppViewType, object> Navigated;

        public AppViewType CurrentViewType
        {
            get => _currentViewType;
            private set
            {
                if (_currentViewType != value)
                {
                    _currentViewType = value;
                    OnPropertyChanged();
                }
            }
        }

        public void NavigateTo(AppViewType viewType, object parameter = null)
        {
            CurrentViewType = viewType;
            _currentViewParameter = parameter;
            Navigated?.Invoke(viewType, parameter);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}