using System;
using System.IO;
using AudioSensei.Models;
using AudioSensei.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

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
                if (dataContext.CurrentlyVisiblePlaylist != null)
                {
                    foreach (var fileName in dragEventArgs.Data.GetFileNames())
                    {
                        var track = new Track(Source.File, fileName);
                        track.LoadMetadataFromFile();
                        dataContext.CurrentlyVisiblePlaylist.Tracks.Add(track);
                        dataContext.CurrentlyVisiblePlaylist.Save(Path.Combine(playlistPath, $"{dataContext.CurrentlyVisiblePlaylist.UniqueId}.json"));
                    }
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
                        var track = new Track(Source.YouTube, dragEventArgs.Data.GetText());
                        dataContext.CurrentlyVisiblePlaylist.Tracks.Add(track);
                        dataContext.CurrentlyVisiblePlaylist.Save(Path.Combine(playlistPath, $"{dataContext.CurrentlyVisiblePlaylist.UniqueId}.json"));
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
    }
}
