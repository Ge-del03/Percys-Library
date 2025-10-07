using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;

namespace ComicReader.Models
{
    public class ContinueItem : INotifyPropertyChanged
    {
        private string _filePath = string.Empty;
        private string _displayName = string.Empty;
        private int _lastPage;
        private int _pageCount;
        private DateTime _lastOpened;
    private bool _isCompleted;

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
            set
            {
                if (_lastPage != value)
                {
                    _lastPage = value;
                    OnPropertyChanged();
                    NotifyProgressChanged();
                }
            }
        }

        [JsonPropertyName("pageCount")]
        public int PageCount
        {
            get => _pageCount;
            set
            {
                if (_pageCount != value)
                {
                    _pageCount = value;
                    OnPropertyChanged();
                    NotifyProgressChanged();
                }
            }
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
            set
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RemainingSummary));
                    OnPropertyChanged(nameof(IsNearlyFinished));
                }
            }
        }

        [JsonIgnore]
        public double ProgressPercent => PageCount <= 0 ? 0 : Math.Round(100.0 * Math.Max(0, Math.Min(PageCount, LastPage)) / PageCount, 1);

        [JsonIgnore]
        public int PagesRemaining => Math.Max(0, PageCount - Math.Max(0, LastPage));

        [JsonIgnore]
        public string ProgressSummary => PageCount <= 0
            ? "Sin progreso"
            : string.Format("{0}/{1} · {2:0.#}%", Math.Max(0, Math.Min(PageCount, LastPage)), PageCount, ProgressPercent);

        [JsonIgnore]
        public string RemainingSummary
        {
            get
            {
                if (IsCompleted || PagesRemaining <= 0)
                {
                    return "Completado";
                }

                if (PagesRemaining == 1)
                {
                    return "1 página restante";
                }

                return PagesRemaining + " páginas restantes";
            }
        }

        [JsonIgnore]
        public bool IsNearlyFinished => PageCount > 0 && !IsCompleted && PagesRemaining <= Math.Max(1, (int)Math.Ceiling(PageCount * 0.05));

        [JsonIgnore]
        private BitmapImage? _coverThumbnail;
        [JsonIgnore]
        public BitmapImage? CoverThumbnail
        {
            get => _coverThumbnail;
            set { if (!Equals(_coverThumbnail, value)) { _coverThumbnail = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? string.Empty));

        private void NotifyProgressChanged()
        {
            OnPropertyChanged(nameof(ProgressPercent));
            OnPropertyChanged(nameof(PagesRemaining));
            OnPropertyChanged(nameof(ProgressSummary));
            OnPropertyChanged(nameof(RemainingSummary));
            OnPropertyChanged(nameof(IsNearlyFinished));
        }
    }
}
