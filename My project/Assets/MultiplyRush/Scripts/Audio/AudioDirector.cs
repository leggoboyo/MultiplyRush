using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplyRush
{
    public enum AudioMusicCue
    {
        None = 0,
        MainMenu = 1,
        Gameplay = 2,
        Pause = 3
    }

    public enum AudioSfxCue
    {
        ButtonTap = 0,
        PlayTransition = 1,
        GatePositive = 2,
        GateNegative = 3,
        PauseOpen = 4,
        PauseClose = 5,
        Win = 6,
        Lose = 7,
        Reinforcement = 8,
        Shield = 9,
        BattleHit = 10,
        BattleStart = 11
    }

    public sealed class AudioDirector : MonoBehaviour
    {
        private const int SampleRate = 44100;
        private const float MusicBaseVolume = 0.62f;
        private const float SfxBaseVolume = 0.94f;

        private static AudioDirector _instance;

        private readonly Dictionary<AudioMusicCue, AudioClip> _musicClips = new Dictionary<AudioMusicCue, AudioClip>(4);
        private readonly Dictionary<AudioSfxCue, AudioClip> _sfxClips = new Dictionary<AudioSfxCue, AudioClip>(12);

        private AudioSource _musicPrimary;
        private AudioSource _musicSecondary;
        private AudioSource _sfxSource;
        private AudioSource _activeMusic;
        private AudioSource _incomingMusic;
        private AudioMusicCue _currentCue = AudioMusicCue.None;
        private float _musicBlend;
        private float _musicBlendDuration = 0.45f;
        private bool _isMusicBlending;

        public static AudioDirector Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AudioDirector>();
                }

                return _instance;
            }
        }

        public static AudioDirector EnsureInstance()
        {
            if (Instance != null)
            {
                return _instance;
            }

            var root = new GameObject("AudioDirector");
            _instance = root.AddComponent<AudioDirector>();
            return _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            BuildAudioGraph();
            BuildProceduralLibrary();
            ApplyMasterVolume();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                _instance = null;
            }
        }

        private void Update()
        {
            if (_isMusicBlending)
            {
                UpdateMusicBlend(Time.unscaledDeltaTime);
            }
        }

        public void SetMusicCue(AudioMusicCue cue, bool immediate = false)
        {
            if (cue == _currentCue && !immediate)
            {
                return;
            }

            if (cue == AudioMusicCue.None)
            {
                StopMusic(immediate);
                _currentCue = cue;
                return;
            }

            if (!_musicClips.TryGetValue(cue, out var clip) || clip == null)
            {
                return;
            }

            if (_activeMusic == null)
            {
                _activeMusic = _musicPrimary;
            }

            if (immediate || _currentCue == AudioMusicCue.None || !_activeMusic.isPlaying)
            {
                _incomingMusic = null;
                _activeMusic.clip = clip;
                _activeMusic.volume = MusicBaseVolume;
                _activeMusic.loop = true;
                _activeMusic.Play();
                if (_musicPrimary != _activeMusic)
                {
                    _musicPrimary.Stop();
                }

                if (_musicSecondary != _activeMusic)
                {
                    _musicSecondary.Stop();
                }

                _isMusicBlending = false;
                _currentCue = cue;
                return;
            }

            _incomingMusic = _activeMusic == _musicPrimary ? _musicSecondary : _musicPrimary;
            _incomingMusic.Stop();
            _incomingMusic.clip = clip;
            _incomingMusic.loop = true;
            _incomingMusic.volume = 0f;
            _incomingMusic.Play();
            _musicBlend = 0f;
            _isMusicBlending = true;
            _currentCue = cue;
        }

        public void PlaySfx(AudioSfxCue cue, float volumeScale = 1f, float pitch = 1f)
        {
            if (_sfxSource == null)
            {
                return;
            }

            if (!_sfxClips.TryGetValue(cue, out var clip) || clip == null)
            {
                return;
            }

            _sfxSource.pitch = Mathf.Clamp(pitch, 0.7f, 1.35f);
            _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * SfxBaseVolume);
        }

        public void RefreshMasterVolume()
        {
            ApplyMasterVolume();
        }

        private void BuildAudioGraph()
        {
            _musicPrimary = CreateChildSource("MusicA", true);
            _musicSecondary = CreateChildSource("MusicB", true);
            _sfxSource = CreateChildSource("Sfx", false);

            _activeMusic = _musicPrimary;
            _musicPrimary.volume = 0f;
            _musicSecondary.volume = 0f;
            _sfxSource.volume = 1f;
        }

        private AudioSource CreateChildSource(string name, bool loop)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);

            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.spread = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.volume = 1f;
            source.ignoreListenerPause = true;
            return source;
        }

        private void BuildProceduralLibrary()
        {
            if (_musicClips.Count == 0)
            {
                _musicClips[AudioMusicCue.MainMenu] = BuildMenuMusic();
                _musicClips[AudioMusicCue.Gameplay] = BuildGameplayMusic();
                _musicClips[AudioMusicCue.Pause] = BuildPauseMusic();
            }

            if (_sfxClips.Count == 0)
            {
                _sfxClips[AudioSfxCue.ButtonTap] = BuildSweepSfx("Sfx_Button", 0.11f, 680f, 420f, 0.35f, 0.04f);
                _sfxClips[AudioSfxCue.PlayTransition] = BuildSweepSfx("Sfx_Play", 0.22f, 320f, 980f, 0.42f, 0.05f);
                _sfxClips[AudioSfxCue.GatePositive] = BuildDualToneSfx("Sfx_GateGood", 0.16f, 420f, 650f, 0.46f);
                _sfxClips[AudioSfxCue.GateNegative] = BuildDualToneSfx("Sfx_GateBad", 0.18f, 310f, 180f, 0.42f);
                _sfxClips[AudioSfxCue.PauseOpen] = BuildSweepSfx("Sfx_PauseOpen", 0.19f, 300f, 180f, 0.35f, 0.03f);
                _sfxClips[AudioSfxCue.PauseClose] = BuildSweepSfx("Sfx_PauseClose", 0.17f, 180f, 320f, 0.35f, 0.03f);
                _sfxClips[AudioSfxCue.Win] = BuildChordSfx("Sfx_Win", 0.62f, new[] { 523.25f, 659.25f, 783.99f }, 0.33f);
                _sfxClips[AudioSfxCue.Lose] = BuildChordSfx("Sfx_Lose", 0.54f, new[] { 329.63f, 246.94f, 196f }, 0.3f);
                _sfxClips[AudioSfxCue.Reinforcement] = BuildDualToneSfx("Sfx_Kit", 0.24f, 390f, 620f, 0.4f);
                _sfxClips[AudioSfxCue.Shield] = BuildDualToneSfx("Sfx_Shield", 0.22f, 260f, 520f, 0.38f);
                _sfxClips[AudioSfxCue.BattleHit] = BuildBurstSfx("Sfx_BattleHit", 0.08f, 0.24f);
                _sfxClips[AudioSfxCue.BattleStart] = BuildSweepSfx("Sfx_BattleStart", 0.28f, 170f, 460f, 0.34f, 0.07f);
            }
        }

        private void UpdateMusicBlend(float deltaTime)
        {
            if (_incomingMusic == null || _activeMusic == null)
            {
                _isMusicBlending = false;
                return;
            }

            var duration = Mathf.Max(0.05f, _musicBlendDuration);
            _musicBlend = Mathf.Clamp01(_musicBlend + (deltaTime / duration));
            var eased = _musicBlend * _musicBlend * (3f - (2f * _musicBlend));

            _activeMusic.volume = Mathf.Lerp(MusicBaseVolume, 0f, eased);
            _incomingMusic.volume = Mathf.Lerp(0f, MusicBaseVolume, eased);

            if (_musicBlend < 1f)
            {
                return;
            }

            _activeMusic.Stop();
            _activeMusic = _incomingMusic;
            _incomingMusic = null;
            _activeMusic.volume = MusicBaseVolume;
            _isMusicBlending = false;
        }

        private void StopMusic(bool immediate)
        {
            if (immediate)
            {
                if (_musicPrimary != null)
                {
                    _musicPrimary.Stop();
                }

                if (_musicSecondary != null)
                {
                    _musicSecondary.Stop();
                }

                _isMusicBlending = false;
                _incomingMusic = null;
                return;
            }

            if (_activeMusic == null || !_activeMusic.isPlaying)
            {
                return;
            }

            _incomingMusic = _activeMusic == _musicPrimary ? _musicSecondary : _musicPrimary;
            _incomingMusic.Stop();
            _incomingMusic.clip = null;
            _incomingMusic.volume = 0f;
            _musicBlend = 0f;
            _isMusicBlending = true;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainMenu")
            {
                SetMusicCue(AudioMusicCue.MainMenu, false);
            }
            else if (scene.name == "Game")
            {
                SetMusicCue(AudioMusicCue.Gameplay, false);
            }
        }

        private static AudioClip BuildMenuMusic()
        {
            var clip = BuildRhythmLoop(
                "Music_Menu",
                bpm: 104f,
                bars: 4,
                melodyMidi: new[] { 72, -1, 76, -1, 79, -1, 76, -1, 74, -1, 76, -1, 79, -1, 83, -1 },
                bassMidi: new[] { 48, 48, 50, 50, 53, 53, 50, 50 },
                brightness: 0.54f,
                kickGain: 0.32f,
                hatGain: 0.16f);
            return clip;
        }

        private static AudioClip BuildGameplayMusic()
        {
            var clip = BuildRhythmLoop(
                "Music_Gameplay",
                bpm: 128f,
                bars: 4,
                melodyMidi: new[] { 74, 79, 81, 79, 76, 79, 83, 81, 74, 79, 81, 79, 76, 74, 72, -1 },
                bassMidi: new[] { 45, 45, 47, 47, 50, 50, 47, 47 },
                brightness: 0.68f,
                kickGain: 0.38f,
                hatGain: 0.2f);
            return clip;
        }

        private static AudioClip BuildPauseMusic()
        {
            var clip = BuildRhythmLoop(
                "Music_Pause",
                bpm: 78f,
                bars: 4,
                melodyMidi: new[] { 69, -1, 71, -1, 74, -1, 71, -1, 67, -1, 69, -1, 71, -1, 69, -1 },
                bassMidi: new[] { 43, 43, 45, 45, 48, 48, 45, 45 },
                brightness: 0.32f,
                kickGain: 0.2f,
                hatGain: 0.1f);
            return clip;
        }

        private static AudioClip BuildRhythmLoop(
            string clipName,
            float bpm,
            int bars,
            int[] melodyMidi,
            int[] bassMidi,
            float brightness,
            float kickGain,
            float hatGain)
        {
            var safeBpm = Mathf.Clamp(bpm, 52f, 170f);
            var stepDuration = (60f / safeBpm) * 0.25f;
            var totalSteps = Mathf.Max(8, bars * 16);
            var totalSamples = Mathf.CeilToInt(totalSteps * stepDuration * SampleRate);
            var data = new float[totalSamples];

            for (var step = 0; step < totalSteps; step++)
            {
                var stepStart = Mathf.FloorToInt(step * stepDuration * SampleRate);
                var envLength = Mathf.FloorToInt(stepDuration * SampleRate * 0.92f);

                var melodyNote = melodyMidi[step % melodyMidi.Length];
                if (melodyNote >= 0)
                {
                    var melodyFrequency = MidiToFrequency(melodyNote);
                    AddTone(data, stepStart, envLength, melodyFrequency, 0.18f + brightness * 0.15f, Waveform.Saw);
                    AddTone(data, stepStart, envLength, melodyFrequency * 2f, 0.06f + brightness * 0.05f, Waveform.Triangle);
                }

                if (step % 2 == 0)
                {
                    var bassNote = bassMidi[(step / 2) % bassMidi.Length];
                    var bassFrequency = MidiToFrequency(bassNote);
                    AddTone(data, stepStart, Mathf.FloorToInt(envLength * 0.95f), bassFrequency, 0.2f, Waveform.Square);
                }

                if (step % 4 == 0)
                {
                    AddKick(data, stepStart, Mathf.FloorToInt(stepDuration * SampleRate * 1.2f), kickGain);
                }

                if (step % 2 == 1)
                {
                    AddHat(data, stepStart, Mathf.FloorToInt(stepDuration * SampleRate * 0.45f), hatGain + brightness * 0.06f);
                }
            }

            SoftClip(data, 0.75f);
            var clip = AudioClip.Create(clipName, totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildSweepSfx(string name, float duration, float startFrequency, float endFrequency, float amplitude, float noiseMix)
        {
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)(sampleCount - 1);
                var eased = 1f - Mathf.Pow(1f - t, 2f);
                var frequency = Mathf.Lerp(startFrequency, endFrequency, eased);
                var env = Mathf.Sin(t * Mathf.PI);
                var tone = Mathf.Sin(2f * Mathf.PI * frequency * i / SampleRate) * amplitude;
                var noise = (UnityEngine.Random.value * 2f - 1f) * noiseMix * amplitude;
                data[i] = (tone + noise) * env;
            }

            SoftClip(data, 0.8f);
            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildDualToneSfx(string name, float duration, float frequencyA, float frequencyB, float amplitude)
        {
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)(sampleCount - 1);
                var env = Mathf.Sin(t * Mathf.PI);
                var toneA = Mathf.Sin(2f * Mathf.PI * frequencyA * i / SampleRate);
                var toneB = Mathf.Sin(2f * Mathf.PI * frequencyB * i / SampleRate);
                var mix = (toneA * 0.6f + toneB * 0.4f) * amplitude;
                data[i] = mix * env;
            }

            SoftClip(data, 0.82f);
            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildChordSfx(string name, float duration, float[] frequencies, float amplitude)
        {
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)(sampleCount - 1);
                var env = Mathf.Pow(1f - t, 2.2f);
                var value = 0f;
                for (var j = 0; j < frequencies.Length; j++)
                {
                    var freq = Mathf.Max(20f, frequencies[j]);
                    value += Mathf.Sin(2f * Mathf.PI * freq * i / SampleRate);
                }

                value /= Mathf.Max(1, frequencies.Length);
                data[i] = value * amplitude * env;
            }

            SoftClip(data, 0.85f);
            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildBurstSfx(string name, float duration, float amplitude)
        {
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            var seed = 1729;
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)(sampleCount - 1);
                var env = Mathf.Pow(1f - t, 3.1f);
                seed = (seed * 1103515245 + 12345) & int.MaxValue;
                var noise = ((seed / (float)int.MaxValue) * 2f - 1f);
                var tone = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(880f, 420f, t) * i / SampleRate) * 0.4f;
                data[i] = (noise * 0.8f + tone * 0.2f) * amplitude * env;
            }

            SoftClip(data, 0.8f);
            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static void AddTone(float[] data, int startSample, int lengthSamples, float frequency, float amplitude, Waveform waveform)
        {
            if (data == null || lengthSamples <= 0 || frequency <= 0f)
            {
                return;
            }

            var start = Mathf.Clamp(startSample, 0, data.Length - 1);
            var end = Mathf.Clamp(start + lengthSamples, 0, data.Length);

            for (var i = start; i < end; i++)
            {
                var local = i - start;
                var t = local / (float)Mathf.Max(1, lengthSamples - 1);
                var env = Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t * 12f)) * Mathf.Pow(1f - t, 1.45f);
                var phase = 2f * Mathf.PI * frequency * local / SampleRate;
                var wave = EvaluateWaveform(waveform, phase);
                data[i] += wave * amplitude * env;
            }
        }

        private static void AddKick(float[] data, int startSample, int lengthSamples, float amplitude)
        {
            var start = Mathf.Clamp(startSample, 0, data.Length - 1);
            var end = Mathf.Clamp(start + lengthSamples, 0, data.Length);
            for (var i = start; i < end; i++)
            {
                var local = i - start;
                var t = local / (float)Mathf.Max(1, lengthSamples - 1);
                var frequency = Mathf.Lerp(160f, 44f, Mathf.Pow(t, 0.45f));
                var env = Mathf.Pow(1f - t, 3.2f);
                var sample = Mathf.Sin(2f * Mathf.PI * frequency * local / SampleRate);
                data[i] += sample * amplitude * env;
            }
        }

        private static void AddHat(float[] data, int startSample, int lengthSamples, float amplitude)
        {
            var start = Mathf.Clamp(startSample, 0, data.Length - 1);
            var end = Mathf.Clamp(start + lengthSamples, 0, data.Length);
            var seed = 28411 + startSample;
            for (var i = start; i < end; i++)
            {
                var local = i - start;
                var t = local / (float)Mathf.Max(1, lengthSamples - 1);
                var env = Mathf.Pow(1f - t, 2.8f);
                seed = (seed * 1664525 + 1013904223) & int.MaxValue;
                var noise = (seed / (float)int.MaxValue) * 2f - 1f;
                data[i] += noise * amplitude * env;
            }
        }

        private static float EvaluateWaveform(Waveform waveform, float phase)
        {
            switch (waveform)
            {
                case Waveform.Square:
                    return Mathf.Sign(Mathf.Sin(phase));
                case Waveform.Saw:
                {
                    var normalized = Mathf.Repeat(phase / (Mathf.PI * 2f), 1f);
                    return (normalized * 2f) - 1f;
                }
                case Waveform.Triangle:
                {
                    var normalized = Mathf.Repeat(phase / (Mathf.PI * 2f), 1f);
                    return 1f - (4f * Mathf.Abs(normalized - 0.5f));
                }
                default:
                    return Mathf.Sin(phase);
            }
        }

        private static float MidiToFrequency(int midiNote)
        {
            return 440f * Mathf.Pow(2f, (midiNote - 69) / 12f);
        }

        private static void SoftClip(float[] samples, float drive)
        {
            if (samples == null)
            {
                return;
            }

            var safeDrive = Mathf.Clamp(drive, 0.2f, 1.4f);
            for (var i = 0; i < samples.Length; i++)
            {
                var x = samples[i] * safeDrive;
                samples[i] = (float)Math.Tanh(x);
            }
        }

        private void ApplyMasterVolume()
        {
            AudioListener.volume = ProgressionStore.GetMasterVolume(0.85f);
        }

        private enum Waveform
        {
            Sine,
            Square,
            Saw,
            Triangle
        }
    }
}
