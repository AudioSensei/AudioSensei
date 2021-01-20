using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using AudioSensei.Models;
using AudioSensei.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace AudioSensei.Views
{
    public class EqualizerWindow : Window
    {
        private bool _isWindowHidden;

        public EqualizerWindow()
        {
            InitializeComponent();
            _isWindowHidden = true;

#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
            _isWindowHidden = true;
        }

        public void OpenClose()
        {
            if (_isWindowHidden)
            {
                Show();
                _isWindowHidden = false;
            }
            else
            {
                Hide();
                _isWindowHidden = true;
            }
        }
    }
}
