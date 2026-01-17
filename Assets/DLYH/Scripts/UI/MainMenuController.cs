using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DLYH.TableUI
{
    /// <summary>
    /// Controls the main menu screen.
    /// Shows game title and navigation buttons.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SetupWizardController _setupWizard;

        private UIDocument _uiDocument;
        private VisualElement _root;

        // Buttons
        private Button _btnStartGame;
        private Button _btnHowToPlay;
        private Button _btnSettings;

        // Events
        public event Action OnStartGame;
        public event Action OnHowToPlay;
        public event Action OnSettings;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _root = _uiDocument.rootVisualElement;

            // Cache button references
            _btnStartGame = _root.Q<Button>("btn-start-game");
            _btnHowToPlay = _root.Q<Button>("btn-how-to-play");
            _btnSettings = _root.Q<Button>("btn-settings");

            // Set up button handlers
            if (_btnStartGame != null)
            {
                _btnStartGame.clicked += HandleStartGame;
            }
            if (_btnHowToPlay != null)
            {
                _btnHowToPlay.clicked += HandleHowToPlay;
            }
            if (_btnSettings != null)
            {
                _btnSettings.clicked += HandleSettings;
            }

            // Set version label
            Label versionLabel = _root.Q<Label>("version-label");
            if (versionLabel != null)
            {
                versionLabel.text = $"v{Application.version}";
            }

        }

        private void HandleStartGame()
        {
            OnStartGame?.Invoke();

            // If setup wizard is assigned, show it and hide menu
            if (_setupWizard != null)
            {
                Hide();
                _setupWizard.gameObject.SetActive(true);
            }
        }

        private void HandleHowToPlay()
        {
            OnHowToPlay?.Invoke();
        }

        private void HandleSettings()
        {
            OnSettings?.Invoke();
        }

        /// <summary>
        /// Shows the main menu.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            _root?.RemoveFromClassList("hidden");
        }

        /// <summary>
        /// Hides the main menu.
        /// </summary>
        public void Hide()
        {
            _root?.AddToClassList("hidden");
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows the main menu and resets setup wizard if returning from game.
        /// </summary>
        public void ReturnToMenu()
        {
            Show();
            if (_setupWizard != null)
            {
                _setupWizard.Reset();
                _setupWizard.gameObject.SetActive(false);
            }
        }
    }
}
