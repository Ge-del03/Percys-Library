using System;
using System.Windows.Controls;
using ComicReader.ViewModels;
using ComicReader.Services;
using ComicReader.Core.Abstractions;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Windows.Media;
using ComicReader.Utils;
using System.Windows.Media.Imaging;

namespace ComicReader.Views
{
	public partial class ContinuousComicView : UserControl
	{
		public ContinuousComicViewModel ViewModel { get; }
		// Control de animaciones y solicitudes de scroll
		private Clock? _currentScrollClock;
		private int _latestScrollRequestId = 0;
		private DateTime _lastProgrammaticScrollAt = DateTime.MinValue;

		public ContinuousComicView()
		{
			InitializeComponent();
			ViewModel = new ContinuousComicViewModel();
			DataContext = ViewModel;
			// Ajustar ancho de decodificación según el viewport disponible
			this.Loaded += (_, __) => TryUpdateDecodeTargetWidth();
			this.SizeChanged += (_, __) => TryUpdateDecodeTargetWidth();
			ViewModel.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(ViewModel.Zoom)) { EnsureDecodeWidthTimer(); _decodeWidthTimer!.Stop(); _decodeWidthTimer!.Start(); } };
			// Al cambiar la página desde comandos/slider, desplazar para llevarla al viewport
			ViewModel.CurrentPageChanged += OnViewModelCurrentPageChanged;
			// Asegurar que el ListBox reciba el foco para manejo de teclas
			this.Loaded += (s, e) =>
			{
				try { (this.FindName("PagesList") as ListBox)?.Focus(); } catch { }
			};
			// Ctrl + rueda para zoom in/out
			this.PreviewMouseWheel += (s, e) =>
			{
				try
				{
					if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
					{
						e.Handled = true;
						if (e.Delta > 0) ViewModel.Zoom += 0.1;
						else ViewModel.Zoom -= 0.1;
					}
				}
				catch { }
			};
		}

		public IComicPageLoader ComicLoader
		{
			get => ViewModel.Loader;
			set => ViewModel.Loader = value;
		}

		private void ContentScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			// Debounce para evitar trabajo excesivo durante scroll continuo
			EnsureMaterializeDebounce();
			_materializeDebounce!.Stop();
			_materializeDebounce!.Start();
			// Medir velocidad aproximada del scroll
			try
			{
				var sv = sender as ScrollViewer;
				if (sv != null)
				{
					// e.VerticalChange es delta entre eventos; normalizar a px/s usando el tiempo entre eventos
					var now = DateTime.UtcNow;
					var dtMs = (now - _lastUserScrollAt).TotalMilliseconds;
					if (dtMs > 0 && Math.Abs(e.VerticalChange) > 0)
					{
						_lastScrollVelocity = Math.Abs(e.VerticalChange) * (1000.0 / dtMs);
					}
					_lastUserScrollAt = now;
				}
			}
			catch { }
			// Actualizar página actual aproximada basada en el elemento centrado/visible
			try
			{
				int nearest = FindNearestVisibleIndex();
				if (nearest >= 0 && nearest < ViewModel.Pages.Count && ViewModel.ShouldReactToUserScroll)
				{
					ViewModel.CurrentPage = nearest;
				}
			}
			catch { }
		}

		private DispatcherTimer? _materializeDebounce;
		private DateTime _lastUserScrollAt = DateTime.MinValue;
		private double _lastScrollVelocity = 0;
		private void EnsureMaterializeDebounce()
		{
			if (_materializeDebounce != null) return;
			_materializeDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(110) };
			_materializeDebounce.Tick += (s, e) =>
			{
				try
				{
					_materializeDebounce.Stop();
					// Si se está desplazando muy rápido, usa overscan menor para no bloquear UI
					var now = DateTime.UtcNow;
					bool isVeryFast = (now - _lastUserScrollAt).TotalMilliseconds < 120 && _lastScrollVelocity > 2000; // heurística
					ViewModel?.RequestVisiblePagesMaterialization(isVeryFast ? 2 : (int?)null);
				}
				catch { }
			};
		}

		protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);
			var sv = FindScrollViewerForList();
			if (sv == null) return;
			double step = Math.Max(24, sv.ViewportHeight * (SettingsManager.Settings?.PageScrollStepRatio ?? 0.8));
			if (e.Key == System.Windows.Input.Key.Down || e.Key == System.Windows.Input.Key.PageDown)
			{
				if (sv.VerticalOffset + sv.ViewportHeight + 1 < sv.ExtentHeight)
				{
					var target = Math.Min(sv.VerticalOffset + step, sv.ExtentHeight - sv.ViewportHeight);
					AnimateOrJumpScroll(sv, target);
					e.Handled = true;
				}
				else if (ViewModel.CurrentPage + 1 < ViewModel.Pages.Count)
				{
					var list = this.FindName("PagesList") as ListBox;
					list?.ScrollIntoView(ViewModel.Pages[ViewModel.CurrentPage + 1]);
					ScrollToPage(ViewModel.CurrentPage + 1);
					e.Handled = true;
				}
			}
			else if (e.Key == System.Windows.Input.Key.Up || e.Key == System.Windows.Input.Key.PageUp)
			{
				if (sv.VerticalOffset > 0)
				{
					var target = Math.Max(0, sv.VerticalOffset - step);
					AnimateOrJumpScroll(sv, target);
					e.Handled = true;
				}
				else if (ViewModel.CurrentPage - 1 >= 0)
				{
					var list = this.FindName("PagesList") as ListBox;
					list?.ScrollIntoView(ViewModel.Pages[ViewModel.CurrentPage - 1]);
					ScrollToPage(ViewModel.CurrentPage - 1);
					e.Handled = true;
				}
			}
		}

		private void Image_Loaded(object? sender, RoutedEventArgs e)
		{
			ViewModel?.RequestVisiblePagesMaterialization();
			try
			{
				var s = SettingsManager.Settings;
				if (s != null && sender is Image img)
				{
					var page = img.DataContext as ComicReader.Models.ComicPage;
					var baseSrc = page?.Image as BitmapSource ?? img.Source as BitmapSource;
					if (baseSrc != null)
					{
						var adjKey = $"{s.Brightness:F3}|{s.Contrast:F3}";
						if (img.Tag is string lastKey && lastKey == adjKey)
						{
							// ya aplicado, evitar reprocesado
						}
						else if (Math.Abs(s.Brightness - 1.0) > 0.001 || Math.Abs(s.Contrast - 1.0) > 0.001)
						{
							if (baseSrc != null)
							{
								try
								{
									var adjusted = ImageAdjuster.ApplyBrightnessContrast(baseSrc, s.Brightness, s.Contrast);
									img.Source = adjusted ?? baseSrc;
								}
								catch { img.Source = baseSrc; }
							}
							img.Tag = adjKey;
						}
						else
						{
							img.Source = baseSrc; // original
							img.Tag = adjKey;
						}
					}
					// Si esta es la página actual y la imagen acaba de materializar, recentrar por si cambió el alto
					if (page != null && ViewModel != null && page.PageIndex == ViewModel.CurrentPage)
					{
						Dispatcher.BeginInvoke(new Action(() =>
						{
							try { ScrollToPage(page.PageIndex); } catch { }
						}), DispatcherPriority.Background);
					}
				}
			}
			catch { }
		}

		private DispatcherTimer? _hideTimer;
		private bool _isMouseOverPanel;

		private void ControlPanel_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		{
			_isMouseOverPanel = true;
			AnimatePanel(true);
		}

		private void ControlPanel_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
		{
			_isMouseOverPanel = false;
			EnsureTimer();
			_hideTimer!.Stop();
			_hideTimer!.Start();
		}

		private void EnsureTimer()
		{
			if (_hideTimer != null) return;
			_hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
			_hideTimer.Tick += (_, __) => { if (!_isMouseOverPanel) AnimatePanel(false); };
		}

		private void AnimatePanel(bool show)
		{
			EnsureTimer();
			double to = show ? 0.95 : 0.15;
			var panel = this.FindName("ControlPanel") as System.Windows.Controls.Border;
			var s = ComicReader.Services.SettingsManager.Settings;
			bool enableAnim = s?.EnableAnimations != false; // default true
			bool reduced = s?.PreferReducedMotion == true;
			if (enableAnim)
			{
				var dur = TimeSpan.FromMilliseconds(reduced ? 150 : 300);
				var anim = new DoubleAnimation(to, dur) { EasingFunction = new QuadraticEase() };
				panel?.BeginAnimation(OpacityProperty, anim);
			}
			else
			{
				if (panel != null) panel.Opacity = to;
			}
			if (show)
			{
				_hideTimer!.Stop();
				_hideTimer!.Start();
			}
		}

		private void TryUpdateDecodeTargetWidth()
		{
			try
			{
				var list = this.FindName("PagesList") as ListBox;
				if (list == null) return;
				// Estimar ancho efectivo disponible para la imagen (restar padding/márgenes y scroll bar)
				double raw = Math.Max(0, list.ActualWidth);
				double effective = Math.Max(400, raw - 32); // margen aproximado
				double zoom = Math.Max(0.25, Math.Min(4.0, ViewModel?.Zoom ?? 1.0));
				int target = (int)Math.Round(effective * zoom);
				int minW = 600, maxW = 2600;
				target = Math.Max(minW, Math.Min(maxW, target));
				// Actualizar solo si hay un cambio significativo para evitar invalidaciones constantes
				// Ajuste por instancia en el loader para no persistir en Settings globales
				try
				{
					if (ViewModel?.Loader is ComicReader.Services.ComicPageLoader cpl)
					{
						if (cpl.DecodeTargetWidthOverride != target)
						{
							cpl.DecodeTargetWidthOverride = target;
							ViewModel?.RequestVisiblePagesMaterialization(overscanOverride: 6);
						}
					}
				}
				catch { }
			}
			catch { }
		}

		private DispatcherTimer? _decodeWidthTimer;
		private void EnsureDecodeWidthTimer()
		{
			if (_decodeWidthTimer != null) return;
			_decodeWidthTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
			_decodeWidthTimer.Tick += (s, e) => { try { _decodeWidthTimer!.Stop(); TryUpdateDecodeTargetWidth(); } catch { } };
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);
			EnsureTimer();
			_hideTimer!.Start();
		}

		private void OnViewModelCurrentPageChanged(int index)
		{
			try
			{
				// Evitar bucle: si ya estamos en esa página, solo asegurar que esté a la vista
				if (index < 0 || index >= ViewModel.Pages.Count) return;
				ScrollToPage(index);
			}
			catch { }
		}

		public void ScrollToPage(int index)
		{
			if (index < 0 || index >= ViewModel.Pages.Count) return;
			try
			{
				// Nuevo id para esta solicitud; invalida cualquier animación/reintento previo
				int requestId = System.Threading.Interlocked.Increment(ref _latestScrollRequestId);
				var now = DateTime.UtcNow;
				bool isRapid = (now - _lastProgrammaticScrollAt).TotalMilliseconds < 180;
				_lastProgrammaticScrollAt = now;
				ViewModel.BeginProgrammaticScroll();
				ViewModel.CurrentPage = index;
				// Traer al viewport el ítem solicitado y centrarlo si es posible
				var list = this.FindName("PagesList") as ListBox;
				if (list != null)
				{
					list.ScrollIntoView(ViewModel.Pages[index]);
					// Segundo ScrollIntoView diferido para casos con navegación ultra-rápida
					Dispatcher.BeginInvoke(new Action(() =>
					{
						try { list.ScrollIntoView(ViewModel.Pages[index]); } catch { }
					}), DispatcherPriority.Background);
					// Reintentar si el contenedor aún no está materializado
					Action? centerAction = null;
					int tries = 0;
					centerAction = () =>
					{
						// Si llegó una nueva solicitud, abortar este intento
						if (requestId != _latestScrollRequestId) return;
						tries++;
						try
						{
							// Forzar actualización del layout tras ScrollIntoView
							list.UpdateLayout();
							var container = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(index);
							var sv = FindScrollViewerForList();
							if (container != null && sv != null && container.ActualHeight > 0)
							{
								// Posición relativa del item respecto al ScrollViewer
								var transform = container.TransformToAncestor(sv);
								var pos = transform.Transform(new Point(0, 0));
								double itemCenter = pos.Y + container.ActualHeight / 2.0;
								// Queremos centrar: objetivo = offset actual + (itemCenter - centro del viewport)
								double delta = itemCenter - (sv.ViewportHeight / 2.0);
								double target = Math.Max(0, Math.Min(sv.ExtentHeight - sv.ViewportHeight, sv.VerticalOffset + delta));
												// Si el usuario está pasando rápido o si las animaciones están deshabilitadas, saltar sin animación
												var s2 = ComicReader.Services.SettingsManager.Settings;
												bool enableAnim2 = s2?.EnableAnimations != false;
												bool reduced2 = s2?.PreferReducedMotion == true;
												int dur = isRapid || !enableAnim2 ? 0 : (reduced2 ? 80 : 120);
												AnimateOrJumpScroll(sv, target, dur, requestId);
								// Finalizar el estado programático tras la animación
								Dispatcher.BeginInvoke(new Action(() => { if (requestId == _latestScrollRequestId) ViewModel.EndProgrammaticScroll(); }), DispatcherPriority.ApplicationIdle);
							}
							else if (tries < 3)
							{
								// Aún no está listo; insistir y volver a solicitar ScrollIntoView
								list.ScrollIntoView(ViewModel.Pages[index]);
								Dispatcher.BeginInvoke(centerAction, DispatcherPriority.Background);
							}
							else
							{
								// Fallback: liberar estado para no quedar bloqueados
								if (requestId == _latestScrollRequestId) ViewModel.EndProgrammaticScroll();
							}
						}
						catch
						{
							if (requestId == _latestScrollRequestId) ViewModel.EndProgrammaticScroll();
						}
					};
					Dispatcher.BeginInvoke(centerAction, DispatcherPriority.Background);
				}
			}
			finally { }
		}

		private int FindNearestVisibleIndex()
		{
			var list = this.FindName("PagesList") as ListBox;
			if (list == null || list.Items.Count == 0) return -1;
			// Calcular el elemento cuya posición está más cerca del centro del viewport
			var sv = FindScrollViewerForList();
			if (sv == null) return -1;
			double viewportTop = sv.VerticalOffset;
			double viewportCenter = viewportTop + sv.ViewportHeight / 2.0;
			int bestIndex = -1;
			double bestDist = double.MaxValue;
			for (int i = 0; i < list.Items.Count; i++)
			{
				var container = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(i);
				if (container == null) continue; // aún virtualizado
				var transform = container.TransformToAncestor(list);
				var pos = transform.Transform(new Point(0, 0));
				double itemTop = pos.Y;
				double itemHeight = container.ActualHeight;
				double itemCenter = itemTop + itemHeight / 2.0;
				double dist = Math.Abs(itemCenter - viewportCenter);
				if (dist < bestDist)
				{
					bestDist = dist;
					bestIndex = i;
				}
			}
			return bestIndex;
		}

		public void ReapplyBrightnessContrastVisible()
		{
			try
			{
				var s = SettingsManager.Settings;
				if (s == null) return;
				var list = this.FindName("PagesList") as ListBox;
				if (list == null) return;
				int start = Math.Max(0, ViewModel.CurrentPage - 3);
				int end = Math.Min(ViewModel.Pages.Count - 1, ViewModel.CurrentPage + 3);
				for (int i = start; i <= end; i++)
				{
					var container = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(i);
					if (container == null) continue;
					var img = FindImageInContainer(container);
					if (img != null)
					{
						var page = container.DataContext as ComicReader.Models.ComicPage;
						var baseSrc = page?.Image as BitmapSource ?? img.Source as BitmapSource;
						if (baseSrc != null)
						{
							var adjKey = $"{s.Brightness:F3}|{s.Contrast:F3}";
							if (img.Tag is string lastKey && lastKey == adjKey)
							{
								// ya aplicado
							}
							else if (Math.Abs(s.Brightness - 1.0) < 0.001 && Math.Abs(s.Contrast - 1.0) < 0.001)
							{
								img.Source = baseSrc; // original
								img.Tag = adjKey;
							}
							else
							{
								try
								{
									var adjusted = ImageAdjuster.ApplyBrightnessContrast(baseSrc, s.Brightness, s.Contrast);
									img.Source = adjusted ?? baseSrc;
									img.Tag = adjKey;
								}
								catch { img.Source = baseSrc; img.Tag = adjKey; }
							}
						}
					}
				}
			}
			catch { }
		}

		private Image? FindImageInContainer(ListBoxItem container)
		{
			try
			{
				return FindDescendant<Image>(container);
			}
			catch { return null; }
		}

		private T? FindDescendant<T>(DependencyObject parent) where T : DependencyObject
		{
			if (parent == null) return null;
			int count = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i);
				if (child is T t) return t;
				var res = FindDescendant<T>(child);
				if (res != null) return res;
			}
			return null;
		}

		private ScrollViewer? FindScrollViewerForList()
		{
			try
			{
				var list = this.FindName("PagesList") as ListBox;
				if (list == null) return null;
				return FindDescendant<ScrollViewer>(list);
			}
			catch { return null; }
		}

		private void AnimateOrJumpScroll(ScrollViewer sv, double target, int? durationMs = null, int? requestId = null)
		{
			try
			{
				var s = SettingsManager.Settings;
				bool smooth = s?.SmoothScrolling == true;
				bool enableAnim = s?.EnableAnimations != false;
				bool reduced = s?.PreferReducedMotion == true;
				int dur = durationMs ?? 180;
				if (reduced) dur = Math.Min(dur, 100);
				if (!enableAnim) dur = 0;
				if (!smooth || dur <= 0)
				{
					sv.ScrollToVerticalOffset(target);
					return;
				}
				// Animación simple basada en un reloj; evita crear DPs personalizados
				var from = sv.VerticalOffset;
				var anim = new DoubleAnimation
				{
					From = from,
					To = target,
					Duration = new Duration(TimeSpan.FromMilliseconds(dur)),
					EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
				};
				// Cancelar animación previa si existe
				try { _currentScrollClock?.Controller?.Stop(); } catch { }
				var clock = anim.CreateClock();
				_currentScrollClock = clock;
				EventHandler? handler = null;
				handler = (o, e) =>
				{
					try
					{
						// Abortar si hay una nueva solicitud activa distinta
						if (requestId.HasValue && requestId.Value != _latestScrollRequestId)
						{
							clock.CurrentTimeInvalidated -= handler!;
							return;
						}
						if (clock.CurrentProgress.HasValue)
						{
							var p = clock.CurrentProgress.Value;
							sv.ScrollToVerticalOffset(from + (target - from) * p);
						}
					}
					catch { }
				};
				clock.CurrentTimeInvalidated += handler;
				clock.Completed += (o, e) => { try { clock.CurrentTimeInvalidated -= handler!; } catch { } };
				// Usar TagProperty como dummy para alojar el reloj
				this.ApplyAnimationClock(TagProperty, clock);
			}
			catch { sv.ScrollToVerticalOffset(target); }
		}
	}
}
