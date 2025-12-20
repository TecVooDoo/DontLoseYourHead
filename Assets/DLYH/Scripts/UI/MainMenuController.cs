// MainMenuController.cs
// Controls the Main Menu navigation and container visibility
// Created: December 13, 2025
// Updated: December 14, 2025 - Added Continue Game, Main Menu from Setup/Gameplay

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
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
        [SerializeField] private Button _feedbackButton;
        [SerializeField] private Button _exitButton;

        [Header("Settings Panel")]
        [SerializeField] private GameObject _settingsPanel;

        [Header("Setup Panel Reference")]
        [SerializeField] private SetupSettingsPanel _setupSettingsPanel;

        [Header("Gameplay Panel Reference")]
        [SerializeField] private GameplayUIController _gameplayUIController;

        [Header("Feedback Panel")]
        [SerializeField] private FeedbackPanel _feedbackPanel;

        [Header("Trivia Display")]
        [SerializeField] private TextMeshProUGUI _triviaText;
        [SerializeField] private float _triviaDisplayDuration = 5f;
        [SerializeField] private float _triviaFadeDuration = 0.5f;

        // Track if a game is in progress (gameplay has started)
        private bool _gameInProgress = false;
        private Coroutine _triviaCoroutine;

        // Guillotine and beheading trivia facts
        private static readonly string[] TriviaFacts = new string[]
        {
            "The guillotine was used in France until 1977.",
            "Dr. Joseph-Ignace Guillotin proposed the device as a humane execution method.",
            "During the Reign of Terror, over 16,000 were guillotined in France.",
            "The guillotine was nicknamed 'The National Razor' in France.",
            "Marie Antoinette was executed by guillotine on October 16, 1793.",
            "King Louis XVI was guillotined on January 21, 1793.",
            "The last public guillotine execution in France was in 1939.",
            "Executioners in France were often from families that held the job for generations.",
            "The guillotine blade falls at approximately 21 feet per second.",
            "Some believe a severed head remains conscious for several seconds.",
            "Anne Boleyn requested a skilled French swordsman for her execution.",
            "Sir Walter Raleigh asked to see the axe before his beheading.",
            "Charles I wore two shirts to his execution so he would not shiver from cold.",
            "Mary, Queen of Scots required three blows of the axe.",
            "The Tower of London saw only 7 beheadings - most were on Tower Hill.",
            "Robespierre, architect of the Terror, was himself guillotined in 1794.",
            "The Halifax Gibbet predated the French guillotine by centuries.",
            "Scotland's 'Maiden' guillotine was used from 1564 to 1708.",
            "Legend says the guillotine blade weighs about 88 pounds.",
            "Heads were sometimes held up to the crowd after execution.",
            "Some executioners became celebrities in revolutionary France.",
            "The guillotine was considered more egalitarian than other methods.",
            "Charlotte Corday was guillotined for assassinating Jean-Paul Marat.",
            "Lavoisier, the father of chemistry, was guillotined in 1794.",
            "The term 'guillotine' was not used until after Dr. Guillotin's proposal."
        };

        private int _currentTriviaIndex = -1;

        // Track last game result for feedback panel
        private bool _lastGamePlayerWon = false;

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

            if (_feedbackButton != null)
            {
                _feedbackButton.onClick.AddListener(OnFeedbackClicked);
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

            if (_feedbackPanel != null)
            {
                _feedbackPanel.OnFeedbackComplete += OnFeedbackComplete;
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

            if (_feedbackButton != null)
            {
                _feedbackButton.onClick.RemoveListener(OnFeedbackClicked);
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

            if (_feedbackPanel != null)
            {
                _feedbackPanel.OnFeedbackComplete -= OnFeedbackComplete;
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

        private void OnFeedbackClicked()
        {
            Debug.Log("[MainMenuController] Feedback clicked");
            ShowFeedbackFromMenu();
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

        private void OnGameEnded(bool playerWon)
        {
            _gameInProgress = false;
            _lastGamePlayerWon = playerWon;
            Debug.Log($"[MainMenuController] Game ended - Player {(playerWon ? "won" : "lost")}");

            // Show feedback panel after a short delay to let the win/lose popup show
            if (_feedbackPanel != null)
            {
                // Use Invoke to delay showing feedback panel
                Invoke(nameof(ShowFeedbackPanel), 2.5f);
            }
            else
            {
                // No feedback panel, go directly to main menu
                ShowMainMenu();
            }
        }

        private void ShowFeedbackPanel()
        {
            // Hide gameplay container
            if (_gameplayContainer != null)
            {
                _gameplayContainer.SetActive(false);
            }

            // Show MainMenuContainer (FeedbackPanel is a child of it)
            if (_mainMenuContainer != null)
            {
                _mainMenuContainer.SetActive(true);
            }

            // Hide the button container so only feedback panel shows
            Transform buttonContainer = _mainMenuContainer?.transform.Find("ButtonContainer");
            if (buttonContainer != null)
            {
                buttonContainer.gameObject.SetActive(false);
            }

            // Hide title text
            Transform titleText = _mainMenuContainer?.transform.Find("TitleText");
            if (titleText != null)
            {
                titleText.gameObject.SetActive(false);
            }

            // Show feedback panel
            if (_feedbackPanel != null)
            {
                _feedbackPanel.Show(_lastGamePlayerWon);
            }
        }

        private void OnFeedbackComplete()
        {
            Debug.Log("[MainMenuController] Feedback complete - returning to main menu");
            ShowMainMenu();
        }

        #endregion

        #region Navigation

        public void ShowMainMenu()
        {
            SetContainerVisibility(mainMenu: true, setup: false, gameplay: false);
            HideSettingsPanel();
            HideFeedbackPanel();

            // Restore button container and title (hidden when showing feedback after game)
            Transform buttonContainer = _mainMenuContainer?.transform.Find("ButtonContainer");
            if (buttonContainer != null)
            {
                buttonContainer.gameObject.SetActive(true);
            }

            Transform titleText = _mainMenuContainer?.transform.Find("TitleText");
            if (titleText != null)
            {
                titleText.gameObject.SetActive(true);
            }

            UpdateContinueButtonVisibility();
            StartTriviaRotation();
        }

        public void StartNewGame()
        {
            StopTriviaRotation();
            SetContainerVisibility(mainMenu: false, setup: true, gameplay: false);
            UpdateContinueButtonVisibility();

            // Setup panel initializes itself via SetupForPlayer() when activated
            // No explicit reset needed here - the panel handles its own state
        }

        public void ContinueGame()
        {
            if (_gameInProgress)
            {
                StopTriviaRotation();
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

        /// <summary>
        /// Show feedback panel from main menu (not after game end)
        /// </summary>
        public void ShowFeedbackFromMenu()
        {
            if (_feedbackPanel != null)
            {
                _feedbackPanel.ShowFromMenu();
            }
        }

        public void HideSettingsPanel()
        {
            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(false);
            }
        }

        public void HideFeedbackPanel()
        {
            if (_feedbackPanel != null)
            {
                _feedbackPanel.Hide();
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

        #region Trivia Display

        private void StartTriviaRotation()
        {
            if (_triviaText == null) return;

            StopTriviaRotation();
            _triviaCoroutine = StartCoroutine(TriviaRotationCoroutine());
        }

        private void StopTriviaRotation()
        {
            if (_triviaCoroutine != null)
            {
                StopCoroutine(_triviaCoroutine);
                _triviaCoroutine = null;
            }

            if (_triviaText != null)
            {
                _triviaText.gameObject.SetActive(false);
            }
        }

        private IEnumerator TriviaRotationCoroutine()
        {
            _triviaText.gameObject.SetActive(true);

            // Shuffle starting point
            _currentTriviaIndex = Random.Range(0, TriviaFacts.Length);

            while (true)
            {
                // Set new trivia text
                _triviaText.text = TriviaFacts[_currentTriviaIndex];

                // Fade in
                yield return FadeTrivia(0f, 1f);

                // Wait for display duration
                yield return new WaitForSeconds(_triviaDisplayDuration);

                // Fade out
                yield return FadeTrivia(1f, 0f);

                // Move to next trivia (wrap around)
                _currentTriviaIndex = (_currentTriviaIndex + 1) % TriviaFacts.Length;
            }
        }

        private IEnumerator FadeTrivia(float startAlpha, float endAlpha)
        {
            float elapsed = 0f;
            Color color = _triviaText.color;

            while (elapsed < _triviaFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _triviaFadeDuration;
                color.a = Mathf.Lerp(startAlpha, endAlpha, t);
                _triviaText.color = color;
                yield return null;
            }

            color.a = endAlpha;
            _triviaText.color = color;
        }

        #endregion
    }
}
