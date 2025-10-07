using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ComicReader.Models
{
    /// <summary>
    /// Representa una colección personalizada de cómics (Sistema de Colecciones v2.0)
    /// </summary>
    public class ComicCollectionV2
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public List<string> ComicPaths { get; set; } = new List<string>();
        public string CoverPath { get; set; } = string.Empty; // Ruta al cómic usado como portada
        public List<string> Tags { get; set; } = new List<string>();
        public string Color { get; set; } = "#3498db"; // Color hex para la colección
        public int SortOrder { get; set; } = 0;

        // Estadísticas calculadas
        public int ComicCount => ComicPaths.Count;
    }

    /// <summary>
    /// Gestiona las colecciones de cómics
    /// </summary>
    public class CollectionManager
    {
        private readonly string _collectionsFile;
        private List<ComicCollectionV2> _collections = new List<ComicCollectionV2>();

        public CollectionManager(string dataFolder = "")
        {
            if (string.IsNullOrEmpty(dataFolder))
            {
                dataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PercysLibrary"
                );
            }
            Directory.CreateDirectory(dataFolder);
            _collectionsFile = Path.Combine(dataFolder, "collections.json");
            LoadCollections();
        }

        public void LoadCollections()
        {
            try
            {
                if (File.Exists(_collectionsFile))
                {
                    var json = File.ReadAllText(_collectionsFile);
                    _collections = System.Text.Json.JsonSerializer.Deserialize<List<ComicCollectionV2>>(json)
                        ?? new List<ComicCollectionV2>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading collections: {ex.Message}");
                _collections = new List<ComicCollectionV2>();
            }
        }

        public void SaveCollections()
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_collections, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_collectionsFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving collections: {ex.Message}");
            }
        }

        public ComicCollectionV2 CreateCollection(string name, string description = "")
        {
            var collection = new ComicCollectionV2
            {
                Name = name,
                Description = description,
                SortOrder = _collections.Count
            };
            _collections.Add(collection);
            SaveCollections();
            return collection;
        }

        public void UpdateCollection(ComicCollectionV2 collection)
        {
            var existing = _collections.FirstOrDefault(c => c.Id == collection.Id);
            if (existing != null)
            {
                var index = _collections.IndexOf(existing);
                collection.ModifiedDate = DateTime.Now;
                _collections[index] = collection;
                SaveCollections();
            }
        }

        public void DeleteCollection(Guid collectionId)
        {
            _collections.RemoveAll(c => c.Id == collectionId);
            SaveCollections();
        }

        public void AddComicToCollection(Guid collectionId, string comicPath)
        {
            var collection = _collections.FirstOrDefault(c => c.Id == collectionId);
            if (collection != null && !collection.ComicPaths.Contains(comicPath))
            {
                collection.ComicPaths.Add(comicPath);
                collection.ModifiedDate = DateTime.Now;
                
                // Si es el primer cómic, usarlo como portada
                if (string.IsNullOrEmpty(collection.CoverPath))
                {
                    collection.CoverPath = comicPath;
                }
                
                SaveCollections();
            }
        }

        public void RemoveComicFromCollection(Guid collectionId, string comicPath)
        {
            var collection = _collections.FirstOrDefault(c => c.Id == collectionId);
            if (collection != null)
            {
                collection.ComicPaths.Remove(comicPath);
                collection.ModifiedDate = DateTime.Now;
                
                // Si era la portada, seleccionar otra
                if (collection.CoverPath == comicPath && collection.ComicPaths.Any())
                {
                    collection.CoverPath = collection.ComicPaths.First();
                }
                else if (!collection.ComicPaths.Any())
                {
                    collection.CoverPath = string.Empty;
                }
                
                SaveCollections();
            }
        }

        public List<ComicCollectionV2> GetAllCollections()
        {
            return _collections.OrderBy(c => c.SortOrder).ToList();
        }

        public ComicCollectionV2? GetCollection(Guid collectionId)
        {
            return _collections.FirstOrDefault(c => c.Id == collectionId);
        }

        public List<ComicCollectionV2> GetCollectionsForComic(string comicPath)
        {
            return _collections
                .Where(c => c.ComicPaths.Contains(comicPath))
                .OrderBy(c => c.SortOrder)
                .ToList();
        }

        public List<ComicCollectionV2> SearchCollections(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText)) return GetAllCollections();

            return _collections
                .Where(c =>
                    c.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    c.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    c.Tags.Any(t => t.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(c => c.SortOrder)
                .ToList();
        }

        public void ReorderCollections(List<Guid> orderedIds)
        {
            for (int i = 0; i < orderedIds.Count; i++)
            {
                var collection = _collections.FirstOrDefault(c => c.Id == orderedIds[i]);
                if (collection != null)
                {
                    collection.SortOrder = i;
                }
            }
            SaveCollections();
        }
    }
}
