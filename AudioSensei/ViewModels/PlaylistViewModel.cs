using System.Windows.Input;
using AudioSensei.Models;

namespace AudioSensei.ViewModels
{
    public class PlaylistViewModel
    {
        public Playlist Playlist { get; set; }
        public ICommand Command { get; set; }
    }
}