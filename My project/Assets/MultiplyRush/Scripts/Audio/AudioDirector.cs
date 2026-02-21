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
        Pause = 3,
        Battle = 4
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
        private const int GameplayMusicTrackCount = 6;
        private static readonly int[] MajorScaleIntervals = { 0, 2, 4, 5, 7, 9, 11 };
        private static readonly int[] MinorScaleIntervals = { 0, 2, 3, 5, 7, 8, 10 };
        private static readonly string[] GameplayTrackNames =
        {
            "Hyper Neon",
            "Skyline Rush",
            "Steel Pulse",
            "Turbo Drift",
            "Glass Horizon",
            "Voltage Lane"
        };

        private static AudioDirector _instance;

        private readonly Dictionary<AudioMusicCue, AudioClip> _musicClips = new Dictionary<AudioMusicCue, AudioClip>(4);
        private readonly Dictionary<AudioSfxCue, AudioClip> _sfxClips = new Dictionary<AudioSfxCue, AudioClip>(12);
        private readonly AudioClip[] _gameplayTracks = new AudioClip[GameplayMusicTrackCount];

        private AudioSource _musicPrimary;
        private AudioSource _musicSecondary;
        private AudioSource _sfxSource;
        private AudioSource _activeMusic;
        private AudioSource _incomingMusic;
        private AudioMusicCue _currentCue = AudioMusicCue.None;
        private float _musicBlend;
        private float _musicBlendDuration = 0.45f;
        private bool _isMusicBlending;
        private int _selectedGameplayTrackIndex;

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
            _selectedGameplayTrackIndex = ProgressionStore.GetGameplayMusicTrack(0, GameplayMusicTrackCount);
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

            AudioClip clip = null;
            if (cue == AudioMusicCue.Gameplay)
            {
                var safeTrack = Mathf.Clamp(_selectedGameplayTrackIndex, 0, GameplayMusicTrackCount - 1);
                clip = _gameplayTracks[safeTrack];
                if (clip == null)
                {
                    for (var i = 0; i < _gameplayTracks.Length; i++)
                    {
                        if (_gameplayTracks[i] != null)
                        {
                            clip = _gameplayTracks[i];
                            break;
                        }
                    }
                }
            }
            else if (_musicClips.TryGetValue(cue, out var mappedClip))
            {
                clip = mappedClip;
            }

            if (clip == null)
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

        public int GetGameplayTrackCount()
        {
            return GameplayMusicTrackCount;
        }

        public int GetSelectedGameplayTrackIndex()
        {
            return _selectedGameplayTrackIndex;
        }

        public string GetGameplayTrackName(int index)
        {
            if (GameplayTrackNames.Length == 0)
            {
                return "Track";
            }

            var safeIndex = Mathf.Clamp(index, 0, GameplayTrackNames.Length - 1);
            return GameplayTrackNames[safeIndex];
        }

        public void SetGameplayTrackIndex(int index, bool refreshActiveCue = true)
        {
            var safeIndex = Mathf.Clamp(index, 0, GameplayMusicTrackCount - 1);
            if (_selectedGameplayTrackIndex == safeIndex)
            {
                return;
            }

            _selectedGameplayTrackIndex = safeIndex;
            ProgressionStore.SetGameplayMusicTrack(_selectedGameplayTrackIndex, GameplayMusicTrackCount);
            if (refreshActiveCue && _currentCue == AudioMusicCue.Gameplay)
            {
                SetMusicCue(AudioMusicCue.Gameplay, false);
            }
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
                _musicClips[AudioMusicCue.Pause] = BuildPauseMusic();
                _musicClips[AudioMusicCue.Battle] = BuildBattleMusic();
            }

            if (_gameplayTracks[0] == null)
            {
                _gameplayTracks[0] = BuildGameplayTrackA();
                _gameplayTracks[1] = BuildGameplayTrackB();
                _gameplayTracks[2] = BuildGameplayTrackC();
                _gameplayTracks[3] = BuildGameplayTrackD();
                _gameplayTracks[4] = BuildGameplayTrackE();
                _gameplayTracks[5] = BuildGameplayTrackF();
            }

            if (_sfxClips.Count == 0)
            {
                _sfxClips[AudioSfxCue.ButtonTap] = BuildSweepSfx("Sfx_Button", 0.11f, 680f, 420f, 0.35f, 0.04f);
                _sfxClips[AudioSfxCue.PlayTransition] = BuildSweepSfx("Sfx_Play", 0.22f, 320f, 980f, 0.42f, 0.05f);
                _sfxClips[AudioSfxCue.GatePositive] = BuildDualToneSfx("Sfx_GateGood", 0.16f, 420f, 650f, 0.46f);
                _sfxClips[AudioSfxCue.GateNegative] = BuildDualToneSfx("Sfx_GateBad", 0.18f, 310f, 180f, 0.42f);
                _sfxClips[AudioSfxCue.PauseOpen] = BuildSweepSfx("Sfx_PauseOpen", 0.19f, 300f, 180f, 0.35f, 0.03f);
                _sfxClips[AudioSfxCue.PauseClose] = BuildSweepSfx("Sfx_PauseClose", 0.17f, 180f, 320f, 0.35f, 0.03f);
                _sfxClips[AudioSfxCue.Win] = BuildVictoryStinger();
                _sfxClips[AudioSfxCue.Lose] = BuildDefeatStinger();
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
            return BuildModernLoop(
                clipName: "Music_Menu",
                bpm: 112f,
                bars: 8,
                chordRoots: new[] { 57, 53, 55, 60 },
                minorKey: false,
                energy: 0.52f,
                atmosphere: 0.58f,
                sparseLead: false);
        }

        private static AudioClip BuildGameplayTrackA()
        {
            return BuildModernLoop(
                clipName: "Music_Gameplay_01_HyperNeon",
                bpm: 126f,
                bars: 8,
                chordRoots: new[] { 45, 48, 43, 50 },
                minorKey: true,
                energy: 0.88f,
                atmosphere: 0.42f,
                sparseLead: false);
        }

        private static AudioClip BuildGameplayTrackB()
        {
            return BuildModernLoop(
                clipName: "Music_Gameplay_02_SkylineRush",
                bpm: 122f,
                bars: 8,
                chordRoots: new[] { 50, 45, 47, 52 },
                minorKey: false,
                energy: 0.76f,
                atmosphere: 0.5f,
                sparseLead: false);
        }

        private static AudioClip BuildGameplayTrackC()
        {
            return BuildModernLoop(
                clipName: "Music_Gameplay_03_SteelPulse",
                bpm: 132f,
                bars: 8,
                chordRoots: new[] { 43, 47, 42, 50 },
                minorKey: true,
                energy: 0.92f,
                atmosphere: 0.36f,
                sparseLead: false);
        }

        private static AudioClip BuildGameplayTrackD()
        {
            return BuildModernLoop(
                clipName: "Music_Gameplay_04_TurboDrift",
                bpm: 128f,
                bars: 8,
                chordRoots: new[] { 55, 50, 48, 53 },
                minorKey: false,
                energy: 0.82f,
                atmosphere: 0.44f,
                sparseLead: false);
        }

        private static AudioClip BuildGameplayTrackE()
        {
            return BuildModernLoop(
                clipName: "Music_Gameplay_05_GlassHorizon",
                bpm: 118f,
                bars: 8,
                chordRoots: new[] { 48, 52, 45, 50 },
                minorKey: true,
                energy: 0.7f,
                atmosphere: 0.66f,
                sparseLead: false);
        }

        private static AudioClip BuildGameplayTrackF()
        {
            return BuildModernLoop(
                clipName: "Music_Gameplay_06_VoltageLane",
                bpm: 134f,
                bars: 8,
                chordRoots: new[] { 47, 52, 45, 54 },
                minorKey: true,
                energy: 0.94f,
                atmosphere: 0.32f,
                sparseLead: false);
        }

        private static AudioClip BuildBattleMusic()
        {
            return BuildModernLoop(
                clipName: "Music_Battle",
                bpm: 138f,
                bars: 4,
                chordRoots: new[] { 45, 43, 47, 50 },
                minorKey: true,
                energy: 0.97f,
                atmosphere: 0.26f,
                sparseLead: false);
        }

        private static AudioClip BuildPauseMusic()
        {
            return BuildModernLoop(
                clipName: "Music_Pause",
                bpm: 84f,
                bars: 8,
                chordRoots: new[] { 50, 47, 45, 52 },
                minorKey: true,
                energy: 0.3f,
                atmosphere: 0.76f,
                sparseLead: true);
        }

        private static AudioClip BuildVictoryStinger()
        {
            var chord = BuildChordSfx("Sfx_Win", 1.05f, new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.34f);
            return chord;
        }

        private static AudioClip BuildDefeatStinger()
        {
            var chord = BuildChordSfx("Sfx_Lose", 0.86f, new[] { 392f, 293.66f, 246.94f }, 0.33f);
            return chord;
        }

        private static AudioClip BuildModernLoop(
            string clipName,
            float bpm,
            int bars,
            int[] chordRoots,
            bool minorKey,
            float energy,
            float atmosphere,
            bool sparseLead)
        {
            var safeBpm = Mathf.Clamp(bpm, 52f, 170f);
            var stepDuration = (60f / safeBpm) * 0.25f;
            var totalSteps = Mathf.Max(8, bars * 16);
            var totalSamples = Mathf.CeilToInt(totalSteps * stepDuration * SampleRate);
            var musicLayer = new float[totalSamples];
            var drumLayer = new float[totalSamples];
            var sidechain = new float[totalSamples];
            var barSamples = Mathf.RoundToInt(stepDuration * 16f * SampleRate);
            var leadPattern = sparseLead
                ? new[] { 0, -1, 2, -1, 4, -1, 5, -1, 4, -1, 2, -1, 1, -1, 0, -1 }
                : new[] { 0, 2, 4, 5, 4, 2, 1, -1, 0, 2, 4, 6, 5, 4, 2, -1 };
            var leadRhythm = sparseLead
                ? new[] { true, false, false, false, true, false, false, false, true, false, false, false, true, false, false, false }
                : new[] { true, false, true, false, true, true, false, false, true, false, true, false, true, true, false, false };
            var bassPattern = new[] { 0, -1, 0, -1, 4, -1, 2, -1, 0, -1, 0, -1, 5, -1, 2, -1 };
            var stabPattern = new[] { false, false, true, false, false, false, true, false, false, false, true, false, false, false, true, false };
            var safeEnergy = Mathf.Clamp01(energy);
            var safeAtmosphere = Mathf.Clamp01(atmosphere);
            var kickDuckDepth = Mathf.Lerp(0.08f, 0.24f, safeEnergy);
            for (var i = 0; i < sidechain.Length; i++)
            {
                sidechain[i] = 1f;
            }

            for (var bar = 0; bar < Mathf.Max(1, bars); bar++)
            {
                var barStart = Mathf.RoundToInt(bar * stepDuration * 16f * SampleRate);
                var rootMidi = chordRoots[bar % chordRoots.Length];
                AddWarmPadChord(
                    musicLayer,
                    barStart,
                    Mathf.RoundToInt(barSamples * 0.98f),
                    rootMidi,
                    minorKey,
                    0.1f + safeAtmosphere * 0.14f,
                    0.22f + safeAtmosphere * 0.35f);

                for (var step = 0; step < 16; step++)
                {
                    var globalStep = bar * 16 + step;
                    var swingSamples = step % 2 == 1
                        ? Mathf.RoundToInt(stepDuration * SampleRate * (0.012f + safeEnergy * 0.03f))
                        : 0;
                    var stepStart = Mathf.FloorToInt(globalStep * stepDuration * SampleRate) + swingSamples;
                    stepStart = Mathf.Clamp(stepStart, 0, totalSamples - 1);
                    var stepSamples = Mathf.FloorToInt(stepDuration * SampleRate);
                    var sixteenth = step % 4;

                    var kick = step == 0 || step == 8 ||
                               (safeEnergy > 0.62f && (step == 6 || step == 12)) ||
                               (safeEnergy > 0.8f && bar % 2 == 1 && step == 14);
                    if (kick)
                    {
                        AddDeepKick(drumLayer, stepStart, Mathf.RoundToInt(stepSamples * 1.35f), 0.22f + safeEnergy * 0.26f);
                        ApplySidechainDuck(
                            sidechain,
                            stepStart,
                            Mathf.RoundToInt(stepSamples * (1.8f + safeAtmosphere * 0.6f)),
                            kickDuckDepth);
                    }

                    if (step == 4 || step == 12)
                    {
                        AddSnare(drumLayer, stepStart, Mathf.RoundToInt(stepSamples * 1.1f), 0.11f + safeEnergy * 0.16f);
                        AddClap(drumLayer, stepStart + Mathf.RoundToInt(stepSamples * 0.02f), Mathf.RoundToInt(stepSamples * 0.8f), 0.08f + safeEnergy * 0.1f);
                    }

                    var hat = step % 2 == 1 || (safeEnergy > 0.72f && sixteenth == 0) || (safeEnergy > 0.86f && sixteenth == 2);
                    if (hat)
                    {
                        var hatGain = (sixteenth == 0 ? 0.048f : 0.032f) + safeEnergy * 0.034f;
                        AddClosedHat(drumLayer, stepStart, Mathf.RoundToInt(stepSamples * 0.45f), hatGain);
                    }

                    if (safeEnergy > 0.45f && (step == 3 || step == 11))
                    {
                        AddOpenHat(
                            drumLayer,
                            stepStart + Mathf.RoundToInt(stepSamples * 0.08f),
                            Mathf.RoundToInt(stepSamples * (1.6f + safeEnergy * 0.5f)),
                            0.035f + safeEnergy * 0.05f);
                    }

                    var bassDegree = bassPattern[(globalStep + 8) % bassPattern.Length];
                    if (bassDegree >= 0 && (sixteenth == 0 || sixteenth == 2))
                    {
                        var bassMidi = BuildScaleMidi(rootMidi - 12, minorKey, bassDegree, 0);
                        var bassFrequency = MidiToFrequency(bassMidi);
                        var bassLength = sixteenth == 0
                            ? Mathf.RoundToInt(stepSamples * (1.45f + safeEnergy * 0.2f))
                            : Mathf.RoundToInt(stepSamples * 0.8f);
                        AddSubBass(musicLayer, stepStart, bassLength, bassFrequency, 0.11f + safeEnergy * 0.12f);
                    }

                    if (stabPattern[step] && !sparseLead)
                    {
                        AddChordStab(
                            musicLayer,
                            stepStart,
                            Mathf.RoundToInt(stepSamples * (0.95f + safeAtmosphere * 0.25f)),
                            rootMidi + (bar % 2 == 0 ? 0 : 12),
                            minorKey,
                            0.05f + safeEnergy * 0.06f);
                    }

                    var leadDegree = leadPattern[(globalStep + (bar % 2 == 0 ? 0 : 5)) % leadPattern.Length];
                    if (leadDegree >= 0 && leadRhythm[step])
                    {
                        var allowLead = !sparseLead || sixteenth == 0 || (sixteenth == 2 && (bar % 2 == 0 || safeEnergy > 0.6f));
                        if (allowLead)
                        {
                            var leadMidi = BuildScaleMidi(rootMidi + 12, minorKey, leadDegree, 0);
                            if (!sparseLead && bar % 4 == 3 && (step == 10 || step == 14))
                            {
                                leadMidi += 12;
                            }

                            var leadFrequency = MidiToFrequency(leadMidi);
                            var leadLength = Mathf.RoundToInt(stepSamples * (sparseLead ? 1.7f : 1.1f));
                            AddLeadPluck(musicLayer, stepStart, leadLength, leadFrequency, 0.04f + safeEnergy * 0.055f);
                        }
                    }
                }
            }

            var data = new float[totalSamples];
            for (var i = 0; i < totalSamples; i++)
            {
                data[i] = drumLayer[i] + (musicLayer[i] * sidechain[i]);
            }

            var delaySamples = Mathf.Clamp(Mathf.RoundToInt(stepDuration * SampleRate * 3f), 2205, 22050);
            ApplyFeedbackDelay(
                data,
                delaySamples,
                feedback: 0.14f + safeAtmosphere * 0.24f,
                mix: 0.08f + safeAtmosphere * 0.1f);
            ApplyChorus(
                data,
                depthSamples: Mathf.Lerp(8f, 22f, safeAtmosphere),
                rateHz: Mathf.Lerp(0.09f, 0.24f, safeAtmosphere),
                mix: Mathf.Lerp(0.07f, 0.16f, safeAtmosphere));
            ApplyOnePoleLowPass(data, Mathf.Lerp(6900f, 11800f, safeAtmosphere));
            ApplyOnePoleHighPass(data, Mathf.Lerp(34f, 72f, safeEnergy));
            NormalizePeak(data, 0.82f);
            SoftClip(data, 0.95f);

            var clip = AudioClip.Create(clipName, totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static int BuildScaleMidi(int rootMidi, bool minorKey, int degree, int octaveOffset)
        {
            if (degree < 0)
            {
                return rootMidi + (octaveOffset * 12);
            }

            var intervals = minorKey ? MinorScaleIntervals : MajorScaleIntervals;
            var scaleLength = intervals.Length;
            var octave = degree / scaleLength;
            var index = degree % scaleLength;
            return rootMidi + intervals[index] + (12 * (octave + octaveOffset));
        }

        private static void AddWarmPadChord(
            float[] data,
            int startSample,
            int lengthSamples,
            int rootMidi,
            bool minor,
            float amplitude,
            float width)
        {
            var third = rootMidi + (minor ? 3 : 4);
            var fifth = rootMidi + 7;
            var octave = rootMidi + 12;
            var ninth = rootMidi + 14;
            AddPadVoice(data, startSample, lengthSamples, MidiToFrequency(rootMidi), amplitude * 0.38f, width);
            AddPadVoice(data, startSample, lengthSamples, MidiToFrequency(third), amplitude * 0.3f, width * 0.95f);
            AddPadVoice(data, startSample, lengthSamples, MidiToFrequency(fifth), amplitude * 0.2f, width * 0.82f);
            AddPadVoice(data, startSample, lengthSamples, MidiToFrequency(octave), amplitude * 0.16f, width * 0.72f);
            AddPadVoice(data, startSample, lengthSamples, MidiToFrequency(ninth), amplitude * 0.08f, width * 0.64f);
        }

        private static void AddPadVoice(float[] data, int startSample, int lengthSamples, float frequency, float amplitude, float width)
        {
            if (data == null || lengthSamples <= 0 || frequency <= 0f)
            {
                return;
            }

            var start = Mathf.Clamp(startSample, 0, data.Length - 1);
            var end = Mathf.Clamp(start + lengthSamples, 0, data.Length);
            var safeWidth = Mathf.Clamp(width, 0f, 1f);
            for (var i = start; i < end; i++)
            {
                var local = i - start;
                var t = local / (float)Mathf.Max(1, lengthSamples - 1);
                var attack = Mathf.Clamp01(t * 4f);
                var release = Mathf.Pow(1f - t, 1.1f);
                var env = attack * release;
                var phaseA = 2f * Mathf.PI * frequency * local / SampleRate;
                var phaseB = 2f * Mathf.PI * frequency * (1f + safeWidth * 0.0036f) * local / SampleRate;
                var phaseC = 2f * Mathf.PI * frequency * (1f - safeWidth * 0.0028f) * local / SampleRate;
                var wave = EvaluateWaveform(Waveform.Triangle, phaseA) * 0.4f +
                           EvaluateWaveform(Waveform.Sine, phaseB) * 0.36f +
                           EvaluateWaveform(Waveform.Saw, phaseC) * 0.24f;
                data[i] += wave * amplitude * env;
            }
        }

        private static void AddChordStab(float[] data, int startSample, int lengthSamples, int rootMidi, bool minor, float amplitude)
        {
            var third = rootMidi + (minor ? 3 : 4);
            var fifth = rootMidi + 7;
            AddStabVoice(data, startSample, lengthSamples, MidiToFrequency(rootMidi), amplitude * 0.38f);
            AddStabVoice(data, startSample, lengthSamples, MidiToFrequency(third), amplitude * 0.34f);
            AddStabVoice(data, startSample, lengthSamples, MidiToFrequency(fifth), amplitude * 0.28f);
        }

        private static void AddStabVoice(float[] data, int startSample, int lengthSamples, float frequency, float amplitude)
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
                var attack = Mathf.Clamp01(t * 24f);
                var decay = Mathf.Exp(-4.8f * t);
                var env = attack * decay;
                var drift = 1f + Mathf.Sin(local * 0.00021f) * 0.0018f;
                var phaseA = 2f * Mathf.PI * frequency * drift * local / SampleRate;
                var phaseB = 2f * Mathf.PI * frequency * (drift + 0.0032f) * local / SampleRate;
                var phaseC = 2f * Mathf.PI * frequency * (drift - 0.0026f) * local / SampleRate;
                var wave = EvaluateWaveform(Waveform.Saw, phaseA) * 0.44f +
                           EvaluateWaveform(Waveform.Triangle, phaseB) * 0.32f +
                           Mathf.Sin(phaseC) * 0.24f;
                data[i] += wave * amplitude * env;
            }
        }

        private static void AddSubBass(float[] data, int startSample, int lengthSamples, float frequency, float amplitude)
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
                var attack = Mathf.Clamp01(t * 18f);
                var release = Mathf.Pow(1f - t, 1.45f);
                var env = attack * release;
                var glideFrequency = Mathf.Lerp(frequency * 1.05f, frequency, Mathf.Clamp01(t * 3f));
                var phase = 2f * Mathf.PI * glideFrequency * local / SampleRate;
                var body = Mathf.Sin(phase) * 0.84f +
                           Mathf.Sin(phase * 2f + 0.35f) * 0.12f +
                           EvaluateWaveform(Waveform.Sine, phase * 0.5f) * 0.04f;
                var wave = (float)Math.Tanh(body * 1.36f);
                data[i] += wave * amplitude * env;
            }
        }

        private static void AddLeadPluck(float[] data, int startSample, int lengthSamples, float frequency, float amplitude)
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
                var attack = Mathf.Clamp01(t * 24f);
                var decay = Mathf.Exp(-4.9f * t);
                var env = attack * decay;
                var vibrato = 1f + Mathf.Sin(local * 0.00055f) * 0.0022f;
                var phase = 2f * Mathf.PI * frequency * vibrato * local / SampleRate;
                var wave = EvaluateWaveform(Waveform.Saw, phase) * 0.38f +
                           EvaluateWaveform(Waveform.Triangle, phase * 1.01f) * 0.34f +
                           EvaluateWaveform(Waveform.Sine, phase * 0.5f) * 0.28f;
                var transient = Mathf.Exp(-72f * t) * Mathf.Sin(phase * 3.05f) * 0.23f;
                data[i] += (wave + transient) * amplitude * env;
            }
        }

        private static void AddDeepKick(float[] data, int startSample, int lengthSamples, float amplitude)
        {
            if (data == null || lengthSamples <= 0)
            {
                return;
            }

            var start = Mathf.Clamp(startSample, 0, data.Length - 1);
            var end = Mathf.Clamp(start + lengthSamples, 0, data.Length);
            for (var i = start; i < end; i++)
            {
                var local = i - start;
                var t = local / (float)Mathf.Max(1, lengthSamples - 1);
                var frequency = Mathf.Lerp(152f, 46f, Mathf.Pow(t, 0.42f));
                var env = Mathf.Pow(1f - t, 3.3f);
                var body = Mathf.Sin(2f * Mathf.PI * frequency * local / SampleRate);
                var click = Mathf.Exp(-48f * t) * Mathf.Sin(2f * Mathf.PI * 2200f * local / SampleRate);
                var punch = Mathf.Sin(2f * Mathf.PI * 96f * local / SampleRate) * Mathf.Exp(-14f * t);
                data[i] += (body * 0.74f + punch * 0.2f + click * 0.14f) * amplitude * env;
            }
        }

        private static void AddSnare(float[] data, int startSample, int lengthSamples, float amplitude)
        {
            if (data == null || lengthSamples <= 0)
            {
                return;
            }

            var start = Mathf.Clamp(startSample, 0, data.Length - 1);
            var end = Mathf.Clamp(start + lengthSamples, 0, data.Length);
            var noiseState = (uint)(startSample + 9176);
            var lowpass = 0f;
            for (var i = start; i < end; i++)
            {
                var local = i - start;
                var t = local / (float)Mathf.Max(1, lengthSamples - 1);
                var noise = NextNoise(ref noiseState);
                lowpass += (noise - lowpass) * 0.17f;
                var highNoise = noise - lowpass;
                var noiseEnv = Mathf.Pow(1f - t, 2.4f);
                var toneEnv = Mathf.Pow(1f - t, 4.5f);
                var tone = Mathf.Sin(2f * Mathf.PI * 196f * local / SampleRate) * toneEnv;
                data[i] += (highNoise * noiseEnv * 0.78f + tone * 0.22f) * amplitude;
            }
        }

        private static void AddClap(float[] data, int startSample, int lengthSamples, float amplitude)
        {
            if (data == null || lengthSamples <= 0)
            {
                return;
            }

            var offsets = new[] { 0, 210, 470 };
            for (var i = 0; i < offsets.Length; i++)
            {
                AddNoiseBurst(data, startSample + offsets[i], Mathf.RoundToInt(lengthSamples * 0.62f), amplitude * (0.92f - i * 0.18f), 0.12f);
            }
        }

        private static void AddOpenHat(float[] data, int startSample, int lengthSamples, float amplitude)
        {
            if (data == null || lengthSamples <= 0)
            {
                return;
            }

            var start = Mathf.Clamp(startSample, 0, data.Length - 1);
            var end = Mathf.Clamp(start + lengthSamples, 0, data.Length);
            var noiseState = (uint)(startSample + 18041);
            var lowpass = 0f;
            for (var i = start; i < end; i++)
            {
                var local = i - start;
                var t = local / (float)Mathf.Max(1, lengthSamples - 1);
                var noise = NextNoise(ref noiseState);
                lowpass += (noise - lowpass) * 0.045f;
                var highNoise = noise - lowpass;
                var env = Mathf.Pow(1f - t, 2.35f);
                var metallic = Mathf.Sin(2f * Mathf.PI * 6320f * local / SampleRate) * 0.16f +
                               Mathf.Sin(2f * Mathf.PI * 9120f * local / SampleRate) * 0.08f;
                data[i] += (highNoise * 0.8f + metallic * 0.2f) * amplitude * env;
            }
        }

        private static void AddClosedHat(float[] data, int startSample, int lengthSamples, float amplitude)
        {
            if (data == null || lengthSamples <= 0)
            {
                return;
            }

            var start = Mathf.Clamp(startSample, 0, data.Length - 1);
            var end = Mathf.Clamp(start + lengthSamples, 0, data.Length);
            var noiseState = (uint)(startSample + 12031);
            var lowpass = 0f;
            for (var i = start; i < end; i++)
            {
                var local = i - start;
                var t = local / (float)Mathf.Max(1, lengthSamples - 1);
                var noise = NextNoise(ref noiseState);
                lowpass += (noise - lowpass) * 0.08f;
                var highNoise = noise - lowpass;
                var env = Mathf.Pow(1f - t, 3.4f);
                var metallic = Mathf.Sin(2f * Mathf.PI * 7900f * local / SampleRate) * 0.17f +
                               Mathf.Sin(2f * Mathf.PI * 10200f * local / SampleRate) * 0.09f;
                data[i] += (highNoise * 0.82f + metallic * 0.18f) * amplitude * env;
            }
        }

        private static void AddNoiseBurst(float[] data, int startSample, int lengthSamples, float amplitude, float lowpassAlpha)
        {
            if (data == null || lengthSamples <= 0)
            {
                return;
            }

            var start = Mathf.Clamp(startSample, 0, data.Length - 1);
            var end = Mathf.Clamp(start + lengthSamples, 0, data.Length);
            var noiseState = (uint)(startSample + 32471);
            var lowpass = 0f;
            var alpha = Mathf.Clamp(lowpassAlpha, 0.01f, 0.8f);
            for (var i = start; i < end; i++)
            {
                var local = i - start;
                var t = local / (float)Mathf.Max(1, lengthSamples - 1);
                var noise = NextNoise(ref noiseState);
                lowpass += (noise - lowpass) * alpha;
                var bright = noise - lowpass;
                var env = Mathf.Pow(1f - t, 2.6f);
                data[i] += bright * amplitude * env;
            }
        }

        private static float NextNoise(ref uint state)
        {
            state = state * 1664525u + 1013904223u;
            return (state / (float)uint.MaxValue) * 2f - 1f;
        }

        private static void ApplySidechainDuck(float[] samples, int startSample, int lengthSamples, float depth)
        {
            if (samples == null || samples.Length == 0 || lengthSamples <= 0)
            {
                return;
            }

            var safeDepth = Mathf.Clamp(depth, 0f, 0.72f);
            var start = Mathf.Clamp(startSample, 0, samples.Length - 1);
            var end = Mathf.Clamp(start + lengthSamples, 0, samples.Length);
            var dropSamples = Mathf.Max(1, Mathf.RoundToInt(lengthSamples * 0.1f));
            var releaseSamples = Mathf.Max(1, lengthSamples - dropSamples);
            for (var i = start; i < end; i++)
            {
                var local = i - start;
                float duckValue;
                if (local <= dropSamples)
                {
                    var t = local / (float)dropSamples;
                    duckValue = Mathf.Lerp(1f, 1f - safeDepth, t);
                }
                else
                {
                    var t = (local - dropSamples) / (float)releaseSamples;
                    duckValue = Mathf.Lerp(1f - safeDepth, 1f, Mathf.Pow(t, 0.8f));
                }

                samples[i] = Mathf.Min(samples[i], duckValue);
            }
        }

        private static void ApplyChorus(float[] samples, float depthSamples, float rateHz, float mix)
        {
            if (samples == null || samples.Length == 0)
            {
                return;
            }

            var safeDepth = Mathf.Clamp(depthSamples, 1f, 35f);
            var safeRate = Mathf.Clamp(rateHz, 0.03f, 1.2f);
            var safeMix = Mathf.Clamp01(mix);
            var dry = new float[samples.Length];
            Array.Copy(samples, dry, samples.Length);

            for (var i = 0; i < samples.Length; i++)
            {
                var lfo = Mathf.Sin(2f * Mathf.PI * safeRate * i / SampleRate) * 0.5f + 0.5f;
                var delay = 8f + safeDepth * lfo;
                var read = i - delay;
                if (read <= 1f)
                {
                    continue;
                }

                var readIndex = Mathf.FloorToInt(read);
                var frac = read - readIndex;
                var a = dry[Mathf.Clamp(readIndex, 0, dry.Length - 1)];
                var b = dry[Mathf.Clamp(readIndex + 1, 0, dry.Length - 1)];
                var delayed = Mathf.Lerp(a, b, frac);
                samples[i] = Mathf.Lerp(dry[i], (dry[i] * 0.74f) + (delayed * 0.58f), safeMix);
            }
        }

        private static void ApplyFeedbackDelay(float[] samples, int delaySamples, float feedback, float mix)
        {
            if (samples == null || samples.Length == 0 || delaySamples <= 0 || delaySamples >= samples.Length)
            {
                return;
            }

            var safeFeedback = Mathf.Clamp(feedback, 0f, 0.86f);
            var safeMix = Mathf.Clamp(mix, 0f, 0.45f);
            for (var i = delaySamples; i < samples.Length; i++)
            {
                var delayed = samples[i - delaySamples];
                samples[i] += delayed * safeFeedback * safeMix;
            }
        }

        private static void ApplyOnePoleLowPass(float[] samples, float cutoffHz)
        {
            if (samples == null || samples.Length == 0)
            {
                return;
            }

            var clampedCutoff = Mathf.Clamp(cutoffHz, 60f, SampleRate * 0.45f);
            var dt = 1f / SampleRate;
            var rc = 1f / (2f * Mathf.PI * clampedCutoff);
            var alpha = dt / (rc + dt);
            var y = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                y += alpha * (samples[i] - y);
                samples[i] = y;
            }
        }

        private static void ApplyOnePoleHighPass(float[] samples, float cutoffHz)
        {
            if (samples == null || samples.Length == 0)
            {
                return;
            }

            var clampedCutoff = Mathf.Clamp(cutoffHz, 20f, SampleRate * 0.45f);
            var dt = 1f / SampleRate;
            var rc = 1f / (2f * Mathf.PI * clampedCutoff);
            var alpha = rc / (rc + dt);
            var prevInput = samples[0];
            var prevOutput = 0f;
            for (var i = 1; i < samples.Length; i++)
            {
                var input = samples[i];
                var output = alpha * (prevOutput + input - prevInput);
                samples[i] = output;
                prevOutput = output;
                prevInput = input;
            }
        }

        private static void NormalizePeak(float[] samples, float targetPeak)
        {
            if (samples == null || samples.Length == 0)
            {
                return;
            }

            var peak = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                var magnitude = Mathf.Abs(samples[i]);
                if (magnitude > peak)
                {
                    peak = magnitude;
                }
            }

            if (peak <= 0.0001f)
            {
                return;
            }

            var scale = Mathf.Clamp(targetPeak, 0.2f, 1f) / peak;
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] *= scale;
            }
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
