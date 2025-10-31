using System;
using System.Linq;
using System.Collections.ObjectModel;
using Xunit;
using ComicReader.Services;
using ComicReader.Models;

namespace Collections.Tests
{
    public class FavoritesStorageTests
    {
        [Fact]
        public void RestoreItems_InsertsAtOriginalIndices()
        {
            var col = new ComicCollection { Id = Guid.NewGuid(), Name = "Col1", Items = new ObservableCollection<FavoriteComic>() };
            for (int i = 0; i < 4; i++) col.Items.Add(new FavoriteComic { Id = Guid.NewGuid(), FilePath = $"/tmp/c{i}.cbz", Title = $"C{i}" });
            var original = col.Items.Select(x => x.FilePath).ToList();

            var toRemove = new[] { col.Items[1], col.Items[3] };
            // remove
            foreach (var it in toRemove) col.Items.Remove(it);

            // restore via FavoritesStorage
            var restores = new[] { (Item: toRemove[0], Index: 1), (Item: toRemove[1], Index: 3) };
            FavoritesStorage.RestoreItems(col, restores);

            var final = col.Items.Select(x => x.FilePath).ToList();
            Assert.Equal(original, final);
        }
    }
}
