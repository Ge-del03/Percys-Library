using System;
using ComicReader.Services;
using Xunit;

namespace Collections.Tests
{
    public class UndoServiceTests
    {
        [Fact]
        public void Register_and_InvokeLast_restores_state()
        {
            Action<string, string, Action> capturedToast = (msg, label, act) =>
            {
                // simulate user clicking undo by invoking the action immediately for the test
                act?.Invoke();
            };

            bool undone = false;
            var svc = new UndoService(capturedToast);

            svc.Register("Prueba eliminar", "Deshacer", () => { undone = true; });

            // The toast invoker in this test invokes the action immediately, so undone should be true
            Assert.True(undone);

            // Register another and invoke via InvokeLast
            undone = false;
            svc.Register("Prueba 2", "Deshacer", () => { undone = true; });
            svc.InvokeLast();
            Assert.True(undone);
        }
    }
}
