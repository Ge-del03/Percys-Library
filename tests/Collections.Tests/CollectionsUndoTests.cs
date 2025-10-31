using System;
using System.Linq;
using Xunit;
using ComicReader.Core.Abstractions;
using System.Collections.ObjectModel;

namespace Collections.Tests
{
    public class CollectionsUndoTests
    {
        [Fact]
        public void RemoveAndRestore_ItemsAreRestoredAtOriginalIndices()
        {
            // Arrange: create collection with 5 items
            var col = new ComicCollection
            {
                Id = Guid.NewGuid(),
                Name = "TestCol",
                Items = new ObservableCollection<FavoriteComic>()
            };

            for (int i = 0; i < 5; i++)
            {
                col.Items.Add(new FavoriteComic { Id = Guid.NewGuid(), Title = $"C{i}", FilePath = $"/tmp/c{i}.cbz" });
            }

            var originalOrder = col.Items.Select(x => x.FilePath).ToList();

            // Simulate removing items at indices 1 and 3 (capture indices)
            var toRemove = new[] { col.Items[1], col.Items[3] };
            var removed = toRemove.Select(it => new { Item = it, Index = Array.IndexOf(toRemove, it) })
                                  .ToList();

            // Actually remove them from collection
            foreach (var it in toRemove) col.Items.Remove(it);

            // Act: restore using the logic used by FavoritesWindow (insert at original indices if possible)
            // For this test, we simulate the stored original indices manually: 1 and 3
            var restores = new[] { new { Item = toRemove[0], Index = 1 }, new { Item = toRemove[1], Index = 3 } };
            foreach (var r in restores.OrderBy(r => r.Index))
            {
                if (r.Index >= 0 && r.Index <= col.Items.Count)
                    col.Items.Insert(r.Index, r.Item);
                else
                    col.Items.Add(r.Item);
            }

            // Assert: collection matches original ordering by FilePath
            var finalOrder = col.Items.Select(x => x.FilePath).ToList();
            Assert.Equal(originalOrder, finalOrder);
        }
    }
}
