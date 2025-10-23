using Xunit;
using ComicReader.ViewModels;

namespace UiUxTests
{
    public class SettingsViewModelTests
    {
        [Fact]
        public void ChangeTabCommand_UpdatesSelectedSection()
        {
            var vm = new SettingsViewModel();
            vm.ChangeTabCommand.Execute("Lectura");
            Assert.Equal("Lectura", vm.SelectedSection);
        }
    }
}
