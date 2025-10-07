using System.Windows.Input;

namespace ComicReader.Commands
{
    /// <summary>
    /// Comandos centralizados del lector para mejorar mantenibilidad y reutilización.
    /// </summary>
    public static class ReaderCommands
    {
        public static readonly RoutedUICommand IncreaseZoom = new(
            text: "Increase Zoom",
            name: nameof(IncreaseZoom),
            ownerType: typeof(ReaderCommands),
            inputGestures: new InputGestureCollection { new KeyGesture(Key.Add, ModifierKeys.Control) });

        public static readonly RoutedUICommand DecreaseZoom = new(
            text: "Decrease Zoom",
            name: nameof(DecreaseZoom),
            ownerType: typeof(ReaderCommands),
            inputGestures: new InputGestureCollection { new KeyGesture(Key.Subtract, ModifierKeys.Control) });

        public static readonly RoutedUICommand ResetZoom = new(
            text: "Reset Zoom",
            name: nameof(ResetZoom),
            ownerType: typeof(ReaderCommands),
            inputGestures: new InputGestureCollection { new KeyGesture(Key.D0, ModifierKeys.Control) });

        public static readonly RoutedUICommand GoToPage = new(
            text: "Go To Page",
            name: nameof(GoToPage),
            ownerType: typeof(ReaderCommands),
            inputGestures: new InputGestureCollection { new KeyGesture(Key.F, ModifierKeys.Control) });

        public static readonly RoutedUICommand ToggleThumbnails = new(
            text: "Toggle Thumbnails",
            name: nameof(ToggleThumbnails),
            ownerType: typeof(ReaderCommands),
            inputGestures: new InputGestureCollection { new KeyGesture(Key.T, ModifierKeys.Control) });

        public static readonly RoutedUICommand ToggleNightMode = new(
            text: "Toggle Night Mode",
            name: nameof(ToggleNightMode),
            ownerType: typeof(ReaderCommands),
            inputGestures: new InputGestureCollection { new KeyGesture(Key.N, ModifierKeys.Control) });

        public static readonly RoutedUICommand ToggleImmersive = new(
            text: "Toggle Immersive Mode",
            name: nameof(ToggleImmersive),
            ownerType: typeof(ReaderCommands),
            inputGestures: new InputGestureCollection { new KeyGesture(Key.Enter, ModifierKeys.Alt) });

        public static readonly RoutedUICommand AddBookmarkToggle = new(
            text: "Add Or Toggle Bookmarks",
            name: nameof(AddBookmarkToggle),
            ownerType: typeof(ReaderCommands),
            inputGestures: new InputGestureCollection { new KeyGesture(Key.B, ModifierKeys.Control) });

            // Ventanas auxiliares / utilidades
            public static readonly RoutedUICommand OpenSearch = new RoutedUICommand(
                "Buscar Cómics", nameof(OpenSearch), typeof(ReaderCommands));
            public static readonly RoutedUICommand OpenFavorites = new RoutedUICommand(
                "Favoritos", nameof(OpenFavorites), typeof(ReaderCommands));
            public static readonly RoutedUICommand OpenStats = new RoutedUICommand(
                "Estadísticas", nameof(OpenStats), typeof(ReaderCommands));
            public static readonly RoutedUICommand OpenLibraryManager = new RoutedUICommand(
                "Gestor Biblioteca", nameof(OpenLibraryManager), typeof(ReaderCommands));
            public static readonly RoutedUICommand OpenAnnotations = new RoutedUICommand(
                "Herramientas de Anotación", nameof(OpenAnnotations), typeof(ReaderCommands));

            public static readonly RoutedUICommand OpenPresentation = new RoutedUICommand(
                "Modo Presentación", nameof(OpenPresentation), typeof(ReaderCommands));

            public static readonly RoutedUICommand ToggleSlideshow = new RoutedUICommand(
                "Modo Slideshow", nameof(ToggleSlideshow), typeof(ReaderCommands),
                new InputGestureCollection { new KeyGesture(Key.S, ModifierKeys.Control) });

            public static readonly RoutedUICommand ToggleContinuous = new RoutedUICommand(
                "Modo Continuo", nameof(ToggleContinuous), typeof(ReaderCommands));

            // Acciones de marcadores (menú contextual)
            public static readonly RoutedUICommand OpenBookmark = new RoutedUICommand(
                "Abrir Marcador", nameof(OpenBookmark), typeof(ReaderCommands));
            public static readonly RoutedUICommand RenameBookmark = new RoutedUICommand(
                "Renombrar Marcador", nameof(RenameBookmark), typeof(ReaderCommands));
            public static readonly RoutedUICommand DeleteBookmark = new RoutedUICommand(
                "Eliminar Marcador", nameof(DeleteBookmark), typeof(ReaderCommands));

            public static readonly RoutedUICommand DeleteCurrentBookmark = new RoutedUICommand(
                "Eliminar Marcador Actual", nameof(DeleteCurrentBookmark), typeof(ReaderCommands),
                new InputGestureCollection { new KeyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Shift) });
    }
}
