#if DESIGN_TIME
// Stubs de InitializeComponent solo para el diseñador de XAML (no se incluyen en builds normales)
using System.Windows;
using System.Windows.Controls;

namespace ComicReader.Views
{
    // Para controles con XAML, declaramos parciales vacías con InitializeComponent sin lógica
    public partial class HomeView : UserControl { private void InitializeComponent() { } }
    public partial class ThumbnailGridView : UserControl { private void InitializeComponent() { } }
    public partial class SettingsView : UserControl { private void InitializeComponent() { } }
    public partial class ComicView : UserControl { private void InitializeComponent() { } }
    public partial class ContinuousComicView : UserControl { private void InitializeComponent() { } }

    public partial class SettingsDialog : Window { private void InitializeComponent() { } }
    public partial class PresentationModeWindow : Window { private void InitializeComponent() { } }
    public partial class ComicSearchWindow : Window { private void InitializeComponent() { } }
    public partial class LibraryManagerWindow : Window { private void InitializeComponent() { } }
    public partial class ComicStatsWindow : Window { private void InitializeComponent() { } }
    public partial class FavoritesWindow : Window { private void InitializeComponent() { } }
    public partial class AnnotationToolsWindow : Window { private void InitializeComponent() { } }
}

namespace ComicReader
{
    public partial class MainWindow : Window { private void InitializeComponent() { } }
    public partial class ThumbnailPanelWindow : Window { private void InitializeComponent() { } }
}
#endif
