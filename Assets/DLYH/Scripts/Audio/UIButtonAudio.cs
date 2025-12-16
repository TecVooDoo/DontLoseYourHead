// UIButtonAudio.cs
// Simple component to add click sound to any Button
// Attach this component to any GameObject with a Button to auto-play click sounds
// Created: December 15, 2025

using UnityEngine;
using UnityEngine.UI;

namespace DLYH.Audio
{
    /// <summary>
    /// Attach this component to any Button to automatically play click sounds.
    /// Uses the UIAudioManager's button click sound group.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonAudio : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField, Tooltip("Optional: Override the default button click sound group")]
        private SFXClipGroup _overrideClipGroup;

        #endregion

        #region Private Fields

        private Button _button;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(PlayClickSound);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(PlayClickSound);
            }
        }

        #endregion

        #region Sound Playback

        private void PlayClickSound()
        {
            if (UIAudioManager.Instance == null)
            {
                return;
            }

            if (_overrideClipGroup != null)
            {
                UIAudioManager.Instance.PlayFromGroup(_overrideClipGroup);
            }
            else
            {
                UIAudioManager.ButtonClick();
            }
        }

        #endregion
    }
}
