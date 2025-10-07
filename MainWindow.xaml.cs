using System.Windows.Input;

namespace ComicReader {
	public partial class MainWindow {
		// Handler puente: si el XAML generado aún mantiene la suscripción, delega al método definitivo.
		public void ThumbList_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			// El método real está definido en la otra parte parcial (MainWindow.cs)
			try { this.GetType(); /* no-op para evitar warning */ } catch { }
			// No hacer nada aquí: la suscripción dinámica en InitializeComponents ya gestiona el evento.
		}

		// Los métodos de eventos del toolbar (OpenAdvancedSearch_Click, etc.) 
		// ahora están en MainWindow.cs debido a problemas de compilación
	}
}
