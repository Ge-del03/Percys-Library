// Auto-generated design-time stubs to calm the editor when .g.cs is unavailable
// Included only during DesignTimeBuild via Directory.Build.props (symbol DESIGN_TIME)

#if DESIGN_TIME
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ComicReader.Views
{
    // Minimal stubs para satisfacer InitializeComponent en tiempo de diseño
    public partial class ComicView : UserControl { public void InitializeComponent() { } }
    public partial class ContinuousComicView : UserControl { public void InitializeComponent() { } }
    public partial class HomeView : UserControl { public void InitializeComponent() { } }
    // Stub ligero para SettingsView: solo InitializeComponent para evitar errores de diseño
    public partial class SettingsView : UserControl { public void InitializeComponent() { } }
    public partial class ThumbnailGridView : UserControl { public void InitializeComponent() { } }

    // Windows/dialogs
    public partial class ComicStatsWindow : Window { public void InitializeComponent() { } }
    public partial class FavoritesWindow : Window { public void InitializeComponent() { } }
    public partial class ComicSearchWindow : Window { public void InitializeComponent() { } }
    public partial class SettingsDialog : Window { public void InitializeComponent() { } }
    public partial class PresentationModeWindow : Window { public void InitializeComponent() { } }
    public partial class LibraryManagerWindow : Window { public void InitializeComponent() { } }
}

namespace ComicReader
{
    public partial class MainWindow : Window { public void InitializeComponent() { } }
    // Stub de diseño para GoToPageDialog
    public partial class GoToPageDialog : Window
    {
        public void InitializeComponent() { }
        public TextBox PageNumberTextBox { get; } = new TextBox();
        public Label PageRangeLabel { get; } = new Label();
    }
}
#endif
