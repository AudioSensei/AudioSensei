namespace AudioSensei.Models
{
	public class TrackModel
	{
		public string Playlist { get; set; }
		public string Author { get; set; }
		public string Time { get; set; }

		public TrackModel(string playlist, string author, string time)
		{
			Playlist = playlist;
			Author = author;
			Time = time;
		}
	}
}