using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ComicReader.Models
{
    public class ComicLibrary : INotifyPropertyChanged
    {
        private Guid _id = Guid.NewGuid();
        private string _name;
        private string _path;
        private string _description;
        private DateTime _dateCreated = DateTime.Now;
        private int _comicCount;
        private string _category;
        private bool _isDefault;
        private ObservableCollection<string> _tags;

        public Guid Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Path
        {
            get => _path;
            set { _path = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public DateTime DateCreated
        {
            get => _dateCreated;
            set { _dateCreated = value; OnPropertyChanged(); }
        }

        public int ComicCount
        {
            get => _comicCount;
            set { _comicCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(ComicCountText)); }
        }

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(); }
        }

        public bool IsDefault
        {
            get => _isDefault;
            set { _isDefault = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Tags
        {
            get => _tags ??= new ObservableCollection<string>();
            set { _tags = value; OnPropertyChanged(); }
        }

        public string StatusIcon => IsDefault ? "🏠" : "📚";
        public string ComicCountText => $"{ComicCount} obra{(ComicCount != 1 ? "s" : "")}";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ComicItem : INotifyPropertyChanged
    {
        private Guid _id = Guid.NewGuid();
        private string _title;
        private string _filePath;
        private long _fileSize;
        private DateTime _dateAdded = DateTime.Now;
        private DateTime _lastRead;
        private string _format;
        private bool _isRead;
        private int _rating;
        private ObservableCollection<string> _tags;

        // Metadatos enriquecidos (ComicInfo.xml)
        private string _series;
        private int _issueNumber;
        private int _volume;
        private int _year;
        private int _month;
        private string _publisher;
        private string _writer;
        private string _penciller;
        private string _author;
        private string _genre;
        private string _summary;
        private int _pageCount;
        private int _currentPage;
        private string _coverThumbnailUri;

        public Guid Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

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
            set { _fileSize = value; OnPropertyChanged(); OnPropertyChanged(nameof(FileSizeFormatted)); }
        }

        public DateTime DateAdded
        {
            get => _dateAdded;
            set { _dateAdded = value; OnPropertyChanged(); }
        }

        public DateTime LastRead
        {
            get => _lastRead;
            set { _lastRead = value; OnPropertyChanged(); }
        }

        public string Format
        {
            get => _format;
            set { _format = value; OnPropertyChanged(); }
        }

        public bool IsRead
        {
            get => _isRead;
            set { _isRead = value; OnPropertyChanged(); OnPropertyChanged(nameof(ReadStatusIcon)); OnPropertyChanged(nameof(ReadStatusText)); }
        }

        public int Rating
        {
            get => _rating;
            set { _rating = value; OnPropertyChanged(); OnPropertyChanged(nameof(RatingStars)); }
        }

        public ObservableCollection<string> Tags
        {
            get => _tags ??= new ObservableCollection<string>();
            set { _tags = value; OnPropertyChanged(); }
        }

        public string Series
        {
            get => _series;
            set { _series = value; OnPropertyChanged(); }
        }

        public int IssueNumber
        {
            get => _issueNumber;
            set { _issueNumber = value; OnPropertyChanged(); OnPropertyChanged(nameof(IssueNumberFormatted)); }
        }

        public int Volume
        {
            get => _volume;
            set { _volume = value; OnPropertyChanged(); }
        }

        public int Year
        {
            get => _year;
            set { _year = value; OnPropertyChanged(); }
        }

        public int Month
        {
            get => _month;
            set { _month = value; OnPropertyChanged(); }
        }

        public string Publisher
        {
            get => _publisher;
            set { _publisher = value; OnPropertyChanged(); }
        }

        public string Writer
        {
            get => _writer;
            set { _writer = value; OnPropertyChanged(); }
        }

        public string Penciller
        {
            get => _penciller;
            set { _penciller = value; OnPropertyChanged(); }
        }

        public string Author
        {
            get => string.IsNullOrEmpty(_author) ? _writer : _author;
            set { _author = value; OnPropertyChanged(); }
        }

        public string Genre
        {
            get => _genre;
            set { _genre = value; OnPropertyChanged(); }
        }

        public string Summary
        {
            get => _summary;
            set { _summary = value; OnPropertyChanged(); }
        }

        public int PageCount
        {
            get => _pageCount;
            set { _pageCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressPercentage)); }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressPercentage)); }
        }

        public string CoverThumbnailUri
        {
            get => _coverThumbnailUri;
            set { _coverThumbnailUri = value; OnPropertyChanged(); }
        }

        public double ProgressPercentage
        {
            get
            {
                if (PageCount <= 0) return 0;
                double pct = ((double)CurrentPage / PageCount) * 100.0;
                return Math.Min(100.0, Math.Max(0, pct));
            }
        }

        public string IssueNumberFormatted => IssueNumber > 0 ? $"#{IssueNumber}" : "";

        public string FileSizeFormatted
        {
            get
            {
                if (FileSize <= 0) return "0 MB";
                string[] suffixes = { "B", "KB", "MB", "GB" };
                int counter = 0;
                decimal number = FileSize;
                while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
                {
                    number /= 1024;
                    counter++;
                }
                return $"{number:n1} {suffixes[counter]}";
            }
        }

        public string ReadStatusIcon => IsRead ? "✓" : (CurrentPage > 0 ? "▶" : "•");
        public string ReadStatusText => IsRead ? "Leído" : (CurrentPage > 0 ? $"En curso ({Math.Round(ProgressPercentage)}%)" : "Pendiente");
        public string RatingStars => Rating > 0 ? new string('★', Math.Min(5, Rating)) + new string('☆', Math.Max(0, 5 - Rating)) : "Sin calificar";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}