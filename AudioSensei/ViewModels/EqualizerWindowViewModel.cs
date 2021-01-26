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
using System.Collections.Generic;

namespace AudioSensei.ViewModels
{
    public class EqualizerWindowViewModel : ViewModelBase
    {
        FxHandle _equalizerEffectHandle;
        BASS_DX8_PARAMEQ _equalizerParams;

        IAudioStream _audioStream;

        Dictionary<float, float> _equalizerValues = new Dictionary<float, float>();

        public EqualizerWindowViewModel()
        {
            _equalizerParams = new BASS_DX8_PARAMEQ();
        }

        public bool IsEffectApplied
        {
            get => _equalizerEffectHandle != FxHandle.Null;
        }

        public void AddAudioEffect(IAudioStream audioStream)
        {
            _audioStream = audioStream;
            _equalizerEffectHandle = _audioStream.AddEffect(Bass.BassModEffect.FX_DX8_PARAMEQ, 1);
            UpdateEfffectParameters();
        }

        private void ActualizeEqualizerEffectParameters()
        {
            if (_audioStream == null)
            {
                throw new NullReferenceException("Audio Stream cannot be null");
            }

            _audioStream.SetEffectParameters(_equalizerEffectHandle, _equalizerParams);
        }

        private  float GetGainForFrequency(float frequency)
        {
            if (!_equalizerValues.TryGetValue(frequency, out var gain))
                return 0;

            return gain;
        }

        private void SetGainForFrequency(float frequency, float gain)
        {
            if (!_equalizerValues.ContainsKey(80))
            {
                _equalizerValues.Add(80, gain);
            }

            _equalizerValues[frequency] = gain;

            ApplyParametersChange();
        }

        public float GainFor80Hz { get => GetGainForFrequency(80); set => SetGainForFrequency(80, value); }
        public float GainFor250Hz { get => GetGainForFrequency(250); set => SetGainForFrequency(250, value); }
        public float GainFor500Hz { get => GetGainForFrequency(500); set => SetGainForFrequency(500, value); }
        public float GainFor1kHz { get => GetGainForFrequency(1000); set => SetGainForFrequency(1000, value); }
        public float GainFor2kHz { get => GetGainForFrequency(2000); set => SetGainForFrequency(2000, value); }
        public float GainFor4kHz { get => GetGainForFrequency(4000); set => SetGainForFrequency(4000, value); }
        public float GainFor8kHz { get => GetGainForFrequency(8000); set => SetGainForFrequency(8000, value); }
        public float GainFor16kHz { get => GetGainForFrequency(16000); set => SetGainForFrequency(16000, value); }

        private void ApplyParametersChange()
        {
            foreach (var entry in _equalizerValues)
            {
                _equalizerParams.fCenter = entry.Key;
                _equalizerParams.fGain = entry.Value;

                if(_audioStream != null)
                    UpdateEfffectParameters();
            }
        }

        private void UpdateEfffectParameters()
        {
            _audioStream.SetEffectParameters(_equalizerEffectHandle, _equalizerParams);
        }
    }
}
