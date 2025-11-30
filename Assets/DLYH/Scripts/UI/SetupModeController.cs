using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using System;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Controller for Setup Mode UI interactions.
    /// Handles keyboard input for word entry and routes it to PlayerGridPanel.
    /// </summary>
    public class SetupModeController : MonoBehaviour
    {
        #region Serialized Fields
        [TitleGroup("References")]
        [SerializeField, Required]
        private PlayerGridPanel _playerGridPanel;

        [SerializeField]
        private AutocompleteDropdown _autocompleteDropdown;

        [TitleGroup("Configuration")]
        [SerializeField, Tooltip("Allow typing letters directly via keyboard")]
        private bool _enableKeyboardLetterInput = true;

        [SerializeField, Tooltip("Auto-select first word row on start")]
        private bool _autoSelectFirstRow = true;
        #endregion

        #region Private Fields
        private bool _isActive = true;
        #endregion

        #region Events
        /// <summary>
        /// Fired when setup is complete (all words placed)
        /// </summary>
        public event Action OnSetupComplete;

        /// <summary>
        /// Fired when a word is accepted
        /// </summary>
        public event Action<int, string> OnWordAccepted;

        /// <summary>
        /// Fired when a word is placed on grid
        /// </summary>
        public event Action<int, string> OnWordPlacedOnGrid;
        #endregion

        #region Properties
        /// <summary>
        /// Whether this controller is currently processing input
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            ValidateReferences();
            SubscribeToEvents();

            if (_autoSelectFirstRow && _playerGridPanel != null)
            {
                // Select first row after a frame to ensure everything is initialized
                StartCoroutine(SelectFirstRowDelayed());
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (!_isActive) return;
            if (_playerGridPanel == null) return;
            if (_playerGridPanel.CurrentMode != PlayerGridPanel.PanelMode.Setup) return;

            ProcessKeyboardInput();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Activates setup mode for this controller.
        /// </summary>
        public void Activate()
        {
            _isActive = true;
            if (_playerGridPanel != null)
            {
                _playerGridPanel.SetMode(PlayerGridPanel.PanelMode.Setup);
            }
        }

        /// <summary>
        /// Deactivates setup mode for this controller.
        /// </summary>
        public void Deactivate()
        {
            _isActive = false;
        }

        /// <summary>
        /// Selects a word row by index (0-based).
        /// </summary>
        public void SelectWordRow(int index)
        {
            if (_playerGridPanel != null)
            {
                _playerGridPanel.SelectWordRow(index);
            }
        }

        /// <summary>
        /// Checks if setup is complete and fires event if so.
        /// </summary>
        public bool CheckSetupComplete()
        {
            if (_playerGridPanel != null && _playerGridPanel.AreAllWordsPlaced())
            {
                OnSetupComplete?.Invoke();
                return true;
            }
            return false;
        }
        #endregion

        #region Private Methods - Initialization
        private void ValidateReferences()
        {
            if (_playerGridPanel == null)
            {
                Debug.LogError("[SetupModeController] PlayerGridPanel reference is not assigned!");
            }
        }

        private void SubscribeToEvents()
        {
            if (_playerGridPanel != null)
            {
                _playerGridPanel.OnWordPlaced += HandleWordPlaced;
            }

            if (_autocompleteDropdown != null)
            {
                _autocompleteDropdown.OnWordSelected += HandleAutocompleteWordSelected;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_playerGridPanel != null)
            {
                _playerGridPanel.OnWordPlaced -= HandleWordPlaced;
            }

            if (_autocompleteDropdown != null)
            {
                _autocompleteDropdown.OnWordSelected -= HandleAutocompleteWordSelected;
            }
        }

        private System.Collections.IEnumerator SelectFirstRowDelayed()
        {
            yield return null; // Wait one frame
            if (_playerGridPanel != null && _playerGridPanel.WordRowCount > 0)
            {
                _playerGridPanel.SelectWordRow(0);
                Debug.Log("[SetupModeController] Auto-selected first word row");
            }
        }
        #endregion

        #region Private Methods - Keyboard Input
private void ProcessKeyboardInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Check for Enter key - accept word
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                HandleEnterKey();
                return;
            }

            // Check for Backspace key - delete last letter or cancel placement
            if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                HandleBackspaceKey();
                return;
            }

            // Check for Delete key - clear placed word
            if (keyboard.deleteKey.wasPressedThisFrame)
            {
                HandleDeleteKey();
                return;
            }

            // Check for Escape key - cancel placement mode or deselect
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                HandleEscapeKey();
                return;
            }

            // Check for Tab key - move to next row
            if (keyboard.tabKey.wasPressedThisFrame)
            {
                HandleTabKey(keyboard.shiftKey.isPressed);
                return;
            }

            // Check for letter keys (A-Z) - only if enabled
            if (_enableKeyboardLetterInput)
            {
                ProcessLetterKeys(keyboard);
            }
        }

