// MusicManager.cs
// Singleton manager for background music with shuffle playlist and crossfade
// Respects the Music volume setting from SettingsPanel
// Created: December 18, 2025

using System.Collections;
using UnityEngine;

namespace DLYH.Audio
{
    /// <summary>
    /// Manages background music playback with shuffle playlist and crossfade between tracks.
    /// Starts at Main Menu and plays continuously.
    /// Supports mute toggle for gameplay and tempo adjustment for tension.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        #region Singleton

        private static MusicManager _instance;

        /// <summary>
        /// Singleton instance for global access
        /// </summary>
        public static MusicManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<MusicManager>();

                    if (_instance == null)
                    {
                        Debug.LogWarning("[MusicManager] No instance found in scene");
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Serialized Fields

        [Header("Music Tracks")]
        [SerializeField, Tooltip("Array of music tracks to play")]
        private AudioClip[] _tracks;

        [Header("Crossfade Settings")]
        [SerializeField, Range(0.5f, 3f), Tooltip("Duration of crossfade between tracks")]
        private float _crossfadeDuration = 1.5f;

        [Header("Tempo Settings")]
        [SerializeField, Range(1f, 1.15f), Tooltip("Normal playback pitch")]
        private float _normalPitch = 1f;

        [SerializeField, Range(1.05f, 1.2f), Tooltip("Medium tension pitch (80% danger)")]
        private float _mediumTensionPitch = 1.08f;

        [SerializeField, Range(1.1f, 1.25f), Tooltip("High tension pitch (95% danger)")]
        private float _highTensionPitch = 1.12f;

        [Header("Audio Sources")]
        [SerializeField, Tooltip("Primary AudioSource (auto-created if null)")]
        private AudioSource _audioSourceA;

        [SerializeField, Tooltip("Secondary AudioSource for crossfade (auto-created if null)")]
        private AudioSource _audioSourceB;

        #endregion

        #region Private Fields

        private int[] _shuffledOrder;
        private int _currentIndex;
        private AudioSource _activeSource;
        private AudioSource _inactiveSource;

        private float _cachedMusicVolume = 0.5f;
        private float _volumeCacheTime;
        private const float VOLUME_CACHE_DURATION = 1f;

        private bool _isMuted;
        private float _volumeBeforeMute;

        private float _currentPitch = 1f;
        private float _targetPitch = 1f;
        private Coroutine _crossfadeCoroutine;
        private Coroutine _pitchLerpCoroutine;

        private bool _isPlaying;

        #endregion

        #region Properties

        /// <summary>
        /// Whether music is currently muted
        /// </summary>
        public bool IsMuted => _isMuted;

        /// <summary>
        /// Whether music is currently playing
        /// </summary>
        public bool IsPlaying => _isPlaying;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[MusicManager] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }

            _instance = this;

