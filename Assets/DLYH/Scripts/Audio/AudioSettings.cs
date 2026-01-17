// AudioSettings.cs
// Static helper for audio volume settings
// Replaces functionality from deleted SettingsPanel class 
// Developer: TecVooDoo LLC

using UnityEngine;

namespace DLYH.UI
{
    /// <summary>
    /// Static helper class for audio volume settings.
    /// Provides centralized access to saved volume preferences.
    /// </summary>
    public static class SettingsPanel
    {
        private const string PREFS_SFX_VOLUME = "DLYH_SFXVolume";
        private const string PREFS_MUSIC_VOLUME = "DLYH_MusicVolume";
        private const float DEFAULT_VOLUME = 0.5f;

        /// <summary>
        /// Gets the saved SFX volume from PlayerPrefs.
        /// </summary>
        /// <returns>SFX volume between 0 and 1</returns>
        public static float GetSavedSFXVolume()
        {
            return PlayerPrefs.GetFloat(PREFS_SFX_VOLUME, DEFAULT_VOLUME);
        }

        /// <summary>
        /// Gets the saved music volume from PlayerPrefs.
        /// </summary>
        /// <returns>Music volume between 0 and 1</returns>
        public static float GetSavedMusicVolume()
        {
            return PlayerPrefs.GetFloat(PREFS_MUSIC_VOLUME, DEFAULT_VOLUME);
        }

        /// <summary>
        /// Sets and saves the SFX volume.
        /// </summary>
        /// <param name="volume">Volume between 0 and 1</param>
        public static void SetSFXVolume(float volume)
        {
            PlayerPrefs.SetFloat(PREFS_SFX_VOLUME, Mathf.Clamp01(volume));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Sets and saves the music volume.
        /// </summary>
        /// <param name="volume">Volume between 0 and 1</param>
        public static void SetMusicVolume(float volume)
        {
            PlayerPrefs.SetFloat(PREFS_MUSIC_VOLUME, Mathf.Clamp01(volume));
            PlayerPrefs.Save();
        }
    }
}
