// MainMenuController.cs
// Controls the Main Menu navigation and container visibility
// Created: December 13, 2025
// Updated: December 14, 2025 - Added Continue Game, Main Menu from Setup/Gameplay

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TecVooDoo.DontLoseYourHead.UI;

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
        [SerializeField] private Button _continueGameButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _exitButton;

        [Header("Settings Panel")]
        [SerializeField] private GameObject _settingsPanel;

        [Header("Setup Panel Reference")]
        [SerializeField] private SetupSettingsPanel _setupSettingsPanel;

        [Header("Gameplay Panel Reference")]
        [SerializeField] private GameplayUIController _gameplayUIController;

        // Track if a game is in progress (gameplay has started)
        private bool _gameInProgress = false;

        private void Start()
        {
            WireButtonEvents();
            WireSetupEvents();
            WireGameplayEvents();
            ShowMainMenu();
            UpdateContinueButtonVisibility();
        }

        private void WireButtonEvents()
        {
            if (_newGameButton != null)
            {
                _newGameButton.onClick.AddListener(OnNewGameClicked);
            }

            if (_continueGameButton != null)
            {
                _continueGameButton.onClick.AddListener(OnContinueGameClicked);
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

        private void WireSetupEvents()
        {
            if (_setupSettingsPanel != null)
            {
                _setupSettingsPanel.OnMainMenuRequested += OnMainMenuRequestedFromSetup;
            }
        }

        private void WireGameplayEvents()
        {
            if (_gameplayUIController != null)
            {
                _gameplayUIController.OnMainMenuRequested += OnMainMenuRequestedFromGameplay;
                _gameplayUIController.OnGameStarted += OnGameStarted;
                _gameplayUIController.OnGameEnded += OnGameEnded;
            }
        }

        private void OnDestroy()
        {
            if (_newGameButton != null)
            {
                _newGameButton.onClick.RemoveListener(OnNewGameClicked);
            }

            if (_continueGameButton != null)
            {
                _continueGameButton.onClick.RemoveListener(OnContinueGameClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            }

            if (_exitButton != null)
            {
                _exitButton.onClick.RemoveListener(OnExitClicked);
            }

            if (_setupSettingsPanel != null)
            {
                _setupSettingsPanel.OnMainMenuRequested -= OnMainMenuRequestedFromSetup;
            }

            if (_gameplayUIController != null)
            {
                _gameplayUIController.OnMainMenuRequested -= OnMainMenuRequestedFromGameplay;
                _gameplayUIController.OnGameStarted -= OnGameStarted;
                _gameplayUIController.OnGameEnded -= OnGameEnded;
            }
        }

        #region Button Handlers

        private void OnNewGameClicked()
        {
            Debug.Log("[MainMenuController] New Game clicked");
            _gameInProgress = false;
            StartNewGame();
        }

        private void OnContinueGameClicked()
        {
            Debug.Log("[MainMenuController] Continue Game clicked");
            ContinueGame();
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

        private void OnMainMenuRequestedFromSetup()
        {
            Debug.Log("[MainMenuController] Main Menu requested from Setup");
            ShowMainMenu();
        }

        private void OnMainMenuRequestedFromGameplay()
        {
            Debug.Log("[MainMenuController] Main Menu requested from Gameplay");
            ShowMainMenu();
        }

        private void OnGameStarted()
        {
            _gameInProgress = true;
            Debug.Log("[MainMenuController] Game started - Continue button will be available");
        }

        private void OnGameEnded()
        {
            _gameInProgress = false;
            Debug.Log("[MainMenuController] Game ended - Continue button will be hidden");
        }

        #endregion

        #region Navigation

        public void ShowMainMenu()
        {
            SetContainerVisibility(mainMenu: true, setup: false, gameplay: false);
            HideSettingsPanel();
            UpdateContinueButtonVisibility();
        }

        public void StartNewGame()
        {
            SetContainerVisibility(mainMenu: false, setup: true, gameplay: false);
            UpdateContinueButtonVisibility();

            // Setup panel initializes itself via SetupForPlayer() when activated
            // No explicit reset needed here - the panel handles its own state
        }

        public void ContinueGame()
        {
            if (_gameInProgress)
            {
                SetContainerVisibility(mainMenu: false, setup: false, gameplay: true);
            }
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

        private void UpdateContinueButtonVisibility()
        {
            if (_continueGameButton != null)
            {
                _continueGameButton.gameObject.SetActive(_gameInProgress);
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
