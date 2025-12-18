// GuillotineAudioManager.cs
// Manages guillotine-specific sound effects
// Created: December 18, 2025

using UnityEngine;

namespace DLYH.Audio
{
    /// <summary>
    /// Manages audio playback for guillotine animations.
    /// Plays blade raise sounds (rope stretch + move up) together,
    /// and chop sounds when blade falls.
    /// </summary>
    public class GuillotineAudioManager : MonoBehaviour
    {
        #region Singleton

        private static GuillotineAudioManager _instance;

        public static GuillotineAudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GuillotineAudioManager>();
                }
                return _instance;
            }
        }

        #endregion

        #region Serialized Fields

        [Header("Blade Raise Sounds")]
        [SerializeField, Tooltip("Rope stretching sound (plays with blade raise)")]
        private AudioClip _ropeStretch;

        [SerializeField, Tooltip("Blade moving up sound (plays with rope stretch)")]
        private AudioClip _bladeRaise;

        [Header("Blade Drop/Chop Sounds")]
        [SerializeField, Tooltip("Fast chop sound for sudden execution")]
        private AudioClip _chopFast;

        [SerializeField, Tooltip("Slow/dramatic chop sound")]
        private AudioClip _chopSlow;

        [SerializeField, Tooltip("Blade moving down sound")]
        private AudioClip _bladeDown;

        [Header("Head Sounds")]
        [SerializeField, Tooltip("Sound when head is separated/removed")]
        private AudioClip _headRemoved;

        [Header("Volume Settings")]
        [SerializeField, Range(0f, 1f), Tooltip("Base volume for guillotine sounds")]
        private float _baseVolume = 0.8f;

        [SerializeField, Range(0f, 1f), Tooltip("Volume for rope stretch (relative to base)")]
        private float _ropeStretchVolume = 0.7f;

        [SerializeField, Range(0f, 1f), Tooltip("Volume for blade movement (relative to base)")]
        private float _bladeMovementVolume = 0.8f;

        [SerializeField, Range(0f, 1f), Tooltip("Volume for chop sounds (relative to base)")]
        private float _chopVolume = 1f;

        [SerializeField, Range(0f, 1f), Tooltip("Volume for head removed sound (relative to base)")]
        private float _headRemovedVolume = 0.9f;

        [Header("Audio Source")]
        [SerializeField, Tooltip("Primary AudioSource for main sounds")]
        private AudioSource _primaryAudioSource;

        [SerializeField, Tooltip("Secondary AudioSource for layered sounds")]
        private AudioSource _secondaryAudioSource;

        #endregion

        #region Private Fields

        private float _cachedSFXVolume = 0.5f;
        private float _volumeCacheTime;
        private const float VOLUME_CACHE_DURATION = 1f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            EnsureAudioSources();
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
            // Get or create primary audio source
            if (_primaryAudioSource == null)
            {
                _primaryAudioSource = GetComponent<AudioSource>();
                if (_primaryAudioSource == null)
                {
                    _primaryAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            // Get or create secondary audio source for layered sounds
            if (_secondaryAudioSource == null)
            {
                AudioSource[] sources = GetComponents<AudioSource>();
                if (sources.Length > 1)
                {
                    _secondaryAudioSource = sources[1];
                }
                else
                {
                    _secondaryAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            // Configure both for 2D playback
            ConfigureAudioSource(_primaryAudioSource);
            ConfigureAudioSource(_secondaryAudioSource);
        }

        private void ConfigureAudioSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.loop = false;
        }

        #endregion

        #region Volume Management

        private float GetSFXVolume()
        {
            if (Time.unscaledTime - _volumeCacheTime > VOLUME_CACHE_DURATION)
            {
                _cachedSFXVolume = DLYH.UI.SettingsPanel.GetSavedSFXVolume();
                _volumeCacheTime = Time.unscaledTime;
            }
            return _cachedSFXVolume;
        }

        #endregion

        #region Public Play Methods

        /// <summary>
        /// Play blade raise sounds - rope stretch and blade movement together.
        /// Called when blade rises due to a miss.
        /// </summary>
        public void PlayBladeRaise()
        {
            float sfxVolume = GetSFXVolume();
            if (sfxVolume <= 0f) return;

            float volume = _baseVolume * sfxVolume;

            // Play rope stretch on primary
            if (_ropeStretch != null)
            {
                _primaryAudioSource.PlayOneShot(_ropeStretch, volume * _ropeStretchVolume);
            }

            // Play blade raise on secondary (layered)
            if (_bladeRaise != null)
            {
                _secondaryAudioSource.PlayOneShot(_bladeRaise, volume * _bladeMovementVolume);
            }
        }

        /// <summary>
        /// Play blade drop sound (without chop).
        /// Used for non-execution blade movements.
        /// </summary>
        public void PlayBladeDrop()
        {
            float sfxVolume = GetSFXVolume();
            if (sfxVolume <= 0f) return;

            if (_bladeDown != null)
            {
                float volume = _baseVolume * sfxVolume * _bladeMovementVolume;
                _primaryAudioSource.PlayOneShot(_bladeDown, volume);
            }
        }

        /// <summary>
        /// Play execution chop sound (fast version).
        /// Used when player loses by reaching miss limit.
        /// </summary>
        public void PlayChopFast()
        {
            float sfxVolume = GetSFXVolume();
            if (sfxVolume <= 0f) return;

            if (_chopFast != null)
            {
                float volume = _baseVolume * sfxVolume * _chopVolume;
                _primaryAudioSource.PlayOneShot(_chopFast, volume);
            }
        }

        /// <summary>
        /// Play execution chop sound (slow/dramatic version).
        /// Used when player loses by opponent finding all words.
        /// </summary>
        public void PlayChopSlow()
        {
            float sfxVolume = GetSFXVolume();
            if (sfxVolume <= 0f) return;

            if (_chopSlow != null)
            {
                float volume = _baseVolume * sfxVolume * _chopVolume;
                _primaryAudioSource.PlayOneShot(_chopSlow, volume);
            }
        }

        /// <summary>
        /// Play the full execution sequence - blade drop with chop.
        /// </summary>
        /// <param name="useFastChop">True for fast chop, false for slow dramatic chop</param>
        public void PlayExecution(bool useFastChop = true)
        {
            float sfxVolume = GetSFXVolume();
            if (sfxVolume <= 0f) return;

            float volume = _baseVolume * sfxVolume;

            // Play blade down on primary
            if (_bladeDown != null)
            {
                _primaryAudioSource.PlayOneShot(_bladeDown, volume * _bladeMovementVolume);
            }

            // Play chop on secondary
            AudioClip chopClip = useFastChop ? _chopFast : _chopSlow;
            if (chopClip != null)
            {
                _secondaryAudioSource.PlayOneShot(chopClip, volume * _chopVolume);
            }
        }

        /// <summary>
        /// Play head removed/separated sound.
        /// Called when the head falls into the basket.
        /// </summary>
        public void PlayHeadRemoved()
        {
            float sfxVolume = GetSFXVolume();
            if (sfxVolume <= 0f) return;

            if (_headRemoved != null)
            {
                float volume = _baseVolume * sfxVolume * _headRemovedVolume;
                _primaryAudioSource.PlayOneShot(_headRemoved, volume);
            }
        }

        #endregion

        #region Static Convenience Methods

        /// <summary>
        /// Static shortcut to play blade raise sounds
        /// </summary>
        public static void BladeRaise()
        {
            if (Instance != null)
            {
                Instance.PlayBladeRaise();
            }
        }

        /// <summary>
        /// Static shortcut to play execution with fast chop
        /// </summary>
        public static void ExecutionFast()
        {
            if (Instance != null)
            {
                Instance.PlayExecution(useFastChop: true);
            }
        }

        /// <summary>
        /// Static shortcut to play execution with slow/dramatic chop
        /// </summary>
        public static void ExecutionSlow()
        {
            if (Instance != null)
            {
                Instance.PlayExecution(useFastChop: false);
            }
        }

        /// <summary>
        /// Static shortcut to play head removed sound
        /// </summary>
        public static void HeadRemoved()
        {
            if (Instance != null)
            {
                Instance.PlayHeadRemoved();
            }
        }

        #endregion
    }
}
