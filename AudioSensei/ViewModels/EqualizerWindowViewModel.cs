using System;
using System.Buffers;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AudioSensei.Bass.Native.Handles;
using AudioSensei.Bass.Native.Effects;
using AudioSensei.Configuration;
using AudioSensei.Crypto;
using AudioSensei.Discord;
using AudioSensei.Models;
using Avalonia.Controls;
using Avalonia.Threading;
using JetBrains.Annotations;
using ReactiveUI;
using Serilog;

namespace AudioSensei.ViewModels
{
    public class EqualizerWindowViewModel : ViewModelBase
    {
        FxHandle _equalizerEffectHandle;
        BASS_DX8_PARAMEQ _equalizerParams;

        IAudioStream _audioStream;

        public EqualizerWindowViewModel()
        {
            _equalizerParams = new BASS_DX8_PARAMEQ();
        }

        public bool IsEffectApplied()
        {
            return _equalizerEffectHandle != FxHandle.Null;
        }

        public void AddAudioEffect(IAudioStream audioStream)
        {
            _audioStream = audioStream;
            _equalizerEffectHandle = _audioStream.AddEffect(Bass.BassModEffect.FX_DX8_PARAMEQ, 1);
        }

        private void ActualizeEqualizerEffectParameters()
        {
            if (_audioStream == null)
            {
                throw new NullReferenceException("Audio Stream cannot be null");
            }

            _audioStream.SetEffectParameters(_equalizerEffectHandle, _equalizerParams);
        }
    }
}
