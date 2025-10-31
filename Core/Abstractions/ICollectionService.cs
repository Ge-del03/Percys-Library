using System;
using System.Collections.Generic;

namespace ComicReader.Core.Abstractions
{
    public interface ICollectionService
    {
        IEnumerable<CollectionDto> GetAll();
        CollectionDto Create(CollectionCreateRequest req);
        CollectionDto Update(Guid id, CollectionCreateRequest req);
        CollectionDto Rename(Guid id, string newName);
        CollectionDto Duplicate(Guid id);
        void Delete(Guid id);
        void ExportCollections(string path);
        void ImportCollections(string path);
    }

    public class CollectionDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Description { get; set; }
        public string CoverPath { get; set; }
        public int Count { get; set; }
        public List<ComicItemDto> Items { get; set; } = new List<ComicItemDto>();
    }

    public class ComicItemDto
    {
        public string Title { get; set; }
        public string Path { get; set; }
        public string ThumbPath { get; set; }
    }

    public class CollectionCreateRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string CoverPath { get; set; }
        public List<ComicItemDto> Items { get; set; } = new List<ComicItemDto>();
    }
}
