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
    public ObservableCollection<ContinueItem> CompletedItems { get; private set; }

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
            CompletedItems = new ObservableCollection<ContinueItem>();
            Items.CollectionChanged += Items_CollectionChanged;
            CompletedItems.CollectionChanged += CompletedItems_CollectionChanged;
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

        private void CompletedItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
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
                    // Deserialize into a wrapper that can contain both lists
                    var wrapper = JsonSerializer.Deserialize<ContinueStorage>(json, _jsonOptions) ?? new ContinueStorage();
                    Items.CollectionChanged -= Items_CollectionChanged;
                    CompletedItems.CollectionChanged -= CompletedItems_CollectionChanged;
                    Items.Clear();
                    CompletedItems.Clear();
                    foreach (var it in (wrapper.Items ?? Array.Empty<ContinueItem>()).OrderByDescending(x => x.LastOpened))
                    {
                        it.PropertyChanged += Item_PropertyChanged;
                        Items.Add(it);
                    }
                    foreach (var it in (wrapper.CompletedItems ?? Array.Empty<ContinueItem>()).OrderByDescending(x => x.DateCompleted))
                    {
                        it.PropertyChanged += Item_PropertyChanged;
                        CompletedItems.Add(it);
                    }
                    Items.CollectionChanged += Items_CollectionChanged;
                    CompletedItems.CollectionChanged += CompletedItems_CollectionChanged;
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
                    var wrapper = new ContinueStorage { Items = Items.ToArray(), CompletedItems = CompletedItems.ToArray() };
                    var json = JsonSerializer.Serialize(wrapper, _jsonOptions);
                    File.WriteAllText(_dataPath, json);
                }
            }
            catch { /* Silencioso */ }
        }

        public void UpsertProgress(string filePath, int currentPageOneBased, int pageCount)
        {
            try
            {
                // Registrar llamada para depuración
                try { System.Diagnostics.Debug.WriteLine($"UpsertProgress called: filePath={filePath}, current={currentPageOneBased}, pageCount={pageCount}"); } catch { }

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    try { System.Diagnostics.Debug.WriteLine("UpsertProgress: filePath is null or whitespace -> skipping"); } catch { }
                    return;
                }
                if (pageCount <= 0)
                {
                    try { System.Diagnostics.Debug.WriteLine("UpsertProgress: pageCount <= 0 -> skipping"); } catch { }
                    return;
                }

                currentPageOneBased = Math.Max(1, Math.Min(pageCount, currentPageOneBased));
                var displayName = Path.GetFileNameWithoutExtension(filePath);

                // Si el usuario ya está en la última página, mover automáticamente a "Completados"
                if (currentPageOneBased >= pageCount)
                {
                    try { System.Diagnostics.Debug.WriteLine($"UpsertProgress: reached last page for {displayName} -> MoveToCompleted"); } catch { }

                    // Mostrar un toast ligero para depuración y confirmación visual
                    try
                    {
                        System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                        {
                            try { ComicReader.Views.ToastWindow.ShowToast($"[Depuración] Se alcanzó 100%: {displayName}"); } catch { }
                        }));
                    }
                    catch { }

                    MoveToCompleted(filePath, displayName, pageCount);
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
            catch (Exception ex)
            {
                try { System.Diagnostics.Debug.WriteLine($"UpsertProgress exception: {ex.Message}"); } catch { }
            }
        }

        public void Remove(string filePath)
        {
            var item = Items.FirstOrDefault(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            if (item != null) Items.Remove(item);
            var citem = CompletedItems.FirstOrDefault(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            if (citem != null) CompletedItems.Remove(citem);
        }

        public void Clear()
        {
            Items.Clear();
        }

        public void ClearCompleted()
        {
            CompletedItems.Clear();
        }

        private void MoveToCompleted(string filePath, string displayName, int pageCount)
        {
            try
            {
                var existing = Items.FirstOrDefault(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    Items.Remove(existing);
                    existing.IsCompleted = true;
                    existing.LastPage = pageCount;
                    existing.PageCount = pageCount;
                    existing.DateCompleted = DateTime.Now;
                    if (existing.TotalReadSeconds == null) existing.TotalReadSeconds = 0;
                    CompletedItems.Insert(0, existing);
                }
                else
                {
                    var newItem = new ContinueItem
                    {
                        FilePath = filePath,
                        DisplayName = displayName,
                        PageCount = pageCount,
                        LastPage = pageCount,
                        IsCompleted = true,
                        DateCompleted = DateTime.Now
                    };
                    CompletedItems.Insert(0, newItem);
                }
                Save();
                SafeNotify();
                try
                {
                    // Mostrar un toast ligero para depuración: indicar que se movió a completados
                    System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        try { ComicReader.Views.ToastWindow.ShowToast($"Completado: {displayName}"); } catch { }
                    }));
                }
                catch { }
            }
            catch { }
        }

    }
}
