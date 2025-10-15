using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ComicReader.Models;

namespace ComicReader.Views
{
    public partial class ComicSearchWindow : Window, INotifyPropertyChanged
    {
        private string _searchText;
        private bool _searchInTitle = true;
        private bool _searchInAuthor;
        private bool _searchInTags;
        private bool _searchInContent;
        private ObservableCollection<ComicSearchResult> _searchResults;
        private bool _isSearching;

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }

        public bool SearchInTitle
        {
            get => _searchInTitle;
            set
            {
                _searchInTitle = value;
                OnPropertyChanged();
            }
        }

        public bool SearchInAuthor
        {
            get => _searchInAuthor;
            set
            {
                _searchInAuthor = value;
                OnPropertyChanged();
            }
        }

        public bool SearchInTags
        {
            get => _searchInTags;
            set
            {
                _searchInTags = value;
                OnPropertyChanged();
            }
        }

        public bool SearchInContent
        {
            get => _searchInContent;
            set
            {
                _searchInContent = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ComicSearchResult> SearchResults
        {
            get => _searchResults;
            set
            {
                _searchResults = value;
                OnPropertyChanged();
            }
        }

        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                _isSearching = value;
                OnPropertyChanged();
            }
        }

        public ComicSearchWindow()
        {
            #pragma warning disable
            InitializeComponent();
            #pragma warning restore
            DataContext = this;
            SearchResults = new ObservableCollection<ComicSearchResult>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                MessageBox.Show("Por favor, ingresa un término de búsqueda.", "Búsqueda", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsSearching = true;
            SearchResults.Clear();

            try
            {
                await PerformSearch();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante la búsqueda: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSearching = false;
            }
        }

        private async Task PerformSearch()
        {
            await Task.Run(() =>
            {
                var searchFolders = new[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Comics",
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                var comicExtensions = new[] { ".cbz", ".cbr", ".zip", ".rar", ".pdf", ".epub" };

                foreach (var folder in searchFolders.Where(Directory.Exists))
                {
                    try
                    {
                        var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
                            .Where(f => comicExtensions.Contains(Path.GetExtension(f).ToLower()));

                        foreach (var file in files)
                        {
                            var fileName = Path.GetFileNameWithoutExtension(file);
                            bool matches = false;

                            if (SearchInTitle && fileName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                                matches = true;

                            if (matches)
                            {
                                var result = new ComicSearchResult
                                {
                                    Title = fileName,
                                    FilePath = file,
                                    FileSize = new FileInfo(file).Length,
                                    LastModified = File.GetLastWriteTime(file),
                                    MatchReason = SearchInTitle ? "Título" : "Contenido"
                                };

                                Application.Current.Dispatcher.Invoke(() => SearchResults.Add(result));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Error silencioso por carpetas inaccesibles
                        System.Diagnostics.Debug.WriteLine($"Error searching folder {folder}: {ex.Message}");
                    }
                }
            });
        }

        private void OpenResult_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ComicSearchResult result)
            {
                OpenResult(result);
            }
        }

        private void OpenResult(ComicSearchResult result)
        {
            if (result == null) return;

            if (File.Exists(result.FilePath))
            {
                var mainWindow = Owner as global::ComicReader.MainWindow;
                if (mainWindow != null)
                {
                    var method = mainWindow.GetType().GetMethod("OpenComicFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    method?.Invoke(mainWindow, new object[] { result.FilePath });
                    mainWindow.ShowComicView();
                    Close();
                }
            }
            else
            {
                MessageBox.Show("El archivo no existe o ha sido movido.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchText = "";
            SearchResults.Clear();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            try { this.Close(); } catch { }
        }

        private void ResultsList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (sender is ListView lv && lv.SelectedItem is ComicSearchResult result)
                {
                    OpenResult(result);
                    e.Handled = true;
                }
            }
        }
    }

    public class ComicSearchResult : INotifyPropertyChanged
    {
        private string _title;
        private string _filePath;
        private long _fileSize;
        private DateTime _lastModified;
        private string _matchReason;

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public string FilePath
        {
            get => _filePath;
            set { _filePath = value; OnPropertyChanged(); }
        }

        public long FileSize
        {
            get => _fileSize;
            set { _fileSize = value; OnPropertyChanged(); }
        }

        public DateTime LastModified
        {
            get => _lastModified;
            set { _lastModified = value; OnPropertyChanged(); }
        }

        public string MatchReason
        {
            get => _matchReason;
            set { _matchReason = value; OnPropertyChanged(); }
        }

        public string FileSizeFormatted => FormatFileSize(FileSize);

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}