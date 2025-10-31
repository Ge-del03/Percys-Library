using System.Linq;
using ComicReader.ViewModels;
using ComicReader.Core.Abstractions;
using Xunit;

namespace Collections.Tests
{
    public class CollectionsViewModelTests
    {
        [Fact]
        public void CreateFromRequest_AddsCollection()
        {
            // Use a lightweight in-memory mock service to avoid filesystem IO
            var mock = new MockCollectionService();
            var vm = new CollectionsViewModel(mock);
            var before = vm.Collections.Count;
            var req = new CollectionCreateRequest { Name = "VM Test", Description = "desc" };
            vm.CreateFromRequest(req);
            Assert.Equal(before + 1, vm.Collections.Count);

            // cleanup the created item
            var created = vm.Collections.Last();
            vm.DeleteCollection(created);
        }
    }
}
