// SettingsPanel.cs
// Handles audio settings with SFX and Music volume sliders
// Persists settings to PlayerPrefs
// Created: December 13, 2025

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DLYH.UI
{
    public class SettingsPanel : MonoBehaviour
    {
        #region Constants

        private const string PREFS_SFX_VOLUME = "DLYH_SFXVolume";
        private const string PREFS_MUSIC_VOLUME = "DLYH_MusicVolume";
        private const float DEFAULT_VOLUME = 0.5f;

        #endregion

        #region Serialized Fields

        [Header("Volume Sliders")]
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;

        [Header("Volume Labels")]
        [SerializeField] private TMP_Text _sfxVolumeLabel;
        [SerializeField] private TMP_Text _musicVolumeLabel;

        [Header("Buttons")]
        [SerializeField] private Button _backButton;

        [Header("References")]
        [SerializeField] private MainMenuController _mainMenuController;

        #endregion

        #region Private Fields

        private float _sfxVolume = DEFAULT_VOLUME;
        private float _musicVolume = DEFAULT_VOLUME;

        #endregion

        #region Properties

        /// <summary>
        /// Current SFX volume (0.0 to 1.0)
        /// </summary>
        public float SFXVolume
        {
            get { return _sfxVolume; }
            private set
            {
                _sfxVolume = Mathf.Clamp01(value);
                UpdateSFXLabel();
                SaveSettings();
            }
        }

        /// <summary>
        /// Current Music volume (0.0 to 1.0)
        /// </summary>
        public float MusicVolume
        {
            get { return _musicVolume; }
            private set
            {
                _musicVolume = Mathf.Clamp01(value);
                UpdateMusicLabel();
                SaveSettings();
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            LoadSettings();
        }

        private void Start()
        {
            InitializeSliders();
            WireEvents();
        }

        private void OnDestroy()
        {
            UnwireEvents();
        }

        private void OnEnable()
        {
            // Refresh slider values when panel is shown
            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.value = _sfxVolume;
            }
            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.value = _musicVolume;
            }
            UpdateLabels();
        }

        #endregion

        #region Initialization

        private void InitializeSliders()
        {
            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.minValue = 0f;
                _sfxVolumeSlider.maxValue = 1f;
                _sfxVolumeSlider.value = _sfxVolume;
            }

            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.minValue = 0f;
                _musicVolumeSlider.maxValue = 1f;
                _musicVolumeSlider.value = _musicVolume;
            }

            UpdateLabels();
        }

        private void WireEvents()
        {
            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }

            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (_backButton != null)
            {
                _backButton.onClick.AddListener(OnBackClicked);
            }
        }

        private void UnwireEvents()
        {
            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
            }

            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            }

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(OnBackClicked);
            }
        }

        #endregion

        #region Event Handlers

        private void OnSFXVolumeChanged(float value)
        {
            SFXVolume = value;
            Debug.Log(string.Format("[SettingsPanel] SFX Volume changed to {0:P0}", value));
        }

        private void OnMusicVolumeChanged(float value)
        {
            MusicVolume = value;
            Debug.Log(string.Format("[SettingsPanel] Music Volume changed to {0:P0}", value));
        }

        private void OnBackClicked()
        {
            Debug.Log("[SettingsPanel] Back clicked");

            if (_mainMenuController != null)
            {
                _mainMenuController.HideSettingsPanel();
            }
            else
            {
                // Fallback: just hide this panel
                gameObject.SetActive(false);
            }
        }

        #endregion

        #region Label Updates

        private void UpdateLabels()
        {
            UpdateSFXLabel();
            UpdateMusicLabel();
        }

        private void UpdateSFXLabel()
        {
            if (_sfxVolumeLabel != null)
            {
                _sfxVolumeLabel.text = string.Format("{0:0}%", _sfxVolume * 100f);
            }
        }

        private void UpdateMusicLabel()
        {
            if (_musicVolumeLabel != null)
            {
                _musicVolumeLabel.text = string.Format("{0:0}%", _musicVolume * 100f);
            }
        }

        #endregion

        #region Persistence

        private void LoadSettings()
        {
            _sfxVolume = PlayerPrefs.GetFloat(PREFS_SFX_VOLUME, DEFAULT_VOLUME);
            _musicVolume = PlayerPrefs.GetFloat(PREFS_MUSIC_VOLUME, DEFAULT_VOLUME);

            Debug.Log(string.Format("[SettingsPanel] Loaded settings - SFX: {0:P0}, Music: {1:P0}",
                _sfxVolume, _musicVolume));
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat(PREFS_SFX_VOLUME, _sfxVolume);
            PlayerPrefs.SetFloat(PREFS_MUSIC_VOLUME, _musicVolume);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Reset audio settings to default values
        /// </summary>
        public void ResetToDefaults()
        {
            _sfxVolume = DEFAULT_VOLUME;
            _musicVolume = DEFAULT_VOLUME;

            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.value = _sfxVolume;
            }

            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.value = _musicVolume;
            }

            UpdateLabels();
            SaveSettings();

            Debug.Log("[SettingsPanel] Reset to default values (50%)");
        }

        #endregion

        #region Static Access

        /// <summary>
        /// Get the current SFX volume from PlayerPrefs without needing a panel instance
        /// </summary>
        public static float GetSavedSFXVolume()
        {
            return PlayerPrefs.GetFloat(PREFS_SFX_VOLUME, DEFAULT_VOLUME);
        }

        /// <summary>
        /// Get the current Music volume from PlayerPrefs without needing a panel instance
        /// </summary>
        public static float GetSavedMusicVolume()
        {
            return PlayerPrefs.GetFloat(PREFS_MUSIC_VOLUME, DEFAULT_VOLUME);
        }

        #endregion
    }
}
