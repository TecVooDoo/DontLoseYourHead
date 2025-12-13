using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using TecVooDoo.DontLoseYourHead.Core;

namespace TecVooDoo.DontLoseYourHead.UI.Controllers
{
    /// <summary>
    /// Manages autocomplete dropdown behavior during Setup Mode.
    /// Listens to WordPatternRow text changes (from keyboard OR letter tracker)
    /// and coordinates with AutocompleteDropdown to show suggestions.
    /// </summary>
    public class AutocompleteManager : MonoBehaviour
    {
        #region Serialized Fields
        [TitleGroup("References")]
        [SerializeField, Required]
        private PlayerGridPanel _playerGridPanel;

        [SerializeField, Required]
        private AutocompleteDropdown _autocompleteDropdown;

        [SerializeField, Tooltip("Word lists by length (index 0 = 3-letter, 1 = 4-letter, etc)")]
        private List<WordListSO> _wordListsByLength = new List<WordListSO>();

        [TitleGroup("Positioning")]
        [SerializeField, Tooltip("Offset from row position for dropdown")]
        private Vector2 _dropdownOffset = new Vector2(-350f, -35f);

        [SerializeField]
        private RectTransform _dropdownRectTransform;
        #endregion

        #region Private Fields
        private int _activeRowIndex = -1;
        private WordPatternRow _activeRow;
        private bool _isInitialized;
        private Dictionary<WordPatternRow, bool> _subscribedRows = new Dictionary<WordPatternRow, bool>();
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            if (_isInitialized)
            {
                SubscribeToEvents();
            }
        }

private void OnDisable()
        {
            UnsubscribeFromEvents();

            // Always hide dropdown when this manager is disabled (e.g., switching to Gameplay mode)
            if (_autocompleteDropdown != null)
            {
                _autocompleteDropdown.Hide();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (_autocompleteDropdown == null) return;
            if (!_autocompleteDropdown.IsVisible) return;

            ProcessNavigationInput();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initializes the manager with references. Called automatically in Start.
        /// </summary>
public void Initialize()
        {
            if (_isInitialized) return;

            if (_playerGridPanel == null)
            {
                Debug.LogError("[AutocompleteManager] PlayerGridPanel reference is missing!");
                return;
            }

            if (_autocompleteDropdown == null)
            {
                Debug.LogError("[AutocompleteManager] AutocompleteDropdown reference is missing!");
                return;
            }

            if (_dropdownRectTransform == null)
            {
                _dropdownRectTransform = _autocompleteDropdown.GetComponent<RectTransform>();
            }

            // Subscribe to PlayerGridPanel events first
            _playerGridPanel.OnWordRowSelected += HandleRowSelected;
            _playerGridPanel.OnWordLengthsChanged += HandleWordLengthsChanged;

            // Subscribe to dropdown events
            if (_autocompleteDropdown != null)
            {
                _autocompleteDropdown.OnWordSelected += HandleWordSelected;
            }

            // Try to subscribe to existing rows (may be 0 at startup)
            SubscribeToAllRows();

            _isInitialized = true;

            int rowCount = _playerGridPanel.WordRowCount;
            Debug.Log(string.Format("[AutocompleteManager] Initialized. Found {0} word rows.", rowCount));
        }

/// <summary>
        /// Called when word lengths change (rows are reconfigured).
        /// Re-subscribes to all rows.
        /// </summary>
        private void HandleWordLengthsChanged()
        {
            Debug.Log("[AutocompleteManager] Word lengths changed, re-subscribing to rows...");
            UnsubscribeFromAllRows();
            SubscribeToAllRows();
        }


        /// <summary>
        /// Forces the dropdown to hide.
        /// </summary>
        public void HideDropdown()
        {
            if (_autocompleteDropdown != null)
            {
                _autocompleteDropdown.Hide();
            }
        }
        #endregion

        #region Private Methods - Event Management
private void SubscribeToEvents()
        {
            // This method is now only called from OnEnable for re-subscription
            // Initial subscription happens in Initialize()
            if (_playerGridPanel == null) return;
            if (!_isInitialized) return;

            SubscribeToAllRows();
        }

private void UnsubscribeFromEvents()
        {
            if (_playerGridPanel != null)
            {
                _playerGridPanel.OnWordRowSelected -= HandleRowSelected;
                _playerGridPanel.OnWordLengthsChanged -= HandleWordLengthsChanged;
            }

            UnsubscribeFromAllRows();

            if (_autocompleteDropdown != null)
            {
                _autocompleteDropdown.OnWordSelected -= HandleWordSelected;
            }
        }

private void SubscribeToAllRows()
        {
            if (_playerGridPanel == null) return;

            WordPatternRow[] rows = _playerGridPanel.GetWordPatternRows();
            Debug.Log(string.Format("[AutocompleteManager] SubscribeToAllRows: found {0} rows", rows.Length));

            foreach (WordPatternRow row in rows)
            {
                if (row != null && !_subscribedRows.ContainsKey(row))
                {
                    row.OnWordTextChanged += HandleWordTextChanged;
                    row.OnWordAccepted += HandleWordAccepted;
                    _subscribedRows[row] = true;
                    Debug.Log(string.Format("[AutocompleteManager] Subscribed to row: {0}", row.name));
                }
            }
        }

        private void UnsubscribeFromAllRows()
        {
            foreach (KeyValuePair<WordPatternRow, bool> kvp in _subscribedRows)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.OnWordTextChanged -= HandleWordTextChanged;
                    kvp.Key.OnWordAccepted -= HandleWordAccepted;
                }
            }
            _subscribedRows.Clear();
        }
        #endregion

