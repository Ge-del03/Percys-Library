using Xunit;
using ComicReader.ViewModels;

namespace ReadingStats.Tests
{
    public class ReadingStatsViewModelTests
    {
        [Fact]
        public void ViewModel_Has_Default_Stats()
        {
            var vm = new ReadingStatsViewModel();
            Assert.NotNull(vm.Stats);
            Assert.Equal(8, vm.Stats.Count);
        }
    }
}
