// UIAudioManager.cs
// Singleton manager for playing UI sound effects with randomization
// Respects the SFX volume setting from SettingsPanel
// Created: December 15, 2025

using UnityEngine;

namespace DLYH.Audio
{
    /// <summary>
    /// Manages UI sound effects playback with randomization support.
    /// Uses SFXClipGroup ScriptableObjects for varied sounds.
    /// Sounds play the same regardless of whether player or opponent triggers them.
    /// </summary>
    public class UIAudioManager : MonoBehaviour
    {
        #region Singleton

        private static UIAudioManager _instance;

        /// <summary>
        /// Singleton instance for global access
        /// </summary>
        public static UIAudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<UIAudioManager>();

                    if (_instance == null)
                    {
                        Debug.LogWarning("[UIAudioManager] No instance found in scene");
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Serialized Fields

        [Header("Sound Groups")]
        [SerializeField, Tooltip("Keyboard/letter button clicks")]
        private SFXClipGroup _keyboardClicks;

        [SerializeField, Tooltip("Grid cell clicks")]
        private SFXClipGroup _gridCellClicks;

        [SerializeField, Tooltip("General button clicks")]
        private SFXClipGroup _buttonClicks;

        [SerializeField, Tooltip("Popup open sounds")]
        private SFXClipGroup _popupOpen;

        [SerializeField, Tooltip("Popup close sounds")]
        private SFXClipGroup _popupClose;

        [SerializeField, Tooltip("Error/invalid action sounds")]
        private SFXClipGroup _errorSounds;

        [SerializeField, Tooltip("Success/positive feedback sounds")]
        private SFXClipGroup _successSounds;

        [Header("Audio Source")]
        [SerializeField, Tooltip("AudioSource component for playback (auto-created if null)")]
        private AudioSource _audioSource;

        #endregion

        #region Private Fields

        private float _cachedSFXVolume = 0.5f;
        private float _volumeCacheTime;
        private const float VOLUME_CACHE_DURATION = 1f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[UIAudioManager] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }

            _instance = this;

            // Ensure AudioSource exists
            EnsureAudioSource();
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

        private void EnsureAudioSource()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();

                if (_audioSource == null)
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            // Configure for UI sounds (2D, no spatial blend)
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
            _audioSource.loop = false;
        }

        #endregion

        #region Volume Management

        /// <summary>
        /// Get the current SFX volume (cached for performance)
        /// </summary>
        private float GetSFXVolume()
        {
            // Refresh cache periodically
            if (Time.unscaledTime - _volumeCacheTime > VOLUME_CACHE_DURATION)
            {
                _cachedSFXVolume = DLYH.UI.SettingsPanel.GetSavedSFXVolume();
                _volumeCacheTime = Time.unscaledTime;
            }

            return _cachedSFXVolume;
        }

        /// <summary>
        /// Force refresh the volume cache (call when settings change)
        /// </summary>
        public void RefreshVolumeCache()
        {
            _cachedSFXVolume = DLYH.UI.SettingsPanel.GetSavedSFXVolume();
            _volumeCacheTime = Time.unscaledTime;
        }

        #endregion

        #region Public Play Methods

        /// <summary>
        /// Play a keyboard/letter button click sound
        /// </summary>
        public void PlayKeyboardClick()
        {
            PlayFromGroup(_keyboardClicks);
        }

        /// <summary>
        /// Play a grid cell click sound
        /// </summary>
        public void PlayGridCellClick()
        {
            PlayFromGroup(_gridCellClicks);
        }

        /// <summary>
        /// Play a general button click sound
        /// </summary>
        public void PlayButtonClick()
        {
            PlayFromGroup(_buttonClicks);
        }

        /// <summary>
        /// Play a popup open sound
        /// </summary>
        public void PlayPopupOpen()
        {
            PlayFromGroup(_popupOpen);
        }

        /// <summary>
        /// Play a popup close sound
        /// </summary>
        public void PlayPopupClose()
        {
            PlayFromGroup(_popupClose);
        }

        /// <summary>
        /// Play an error/invalid action sound
        /// </summary>
        public void PlayError()
        {
            PlayFromGroup(_errorSounds);
        }

        /// <summary>
        /// Play a success/positive feedback sound
        /// </summary>
        public void PlaySuccess()
        {
            PlayFromGroup(_successSounds);
        }

        /// <summary>
        /// Play a sound from a specific clip group (for custom sounds)
        /// </summary>
        public void PlayFromGroup(SFXClipGroup group)
        {
            if (group == null)
            {
                return;
            }

            AudioClip clip = group.GetRandomClip();
            if (clip == null)
            {
                return;
            }

            float sfxVolume = GetSFXVolume();
            if (sfxVolume <= 0f)
            {
                return;
            }

            float volume = group.GetRandomVolume() * sfxVolume;
            float pitch = group.GetRandomPitch();

            PlayClip(clip, volume, pitch);
        }

        /// <summary>
        /// Play a single clip directly (for one-off sounds)
        /// </summary>
        public void PlayClip(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || _audioSource == null)
            {
                return;
            }

            float sfxVolume = GetSFXVolume();
            if (sfxVolume <= 0f)
            {
                return;
            }

            _audioSource.pitch = pitch;
            _audioSource.PlayOneShot(clip, volume * sfxVolume);
        }

        #endregion

        #region Static Convenience Methods

        /// <summary>
        /// Static shortcut to play keyboard click
        /// </summary>
        public static void KeyboardClick()
        {
            if (Instance != null)
            {
                Instance.PlayKeyboardClick();
            }
        }

        /// <summary>
        /// Static shortcut to play grid cell click
        /// </summary>
        public static void GridCellClick()
        {
            if (Instance != null)
            {
                Instance.PlayGridCellClick();
            }
        }

        /// <summary>
        /// Static shortcut to play button click
        /// </summary>
        public static void ButtonClick()
        {
            if (Instance != null)
            {
                Instance.PlayButtonClick();
            }
        }

        /// <summary>
        /// Static shortcut to play popup open
        /// </summary>
        public static void PopupOpen()
        {
            if (Instance != null)
            {
                Instance.PlayPopupOpen();
            }
        }

        /// <summary>
        /// Static shortcut to play popup close
        /// </summary>
        public static void PopupClose()
        {
            if (Instance != null)
            {
                Instance.PlayPopupClose();
            }
        }

        /// <summary>
        /// Static shortcut to play error sound
        /// </summary>
        public static void Error()
        {
            if (Instance != null)
            {
                Instance.PlayError();
            }
        }

        /// <summary>
        /// Static shortcut to play success sound
        /// </summary>
        public static void Success()
        {
            if (Instance != null)
            {
                Instance.PlaySuccess();
            }
        }

        #endregion
    }
}
