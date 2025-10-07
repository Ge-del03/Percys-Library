using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ComicReader.Models;

namespace ComicReader.Services
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

        public event Action? ListChanged;

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

        private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Save();
            SafeNotify();
        }

        private void SafeNotify()
        {
            try { ListChanged?.Invoke(); } catch (Exception ex) { Logger.LogException("Error notificando cambios en ContinueReadingService.ListChanged", ex); }
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
                    foreach (var it in list)
                    {
                        NormalizeItem(it);
                        it.PropertyChanged += Item_PropertyChanged;
                        Items.Add(it);
                    }
                    Items.CollectionChanged += Items_CollectionChanged;
                    ReorderItems(persist: false, notify: false);
                }
            }
            catch (Exception ex) { Logger.LogException("Error cargando ContinueReadingService data", ex); }
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
            catch (Exception ex) { Logger.LogException("Error guardando ContinueReadingService data", ex); }
        }

        public void UpsertProgress(string filePath, int currentPageOneBased, int pageCount)
        {
            if (string.IsNullOrWhiteSpace(filePath) || pageCount <= 0) return;

            var displayName = Path.GetFileNameWithoutExtension(filePath);
            var clampedPage = Math.Max(1, Math.Min(pageCount, currentPageOneBased));
            var isCompleted = clampedPage >= pageCount;

            var existing = Items.FirstOrDefault(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                existing = new ContinueItem
                {
                    FilePath = filePath,
                    DisplayName = displayName,
                    PageCount = pageCount,
                    LastPage = clampedPage,
                    LastOpened = DateTime.Now,
                    IsCompleted = isCompleted
                };
                NormalizeItem(existing);
                Items.Insert(0, existing);
            }
            else
            {
                existing.DisplayName = displayName;
                existing.PageCount = pageCount;
                existing.LastPage = clampedPage;
                existing.LastOpened = DateTime.Now;
                existing.IsCompleted = isCompleted;
                NormalizeItem(existing);

                var idx = Items.IndexOf(existing);
                if (idx > 0)
                {
                    Items.Move(idx, 0);
                }
            }

            ReorderItems(persist: false, notify: false);
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

        private static void NormalizeItem(ContinueItem? item)
        {
            if (item == null) return;

            if (item.PageCount < 0)
            {
                item.PageCount = 0;
            }

            if (item.PageCount == 0)
            {
                if (item.LastPage != 0)
                    item.LastPage = 0;
                if (item.IsCompleted)
                    item.IsCompleted = false;
                return;
            }

            if (item.LastPage > item.PageCount)
            {
                item.LastPage = item.PageCount;
            }
            else if (item.LastPage < 0)
            {
                item.LastPage = 0;
            }

            var shouldBeCompleted = item.LastPage >= item.PageCount;
            if (item.IsCompleted != shouldBeCompleted)
            {
                item.IsCompleted = shouldBeCompleted;
            }
        }

        private void ReorderItems(bool persist = true, bool notify = true)
        {
            try
            {
                var ordered = Items
                    .OrderByDescending(x => x.LastOpened)
                    .ThenBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

                if (ordered.Count == Items.Count)
                {
                    bool isDifferent = false;
                    for (int i = 0; i < ordered.Count; i++)
                    {
                        if (!ReferenceEquals(ordered[i], Items[i]))
                        {
                            isDifferent = true;
                            break;
                        }
                    }
                    if (!isDifferent)
                    {
                        if (persist) Save();
                        if (notify) SafeNotify();
                        return;
                    }
                }

                Items.CollectionChanged -= Items_CollectionChanged;
                foreach (var existing in Items)
                {
                    existing.PropertyChanged -= Item_PropertyChanged;
                }

                Items.Clear();
                foreach (var entry in ordered)
                {
                    entry.PropertyChanged -= Item_PropertyChanged;
                    entry.PropertyChanged += Item_PropertyChanged;
                    Items.Add(entry);
                }
            }
            finally
            {
                Items.CollectionChanged += Items_CollectionChanged;
            }

            if (persist) { Save(); }
            if (notify) { SafeNotify(); }
        }
    }
}
