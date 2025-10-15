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
		private int _itemSpacing = 8;
		// Espaciado entre items (en px). Cuando se establece, re-aplica a los ListBoxItems visibles.
		public int ItemSpacing
		{
			get => _itemSpacing;
			set
			{
				_itemSpacing = Math.Max(0, value);
				try { ApplyItemSpacing(); } catch { }
			}
		}


		public ContinuousComicViewModel ViewModel { get; }

		public ContinuousComicView()
		{
			InitializeComponent();
			ViewModel = new ContinuousComicViewModel();
			DataContext = ViewModel;
		}

		public IComicPageLoader ComicLoader
		{
			get => ViewModel.Loader;
			set => ViewModel.Loader = value;
		}

		private void ContentScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			ViewModel?.RequestVisiblePagesMaterialization();
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

		private void Image_Loaded(object sender, RoutedEventArgs e)
		{
			ViewModel?.RequestVisiblePagesMaterialization();
			try
			{
				var s = SettingsManager.Settings;
				if (s != null && sender is Image img)
				{
					var page = img.DataContext as ComicReader.Models.ComicPage;
					var baseSrc = page?.Image as BitmapSource ?? img.Source as BitmapSource;
					if (baseSrc != null && (Math.Abs(s.Brightness - 1.0) > 0.001 || Math.Abs(s.Contrast - 1.0) > 0.001))
					{
						img.Source = ImageAdjuster.ApplyBrightnessContrast(baseSrc, s.Brightness, s.Contrast);
					}
				}
			}
			catch { }
		}

		private DispatcherTimer _hideTimer;
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
			_hideTimer.Stop();
			_hideTimer.Start();
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
			var anim = new DoubleAnimation(to, TimeSpan.FromMilliseconds(300)) { EasingFunction = new QuadraticEase() };
			var panel = this.FindName("ControlPanel") as System.Windows.Controls.Border;
			panel?.BeginAnimation(OpacityProperty, anim);
			if (show)
			{
				_hideTimer.Stop();
				_hideTimer.Start();
			}
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);
			EnsureTimer();
			_hideTimer.Start();
		}

		public void ScrollToPage(int index)
		{
			if (index < 0 || index >= ViewModel.Pages.Count) return;
			try
			{
				ViewModel.BeginProgrammaticScroll();
				ViewModel.CurrentPage = index;
				// Traer al viewport el ítem solicitado
				var list = this.FindName("PagesList") as ListBox;
				list?.ScrollIntoView(ViewModel.Pages[index]);
			}
			finally
			{
				// liberar tras un breve diferido para evitar rebotes
				Dispatcher.BeginInvoke(new Action(() => ViewModel.EndProgrammaticScroll()), DispatcherPriority.Background);
			}
		}

		private int FindNearestVisibleIndex()
		{
			var list = this.FindName("PagesList") as ListBox;
			if (list == null || list.Items.Count == 0) return -1;
			// Calcular el elemento cuya posición está más cerca del centro del viewport
			var sv = this.FindName("ContentScroll") as ScrollViewer;
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
							if (Math.Abs(s.Brightness - 1.0) < 0.001 && Math.Abs(s.Contrast - 1.0) < 0.001)
							{
								img.Source = baseSrc; // original
							}
							else
							{
								img.Source = ImageAdjuster.ApplyBrightnessContrast(baseSrc, s.Brightness, s.Contrast);
							}
						}
					}
				}
			}
			catch { }
		}

		private Image FindImageInContainer(ListBoxItem container)
		{
			try
			{
				return FindDescendant<Image>(container);
			}
			catch { return null; }
		}

		private T FindDescendant<T>(DependencyObject parent) where T : DependencyObject
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

		private void ApplyItemSpacing()
		{
			try
			{
				var list = this.FindName("PagesList") as ListBox;
				if (list == null) return;
				foreach (var item in list.Items)
				{
					var container = list.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
					if (container == null) continue;
					container.Margin = new Thickness(0, 0, 0, ItemSpacing);
				}
			}
			catch { }
		}
	}
}
