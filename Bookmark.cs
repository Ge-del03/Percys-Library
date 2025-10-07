// FileName: /Models/Bookmark.cs
using System;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace ComicReader.Models
{
    public class Bookmark : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string ComicFilePath { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }

        private BitmapImage? _thumbnail;
        [System.Xml.Serialization.XmlIgnore]
        public BitmapImage? Thumbnail
        {
            get => _thumbnail;
            set
            {
                _thumbnail = value;
                OnPropertyChanged(nameof(Thumbnail));
            }
        }
        // Cadena Base64 (PNG) para persistir la miniatura entre sesiones
        public string? ThumbnailBase64 { get; set; }
    }
}