            EnsureAudioSources();
            InitializePlaylist();
        }

        private void Start()
        {
            // Start playing music at Main Menu
            PlayMusic();
        }

        private void Update()
        {
            if (!_isPlaying || _isMuted)
            {
                return;
            }

            // Check if current track finished (and no crossfade in progress)
            if (_activeSource != null && !_activeSource.isPlaying && _crossfadeCoroutine == null)
            {
                PlayNextTrack();
            }

            // Update volume from settings periodically
            UpdateVolumeFromSettings();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Initialization

        private void EnsureAudioSources()
        {
            if (_audioSourceA == null)
            {
                _audioSourceA = gameObject.AddComponent<AudioSource>();
            }

            if (_audioSourceB == null)
            {
                _audioSourceB = gameObject.AddComponent<AudioSource>();
            }

            // Configure both for music (2D, no spatial blend)
            ConfigureAudioSource(_audioSourceA);
            ConfigureAudioSource(_audioSourceB);

            _activeSource = _audioSourceA;
            _inactiveSource = _audioSourceB;
        }

        private void ConfigureAudioSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.loop = false;
            source.priority = 0; // Highest priority for music
        }

        private void InitializePlaylist()
        {
            if (_tracks == null || _tracks.Length == 0)
            {
                Debug.LogWarning("[MusicManager] No music tracks assigned");
                return;
            }

            // Create shuffled order array
            _shuffledOrder = new int[_tracks.Length];
            for (int i = 0; i < _tracks.Length; i++)
            {
                _shuffledOrder[i] = i;
            }

            ShufflePlaylist();
            _currentIndex = 0;

            Debug.Log(string.Format("[MusicManager] Initialized playlist with {0} tracks", _tracks.Length));
        }

        /// <summary>
        /// Fisher-Yates shuffle algorithm
        /// </summary>
        private void ShufflePlaylist()
        {
            for (int i = _shuffledOrder.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int temp = _shuffledOrder[i];
                _shuffledOrder[i] = _shuffledOrder[j];
                _shuffledOrder[j] = temp;
            }
        }

        #endregion

        #region Volume Management

        private void UpdateVolumeFromSettings()
        {
            // Refresh cache periodically
            if (Time.unscaledTime - _volumeCacheTime > VOLUME_CACHE_DURATION)
            {
                float newVolume = DLYH.UI.SettingsPanel.GetSavedMusicVolume();

                if (!Mathf.Approximately(newVolume, _cachedMusicVolume))
                {
                    _cachedMusicVolume = newVolume;
                    ApplyVolume();
                }

                _volumeCacheTime = Time.unscaledTime;
            }
        }

        private void ApplyVolume()
        {
            if (_isMuted)
            {
                return;
            }

            if (_activeSource != null)
            {
                _activeSource.volume = _cachedMusicVolume;
            }
        }

        /// <summary>
        /// Force refresh the volume cache (call when settings change)
        /// </summary>
        public void RefreshVolumeCache()
        {
            _cachedMusicVolume = DLYH.UI.SettingsPanel.GetSavedMusicVolume();
            _volumeCacheTime = Time.unscaledTime;
            ApplyVolume();
        }

        #endregion

        #region Playback Control

        /// <summary>
        /// Start playing music from the shuffled playlist
        /// </summary>
        public void PlayMusic()
        {
            if (_tracks == null || _tracks.Length == 0)
            {
                Debug.LogWarning("[MusicManager] Cannot play - no tracks assigned");
                return;
            }

            _isPlaying = true;
            _cachedMusicVolume = DLYH.UI.SettingsPanel.GetSavedMusicVolume();

            PlayCurrentTrack();

            Debug.Log("[MusicManager] Music started");
        }

        /// <summary>
        /// Stop all music playback
        /// </summary>
        public void StopMusic()
        {
            _isPlaying = false;

            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
                _crossfadeCoroutine = null;
            }

            if (_audioSourceA != null)
            {
                _audioSourceA.Stop();
            }

            if (_audioSourceB != null)
            {
                _audioSourceB.Stop();
            }

            Debug.Log("[MusicManager] Music stopped");
        }

        private void PlayCurrentTrack()
        {
            if (_shuffledOrder == null || _shuffledOrder.Length == 0)
            {
                return;
            }

            int trackIndex = _shuffledOrder[_currentIndex];
            AudioClip clip = _tracks[trackIndex];

            if (clip == null)
            {
                Debug.LogWarning(string.Format("[MusicManager] Track at index {0} is null", trackIndex));
                PlayNextTrack();
                return;
            }

            _activeSource.clip = clip;
            _activeSource.pitch = _currentPitch;
            _activeSource.volume = _isMuted ? 0f : _cachedMusicVolume;
            _activeSource.Play();

            Debug.Log(string.Format("[MusicManager] Now playing: {0}", clip.name));
        }

        /// <summary>
        /// Play the next track with crossfade
        /// </summary>
        public void PlayNextTrack()
        {
            if (_tracks == null || _tracks.Length == 0)
            {
                return;
            }

            // Remember current track to avoid immediate repeat
            int previousTrackIndex = _shuffledOrder[_currentIndex];

            // Advance index
            _currentIndex++;

            // If we've played all tracks, reshuffle
            if (_currentIndex >= _shuffledOrder.Length)
            {
                _currentIndex = 0;
                ShufflePlaylist();

                // If first track after shuffle is same as last played, swap it
                if (_shuffledOrder[0] == previousTrackIndex && _shuffledOrder.Length > 1)
                {
                    int swapIndex = Random.Range(1, _shuffledOrder.Length);
                    int temp = _shuffledOrder[0];
                    _shuffledOrder[0] = _shuffledOrder[swapIndex];
                    _shuffledOrder[swapIndex] = temp;
                }
            }

            // Start crossfade to next track
            int nextTrackIndex = _shuffledOrder[_currentIndex];
            AudioClip nextClip = _tracks[nextTrackIndex];

            if (nextClip != null)
            {
                StartCrossfade(nextClip);
            }
        }

        /// <summary>
        /// Play the previous track with crossfade
        /// </summary>
        public void PlayPreviousTrack()
        {
            if (_tracks == null || _tracks.Length == 0)
            {
                return;
            }

            _currentIndex--;

            if (_currentIndex < 0)
            {
                _currentIndex = _shuffledOrder.Length - 1;
            }

            int trackIndex = _shuffledOrder[_currentIndex];
            AudioClip clip = _tracks[trackIndex];

            if (clip != null)
            {
                StartCrossfade(clip);
            }
        }

        #endregion

        #region Crossfade

        private void StartCrossfade(AudioClip nextClip)
        {
            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
            }

            _crossfadeCoroutine = StartCoroutine(CrossfadeCoroutine(nextClip));
        }

        private IEnumerator CrossfadeCoroutine(AudioClip nextClip)
        {
            // Swap active/inactive sources
            AudioSource fadingOut = _activeSource;
            AudioSource fadingIn = _inactiveSource;

            // Setup the new track
            fadingIn.clip = nextClip;
            fadingIn.pitch = _currentPitch;
            fadingIn.volume = 0f;
            fadingIn.Play();

            Debug.Log(string.Format("[MusicManager] Crossfading to: {0}", nextClip.name));

            // Crossfade
            float elapsed = 0f;
            float startVolumeOut = fadingOut.volume;
            float targetVolume = _isMuted ? 0f : _cachedMusicVolume;

            while (elapsed < _crossfadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _crossfadeDuration;

                // Smooth crossfade curve
                float smoothT = t * t * (3f - 2f * t);

                fadingOut.volume = Mathf.Lerp(startVolumeOut, 0f, smoothT);
                fadingIn.volume = Mathf.Lerp(0f, targetVolume, smoothT);

                yield return null;
            }

            // Finalize
            fadingOut.Stop();
            fadingOut.volume = 0f;
            fadingIn.volume = targetVolume;

            // Swap references
            _activeSource = fadingIn;
            _inactiveSource = fadingOut;

            _crossfadeCoroutine = null;
        }

        #endregion

        #region Mute Control

        /// <summary>
        /// Toggle music mute state
        /// </summary>
        public void ToggleMute()
        {
            if (_isMuted)
            {
                Unmute();
            }
            else
            {
                Mute();
            }
        }

        /// <summary>
        /// Mute the music
        /// </summary>
        public void Mute()
        {
            if (_isMuted)
            {
                return;
            }

            _isMuted = true;
            _volumeBeforeMute = _activeSource != null ? _activeSource.volume : _cachedMusicVolume;

            if (_activeSource != null)
            {
                _activeSource.volume = 0f;
            }

            if (_inactiveSource != null)
            {
                _inactiveSource.volume = 0f;
            }

            Debug.Log("[MusicManager] Music muted");
        }

        /// <summary>
        /// Unmute the music
        /// </summary>
        public void Unmute()
        {
            if (!_isMuted)
            {
                return;
            }

            _isMuted = false;

            // Refresh volume from settings in case it changed while muted
            _cachedMusicVolume = DLYH.UI.SettingsPanel.GetSavedMusicVolume();

            if (_activeSource != null)
            {
                _activeSource.volume = _cachedMusicVolume;
            }

            Debug.Log("[MusicManager] Music unmuted");
        }

        #endregion

        #region Tempo/Pitch Control

        /// <summary>
        /// Set the tension level for tempo adjustment
        /// </summary>
        /// <param name="dangerPercentage">0.0 to 1.0 representing how close to miss limit</param>
        public void SetTensionLevel(float dangerPercentage)
        {
            float newPitch;

            if (dangerPercentage >= 0.95f)
            {
                newPitch = _highTensionPitch;
            }
            else if (dangerPercentage >= 0.80f)
            {
                newPitch = _mediumTensionPitch;
            }
            else
            {
                newPitch = _normalPitch;
            }

            if (!Mathf.Approximately(newPitch, _targetPitch))
            {
                _targetPitch = newPitch;
                StartPitchLerp();
            }
        }

        /// <summary>
        /// Reset tension to normal tempo
        /// </summary>
        public void ResetTension()
        {
            _targetPitch = _normalPitch;
            StartPitchLerp();
        }

        private void StartPitchLerp()
        {
            if (_pitchLerpCoroutine != null)
            {
                StopCoroutine(_pitchLerpCoroutine);
            }

            _pitchLerpCoroutine = StartCoroutine(PitchLerpCoroutine());
        }

        private IEnumerator PitchLerpCoroutine()
        {
            float startPitch = _currentPitch;
            float elapsed = 0f;
            float duration = 0.5f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float smoothT = t * t * (3f - 2f * t);

                _currentPitch = Mathf.Lerp(startPitch, _targetPitch, smoothT);

                if (_activeSource != null)
                {
                    _activeSource.pitch = _currentPitch;
                }

                yield return null;
            }

            _currentPitch = _targetPitch;

            if (_activeSource != null)
            {
                _activeSource.pitch = _currentPitch;
            }

            _pitchLerpCoroutine = null;
        }

        #endregion

        #region Static Convenience Methods

        /// <summary>
        /// Static shortcut to toggle music mute
        /// </summary>
        public static void ToggleMuteMusic()
        {
            if (Instance != null)
            {
                Instance.ToggleMute();
            }
        }

        /// <summary>
        /// Static shortcut to set tension level
        /// </summary>
        public static void SetTension(float dangerPercentage)
        {
            if (Instance != null)
            {
                Instance.SetTensionLevel(dangerPercentage);
            }
        }

        /// <summary>
        /// Static shortcut to reset tension
        /// </summary>
        public static void ResetMusicTension()
        {
            if (Instance != null)
            {
                Instance.ResetTension();
            }
        }

        /// <summary>
        /// Static check if music is muted
        /// </summary>
        public static bool IsMusicMuted()
        {
            return Instance != null && Instance.IsMuted;
        }

        #endregion
    }
}
