// MainMenuController.cs
// Controls the Main Menu navigation and container visibility
// Created: December 13, 2025

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DLYH.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Containers")]
        [SerializeField] private GameObject _mainMenuContainer;
        [SerializeField] private GameObject _setupContainer;
        [SerializeField] private GameObject _gameplayContainer;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _exitButton;

        [Header("Settings Panel")]
        [SerializeField] private GameObject _settingsPanel;

        private void Start()
        {
            WireButtonEvents();
            ShowMainMenu();
        }

        private void WireButtonEvents()
        {
            if (_newGameButton != null)
            {
                _newGameButton.onClick.AddListener(OnNewGameClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (_exitButton != null)
            {
                _exitButton.onClick.AddListener(OnExitClicked);
            }
        }

        private void OnDestroy()
        {
            if (_newGameButton != null)
            {
                _newGameButton.onClick.RemoveListener(OnNewGameClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            }

            if (_exitButton != null)
            {
                _exitButton.onClick.RemoveListener(OnExitClicked);
            }
        }

        #region Button Handlers

        private void OnNewGameClicked()
        {
            Debug.Log("[MainMenuController] New Game clicked");
            StartNewGame();
        }

        private void OnSettingsClicked()
        {
            Debug.Log("[MainMenuController] Settings clicked");
            ShowSettingsPanel();
        }

        private void OnExitClicked()
        {
            Debug.Log("[MainMenuController] Exit clicked");
            ExitGame();
        }

        #endregion

        #region Navigation

        public void ShowMainMenu()
        {
            SetContainerVisibility(mainMenu: true, setup: false, gameplay: false);
            HideSettingsPanel();
        }

        public void StartNewGame()
        {
            SetContainerVisibility(mainMenu: false, setup: true, gameplay: false);

            // Setup panel initializes itself via SetupForPlayer() when activated
            // No explicit reset needed here - the panel handles its own state
        }

        public void ShowSettingsPanel()
        {
            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(true);
            }
        }

        public void HideSettingsPanel()
        {
            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(false);
            }
        }

        public void ReturnToMainMenu()
        {
            ShowMainMenu();
        }

        private void SetContainerVisibility(bool mainMenu, bool setup, bool gameplay)
        {
            if (_mainMenuContainer != null)
            {
                _mainMenuContainer.SetActive(mainMenu);
            }

            if (_setupContainer != null)
            {
                _setupContainer.SetActive(setup);
            }

            if (_gameplayContainer != null)
            {
                _gameplayContainer.SetActive(gameplay);
            }
        }

        #endregion

        #region Exit

        private void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion
    }
}