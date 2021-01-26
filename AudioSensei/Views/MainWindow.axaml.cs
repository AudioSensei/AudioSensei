using System;
using System.IO;
using System.Threading.Tasks;
using AudioSensei.Models;
using AudioSensei.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System.Windows;
using Avalonia.Markup.Xaml;
using Windows.ApplicationModel.DataTransfer;

namespace AudioSensei.Views
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            AddHandler(DragDrop.DropEvent, OnDrop);
            AddHandler(DragDrop.DragOverEvent, OnDragOver);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnDrop(object sender, DragEventArgs dragEventArgs)
        {
            var dataContext = DataContext as MainWindowViewModel;
            var playlistPath = Path.Combine(App.ApplicationDataPath, "Playlists");

            if (dragEventArgs.Data.Contains(DataFormats.FileNames))
            {
                foreach (var fileName in dragEventArgs.Data.GetFileNames())
                {
                    var track = new Track(Source.File, fileName);
                    track.LoadMetadataFromFile();
                    dataContext.CurrentlyVisiblePlaylist.Tracks.Add(track);
                    dataContext.CurrentlyVisiblePlaylist.Save(Path.Combine(playlistPath, $"{dataContext.CurrentlyVisiblePlaylist.UniqueId}.json"));
                }
            }

            if (dragEventArgs.Data.Contains(DataFormats.Text))
            {
                try
                {
                    var uri = new Uri(dragEventArgs.Data.GetText());
                    var domain = uri.Host.Split(".")[uri.Host.StartsWith("www") ? 1 : 0];

                    if (domain.Equals("youtube", StringComparison.CurrentCultureIgnoreCase) || domain.Equals("youtu", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Task.Run(async () =>
                        {
                            var track = new Track(Source.YouTube, dragEventArgs.Data.GetText());
                            var info = await dataContext.YoutubePlayer.GetInfo(track.Url);
                            track.Name = info.Video.Title;
                            track.Author = info.Video.Author;
                            dataContext.CurrentlyVisiblePlaylist.Tracks.Add(track);
                            dataContext.CurrentlyVisiblePlaylist.Save(Path.Combine(playlistPath, $"{dataContext.CurrentlyVisiblePlaylist.UniqueId}.json"));
                        });
                    }
                }
                catch
                {
                    // Ignore exceptions
                }
            }
        }

        private void OnDragOver(object sender, DragEventArgs dragEventArgs)
        {
            dragEventArgs.DragEffects = DragDropEffects.Copy | DragDropEffects.Link;
        }

        private string _clipboardURL;
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.V)
            {
                GetURLFromClipboard();
                AddTrack();
            }
        }

        private async void GetURLFromClipboard()
        {
            _clipboardURL = await Application.Current.Clipboard.GetTextAsync();
        }

        private async void AddTrack()
        {
            var dataContext = DataContext as MainWindowViewModel;
            var playlistPath = Path.Combine(App.ApplicationDataPath, "Playlists");

            if (_clipboardURL.Contains(@"\"))
            {
                try
                {
                    var track = new Track(Source.File, _clipboardURL);
                    track.LoadMetadataFromFile();
                    dataContext.CurrentlyVisiblePlaylist.Tracks.Add(track);
                    dataContext.CurrentlyVisiblePlaylist.Save(Path.Combine(playlistPath, $"{dataContext.CurrentlyVisiblePlaylist.UniqueId}.json"));
                }
                catch
                {
                    //exeption
                }
            }
            else if (_clipboardURL.Contains("."))
            {
                try
                {
                    var uri = new Uri(_clipboardURL);
                    var domain = uri.Host.Split(".")[uri.Host.StartsWith("www") ? 1 : 0];

                    if (domain.Equals("youtube", StringComparison.CurrentCultureIgnoreCase) || domain.Equals("youtu", StringComparison.CurrentCultureIgnoreCase))
                    {
                        await Task.Run(async () =>
                        {
                            var track = new Track(Source.YouTube, _clipboardURL);
                            var info = await dataContext.YoutubePlayer.GetInfo(track.Url);
                            track.Name = info.Video.Title;
                            track.Author = info.Video.Author;
                            dataContext.CurrentlyVisiblePlaylist.Tracks.Add(track);
                            dataContext.CurrentlyVisiblePlaylist.Save(Path.Combine(playlistPath, $"{dataContext.CurrentlyVisiblePlaylist.UniqueId}.json"));
                        });
                    }
                }
                catch
                {
                    //exeption
                }
            }
        }
    }
}
