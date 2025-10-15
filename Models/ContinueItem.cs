using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;

namespace ComicReader.Models
{
    public class ContinueItem : INotifyPropertyChanged
    {
        private string _filePath;
        private string _displayName;
        private int _lastPage;
        private int _pageCount;
        private DateTime _lastOpened;
        private bool _isCompleted;
    private DateTime? _dateCompleted;
    private int? _totalReadSeconds;
    private int? _rating;
    private string _review;

        [JsonPropertyName("filePath")]
        public string FilePath
        {
            get => _filePath;
            set { if (_filePath != value) { _filePath = value; OnPropertyChanged(); } }
        }

        [JsonPropertyName("displayName")]
        public string DisplayName
        {
            get => _displayName;
            set { if (_displayName != value) { _displayName = value; OnPropertyChanged(); } }
        }

        [JsonPropertyName("lastPage")]
        public int LastPage
        {
            get => _lastPage;
            set { if (_lastPage != value) { _lastPage = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressPercent)); } }
        }

        [JsonPropertyName("pageCount")]
        public int PageCount
        {
            get => _pageCount;
            set { if (_pageCount != value) { _pageCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressPercent)); } }
        }

        [JsonPropertyName("lastOpened")]
        public DateTime LastOpened
        {
            get => _lastOpened;
            set { if (_lastOpened != value) { _lastOpened = value; OnPropertyChanged(); } }
        }

        [JsonPropertyName("isCompleted")]
        public bool IsCompleted
        {
            get => _isCompleted;
            set { if (_isCompleted != value) { _isCompleted = value; OnPropertyChanged(); } }
        }

        [JsonPropertyName("dateCompleted")]
        public DateTime? DateCompleted
        {
            get => _dateCompleted;
            set { if (_dateCompleted != value) { _dateCompleted = value; OnPropertyChanged(); } }
        }

        [JsonPropertyName("totalReadSeconds")]
        public int? TotalReadSeconds
        {
            get => _totalReadSeconds;
            set { if (_totalReadSeconds != value) { _totalReadSeconds = value; OnPropertyChanged(); } }
        }

        [JsonPropertyName("rating")]
        public int? Rating
        {
            get => _rating;
            set { if (_rating != value) { _rating = value; OnPropertyChanged(); } }
        }

        [JsonPropertyName("review")]
        public string Review
        {
            get => _review;
            set { if (_review != value) { _review = value; OnPropertyChanged(); } }
        }

        [JsonIgnore]
        public double ProgressPercent => PageCount <= 0 ? 0 : Math.Round(100.0 * Math.Max(0, Math.Min(PageCount, LastPage)) / PageCount, 1);

        [JsonIgnore]
        private BitmapImage _coverThumbnail;
        [JsonIgnore]
        public BitmapImage CoverThumbnail
        {
            get => _coverThumbnail;
            set { if (!Equals(_coverThumbnail, value)) { _coverThumbnail = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
