using System;
using System.Windows.Controls;
using ComicReader.ViewModels;
using ComicReader.Services;
using ComicReader.Core.Abstractions;

namespace ComicReader.Views
{
    public partial class ThumbnailGridView : UserControl
    {
        public ViewModels.ThumbnailGridViewViewModel ViewModel { get; }

        public event Action<int> PageSelected
        {
            add { ViewModel.PageSelected += value; }
            remove { ViewModel.PageSelected -= value; }
        }
        public event Action<int> PageDoubleClicked
        {
            add { ViewModel.PageDoubleClicked += value; }
            remove { ViewModel.PageDoubleClicked -= value; }
        }

        public ThumbnailGridView()
        {
            InitializeComponent();
            ViewModel = new ViewModels.ThumbnailGridViewViewModel();
            DataContext = ViewModel;
        }

    public IComicPageLoader ComicLoader { get => ViewModel.ComicLoader; set => ViewModel.ComicLoader = value; }
        public int CurrentPageIndex { get => ViewModel.CurrentPageIndex; set => ViewModel.CurrentPageIndex = value; }
        public int ThumbnailSize { get => ViewModel.ThumbnailSize; set => ViewModel.ThumbnailSize = value; }
        public bool ShowPageNumbers { get => ViewModel.ShowPageNumbers; set => ViewModel.ShowPageNumbers = value; }
        public bool ShowBookmarks { get => ViewModel.ShowBookmarks; set => ViewModel.ShowBookmarks = value; }
        public bool IsLoading => ViewModel.IsLoading;
        public bool IsNotLoading => ViewModel.IsNotLoading;
    }
}
