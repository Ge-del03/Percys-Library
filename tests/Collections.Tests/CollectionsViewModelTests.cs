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

        [Fact]
        public void Delete_then_Undo_restores_collection()
        {
            var mock = new MockCollectionService();
            Action capturedAction = null;
            // inject a toast invoker that captures the undo action instead of showing UI
            var vm = new CollectionsViewModel(mock, (msg, label, act) => { capturedAction = act; });

            var req = new CollectionCreateRequest { Name = "ToDelete", Description = "desc" };
            vm.CreateFromRequest(req);
            var created = vm.Collections.Last();

            // delete and ensure it's gone
            vm.DeleteCollection(created);
            Assert.Empty(vm.Collections);
            Assert.Empty(mock.GetAll());

            // simulate user clicking 'Deshacer'
            Assert.NotNull(capturedAction);
            capturedAction();

            // after undo, it should be back in both service and VM
            Assert.Single(vm.Collections);
            Assert.Single(mock.GetAll());
            Assert.Equal("ToDelete", vm.Collections.First().Name);
        }
    }
}
