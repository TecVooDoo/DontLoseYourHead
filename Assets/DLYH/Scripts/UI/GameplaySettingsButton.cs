// GameplaySettingsButton.cs
// Handles the Settings button click during gameplay
// Shows settings panel with contextual back behavior (returns to game, not main menu)
// Created: December 22, 2025

using UnityEngine;
using UnityEngine.UI;

namespace DLYH.UI
{
    /// <summary>
    /// Attach to the Settings button in the gameplay ButtonBarStrip.
    /// Opens the SettingsPanel with gameplay context so Back returns to the game.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class GameplaySettingsButton : MonoBehaviour
    {
        [SerializeField] private SettingsPanel _settingsPanel;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void Start()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(OnSettingsClicked);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnSettingsClicked);
            }
        }

        private void OnSettingsClicked()
        {
            if (_settingsPanel != null)
            {
                _settingsPanel.ShowFromGameplay();
            }
            else
            {
                Debug.LogWarning("[GameplaySettingsButton] SettingsPanel reference not set!");
            }
        }
    }
}
