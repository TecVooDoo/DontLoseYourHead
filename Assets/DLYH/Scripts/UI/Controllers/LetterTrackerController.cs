using System;
using System.Collections.Generic;
using UnityEngine;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Manages letter tracker/keyboard functionality.
    /// Plain class - receives container reference via constructor.
    /// </summary>
    public class LetterTrackerController : ILetterTrackerController
    {
        private readonly Transform _container;
        private readonly Dictionary<char, LetterButton> _letterButtons;
        private bool _isCached;

        public event Action<char> OnLetterClicked;
        public event Action<char> OnLetterHoverEnter;
        public event Action<char> OnLetterHoverExit;

        /// <summary>
        /// Creates a new LetterTrackerController.
        /// </summary>
        /// <param name="letterTrackerContainer">Transform containing LetterButton components</param>
        public LetterTrackerController(Transform letterTrackerContainer)
        {
            _container = letterTrackerContainer;
            _letterButtons = new Dictionary<char, LetterButton>();
        }

        /// <summary>
        /// Caches letter button references from the container.
        /// </summary>
        public void CacheLetterButtons()
        {
            _letterButtons.Clear();

            if (_container == null)
            {
                Debug.LogWarning("[LetterTrackerController] Container not assigned.");
                return;
            }

            var buttons = _container.GetComponentsInChildren<LetterButton>(true);

            foreach (var button in buttons)
            {
                button.EnsureInitialized();

                if (!button.IsInitialized || button.Letter == '\0')
                {
                    Debug.LogWarning(string.Format("[LetterTrackerController] LetterButton on {0} could not be initialized.", button.gameObject.name));
                    continue;
                }

                // Unsubscribe first to prevent duplicates
                button.OnLetterClicked -= HandleLetterClicked;
                button.OnLetterClicked += HandleLetterClicked;
                button.OnLetterHoverEnter -= HandleLetterHoverEnter;
                button.OnLetterHoverEnter += HandleLetterHoverEnter;
                button.OnLetterHoverExit -= HandleLetterHoverExit;
                button.OnLetterHoverExit += HandleLetterHoverExit;

                if (!_letterButtons.ContainsKey(button.Letter))
                {
                    _letterButtons[button.Letter] = button;
                }
                else
                {
                    Debug.LogWarning(string.Format("[LetterTrackerController] Duplicate letter button for '{0}' on {1}", button.Letter, button.gameObject.name));
                }
            }

            _isCached = true;
            Debug.Log(string.Format("[LetterTrackerController] Cached {0} letter buttons", _letterButtons.Count));
        }

        /// <summary>
        /// Gets a letter button by its letter.
        /// </summary>
        public LetterButton GetLetterButton(char letter)
        {
            char upperLetter = char.ToUpper(letter);
            if (_letterButtons.TryGetValue(upperLetter, out LetterButton button))
            {
                return button;
            }
            return null;
        }

        /// <summary>
        /// Gets the current state of a letter button.
        /// </summary>
        public LetterButton.LetterState GetLetterState(char letter)
        {
            var button = GetLetterButton(letter);
            if (button != null)
            {
                return button.CurrentState;
            }
            return LetterButton.LetterState.Normal;
        }

        /// <summary>
        /// Sets the state of a letter button.
        /// </summary>
        public void SetLetterState(char letter, LetterButton.LetterState state)
        {
            var button = GetLetterButton(letter);
            if (button != null)
            {
                button.SetState(state);
            }
        }

        /// <summary>
        /// Resets all letter buttons to normal state.
        /// </summary>
        public void ResetAllLetterButtons()
        {
            foreach (var button in _letterButtons.Values)
            {
                button.ResetState();
            }
        }

        /// <summary>
        /// Sets whether letter buttons are interactable.
        /// </summary>
        public void SetLetterButtonsInteractable(bool interactable)
        {
            foreach (var button in _letterButtons.Values)
            {
                button.IsInteractable = interactable;
            }
        }

        /// <summary>
        /// Checks if buttons have been cached.
        /// </summary>
        public bool IsCached => _isCached;

        /// <summary>
        /// Gets the number of cached letter buttons.
        /// </summary>
        public int ButtonCount => _letterButtons.Count;

        /// <summary>
        /// Disposes event subscriptions. Call when destroying the controller.
        /// </summary>
        public void Dispose()
        {
            foreach (var button in _letterButtons.Values)
            {
                if (button != null)
                {
                    button.OnLetterClicked -= HandleLetterClicked;
                    button.OnLetterHoverEnter -= HandleLetterHoverEnter;
                    button.OnLetterHoverExit -= HandleLetterHoverExit;
                }
            }
            _letterButtons.Clear();
        }

        private void HandleLetterClicked(char letter)
        {
            OnLetterClicked?.Invoke(letter);
        }

        private void HandleLetterHoverEnter(char letter)
        {
            OnLetterHoverEnter?.Invoke(letter);
        }

        private void HandleLetterHoverExit(char letter)
        {
            OnLetterHoverExit?.Invoke(letter);
        }
    }
}