private void HandleEnterKey()
        {
            if (_playerGridPanel.IsInPlacementMode)
            {
                // In placement mode - could trigger random placement
                Debug.Log("[SetupModeController] Enter pressed in placement mode");
                return;
            }

            int selectedIndex = _playerGridPanel.SelectedWordRowIndex;
            if (selectedIndex < 0) return;

            var row = _playerGridPanel.GetWordPatternRow(selectedIndex);
            if (row == null) return;

            // If word is being entered, try to accept it
            if (row.CurrentState == WordPatternRow.RowState.Entering)
            {
                if (row.EnteredText.Length == row.RequiredWordLength)
                {
                    if (row.AcceptWord())
                    {
                        Debug.Log($"[SetupModeController] Word accepted: {row.CurrentWord}");
                        OnWordAccepted?.Invoke(selectedIndex, row.CurrentWord);
                        
                        // Automatically enter coordinate mode after accepting
                        _playerGridPanel.EnterPlacementMode(selectedIndex);
                        Debug.Log($"[SetupModeController] Entered coordinate placement mode for row {selectedIndex + 1}");
                    }
                }
                else
                {
                    Debug.Log($"[SetupModeController] Word not complete: {row.EnteredText.Length}/{row.RequiredWordLength}");
                }
            }
            // If word is already accepted, enter coordinate mode
            else if (row.CurrentState == WordPatternRow.RowState.WordEntered)
            {
                _playerGridPanel.EnterPlacementMode(selectedIndex);
                Debug.Log($"[SetupModeController] Entered coordinate placement mode for row {selectedIndex + 1}");
            }
        }

        private void HandleBackspaceKey()
        {
            if (_playerGridPanel.IsInPlacementMode)
            {
                // Cancel placement mode
                _playerGridPanel.CancelPlacementMode();
                Debug.Log("[SetupModeController] Cancelled placement mode");
                return;
            }

            bool removed = _playerGridPanel.RemoveLastLetterFromSelectedRow();
            if (removed)
            {
                Debug.Log("[SetupModeController] Removed last letter");
            }
        }

        private void HandleEscapeKey()
        {
            if (_playerGridPanel.IsInPlacementMode)
            {
                _playerGridPanel.CancelPlacementMode();
                Debug.Log("[SetupModeController] Cancelled placement mode via Escape");
            }
            else if (_playerGridPanel.SelectedWordRowIndex >= 0)
            {
                // Could deselect the current row, but for now just log
                Debug.Log("[SetupModeController] Escape pressed - no action");
            }
        }

