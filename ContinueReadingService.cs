using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ComicReader.Models;

namespace ComicReader.Legacy.Services
{
    /// Servicio central para gestionar la lista de "Seguir leyendo" con persistencia en JSON.
    public sealed class ContinueReadingService
    {
        private static readonly Lazy<ContinueReadingService> _lazy = new Lazy<ContinueReadingService>(() => new ContinueReadingService());
        public static ContinueReadingService Instance => _lazy.Value;

        private readonly object _lock = new object();
        private readonly string _dataPath;
        private readonly JsonSerializerOptions _jsonOptions;

        public ObservableCollection<ContinueItem> Items { get; private set; }

        public event Action ListChanged;

        private ContinueReadingService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "PercysLibrary");
            Directory.CreateDirectory(dir);
            _dataPath = Path.Combine(dir, "continue_reading.json");
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                AllowTrailingCommas = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            Items = new ObservableCollection<ContinueItem>();
            Items.CollectionChanged += Items_CollectionChanged;
            Load();
        }

        private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Propagar cambios y persistir
            if (e.NewItems != null)
            {
                foreach (var it in e.NewItems.OfType<ContinueItem>())
                {
                    it.PropertyChanged -= Item_PropertyChanged;
                    it.PropertyChanged += Item_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (var it in e.OldItems.OfType<ContinueItem>())
                {
                    it.PropertyChanged -= Item_PropertyChanged;
                }
            }
            Save();
            SafeNotify();
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Save();
            SafeNotify();
        }

        private void SafeNotify()
        {
            try { ListChanged?.Invoke(); } catch { }
        }

        public void Load()
        {
            try
            {
                if (File.Exists(_dataPath))
                {
                    var json = File.ReadAllText(_dataPath);
                    var list = JsonSerializer.Deserialize<ContinueItem[]>(json, _jsonOptions) ?? Array.Empty<ContinueItem>();
                    Items.CollectionChanged -= Items_CollectionChanged;
                    Items.Clear();
                    foreach (var it in list.OrderByDescending(x => x.LastOpened))
                    {
                        it.PropertyChanged += Item_PropertyChanged;
                        Items.Add(it);
                    }
                    Items.CollectionChanged += Items_CollectionChanged;
                }
            }
            catch { /* Silencioso */ }
        }

        public void Save()
        {
            try
            {
                lock (_lock)
                {
                    var json = JsonSerializer.Serialize(Items.ToArray(), _jsonOptions);
                    File.WriteAllText(_dataPath, json);
                }
            }
            catch { /* Silencioso */ }
        }

        public void UpsertProgress(string filePath, int currentPageOneBased, int pageCount)
        {
            if (string.IsNullOrWhiteSpace(filePath) || pageCount <= 0) return;
            currentPageOneBased = Math.Max(1, Math.Min(pageCount, currentPageOneBased));
            var displayName = Path.GetFileNameWithoutExtension(filePath);

            // Si el usuario ya está en la última página, eliminar automáticamente de "Seguir leyendo"
            if (currentPageOneBased >= pageCount)
            {
                Remove(filePath);
                return;
            }

            var existing = Items.FirstOrDefault(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                existing = new ContinueItem
                {
                    FilePath = filePath,
                    DisplayName = displayName,
                    PageCount = pageCount,
                    LastPage = currentPageOneBased,
                    LastOpened = DateTime.Now,
                };
                Items.Insert(0, existing);
            }
            else
            {
                existing.PageCount = pageCount;
                existing.LastPage = currentPageOneBased;
                existing.LastOpened = DateTime.Now;
                // Mover al principio para mantener orden de uso reciente
                var idx = Items.IndexOf(existing);
                if (idx > 0)
                {
                    Items.Move(idx, 0);
                }
            }

            // Ya gestionamos la eliminación automática cuando se completa; mantener flag en falso aquí
            existing.IsCompleted = false;
            Save();
            SafeNotify();
        }

        public void Remove(string filePath)
        {
            var item = Items.FirstOrDefault(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            if (item != null) Items.Remove(item);
        }

        public void Clear()
        {
            Items.Clear();
        }
    }
}
