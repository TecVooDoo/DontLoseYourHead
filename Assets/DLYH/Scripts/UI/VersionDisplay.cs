// VersionDisplay.cs
// Displays the application version on the main menu
// Created: December 22, 2025

using UnityEngine;
using TMPro;

namespace DLYH.UI
{
    /// <summary>
    /// Displays the application version from Unity's Player Settings.
    /// Attach to a TextMeshProUGUI component on the main menu.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class VersionDisplay : MonoBehaviour
    {
        [SerializeField, Tooltip("Format string for version display. {0} = version number")]
        private string _format = "v{0}";

        private TextMeshProUGUI _text;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
            UpdateVersionText();
        }

        private void UpdateVersionText()
        {
            if (_text != null)
            {
                _text.text = string.Format(_format, Application.version);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Update in editor when format changes
            if (_text == null)
            {
                _text = GetComponent<TextMeshProUGUI>();
            }
            UpdateVersionText();
        }
#endif
    }
}
