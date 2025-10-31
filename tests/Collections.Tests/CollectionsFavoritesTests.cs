using System.Linq;
using ComicReader.ViewModels;
using ComicReader.Core.Abstractions;
using Xunit;

namespace Collections.Tests
{
    public class CollectionsFavoritesTests
    {
        [Fact]
        public void MarkItemAsFavorite_IsIncludedInFavoriteItems()
        {
            var mock = new MockCollectionService();
            var vm = new CollectionsViewModel(mock);

            var item = new ComicItemDto { Path = "C:\\temp\\comic1.cbz", Title = "Comic 1", ThumbPath = string.Empty, IsFavorite = false };
            var req = new CollectionCreateRequest { Name = "FavTest", Description = "desc", Items = new System.Collections.Generic.List<ComicItemDto> { item } };
            vm.CreateFromRequest(req);

            // Initially no favorites
            vm.RefreshFavorites();
            Assert.Empty(vm.FavoriteItems);

            // Mark as favorite and refresh
            var col = vm.Collections.Last();
            col.Items[0].IsFavorite = true;
            vm.RefreshFavorites();

            Assert.Single(vm.FavoriteItems);
            var fav = vm.FavoriteItems.First();
            Assert.Equal(item.Path, fav.Path);
            Assert.True(fav.IsFavorite);
        }
    }
}
