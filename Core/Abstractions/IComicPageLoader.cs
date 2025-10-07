using System.Collections.Generic;
using System.Threading.Tasks;
using ComicReader.Models;

namespace ComicReader.Core.Abstractions
{
    public interface IComicPageLoader
    {
        List<ComicPage> Pages { get; }
        string? FilePath { get; }
        string ComicTitle { get; }
        int PageCount { get; }
        Task LoadComicAsync(string? filePath = null);
        System.Threading.Tasks.Task<System.Windows.Media.Imaging.BitmapImage?> GetPageImageAsync(int pageNumber);
        System.Threading.Tasks.Task<System.Windows.Media.Imaging.BitmapImage> GetPageThumbnailAsync(int pageNumber, int width, int height);
        void ClearCurrent();
        void PreloadPages(int currentPageIndex);
        void RefreshTuningFromSettings();
    }
}
