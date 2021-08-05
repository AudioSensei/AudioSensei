using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AudioSensei.Models
{
	public class PlaylistModel
	{
		public IImage Cover { get; set; }
		public string Title { get; set; }
		public string Time { get; set; }

		public PlaylistModel(string coverUrl, string title, string time)
		{
			Cover = new Bitmap(AvaloniaLocator.Current.GetService<IAssetLoader>().Open(new Uri(coverUrl)));
			Title = title;
			Time = time;
		}
	}
}