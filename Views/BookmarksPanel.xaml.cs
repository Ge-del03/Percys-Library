using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ComicReader.Views
{
	public partial class BookmarksPanelControl : UserControl
    {
		public BookmarksPanelControl()
		{
			BuildUi();
		}

		private void BuildUi()
		{
			var grid = new Grid();
			this.Content = grid;
			var text = new TextBlock
			{
				Text = "Panel de Marcadores",
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				FontSize = 20,
				Foreground = Brushes.White
			};
			grid.Children.Add(text);
		}
	}
}