private void HandleDeleteKey()
        {
            if (_playerGridPanel.IsInPlacementMode)
            {
                // Cancel placement mode first
                _playerGridPanel.CancelPlacementMode();
                Debug.Log("[SetupModeController] Cancelled placement mode via Delete");
                return;
            }

            int selectedIndex = _playerGridPanel.SelectedWordRowIndex;
            if (selectedIndex < 0) return;

            var row = _playerGridPanel.GetWordPatternRow(selectedIndex);
            if (row == null) return;

            // If the row is placed, clear it
            if (row.IsPlaced)
            {
                bool cleared = _playerGridPanel.ClearPlacedWord(selectedIndex);
                if (cleared)
                {
                    Debug.Log($"[SetupModeController] Cleared placed word from row {selectedIndex + 1}");
                }
            }
            // If the row has an entered word (not placed), clear it back to empty
            else if (row.CurrentState == WordPatternRow.RowState.WordEntered)
            {
                row.ResetToEmpty();
                Debug.Log($"[SetupModeController] Cleared accepted word from row {selectedIndex + 1}");
            }
            // If entering, clear all entered text
            else if (row.CurrentState == WordPatternRow.RowState.Entering)
            {
                while (row.EnteredText.Length > 0)
                {
                    row.RemoveLastLetter();
                }
                Debug.Log($"[SetupModeController] Cleared entered text from row {selectedIndex + 1}");
            }
        }


        private void HandleTabKey(bool shiftHeld)
        {
            if (_playerGridPanel.IsInPlacementMode) return;

            int currentIndex = _playerGridPanel.SelectedWordRowIndex;
            int rowCount = _playerGridPanel.WordRowCount;

            if (rowCount == 0) return;

            int newIndex;
            if (shiftHeld)
            {
                // Move to previous row
                newIndex = (currentIndex <= 0) ? rowCount - 1 : currentIndex - 1;
            }
            else
            {
                // Move to next row
                newIndex = (currentIndex >= rowCount - 1) ? 0 : currentIndex + 1;
            }

            // Skip rows that are already placed
            int attempts = 0;
            while (attempts < rowCount)
            {
                var row = _playerGridPanel.GetWordPatternRow(newIndex);
                if (row != null && !row.IsPlaced)
                {
                    break;
                }

                if (shiftHeld)
                {
                    newIndex = (newIndex <= 0) ? rowCount - 1 : newIndex - 1;
                }
                else
                {
                    newIndex = (newIndex >= rowCount - 1) ? 0 : newIndex + 1;
                }
                attempts++;
            }

            if (attempts < rowCount)
            {
                _playerGridPanel.SelectWordRow(newIndex);
                Debug.Log($"[SetupModeController] Tab -> Selected row {newIndex + 1}");
            }
        }

        private void ProcessLetterKeys(Keyboard keyboard)
        {
            // Check each letter key A-Z
            for (int i = 0; i < 26; i++)
            {
                char letter = (char)('A' + i);
                Key key = GetKeyForLetter(letter);

                if (keyboard[key].wasPressedThisFrame)
                {
                    HandleLetterKey(letter);
                    return; // Only process one letter per frame
                }
            }
        }

        private void HandleLetterKey(char letter)
        {
            if (_playerGridPanel.IsInPlacementMode) return;

            bool added = _playerGridPanel.AddLetterToSelectedRow(letter);
            if (added)
            {
                Debug.Log($"[SetupModeController] Added letter via keyboard: {letter}");
            }
        }

        private Key GetKeyForLetter(char letter)
        {
            // Map A-Z to Key enum
            switch (char.ToUpper(letter))
            {
                case 'A': return Key.A;
                case 'B': return Key.B;
                case 'C': return Key.C;
                case 'D': return Key.D;
                case 'E': return Key.E;
                case 'F': return Key.F;
                case 'G': return Key.G;
                case 'H': return Key.H;
                case 'I': return Key.I;
                case 'J': return Key.J;
                case 'K': return Key.K;
                case 'L': return Key.L;
                case 'M': return Key.M;
                case 'N': return Key.N;
                case 'O': return Key.O;
                case 'P': return Key.P;
                case 'Q': return Key.Q;
                case 'R': return Key.R;
                case 'S': return Key.S;
                case 'T': return Key.T;
                case 'U': return Key.U;
                case 'V': return Key.V;
                case 'W': return Key.W;
                case 'X': return Key.X;
                case 'Y': return Key.Y;
                case 'Z': return Key.Z;
                default: return Key.None;
            }
        }
        #endregion

        #region Private Methods - Event Handlers
        private void HandleWordPlaced(int rowIndex, string word, System.Collections.Generic.List<Vector2Int> positions)
        {
            Debug.Log($"[SetupModeController] Word '{word}' placed at row {rowIndex + 1}");
            OnWordPlacedOnGrid?.Invoke(rowIndex, word);

            // Check if setup is complete
            CheckSetupComplete();

            // Auto-select next unplaced row
            SelectNextUnplacedRow(rowIndex);
        }

        private void HandleAutocompleteWordSelected(string word)
        {
            int selectedIndex = _playerGridPanel.SelectedWordRowIndex;
            if (selectedIndex < 0) return;

            var row = _playerGridPanel.GetWordPatternRow(selectedIndex);
            if (row == null) return;

            // Set the word from autocomplete
            row.SetEnteredText(word);
            Debug.Log($"[SetupModeController] Autocomplete selected: {word}");
        }

        private void SelectNextUnplacedRow(int afterIndex)
        {
            int rowCount = _playerGridPanel.WordRowCount;

            // Start from the row after the one just placed
            for (int i = 1; i <= rowCount; i++)
            {
                int checkIndex = (afterIndex + i) % rowCount;
                var row = _playerGridPanel.GetWordPatternRow(checkIndex);

                if (row != null && !row.IsPlaced)
                {
                    _playerGridPanel.SelectWordRow(checkIndex);
                    Debug.Log($"[SetupModeController] Auto-selected next row: {checkIndex + 1}");
                    return;
                }
            }

            // All rows placed - deselect
            Debug.Log("[SetupModeController] All rows placed!");
        }
        #endregion

        #region Editor Helpers
#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Test Select Row 1")]
        private void TestSelectRow1()
        {
            SelectWordRow(0);
        }

        [Button("Test Select Row 2")]
        private void TestSelectRow2()
        {
            SelectWordRow(1);
        }

        [Button("Test Check Complete")]
        private void TestCheckComplete()
        {
            bool complete = CheckSetupComplete();
            Debug.Log($"[SetupModeController] Setup complete: {complete}");
        }

        [Button("Log Current State")]
        private void LogCurrentState()
        {
            if (_playerGridPanel == null)
            {
                Debug.Log("[SetupModeController] No PlayerGridPanel assigned");
                return;
            }

            Debug.Log($"[SetupModeController] Mode: {_playerGridPanel.CurrentMode}");
            Debug.Log($"[SetupModeController] Selected Row: {_playerGridPanel.SelectedWordRowIndex}");
            Debug.Log($"[SetupModeController] In Placement Mode: {_playerGridPanel.IsInPlacementMode}");
            Debug.Log($"[SetupModeController] All Words Placed: {_playerGridPanel.AreAllWordsPlaced()}");
        }
#endif
        #endregion
    }
}
