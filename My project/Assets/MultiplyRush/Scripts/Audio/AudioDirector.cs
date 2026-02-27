using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        Battle = 4,
        ResultWin = 5,
        ResultLose = 6
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
        BattleStart = 11,
        WinIntro = 12
    }

    public sealed class AudioDirector : MonoBehaviour
    {
        private const int SampleRate = 44100;
        private const float MusicBaseVolume = 0.62f;
        private const float SfxBaseVolume = 0.88f;
        private const int GameplayMusicTrackCount = 10;
        private const int SfxSourcePoolSize = 56;
        private const int MaxConcurrentBattleHitVoices = 3;
        private const int MaxConcurrentGateVoices = 5;
        private const int MaxActiveSfxSoftLimit = 38;
        private const float ForegroundRecoveryDebounceSeconds = 0.2f;
        private static readonly int[] MajorScaleIntervals = { 0, 2, 4, 5, 7, 9, 11 };
        private static readonly int[] MinorScaleIntervals = { 0, 2, 3, 5, 7, 8, 10 };
        private static readonly string[] DefaultGameplayTrackNames =
        {
            "Hyper Neon",
            "Skyline Rush",
            "Steel Pulse",
            "Turbo Drift",
            "Glass Horizon",
            "Voltage Lane",
            "Crimson Rush",
            "Pulse Driver",
            "Night Voltage",
            "Afterburn Echo"
        };

        private static AudioDirector _instance;

        private readonly Dictionary<AudioMusicCue, AudioClip> _musicClips = new Dictionary<AudioMusicCue, AudioClip>(6);
        private readonly Dictionary<AudioSfxCue, AudioClip> _sfxClips = new Dictionary<AudioSfxCue, AudioClip>(13);
        private readonly Dictionary<AudioSfxCue, AudioClip[]> _sfxClipVariants = new Dictionary<AudioSfxCue, AudioClip[]>(13);
        private readonly AudioClip[] _gameplayTracks = new AudioClip[GameplayMusicTrackCount];
        private readonly string[] _gameplayTrackNames = new string[GameplayMusicTrackCount];
        private readonly Dictionary<AudioSfxCue, float> _sfxLastPlayedAt = new Dictionary<AudioSfxCue, float>(13);
        private int _loadedExternalTrackCount;

        private AudioSource _musicPrimary;
        private AudioSource _musicSecondary;
        private AudioSource[] _sfxSources;
        private AudioSfxCue[] _sfxSourceCueTags;
        private float[] _sfxSourceReleaseTimes;
        private int _sfxSourceCursor;
        private AudioSource _activeMusic;
        private AudioSource _incomingMusic;
        private AudioMusicCue _currentCue = AudioMusicCue.None;
        private bool _musicCueLockActive;
        private AudioMusicCue _musicCueLock = AudioMusicCue.None;
        private float _musicBlend;
        private float _musicBlendDuration = 0.45f;
        private bool _isMusicBlending;
        private int _selectedGameplayTrackIndex;
        private bool _hasQueuedCue;
        private AudioMusicCue _queuedCue = AudioMusicCue.None;
        private float _queuedCueTimer;
        private bool _hasGameplayPreview;
        private float _gameplayPreviewTimer;
        private AudioMusicCue _gameplayPreviewRestoreCue = AudioMusicCue.None;
        private float _lastForegroundRecoveryTime = -10f;

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

            UpdateQueuedCue(Time.unscaledDeltaTime);
            UpdateGameplayPreview(Time.unscaledDeltaTime);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                return;
            }

            TryRecoverMusicAfterForeground();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                return;
            }

            TryRecoverMusicAfterForeground();
        }

        public void SetMusicCue(AudioMusicCue cue, bool immediate = false)
        {
            if (_musicCueLockActive && cue != _musicCueLock && cue != AudioMusicCue.None)
            {
                return;
            }

            if (cue != AudioMusicCue.Gameplay)
            {
                _hasGameplayPreview = false;
            }

            if (cue == AudioMusicCue.None)
            {
                _hasQueuedCue = false;
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

            if (cue == _currentCue && !immediate)
            {
                var cueAlreadyPlaying = _activeMusic != null && _activeMusic.isPlaying && _activeMusic.clip == clip;
                if (cueAlreadyPlaying)
                {
                    return;
                }

                // Recover from any unexpected stop/drop by forcing this cue to restart.
                immediate = true;
            }

            _hasQueuedCue = false;
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

        public void SetMusicCueLock(AudioMusicCue cue, bool active, bool forceCueNow = true)
        {
            if (active)
            {
                _musicCueLockActive = true;
                _musicCueLock = cue;
                _hasGameplayPreview = false;
                _hasQueuedCue = false;
                if (forceCueNow && cue != AudioMusicCue.None)
                {
                    SetMusicCue(cue, true);
                }

                return;
            }

            if (!_musicCueLockActive)
            {
                return;
            }

            if (cue != AudioMusicCue.None && cue != _musicCueLock)
            {
                return;
            }

            _musicCueLockActive = false;
            _musicCueLock = AudioMusicCue.None;
        }

        public void PlaySfx(AudioSfxCue cue, float volumeScale = 1f, float pitch = 1f)
        {
            if (_sfxSources == null || _sfxSources.Length == 0)
            {
                return;
            }

            var now = Time.unscaledTime;
            if (!CanPlaySfxCue(cue, now))
            {
                return;
            }

            if (ShouldDropSfxForVoiceBudget(cue, now))
            {
                return;
            }

            AudioClip clip = null;
            if (_sfxClipVariants.TryGetValue(cue, out var variants) && variants != null && variants.Length > 0)
            {
                clip = variants.Length == 1
                    ? variants[0]
                    : variants[UnityEngine.Random.Range(0, variants.Length)];
            }

            if (clip == null && !_sfxClips.TryGetValue(cue, out clip))
            {
                return;
            }

            if (clip == null)
            {
                return;
            }

            if (!TryAcquireSfxSource(cue, now, out var source, out var sourceIndex))
            {
                return;
            }

            source.loop = false;
            source.pitch = ShapeSfxPitch(cue, pitch);
            source.priority = GetSfxPriority(cue);
            var shapedVolume = ShapeSfxVolume(cue, volumeScale) * SfxBaseVolume;
            source.PlayOneShot(clip, shapedVolume);
            TrackSfxVoice(cue, sourceIndex, now, clip, source.pitch);
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
            if (_gameplayTrackNames == null || _gameplayTrackNames.Length == 0)
            {
                return "Track";
            }

            var safeIndex = Mathf.Clamp(index, 0, _gameplayTrackNames.Length - 1);
            var label = _gameplayTrackNames[safeIndex];
            return string.IsNullOrWhiteSpace(label) ? "Track " + (safeIndex + 1) : label;
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
                // Force a refresh when staying on the gameplay cue so the newly selected track swaps immediately.
                SetMusicCue(AudioMusicCue.Gameplay, true);
            }
        }

        public void PreviewGameplayTrack(float durationSeconds, AudioMusicCue restoreCue)
        {
            var safeDuration = Mathf.Clamp(durationSeconds, 0.35f, 8f);
            _gameplayPreviewRestoreCue = restoreCue;
            _gameplayPreviewTimer = safeDuration;
            _hasGameplayPreview = true;
            SetMusicCue(AudioMusicCue.Gameplay, false);
        }

        public void StopGameplayPreview()
        {
            _hasGameplayPreview = false;
        }

        public void PlayResultSequence(bool didWin)
        {
            _hasGameplayPreview = false;
            _hasQueuedCue = false;
            if (didWin)
            {
                SetMusicCue(AudioMusicCue.None, true);
                PlaySfx(AudioSfxCue.WinIntro, 0.96f, 1f);
                QueueCue(AudioMusicCue.ResultWin, 2.95f);
                return;
            }

            PlaySfx(AudioSfxCue.Lose, 0.9f, 1f);
            SetMusicCue(AudioMusicCue.ResultLose, false);
        }

        private void BuildAudioGraph()
        {
            _musicPrimary = CreateChildSource("MusicA", true);
            _musicSecondary = CreateChildSource("MusicB", true);
            _sfxSources = new AudioSource[Mathf.Max(8, SfxSourcePoolSize)];
            _sfxSourceCueTags = new AudioSfxCue[_sfxSources.Length];
            _sfxSourceReleaseTimes = new float[_sfxSources.Length];
            for (var i = 0; i < _sfxSources.Length; i++)
            {
                _sfxSources[i] = CreateChildSource("Sfx_" + i, false);
                _sfxSources[i].priority = 170;
            }

            _activeMusic = _musicPrimary;
            _musicPrimary.volume = 0f;
            _musicSecondary.volume = 0f;
            for (var i = 0; i < _sfxSources.Length; i++)
            {
                if (_sfxSources[i] != null)
                {
                    _sfxSources[i].volume = 1f;
                }
            }
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
                _musicClips[AudioMusicCue.ResultWin] = BuildResultWinLoop();
                _musicClips[AudioMusicCue.ResultLose] = BuildResultLoseLoop();
            }

            for (var i = 0; i < GameplayMusicTrackCount; i++)
            {
                if (i < DefaultGameplayTrackNames.Length)
                {
                    _gameplayTrackNames[i] = DefaultGameplayTrackNames[i];
                }
                else
                {
                    _gameplayTrackNames[i] = "Track " + (i + 1);
                }
            }

            if (_gameplayTracks[0] == null)
            {
                _gameplayTracks[0] = BuildGameplayTrackA();
                _gameplayTracks[1] = BuildGameplayTrackB();
                _gameplayTracks[2] = BuildGameplayTrackC();
                _gameplayTracks[3] = BuildGameplayTrackD();
                _gameplayTracks[4] = BuildGameplayTrackE();
                _gameplayTracks[5] = BuildGameplayTrackF();
                _gameplayTracks[6] = BuildGameplayTrackG();
                _gameplayTracks[7] = BuildGameplayTrackH();
                _gameplayTracks[8] = BuildGameplayTrackI();
                _gameplayTracks[9] = BuildGameplayTrackJ();
            }

            LoadExternalGameplayTracks();

            if (_sfxClips.Count == 0)
            {
                var buttonTap = BuildButtonTapSfx();
                var buttonTapBright = BuildButtonTapSfxBright();
                var buttonTapSoft = BuildButtonTapSfxSoft();
                var playTransition = BuildPlayTransitionSfx();
                var gatePositive = BuildGatePositiveSfx();
                var gatePositiveSpark = BuildGatePositiveSfxSpark();
                var gateNegative = BuildGateNegativeSfx();
                var gateNegativeThud = BuildGateNegativeSfxThud();
                var pauseOpen = BuildPauseOpenSfx();
                var pauseOpenSoft = BuildPauseOpenSfxSoft();
                var pauseClose = BuildPauseCloseSfx();
                var pauseCloseSoft = BuildPauseCloseSfxSoft();
                var win = BuildVictoryStinger();
                var lose = BuildDefeatStinger();
                var reinforcement = BuildReinforcementSfx();
                var reinforcementHeroic = BuildReinforcementSfxHeroic();
                var shield = BuildShieldSfx();
                var shieldPulse = BuildShieldSfxPulse();
                var battleHit = BuildBattleHitSfx();
                var battleHitThump = BuildBattleHitSfxThump();
                var battleStart = BuildBattleStartSfx();
                var battleStartHeavy = BuildBattleStartSfxHeavy();
                var winIntro = BuildVictoryIntro();

                _sfxClips[AudioSfxCue.ButtonTap] = buttonTap;
                _sfxClips[AudioSfxCue.PlayTransition] = playTransition;
                _sfxClips[AudioSfxCue.GatePositive] = gatePositive;
                _sfxClips[AudioSfxCue.GateNegative] = gateNegative;
                _sfxClips[AudioSfxCue.PauseOpen] = pauseOpen;
                _sfxClips[AudioSfxCue.PauseClose] = pauseClose;
                _sfxClips[AudioSfxCue.Win] = win;
                _sfxClips[AudioSfxCue.Lose] = lose;
                _sfxClips[AudioSfxCue.Reinforcement] = reinforcement;
                _sfxClips[AudioSfxCue.Shield] = shield;
                _sfxClips[AudioSfxCue.BattleHit] = battleHit;
                _sfxClips[AudioSfxCue.BattleStart] = battleStart;
                _sfxClips[AudioSfxCue.WinIntro] = winIntro;

                _sfxClipVariants[AudioSfxCue.ButtonTap] = new[] { buttonTap, buttonTapBright, buttonTapSoft };
                _sfxClipVariants[AudioSfxCue.PlayTransition] = new[] { playTransition };
                _sfxClipVariants[AudioSfxCue.GatePositive] = new[] { gatePositive, gatePositiveSpark };
                _sfxClipVariants[AudioSfxCue.GateNegative] = new[] { gateNegative, gateNegativeThud };
                _sfxClipVariants[AudioSfxCue.PauseOpen] = new[] { pauseOpen, pauseOpenSoft };
                _sfxClipVariants[AudioSfxCue.PauseClose] = new[] { pauseClose, pauseCloseSoft };
                _sfxClipVariants[AudioSfxCue.Win] = new[] { win };
                _sfxClipVariants[AudioSfxCue.Lose] = new[] { lose };
                _sfxClipVariants[AudioSfxCue.Reinforcement] = new[] { reinforcement, reinforcementHeroic };
                _sfxClipVariants[AudioSfxCue.Shield] = new[] { shield, shieldPulse };
                _sfxClipVariants[AudioSfxCue.BattleHit] = new[] { battleHitThump, battleHit };
                _sfxClipVariants[AudioSfxCue.BattleStart] = new[] { battleStart, battleStartHeavy };
                _sfxClipVariants[AudioSfxCue.WinIntro] = new[] { winIntro };
            }
        }

        private static float ShapeSfxVolume(AudioSfxCue cue, float volumeScale)
        {
            var clamped = Mathf.Clamp01(volumeScale);
            float minimum;
            switch (cue)
            {
                case AudioSfxCue.ButtonTap:
                    minimum = 0.15f;
                    break;
                case AudioSfxCue.PlayTransition:
                case AudioSfxCue.PauseOpen:
                case AudioSfxCue.PauseClose:
                    minimum = 0.28f;
                    break;
                case AudioSfxCue.GatePositive:
                case AudioSfxCue.GateNegative:
                    minimum = 0.3f;
                    break;
                case AudioSfxCue.Reinforcement:
                case AudioSfxCue.Shield:
                    minimum = 0.34f;
                    break;
                case AudioSfxCue.BattleStart:
                    minimum = 0.35f;
                    break;
                case AudioSfxCue.BattleHit:
                    minimum = 0.14f;
                    break;
                default:
                    minimum = 0f;
                    break;
            }

            return Mathf.Max(minimum, clamped);
        }

        private static float ShapeSfxPitch(AudioSfxCue cue, float pitch)
        {
            var clamped = Mathf.Clamp(pitch, 0.8f, 1.2f);
            switch (cue)
            {
                case AudioSfxCue.ButtonTap:
                    return Mathf.Clamp(clamped, 0.92f, 1.08f);
                case AudioSfxCue.GateNegative:
                    return Mathf.Clamp(clamped, 0.84f, 1.04f);
                case AudioSfxCue.BattleHit:
                    return Mathf.Clamp(clamped, 0.9f, 1.02f);
                default:
                    return clamped;
            }
        }

        private bool CanPlaySfxCue(AudioSfxCue cue, float now)
        {
            var minInterval = GetSfxMinInterval(cue);
            if (minInterval <= 0f)
            {
                return true;
            }

            if (_sfxLastPlayedAt.TryGetValue(cue, out var lastPlayedAt))
            {
                if (now - lastPlayedAt < minInterval)
                {
                    return false;
                }
            }

            _sfxLastPlayedAt[cue] = now;
            return true;
        }

        private static float GetSfxMinInterval(AudioSfxCue cue)
        {
            switch (cue)
            {
                case AudioSfxCue.BattleHit:
                    return 0.15f;
                case AudioSfxCue.GatePositive:
                case AudioSfxCue.GateNegative:
                    return 0.075f;
                case AudioSfxCue.ButtonTap:
                    return 0.07f;
                default:
                    return 0f;
            }
        }

        private bool ShouldDropSfxForVoiceBudget(AudioSfxCue cue, float now)
        {
            if (_sfxSources == null || _sfxSources.Length == 0)
            {
                return true;
            }

            var activeVoices = 0;
            var battleHitVoices = 0;
            var gateVoices = 0;

            for (var i = 0; i < _sfxSources.Length; i++)
            {
                var source = _sfxSources[i];
                if (source == null)
                {
                    continue;
                }

                var active = source.isPlaying || now < _sfxSourceReleaseTimes[i];
                if (!active)
                {
                    continue;
                }

                activeVoices++;
                var activeCue = _sfxSourceCueTags[i];
                if (activeCue == AudioSfxCue.BattleHit)
                {
                    battleHitVoices++;
                }

                if (activeCue == AudioSfxCue.GatePositive || activeCue == AudioSfxCue.GateNegative)
                {
                    gateVoices++;
                }
            }

            if (cue == AudioSfxCue.BattleHit && battleHitVoices >= MaxConcurrentBattleHitVoices)
            {
                return true;
            }

            if ((cue == AudioSfxCue.GatePositive || cue == AudioSfxCue.GateNegative) && gateVoices >= MaxConcurrentGateVoices)
            {
                return true;
            }

            if (activeVoices >= MaxActiveSfxSoftLimit && IsLowPrioritySfxCue(cue))
            {
                return true;
            }

            return false;
        }

        private static bool IsLowPrioritySfxCue(AudioSfxCue cue)
        {
            switch (cue)
            {
                case AudioSfxCue.BattleHit:
                case AudioSfxCue.GatePositive:
                case AudioSfxCue.GateNegative:
                    return true;
                default:
                    return false;
            }
        }

        private static int GetSfxPriority(AudioSfxCue cue)
        {
            switch (cue)
            {
                case AudioSfxCue.BattleHit:
                    return 220;
                case AudioSfxCue.GatePositive:
                case AudioSfxCue.GateNegative:
                    return 190;
                case AudioSfxCue.ButtonTap:
                case AudioSfxCue.PauseOpen:
                case AudioSfxCue.PauseClose:
                    return 110;
                case AudioSfxCue.BattleStart:
                    return 120;
                default:
                    return 140;
            }
        }

        private void TrackSfxVoice(AudioSfxCue cue, int sourceIndex, float startedAt, AudioClip clip, float pitch)
        {
            if (_sfxSourceCueTags == null || _sfxSourceReleaseTimes == null)
            {
                return;
            }

            if (sourceIndex < 0 || sourceIndex >= _sfxSourceCueTags.Length)
            {
                return;
            }

            _sfxSourceCueTags[sourceIndex] = cue;
            var clipLength = clip != null ? clip.length : 0f;
            var safePitch = Mathf.Max(0.25f, Mathf.Abs(pitch));
            _sfxSourceReleaseTimes[sourceIndex] = startedAt + Mathf.Max(0.02f, clipLength / safePitch);
        }

        private bool TryAcquireSfxSource(AudioSfxCue requestedCue, float now, out AudioSource source, out int sourceIndex)
        {
            source = null;
            sourceIndex = -1;
            if (_sfxSources == null || _sfxSources.Length == 0)
            {
                return false;
            }

            var fallbackStealIndex = -1;
            var fallbackStealRemaining = float.MaxValue;
            var requestedPriority = GetSfxPriority(requestedCue);
            for (var i = 0; i < _sfxSources.Length; i++)
            {
                var index = (_sfxSourceCursor + i) % _sfxSources.Length;
                var sourceCandidate = _sfxSources[index];
                if (sourceCandidate == null)
                {
                    continue;
                }

                if (!sourceCandidate.isPlaying)
                {
                    _sfxSourceCursor = (index + 1) % _sfxSources.Length;
                    source = sourceCandidate;
                    sourceIndex = index;
                    return true;
                }

                var remaining = Mathf.Max(0f, _sfxSourceReleaseTimes[index] - now);
                var activeCue = _sfxSourceCueTags[index];
                var activePriority = GetSfxPriority(activeCue);
                if (!IsLowPrioritySfxCue(activeCue))
                {
                    continue;
                }

                if (activePriority < requestedPriority)
                {
                    continue;
                }

                if (remaining >= fallbackStealRemaining)
                {
                    continue;
                }

                fallbackStealRemaining = remaining;
                fallbackStealIndex = index;
            }

            if (fallbackStealIndex >= 0 && fallbackStealRemaining <= 0.06f)
            {
                var stealSource = _sfxSources[fallbackStealIndex];
                if (stealSource != null)
                {
                    stealSource.Stop();
                    _sfxSourceCursor = (fallbackStealIndex + 1) % _sfxSources.Length;
                    source = stealSource;
                    sourceIndex = fallbackStealIndex;
                    return true;
                }
            }

            return false;
        }

        private void QueueCue(AudioMusicCue cue, float delaySeconds)
        {
            _queuedCue = cue;
            _queuedCueTimer = Mathf.Max(0.01f, delaySeconds);
            _hasQueuedCue = true;
        }

        private void UpdateQueuedCue(float deltaTime)
        {
            if (!_hasQueuedCue || deltaTime <= 0f)
            {
                return;
            }

            _queuedCueTimer = Mathf.Max(0f, _queuedCueTimer - deltaTime);
            if (_queuedCueTimer > 0f)
            {
                return;
            }

            _hasQueuedCue = false;
            SetMusicCue(_queuedCue, false);
        }

        private void UpdateGameplayPreview(float deltaTime)
        {
            if (!_hasGameplayPreview || deltaTime <= 0f)
            {
                return;
            }

            _gameplayPreviewTimer = Mathf.Max(0f, _gameplayPreviewTimer - deltaTime);
            if (_gameplayPreviewTimer > 0f)
            {
                return;
            }

            _hasGameplayPreview = false;
            if (_gameplayPreviewRestoreCue != AudioMusicCue.Gameplay)
            {
                SetMusicCue(_gameplayPreviewRestoreCue, false);
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

        private void TryRecoverMusicAfterForeground()
        {
            var now = Time.realtimeSinceStartup;
            if (now <= _lastForegroundRecoveryTime + ForegroundRecoveryDebounceSeconds)
            {
                return;
            }

            _lastForegroundRecoveryTime = now;
            if (_currentCue == AudioMusicCue.None || _isMusicBlending)
            {
                return;
            }

            if (_activeMusic != null && _activeMusic.isPlaying)
            {
                return;
            }

            // iOS can occasionally resume without restarting the active AudioSource.
            SetMusicCue(_currentCue, true);
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

        private static AudioClip BuildGameplayTrackG()
        {
            return BuildModernLoop(
                clipName: "Music_Gameplay_07_CrimsonRush",
                bpm: 124f,
                bars: 8,
                chordRoots: new[] { 40, 45, 43, 47 },
                minorKey: true,
                energy: 0.84f,
                atmosphere: 0.36f,
                sparseLead: false);
        }

        private static AudioClip BuildGameplayTrackH()
        {
            return BuildModernLoop(
                clipName: "Music_Gameplay_08_PulseDriver",
                bpm: 128f,
                bars: 8,
                chordRoots: new[] { 52, 48, 55, 50 },
                minorKey: false,
                energy: 0.88f,
                atmosphere: 0.4f,
                sparseLead: false);
        }

        private static AudioClip BuildGameplayTrackI()
        {
            return BuildModernLoop(
                clipName: "Music_Gameplay_09_NightVoltage",
                bpm: 132f,
                bars: 8,
                chordRoots: new[] { 45, 50, 47, 43 },
                minorKey: true,
                energy: 0.9f,
                atmosphere: 0.34f,
                sparseLead: false);
        }

        private static AudioClip BuildGameplayTrackJ()
        {
            return BuildModernLoop(
                clipName: "Music_Gameplay_10_AfterburnEcho",
                bpm: 120f,
                bars: 8,
                chordRoots: new[] { 55, 52, 50, 57 },
                minorKey: false,
                energy: 0.74f,
                atmosphere: 0.58f,
                sparseLead: false);
        }

        private void LoadExternalGameplayTracks()
        {
            var loaded = Resources.LoadAll<AudioClip>("MultiplyRush/Music/Gameplay");
            if (loaded == null || loaded.Length == 0)
            {
                loaded = Resources.LoadAll<AudioClip>("Music/Gameplay");
            }

            if (loaded == null || loaded.Length == 0)
            {
                _loadedExternalTrackCount = 0;
                Debug.Log("Multiply Rush Audio: no external gameplay tracks found in Resources; using procedural fallback.");
                return;
            }

            var ordered = loaded
                .Where(clip => clip != null)
                .OrderBy(clip => clip.name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (ordered.Length == 0)
            {
                return;
            }

            var replaceCount = Mathf.Min(GameplayMusicTrackCount, ordered.Length);
            for (var i = 0; i < replaceCount; i++)
            {
                _gameplayTracks[i] = ordered[i];
                _gameplayTrackNames[i] = SanitizeTrackLabel(ordered[i].name, i);
            }

            _loadedExternalTrackCount = replaceCount;
            Debug.Log("Multiply Rush Audio: loaded " + _loadedExternalTrackCount + " external gameplay track(s).");
        }

        private static string SanitizeTrackLabel(string rawName, int fallbackIndex)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return "Track " + (fallbackIndex + 1);
            }

            var normalized = rawName.Replace("_", " ").Replace("-", " ");
            while (normalized.Contains("  "))
            {
                normalized = normalized.Replace("  ", " ");
            }

            var trimIndex = 0;
            while (trimIndex < normalized.Length && (char.IsDigit(normalized[trimIndex]) || normalized[trimIndex] == ' '))
            {
                trimIndex++;
            }

            if (trimIndex > 0 && trimIndex < normalized.Length)
            {
                normalized = normalized.Substring(trimIndex);
            }

            normalized = normalized.Trim();
            if (normalized.Length == 0)
            {
                return "Track " + (fallbackIndex + 1);
            }

            normalized = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized.ToLowerInvariant());
            return normalized;
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

        private static AudioClip BuildResultWinLoop()
        {
            return BuildModernLoop(
                clipName: "Music_Result_Win",
                bpm: 110f,
                bars: 4,
                chordRoots: new[] { 60, 64, 67, 69 },
                minorKey: false,
                energy: 0.58f,
                atmosphere: 0.74f,
                sparseLead: true);
        }

        private static AudioClip BuildResultLoseLoop()
        {
            return BuildModernLoop(
                clipName: "Music_Result_Lose",
                bpm: 78f,
                bars: 4,
                chordRoots: new[] { 45, 43, 40, 38 },
                minorKey: true,
                energy: 0.34f,
                atmosphere: 0.62f,
                sparseLead: true);
        }

        private static AudioClip BuildVictoryStinger()
        {
            const float duration = 1.12f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddTriadStab(data, 0f, 0.42f, new[] { 523.25f, 659.25f, 783.99f }, 0.26f, true);
            AddTriadStab(data, 0.24f, 0.38f, new[] { 659.25f, 783.99f, 1046.5f }, 0.24f, true);
            AddTriadStab(data, 0.52f, 0.56f, new[] { 783.99f, 987.77f, 1174.66f }, 0.22f, true);
            AddSweepLayer(data, 0.02f, 0.36f, 430f, 1100f, 0.12f, Waveform.Sine, 0.58f, 5.6f, 0.004f);
            AddNoiseBand(data, 0f, 0.24f, 0.04f, 0.08f, 0.22f);
            ApplyFeedbackDelay(data, Mathf.RoundToInt(0.18f * SampleRate), 0.22f, 0.14f);
            ApplyOnePoleLowPass(data, 9500f);
            NormalizePeak(data, 0.86f);
            SoftClip(data, 0.98f);
            var clip = AudioClip.Create("Sfx_Win", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildDefeatStinger()
        {
            const float duration = 0.92f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddTriadStab(data, 0f, 0.56f, new[] { 392f, 293.66f, 246.94f }, 0.24f, false);
            AddSweepLayer(data, 0f, 0.62f, 230f, 74f, 0.18f, Waveform.Triangle, 1.35f, 2.8f, 0.002f);
            AddNoiseBand(data, 0f, 0.32f, 0.07f, 0.16f, 0.18f);
            ApplyOnePoleLowPass(data, 4200f);
            ApplyOnePoleHighPass(data, 48f);
            NormalizePeak(data, 0.84f);
            SoftClip(data, 0.94f);
            var clip = AudioClip.Create("Sfx_Lose", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildVictoryIntro()
        {
            const float duration = 3.2f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            var beat = 60f / 126f;
            var introNotes = new[]
            {
                (0f, 0.26f, 523.25f, 0.36f),
                (beat * 0.5f, 0.26f, 659.25f, 0.34f),
                (beat, 0.3f, 783.99f, 0.35f),
                (beat * 1.6f, 0.34f, 1046.5f, 0.38f),
                (beat * 2.2f, 0.36f, 880f, 0.34f),
                (beat * 2.9f, 0.55f, 1046.5f, 0.36f)
            };

            for (var i = 0; i < introNotes.Length; i++)
            {
                var note = introNotes[i];
                var startSample = Mathf.RoundToInt(note.Item1 * SampleRate);
                var noteSamples = Mathf.RoundToInt(note.Item2 * SampleRate);
                AddTone(data, startSample, noteSamples, note.Item3, note.Item4, Waveform.Sine);
                AddTone(data, startSample, noteSamples, note.Item3 * 2f, note.Item4 * 0.26f, Waveform.Triangle);
            }

            AddChordAccent(data, 0.05f, 0.72f, new[] { 523.25f, 659.25f, 783.99f }, 0.13f);
            AddChordAccent(data, 1.35f, 0.9f, new[] { 659.25f, 783.99f, 1046.5f }, 0.15f);
            AddChordAccent(data, 2.45f, 1.2f, new[] { 783.99f, 987.77f, 1174.66f }, 0.16f);

            ApplyOnePoleLowPass(data, 12000f);
            NormalizePeak(data, 0.86f);
            SoftClip(data, 0.96f);
            var clip = AudioClip.Create("Sfx_WinIntro", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildButtonTapSfx()
        {
            const float duration = 0.15f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddNoiseBand(data, 0f, 0.012f, 0.03f, 0.06f, 0.16f);
            AddSweepLayer(data, 0f, 0.055f, 1120f, 520f, 0.1f, Waveform.Triangle, 0.62f, 4.2f, 0.0018f);
            AddSweepLayer(data, 0f, 0.082f, 430f, 205f, 0.08f, Waveform.Sine, 0.86f);
            AddSweepLayer(data, 0.01f, 0.1f, 300f, 180f, 0.06f, Waveform.Sine, 1.08f);
            ApplyOnePoleLowPass(data, 6200f);
            ApplyOnePoleHighPass(data, 110f);
            ApplyEdgeFade(data, 0.004f, 0.02f);
            NormalizePeak(data, 0.56f);
            SoftClip(data, 0.8f);
            var clip = AudioClip.Create("Sfx_Button", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildPlayTransitionSfx()
        {
            const float duration = 0.34f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddSweepLayer(data, 0f, 0.28f, 240f, 1260f, 0.2f, Waveform.Saw, 0.72f, 3.2f, 0.002f);
            AddSweepLayer(data, 0.04f, 0.24f, 420f, 1780f, 0.14f, Waveform.Triangle, 0.56f, 4.8f, 0.003f);
            AddNoiseBand(data, 0.01f, 0.18f, 0.06f, 0.11f, 0.24f);
            AddTriadStab(data, 0.19f, 0.16f, new[] { 659.25f, 783.99f, 1174.66f }, 0.12f, true);
            ApplyFeedbackDelay(data, Mathf.RoundToInt(0.12f * SampleRate), 0.25f, 0.14f);
            ApplyOnePoleLowPass(data, 10400f);
            ApplyOnePoleHighPass(data, 80f);
            NormalizePeak(data, 0.83f);
            SoftClip(data, 0.97f);
            var clip = AudioClip.Create("Sfx_Play", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildGatePositiveSfx()
        {
            const float duration = 0.22f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddTriadStab(data, 0f, 0.1f, new[] { 523.25f, 783.99f }, 0.15f, true);
            AddTriadStab(data, 0.06f, 0.14f, new[] { 659.25f, 987.77f, 1318.51f }, 0.16f, true);
            AddSweepLayer(data, 0f, 0.18f, 420f, 920f, 0.1f, Waveform.Triangle, 0.72f, 5f, 0.0025f);
            AddNoiseBand(data, 0f, 0.07f, 0.018f, 0.05f, 0.15f);
            ApplyOnePoleLowPass(data, 9200f);
            NormalizePeak(data, 0.8f);
            SoftClip(data, 0.94f);
            var clip = AudioClip.Create("Sfx_GateGood", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildGateNegativeSfx()
        {
            const float duration = 0.26f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddSweepLayer(data, 0f, 0.24f, 420f, 110f, 0.18f, Waveform.Saw, 1.12f, 2.6f, 0.002f);
            AddSweepLayer(data, 0f, 0.18f, 310f, 130f, 0.13f, Waveform.Triangle, 1.2f);
            AddNoiseBand(data, 0f, 0.12f, 0.06f, 0.12f, 0.26f);
            ApplyOnePoleLowPass(data, 4600f);
            ApplyOnePoleHighPass(data, 64f);
            NormalizePeak(data, 0.84f);
            SoftClip(data, 0.96f);
            var clip = AudioClip.Create("Sfx_GateBad", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildPauseOpenSfx()
        {
            const float duration = 0.22f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddSweepLayer(data, 0f, 0.18f, 420f, 190f, 0.14f, Waveform.Sine, 0.85f);
            AddSweepLayer(data, 0f, 0.12f, 820f, 420f, 0.09f, Waveform.Triangle, 1.08f);
            AddNoiseBand(data, 0f, 0.08f, 0.02f, 0.05f, 0.14f);
            ApplyOnePoleLowPass(data, 7400f);
            NormalizePeak(data, 0.74f);
            SoftClip(data, 0.9f);
            var clip = AudioClip.Create("Sfx_PauseOpen", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildPauseCloseSfx()
        {
            const float duration = 0.2f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddSweepLayer(data, 0f, 0.16f, 180f, 500f, 0.12f, Waveform.Sine, 0.78f);
            AddSweepLayer(data, 0f, 0.1f, 320f, 740f, 0.08f, Waveform.Triangle, 0.62f);
            AddNoiseBand(data, 0f, 0.08f, 0.015f, 0.04f, 0.15f);
            ApplyOnePoleLowPass(data, 7800f);
            NormalizePeak(data, 0.74f);
            SoftClip(data, 0.9f);
            var clip = AudioClip.Create("Sfx_PauseClose", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildReinforcementSfx()
        {
            const float duration = 0.34f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddTriadStab(data, 0f, 0.14f, new[] { 493.88f, 739.99f }, 0.12f, true);
            AddTriadStab(data, 0.09f, 0.18f, new[] { 587.33f, 880f, 1174.66f }, 0.16f, true);
            AddSweepLayer(data, 0f, 0.24f, 360f, 980f, 0.1f, Waveform.Saw, 0.64f, 4.2f, 0.0025f);
            AddNoiseBand(data, 0f, 0.09f, 0.02f, 0.06f, 0.12f);
            ApplyFeedbackDelay(data, Mathf.RoundToInt(0.095f * SampleRate), 0.2f, 0.12f);
            ApplyOnePoleLowPass(data, 9300f);
            NormalizePeak(data, 0.82f);
            SoftClip(data, 0.95f);
            var clip = AudioClip.Create("Sfx_Kit", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildShieldSfx()
        {
            const float duration = 0.3f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddSweepLayer(data, 0f, 0.24f, 250f, 980f, 0.12f, Waveform.Sine, 0.7f, 7.4f, 0.005f);
            AddSweepLayer(data, 0.02f, 0.2f, 520f, 1640f, 0.1f, Waveform.Triangle, 0.68f, 9.2f, 0.007f);
            AddTriadStab(data, 0.07f, 0.2f, new[] { 783.99f, 1046.5f, 1318.51f }, 0.08f, true);
            AddNoiseBand(data, 0f, 0.16f, 0.02f, 0.06f, 0.24f);
            ApplyFeedbackDelay(data, Mathf.RoundToInt(0.11f * SampleRate), 0.24f, 0.14f);
            ApplyOnePoleLowPass(data, 11200f);
            NormalizePeak(data, 0.82f);
            SoftClip(data, 0.95f);
            var clip = AudioClip.Create("Sfx_Shield", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildBattleHitSfx()
        {
            const float duration = 0.22f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddSweepLayer(data, 0f, 0.15f, 160f, 78f, 0.16f, Waveform.Sine, 1.05f);
            AddSweepLayer(data, 0f, 0.08f, 680f, 190f, 0.08f, Waveform.Triangle, 0.95f);
            AddNoiseBand(data, 0f, 0.13f, 0.03f, 0.07f, 0.22f);
            AddNoiseBand(data, 0.022f, 0.09f, 0.015f, 0.04f, 0.14f);
            ApplyOnePoleLowPass(data, 3600f);
            ApplyOnePoleHighPass(data, 52f);
            ApplyEdgeFade(data, 0.003f, 0.03f);
            NormalizePeak(data, 0.62f);
            SoftClip(data, 0.84f);
            var clip = AudioClip.Create("Sfx_BattleHit", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildBattleStartSfx()
        {
            const float duration = 0.44f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddSweepLayer(data, 0f, 0.34f, 140f, 620f, 0.16f, Waveform.Saw, 0.74f, 3.8f, 0.002f);
            AddSweepLayer(data, 0.04f, 0.28f, 320f, 1520f, 0.11f, Waveform.Triangle, 0.62f, 5.4f, 0.003f);
            AddTriadStab(data, 0.23f, 0.21f, new[] { 392f, 493.88f, 659.25f }, 0.14f, false);
            AddNoiseBand(data, 0f, 0.3f, 0.04f, 0.1f, 0.26f);
            AddNoiseBand(data, 0.25f, 0.16f, 0.04f, 0.09f, 0.22f);
            ApplyFeedbackDelay(data, Mathf.RoundToInt(0.12f * SampleRate), 0.18f, 0.12f);
            ApplyOnePoleLowPass(data, 8700f);
            ApplyOnePoleHighPass(data, 52f);
            NormalizePeak(data, 0.9f);
            SoftClip(data, 1.02f);
            var clip = AudioClip.Create("Sfx_BattleStart", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildButtonTapSfxBright()
        {
            const float duration = 0.14f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddNoiseBand(data, 0f, 0.012f, 0.04f, 0.08f, 0.22f);
            AddSweepLayer(data, 0f, 0.05f, 1360f, 640f, 0.1f, Waveform.Triangle, 0.58f, 5.2f, 0.0014f);
            AddSweepLayer(data, 0.005f, 0.084f, 660f, 300f, 0.07f, Waveform.Sine, 0.7f, 3.2f, 0.0012f);
            ApplyOnePoleLowPass(data, 7200f);
            ApplyOnePoleHighPass(data, 130f);
            ApplyEdgeFade(data, 0.004f, 0.02f);
            NormalizePeak(data, 0.54f);
            SoftClip(data, 0.8f);
            var clip = AudioClip.Create("Sfx_Button_Bright", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildButtonTapSfxSoft()
        {
            const float duration = 0.17f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddSweepLayer(data, 0f, 0.08f, 820f, 360f, 0.08f, Waveform.Sine, 0.76f, 2.8f, 0.0018f);
            AddSweepLayer(data, 0.01f, 0.115f, 320f, 190f, 0.07f, Waveform.Triangle, 1.02f, 1.2f, 0.0008f);
            AddNoiseBand(data, 0f, 0.018f, 0.02f, 0.03f, 0.12f);
            ApplyOnePoleLowPass(data, 5800f);
            ApplyOnePoleHighPass(data, 100f);
            ApplyEdgeFade(data, 0.005f, 0.024f);
            NormalizePeak(data, 0.5f);
            SoftClip(data, 0.78f);
            var clip = AudioClip.Create("Sfx_Button_Soft", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildGatePositiveSfxSpark()
        {
            const float duration = 0.25f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddTriadStab(data, 0f, 0.12f, new[] { 587.33f, 880f, 1174.66f }, 0.15f, true);
            AddTriadStab(data, 0.08f, 0.14f, new[] { 783.99f, 1174.66f, 1567.98f }, 0.13f, true);
            AddSweepLayer(data, 0f, 0.2f, 520f, 1460f, 0.1f, Waveform.Sine, 0.62f, 7.8f, 0.004f);
            AddNoiseBand(data, 0f, 0.09f, 0.02f, 0.05f, 0.2f);
            ApplyFeedbackDelay(data, Mathf.RoundToInt(0.065f * SampleRate), 0.16f, 0.1f);
            ApplyOnePoleLowPass(data, 11200f);
            NormalizePeak(data, 0.84f);
            SoftClip(data, 0.95f);
            var clip = AudioClip.Create("Sfx_GateGood_Spark", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildGatePositiveSfxPunch()
        {
            const float duration = 0.24f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddSweepLayer(data, 0f, 0.14f, 220f, 130f, 0.16f, Waveform.Sine, 1.18f);
            AddTriadStab(data, 0.015f, 0.12f, new[] { 493.88f, 739.99f, 987.77f }, 0.13f, true);
            AddTriadStab(data, 0.085f, 0.14f, new[] { 659.25f, 987.77f, 1318.51f }, 0.14f, true);
            AddNoiseBand(data, 0f, 0.06f, 0.028f, 0.06f, 0.16f);
            ApplyOnePoleLowPass(data, 9000f);
            ApplyOnePoleHighPass(data, 70f);
            NormalizePeak(data, 0.84f);
            SoftClip(data, 0.96f);
            var clip = AudioClip.Create("Sfx_GateGood_Punch", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildGateNegativeSfxCrunch()
        {
            const float duration = 0.28f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddSweepLayer(data, 0f, 0.24f, 520f, 90f, 0.2f, Waveform.Saw, 1.24f, 2.8f, 0.002f);
            AddSweepLayer(data, 0f, 0.15f, 980f, 220f, 0.12f, Waveform.Saw, 0.74f, 5.4f, 0.003f);
            AddNoiseBand(data, 0f, 0.18f, 0.08f, 0.2f, 0.28f);
            ApplyFeedbackDelay(data, Mathf.RoundToInt(0.052f * SampleRate), 0.22f, 0.12f);
            ApplyOnePoleLowPass(data, 4300f);
            ApplyOnePoleHighPass(data, 54f);
            NormalizePeak(data, 0.88f);
            SoftClip(data, 1.01f);
            var clip = AudioClip.Create("Sfx_GateBad_Crunch", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildGateNegativeSfxThud()
        {
            const float duration = 0.3f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddSweepLayer(data, 0f, 0.23f, 210f, 58f, 0.22f, Waveform.Sine, 1.35f);
            AddSweepLayer(data, 0.02f, 0.14f, 310f, 110f, 0.13f, Waveform.Triangle, 1.2f);
            AddNoiseBand(data, 0f, 0.15f, 0.04f, 0.1f, 0.2f);
            ApplyOnePoleLowPass(data, 3700f);
            ApplyOnePoleHighPass(data, 44f);
            NormalizePeak(data, 0.86f);
            SoftClip(data, 0.98f);
            var clip = AudioClip.Create("Sfx_GateBad_Thud", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildPauseOpenSfxSoft()
        {
            const float duration = 0.24f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddSweepLayer(data, 0f, 0.18f, 360f, 170f, 0.13f, Waveform.Sine, 0.86f);
            AddSweepLayer(data, 0.01f, 0.14f, 680f, 320f, 0.08f, Waveform.Triangle, 0.74f);
            AddTriadStab(data, 0.06f, 0.14f, new[] { 392f, 523.25f }, 0.07f, true);
            AddNoiseBand(data, 0f, 0.06f, 0.015f, 0.04f, 0.11f);
            ApplyOnePoleLowPass(data, 7800f);
            NormalizePeak(data, 0.72f);
            SoftClip(data, 0.89f);
            var clip = AudioClip.Create("Sfx_PauseOpen_Soft", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildPauseCloseSfxSoft()
        {
            const float duration = 0.22f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddSweepLayer(data, 0f, 0.17f, 170f, 560f, 0.11f, Waveform.Sine, 0.76f);
            AddSweepLayer(data, 0.02f, 0.1f, 320f, 860f, 0.07f, Waveform.Triangle, 0.66f);
            AddNoiseBand(data, 0f, 0.07f, 0.014f, 0.03f, 0.1f);
            ApplyOnePoleLowPass(data, 7600f);
            NormalizePeak(data, 0.72f);
            SoftClip(data, 0.89f);
            var clip = AudioClip.Create("Sfx_PauseClose_Soft", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildReinforcementSfxHeroic()
        {
            const float duration = 0.42f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddTriadStab(data, 0f, 0.12f, new[] { 440f, 659.25f }, 0.11f, true);
            AddTriadStab(data, 0.07f, 0.14f, new[] { 523.25f, 783.99f, 1046.5f }, 0.13f, true);
            AddTriadStab(data, 0.15f, 0.16f, new[] { 659.25f, 987.77f, 1318.51f }, 0.14f, true);
            AddSweepLayer(data, 0f, 0.3f, 320f, 1250f, 0.11f, Waveform.Saw, 0.62f, 4.4f, 0.0025f);
            AddNoiseBand(data, 0f, 0.1f, 0.02f, 0.06f, 0.12f);
            ApplyFeedbackDelay(data, Mathf.RoundToInt(0.084f * SampleRate), 0.24f, 0.14f);
            ApplyOnePoleLowPass(data, 9800f);
            NormalizePeak(data, 0.84f);
            SoftClip(data, 0.96f);
            var clip = AudioClip.Create("Sfx_Kit_Heroic", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildShieldSfxPulse()
        {
            const float duration = 0.34f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddSweepLayer(data, 0f, 0.28f, 280f, 1220f, 0.13f, Waveform.Sine, 0.74f, 8.2f, 0.006f);
            AddSweepLayer(data, 0.03f, 0.22f, 720f, 1960f, 0.1f, Waveform.Triangle, 0.76f, 10.4f, 0.008f);
            AddTriadStab(data, 0.09f, 0.19f, new[] { 880f, 1174.66f, 1567.98f }, 0.09f, true);
            AddNoiseBand(data, 0f, 0.14f, 0.02f, 0.06f, 0.22f);
            ApplyFeedbackDelay(data, Mathf.RoundToInt(0.1f * SampleRate), 0.28f, 0.16f);
            ApplyOnePoleLowPass(data, 11600f);
            NormalizePeak(data, 0.84f);
            SoftClip(data, 0.96f);
            var clip = AudioClip.Create("Sfx_Shield_Pulse", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildBattleHitSfxCrunch()
        {
            const float duration = 0.24f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddSweepLayer(data, 0f, 0.13f, 210f, 84f, 0.18f, Waveform.Sine, 1.16f);
            AddSweepLayer(data, 0f, 0.1f, 760f, 220f, 0.1f, Waveform.Triangle, 0.9f, 5.8f, 0.0018f);
            AddNoiseBand(data, 0f, 0.14f, 0.04f, 0.1f, 0.28f);
            ApplyFeedbackDelay(data, Mathf.RoundToInt(0.04f * SampleRate), 0.18f, 0.1f);
            ApplyOnePoleLowPass(data, 3400f);
            ApplyOnePoleHighPass(data, 50f);
            ApplyEdgeFade(data, 0.003f, 0.03f);
            NormalizePeak(data, 0.64f);
            SoftClip(data, 0.86f);
            var clip = AudioClip.Create("Sfx_BattleHit_Crunch", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildBattleHitSfxThump()
        {
            const float duration = 0.28f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddSweepLayer(data, 0f, 0.18f, 170f, 62f, 0.2f, Waveform.Sine, 1.32f);
            AddSweepLayer(data, 0.012f, 0.12f, 460f, 150f, 0.08f, Waveform.Triangle, 1.12f);
            AddNoiseBand(data, 0f, 0.1f, 0.03f, 0.08f, 0.2f);
            ApplyOnePoleLowPass(data, 3000f);
            ApplyOnePoleHighPass(data, 44f);
            ApplyEdgeFade(data, 0.003f, 0.04f);
            NormalizePeak(data, 0.64f);
            SoftClip(data, 0.86f);
            var clip = AudioClip.Create("Sfx_BattleHit_Thump", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildBattleStartSfxHeavy()
        {
            const float duration = 0.52f;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];
            AddSweepLayer(data, 0f, 0.38f, 120f, 620f, 0.2f, Waveform.Saw, 0.82f, 3.2f, 0.002f);
            AddSweepLayer(data, 0.06f, 0.28f, 360f, 1940f, 0.13f, Waveform.Triangle, 0.66f, 5.8f, 0.0035f);
            AddTriadStab(data, 0.24f, 0.23f, new[] { 349.23f, 440f, 587.33f }, 0.16f, false);
            AddNoiseBand(data, 0f, 0.34f, 0.05f, 0.14f, 0.3f);
            AddNoiseBand(data, 0.26f, 0.2f, 0.04f, 0.1f, 0.24f);
            ApplyFeedbackDelay(data, Mathf.RoundToInt(0.13f * SampleRate), 0.2f, 0.14f);
            ApplyOnePoleLowPass(data, 9000f);
            ApplyOnePoleHighPass(data, 46f);
            NormalizePeak(data, 0.92f);
            SoftClip(data, 1.03f);
            var clip = AudioClip.Create("Sfx_BattleStart_Heavy", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static void AddChordAccent(float[] data, float startSeconds, float durationSeconds, float[] frequencies, float amplitude)
        {
            if (data == null || frequencies == null || frequencies.Length == 0)
            {
                return;
            }

            var startSample = Mathf.RoundToInt(Mathf.Max(0f, startSeconds) * SampleRate);
            var lengthSamples = Mathf.RoundToInt(Mathf.Max(0.02f, durationSeconds) * SampleRate);
            for (var i = 0; i < frequencies.Length; i++)
            {
                AddTone(
                    data,
                    startSample,
                    lengthSamples,
                    Mathf.Max(40f, frequencies[i]),
                    amplitude * (0.8f - i * 0.12f),
                    i == 0 ? Waveform.Sine : Waveform.Triangle);
            }
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

        private static void ApplyEdgeFade(float[] samples, float fadeInSeconds, float fadeOutSeconds)
        {
            if (samples == null || samples.Length < 2)
            {
                return;
            }

            var fadeIn = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(0f, fadeInSeconds) * SampleRate), 0, samples.Length - 1);
            if (fadeIn > 0)
            {
                for (var i = 0; i < fadeIn; i++)
                {
                    var t = i / (float)Mathf.Max(1, fadeIn - 1);
                    var gain = Mathf.SmoothStep(0f, 1f, t);
                    samples[i] *= gain;
                }
            }

            var fadeOut = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(0f, fadeOutSeconds) * SampleRate), 0, samples.Length - 1);
            if (fadeOut > 0)
            {
                var start = samples.Length - fadeOut;
                for (var i = start; i < samples.Length; i++)
                {
                    var t = (i - start) / (float)Mathf.Max(1, fadeOut - 1);
                    var gain = Mathf.SmoothStep(1f, 0f, t);
                    samples[i] *= gain;
                }
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

        private static void AddSweepLayer(
            float[] data,
            float startSeconds,
            float durationSeconds,
            float startFrequency,
            float endFrequency,
            float amplitude,
            Waveform waveform,
            float curve,
            float vibratoHz = 0f,
            float vibratoDepth = 0f)
        {
            if (data == null || data.Length == 0 || durationSeconds <= 0f)
            {
                return;
            }

            var startSample = Mathf.Clamp(Mathf.RoundToInt(startSeconds * SampleRate), 0, data.Length - 1);
            var lengthSamples = Mathf.Clamp(Mathf.RoundToInt(durationSeconds * SampleRate), 1, data.Length - startSample);
            var safeCurve = Mathf.Clamp(curve, 0.2f, 2.8f);
            var safeVibratoHz = Mathf.Max(0f, vibratoHz);
            var safeVibratoDepth = Mathf.Clamp(vibratoDepth, 0f, 0.08f);
            var phase = 0f;

            for (var i = 0; i < lengthSamples; i++)
            {
                var index = startSample + i;
                if (index < 0 || index >= data.Length)
                {
                    continue;
                }

                var t = i / (float)Mathf.Max(1, lengthSamples - 1);
                var sweepT = Mathf.Pow(t, safeCurve);
                var freq = Mathf.Lerp(startFrequency, endFrequency, sweepT);
                var vibrato = safeVibratoHz > 0f
                    ? Mathf.Sin(2f * Mathf.PI * safeVibratoHz * t) * safeVibratoDepth
                    : 0f;
                phase += 2f * Mathf.PI * Mathf.Max(20f, freq * (1f + vibrato)) / SampleRate;
                var env = Mathf.Pow(Mathf.Sin(t * Mathf.PI), 0.68f) * Mathf.Pow(1f - t, 0.35f);
                data[index] += EvaluateWaveform(waveform, phase) * amplitude * env;
            }
        }

        private static void AddNoiseBand(
            float[] data,
            float startSeconds,
            float durationSeconds,
            float amplitudeStart,
            float amplitudeEnd,
            float lowpassAlpha)
        {
            if (data == null || data.Length == 0 || durationSeconds <= 0f)
            {
                return;
            }

            var startSample = Mathf.Clamp(Mathf.RoundToInt(startSeconds * SampleRate), 0, data.Length - 1);
            var lengthSamples = Mathf.Clamp(Mathf.RoundToInt(durationSeconds * SampleRate), 1, data.Length - startSample);
            var noiseState = (uint)(startSample + 9127);
            var lowpass = 0f;
            var alpha = Mathf.Clamp(lowpassAlpha, 0.01f, 0.88f);

            for (var i = 0; i < lengthSamples; i++)
            {
                var index = startSample + i;
                if (index < 0 || index >= data.Length)
                {
                    continue;
                }

                var t = i / (float)Mathf.Max(1, lengthSamples - 1);
                var amp = Mathf.Lerp(amplitudeStart, amplitudeEnd, t);
                var noise = NextNoise(ref noiseState);
                lowpass += (noise - lowpass) * alpha;
                var bright = noise - lowpass;
                var env = Mathf.Pow(1f - t, 2.1f);
                data[index] += bright * amp * env;
            }
        }

        private static void AddTriadStab(
            float[] data,
            float startSeconds,
            float durationSeconds,
            float[] frequencies,
            float amplitude,
            bool brighter)
        {
            if (data == null || frequencies == null || frequencies.Length == 0 || durationSeconds <= 0f)
            {
                return;
            }

            var startSample = Mathf.Clamp(Mathf.RoundToInt(startSeconds * SampleRate), 0, data.Length - 1);
            var lengthSamples = Mathf.Clamp(Mathf.RoundToInt(durationSeconds * SampleRate), 1, data.Length - startSample);
            for (var i = 0; i < frequencies.Length; i++)
            {
                var baseFreq = Mathf.Max(30f, frequencies[i]);
                var layerGain = amplitude * (0.9f - (i * 0.16f));
                var waveform = i == 0
                    ? Waveform.Triangle
                    : brighter ? Waveform.Saw : Waveform.Sine;
                AddSweepLayer(
                    data,
                    startSeconds,
                    durationSeconds,
                    baseFreq * (brighter ? 1.03f : 1.01f),
                    baseFreq * (brighter ? 0.995f : 0.985f),
                    layerGain,
                    waveform,
                    0.92f,
                    brighter ? 5.8f : 3.1f,
                    brighter ? 0.0035f : 0.0015f);

                if (brighter)
                {
                    AddSweepLayer(
                        data,
                        startSeconds,
                        durationSeconds * 0.86f,
                        baseFreq * 2f,
                        baseFreq * 1.94f,
                        layerGain * 0.24f,
                        Waveform.Sine,
                        1.1f);
                }
            }
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