        #region Private Methods - Event Handlers
private void HandleRowSelected(int rowIndex)
        {
            Debug.Log(string.Format("[AutocompleteManager] HandleRowSelected: index {0}", rowIndex));

            _activeRowIndex = rowIndex;
            _activeRow = _playerGridPanel.GetWordPatternRow(rowIndex);

            if (_activeRow != null)
            {
                SetWordListForLength(_activeRow.RequiredWordLength);
                PositionDropdownNearRow(_activeRow);

                string currentText = _activeRow.EnteredText;
                Debug.Log(string.Format("[AutocompleteManager] Row has text: '{0}', length requirement: {1}", currentText, _activeRow.RequiredWordLength));

                if (!string.IsNullOrEmpty(currentText))
                {
                    _autocompleteDropdown.UpdateFilter(currentText);
                }
                else
                {
                    _autocompleteDropdown.ClearFilter();
                }
            }
            else
            {
                Debug.LogWarning(string.Format("[AutocompleteManager] Could not get row at index {0}", rowIndex));
                _autocompleteDropdown.ClearFilter();
            }
        }

private void HandleWordTextChanged(int rowNumber, string currentText)
        {
            int rowIndex = rowNumber - 1;
            if (rowIndex != _activeRowIndex) return;

            if (_autocompleteDropdown == null) return;

            // Check if word is complete (reached required length)
            if (_activeRow != null && currentText.Length >= _activeRow.RequiredWordLength)
            {
                _autocompleteDropdown.Hide();
                return;
            }

            _autocompleteDropdown.UpdateFilter(currentText);
        }

private void HandleWordAccepted(int rowNumber, string word)
        {
            // Hide dropdown when any word is accepted
            if (_autocompleteDropdown != null)
            {
                _autocompleteDropdown.Hide();
            }

            // Clear active row since word is complete
            _activeRowIndex = -1;
            _activeRow = null;
        }

private void HandleWordSelected(string selectedWord)
        {
            if (_activeRow == null) return;

            _activeRow.SetEnteredText(selectedWord);

            // Explicitly hide dropdown after selection
            if (_autocompleteDropdown != null)
            {
                _autocompleteDropdown.Hide();
            }

            // Clear active state since word is now complete
            _activeRowIndex = -1;
            _activeRow = null;

            Debug.Log(string.Format("[AutocompleteManager] Selected word from dropdown: {0}", selectedWord));
        }
        #endregion

        #region Private Methods - Navigation
        private void ProcessNavigationInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.upArrowKey.wasPressedThisFrame)
            {
                _autocompleteDropdown.SelectPrevious();
            }
            else if (keyboard.downArrowKey.wasPressedThisFrame)
            {
                _autocompleteDropdown.SelectNext();
            }
            else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                if (_autocompleteDropdown.SelectedIndex >= 0)
                {
                    _autocompleteDropdown.ConfirmSelection();
                }
            }
            else if (keyboard.escapeKey.wasPressedThisFrame)
            {
                _autocompleteDropdown.Hide();
            }
        }
        #endregion

        #region Private Methods - Positioning
        private void PositionDropdownNearRow(WordPatternRow row)
        {
            if (_dropdownRectTransform == null) return;
            if (row == null) return;

            RectTransform rowRect = row.GetComponent<RectTransform>();
            if (rowRect == null) return;

            Vector3 rowWorldPos = rowRect.position;
            Vector3 dropdownPos = rowWorldPos + new Vector3(_dropdownOffset.x, _dropdownOffset.y, 0f);

            _dropdownRectTransform.position = dropdownPos;
        }

        private void SetWordListForLength(int wordLength)
        {
            if (_autocompleteDropdown == null) return;

            int listIndex = wordLength - 3;
            if (listIndex >= 0 && listIndex < _wordListsByLength.Count)
            {
                WordListSO wordList = _wordListsByLength[listIndex];
                if (wordList != null)
                {
                    _autocompleteDropdown.SetWordListFromSO(wordList);
                    _autocompleteDropdown.SetRequiredWordLength(wordLength);
                }
            }
        }
        #endregion

        #region Editor Helpers
#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Test Initialize")]
        private void TestInitialize()
        {
            Initialize();
        }

        [Button("Test Show Dropdown")]
        private void TestShowDropdown()
        {
            if (_autocompleteDropdown != null)
            {
                _autocompleteDropdown.Show();
            }
        }

        [Button("Test Hide Dropdown")]
        private void TestHideDropdown()
        {
            HideDropdown();
        }

        [Button("Log State")]
        private void LogState()
        {
            Debug.Log(string.Format("[AutocompleteManager] Active Row: {0}", _activeRowIndex));
            Debug.Log(string.Format("[AutocompleteManager] Dropdown Visible: {0}",
                _autocompleteDropdown != null ? _autocompleteDropdown.IsVisible.ToString() : "null"));
            Debug.Log(string.Format("[AutocompleteManager] Subscribed Rows: {0}", _subscribedRows.Count));
        }
#endif
        #endregion
    }
}
