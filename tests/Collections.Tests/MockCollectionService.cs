using System;
using System.Collections.Generic;
using System.Linq;
using ComicReader.Core.Abstractions;

namespace Collections.Tests
{
    // Lightweight in-memory mock of ICollectionService for unit tests
    public class MockCollectionService : ICollectionService
    {
        private readonly List<CollectionDto> _store = new List<CollectionDto>();

        public CollectionDto Create(CollectionCreateRequest req)
        {
            var c = new CollectionDto { Name = req.Name, Description = req.Description, CoverPath = req.CoverPath, Items = req.Items ?? new List<ComicItemDto>(), Count = req.Items?.Count ?? 0 };
            _store.Add(c);
            return c;
        }

        public void Delete(Guid id) => _store.RemoveAll(x => x.Id == id);

        public IEnumerable<CollectionDto> GetAll() => _store.Select(x => x).ToList();

        public CollectionDto Duplicate(Guid id)
        {
            var orig = _store.FirstOrDefault(x => x.Id == id);
            if (orig == null) return null;
            var copy = new CollectionDto { Name = orig.Name + " (copia)", Description = orig.Description, CoverPath = orig.CoverPath, Items = new List<ComicItemDto>(orig.Items), Count = orig.Count };
            _store.Add(copy);
            return copy;
        }

        public void ExportCollections(string path) => throw new NotSupportedException();

        public void ImportCollections(string path) => throw new NotSupportedException();

        public CollectionDto Rename(Guid id, string newName)
        {
            var it = _store.FirstOrDefault(x => x.Id == id);
            if (it == null) return null;
            it.Name = newName;
            return it;
        }

        public CollectionDto Update(Guid id, CollectionCreateRequest req)
        {
            var it = _store.FirstOrDefault(x => x.Id == id);
            if (it == null) return null;
            it.Name = req.Name ?? it.Name;
            it.Description = req.Description;
            it.CoverPath = req.CoverPath;
            if (req.Items != null) { it.Items = req.Items; it.Count = req.Items.Count; }
            return it;
        }
    }
}
