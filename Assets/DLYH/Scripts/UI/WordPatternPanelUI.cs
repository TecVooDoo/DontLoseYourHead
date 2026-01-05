using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Manages a panel containing multiple WordPatternRowUI instances.
    /// Handles row creation, configuration, and event forwarding.
    /// </summary>
    public class WordPatternPanelUI : MonoBehaviour
    {
        // ============================================================
        // CONSTANTS
        // ============================================================

        public const int MAX_ROWS = 5;
        public const int MIN_WORD_LENGTH = 3;
        public const int MAX_WORD_LENGTH = 8;

        // ============================================================
        // SERIALIZED FIELDS
        // ============================================================

        [Header("Panel Configuration")]
        [SerializeField] private int _activeRowCount = 3;
        [SerializeField] private WordRowMode _mode = WordRowMode.Setup;
        [SerializeField] private int _defaultWordLength = 3;

        [Header("Layout")]
        [SerializeField] private Transform _rowContainer;
        [SerializeField] private WordPatternRowUI _rowPrefab;
        [SerializeField] private float _rowSpacing = 5f;
        [SerializeField] private float _rowHeight = 50f;
        [SerializeField] private float _rowWidth = 600f;

        [Header("Panel Background")]
        [SerializeField] private Image _panelBackground;

        [Header("Player Configuration")]
        [SerializeField] private Color _playerColor = Color.cyan;

        // ============================================================
        // EVENTS
        // ============================================================

        /// <summary>Fired when a letter cell is clicked. Params: rowIndex, letterIndex</summary>
        public event Action<int, int> OnLetterCellClicked;

        /// <summary>Fired when select button clicked. Param: rowIndex</summary>
        public event Action<int> OnSelectClicked;

        /// <summary>Fired when place button clicked. Param: rowIndex</summary>
        public event Action<int> OnPlaceClicked;

        /// <summary>Fired when delete button clicked. Param: rowIndex</summary>
        public event Action<int> OnDeleteClicked;

        /// <summary>Fired when guess word button clicked. Param: rowIndex</summary>
        public event Action<int> OnGuessWordClicked;

        // ============================================================
        // PRIVATE FIELDS
        // ============================================================

        private List<WordPatternRowUI> _rows = new List<WordPatternRowUI>();
        private int[] _wordLengths = new int[MAX_ROWS];
        private bool _isInitialized;

        // ============================================================
        // PROPERTIES
        // ============================================================

        /// <summary>Number of currently active (visible) rows.</summary>
        public int ActiveRowCount => _activeRowCount;

        /// <summary>Current mode (Setup or Gameplay).</summary>
        public WordRowMode Mode => _mode;

        /// <summary>Gets a row by index (0-based).</summary>
        public WordPatternRowUI GetRow(int index)
        {
            if (index >= 0 && index < _rows.Count)
            {
                return _rows[index];
            }
            return null;
        }

        /// <summary>Gets all rows.</summary>
        public IReadOnlyList<WordPatternRowUI> Rows => _rows;

        // ============================================================
        // UNITY LIFECYCLE
        // ============================================================

        private void Start()
        {
            // Use Start instead of Awake and delay one frame to ensure layout is calculated
            StartCoroutine(DelayedInitialize());
        }

        private System.Collections.IEnumerator DelayedInitialize()
        {
            // Wait for end of frame to ensure RectTransform layout has been calculated
            yield return new WaitForEndOfFrame();
            Initialize();
        }

        private void OnDestroy()
        {
            UnsubscribeFromRows();
        }

        // ============================================================
        // INITIALIZATION
        // ============================================================

        /// <summary>
        /// Initializes the panel and creates rows if needed.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            if (_rowContainer == null)
            {
                Debug.LogError("[WordPatternPanelUI] Row container not assigned!");
                return;
            }

            // Set up Vertical Layout Group if not present
            VerticalLayoutGroup layoutGroup = _rowContainer.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = _rowContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            layoutGroup.spacing = _rowSpacing;
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = false;

            // Create rows if we have a prefab and container is empty
            if (_rowPrefab != null && _rows.Count == 0)
            {
                CreateRows();
            }
            else
            {
                // Collect existing rows from container
                CollectExistingRows();
            }

            // Configure rows
            ConfigureAllRows();

            _isInitialized = true;
        }

        /// <summary>
        /// Creates all row instances from prefab.
        /// </summary>
        private void CreateRows()
        {
            // Use container width if available, otherwise fall back to _rowWidth
            float actualRowWidth = _rowWidth;
            RectTransform containerRect = _rowContainer as RectTransform;
            if (containerRect != null && containerRect.rect.width > 0)
            {
                actualRowWidth = containerRect.rect.width;
            }

            for (int i = 0; i < MAX_ROWS; i++)
            {
                WordPatternRowUI row = Instantiate(_rowPrefab, _rowContainer);
                row.gameObject.name = $"WordPatternRow_{i + 1}";

                // Set up RectTransform size
                RectTransform rowRect = row.GetComponent<RectTransform>();
                if (rowRect != null)
                {
                    rowRect.sizeDelta = new Vector2(actualRowWidth, _rowHeight);
                }

                // Add LayoutElement to ensure proper sizing in Vertical Layout Group
                LayoutElement layoutElement = row.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = row.gameObject.AddComponent<LayoutElement>();
                }
                layoutElement.preferredHeight = _rowHeight;
                layoutElement.preferredWidth = actualRowWidth;
                layoutElement.minHeight = _rowHeight;

                _rows.Add(row);
            }
        }

        /// <summary>
        /// Collects existing row instances from container.
        /// </summary>
        private void CollectExistingRows()
        {
            _rows.Clear();
            for (int i = 0; i < _rowContainer.childCount && i < MAX_ROWS; i++)
            {
                WordPatternRowUI row = _rowContainer.GetChild(i).GetComponent<WordPatternRowUI>();
                if (row != null)
                {
                    _rows.Add(row);
                }
            }
        }

        /// <summary>
        /// Configures all rows with proper indices, modes, and event subscriptions.
        /// </summary>
        private void ConfigureAllRows()
        {
            UnsubscribeFromRows();

            // Initialize word lengths array with defaults
            for (int i = 0; i < MAX_ROWS; i++)
            {
                if (_wordLengths[i] < MIN_WORD_LENGTH)
                {
                    _wordLengths[i] = _defaultWordLength;
                }
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                WordPatternRowUI row = _rows[i];
                if (row == null) continue;

                // Initialize the row with index, word length, and mode
                row.Initialize(i, _wordLengths[i], _mode);

                // Set visibility based on active row count
                bool isActive = i < _activeRowCount;
                row.gameObject.SetActive(isActive);

                // Subscribe to events
                SubscribeToRow(row);
            }
        }

        // ============================================================
        // PUBLIC METHODS
        // ============================================================

        /// <summary>
        /// Sets the number of active (visible) rows.
        /// </summary>
        public void SetActiveRowCount(int count)
        {
            _activeRowCount = Mathf.Clamp(count, 1, MAX_ROWS);

            for (int i = 0; i < _rows.Count; i++)
            {
                bool isActive = i < _activeRowCount;
                _rows[i].gameObject.SetActive(isActive);
            }
        }

        /// <summary>
        /// Sets the mode for all rows (Setup or Gameplay).
        /// Reinitializes rows with the new mode.
        /// </summary>
        public void SetMode(WordRowMode mode)
        {
            _mode = mode;

            for (int i = 0; i < _rows.Count; i++)
            {
                WordPatternRowUI row = _rows[i];
                if (row != null)
                {
                    row.Initialize(i, _wordLengths[i], _mode);
                }
            }
        }

        /// <summary>
        /// Sets word length for a specific row.
        /// Reinitializes the row with the new length.
        /// </summary>
        public void SetWordLength(int rowIndex, int length)
        {
            if (rowIndex >= 0 && rowIndex < MAX_ROWS)
            {
                _wordLengths[rowIndex] = Mathf.Clamp(length, MIN_WORD_LENGTH, MAX_WORD_LENGTH);

                WordPatternRowUI row = GetRow(rowIndex);
                if (row != null)
                {
                    row.Initialize(rowIndex, _wordLengths[rowIndex], _mode);
                }
            }
        }

        /// <summary>
        /// Sets the same word length for all rows.
        /// Reinitializes all rows with the new length.
        /// </summary>
        public void SetAllWordLengths(int length)
        {
            int clampedLength = Mathf.Clamp(length, MIN_WORD_LENGTH, MAX_WORD_LENGTH);

            for (int i = 0; i < MAX_ROWS; i++)
            {
                _wordLengths[i] = clampedLength;
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                WordPatternRowUI row = _rows[i];
                if (row != null)
                {
                    row.Initialize(i, _wordLengths[i], _mode);
                }
            }
        }

        /// <summary>
        /// Sets word lengths from an array (one per row).
        /// Reinitializes rows with the new lengths.
        /// </summary>
        public void SetWordLengths(int[] lengths)
        {
            for (int i = 0; i < MAX_ROWS && i < lengths.Length; i++)
            {
                _wordLengths[i] = Mathf.Clamp(lengths[i], MIN_WORD_LENGTH, MAX_WORD_LENGTH);
            }

            for (int i = 0; i < _rows.Count && i < lengths.Length; i++)
            {
                WordPatternRowUI row = _rows[i];
                if (row != null)
                {
                    row.Initialize(i, _wordLengths[i], _mode);
                }
            }
        }

        /// <summary>
        /// Sets a word on a specific row.
        /// </summary>
        public void SetWord(int rowIndex, string word)
        {
            WordPatternRowUI row = GetRow(rowIndex);
            if (row != null)
            {
                row.SetWord(word);
            }
        }

        /// <summary>
        /// Gets the word from a specific row.
        /// </summary>
        public string GetWord(int rowIndex)
        {
            WordPatternRowUI row = GetRow(rowIndex);
            return row != null ? row.CurrentWord : "";
        }

        /// <summary>
        /// Gets all words from active rows.
        /// </summary>
        public List<string> GetAllWords()
        {
            List<string> words = new List<string>();
            for (int i = 0; i < _activeRowCount && i < _rows.Count; i++)
            {
                words.Add(_rows[i].CurrentWord);
            }
            return words;
        }

        /// <summary>
        /// Checks if all active rows have complete words.
        /// </summary>
        public bool AllRowsHaveWords()
        {
            for (int i = 0; i < _activeRowCount && i < _rows.Count; i++)
            {
                if (!_rows[i].HasWord)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks if all active rows are placed.
        /// </summary>
        public bool AllRowsPlaced()
        {
            for (int i = 0; i < _activeRowCount && i < _rows.Count; i++)
            {
                if (!_rows[i].IsPlaced)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Resets all rows to empty state.
        /// </summary>
        public void ResetAllRows()
        {
            foreach (WordPatternRowUI row in _rows)
            {
                if (row != null)
                {
                    row.ResetToEmpty();
                }
            }
        }

        /// <summary>
        /// Sets player color for highlighting.
        /// </summary>
        public void SetPlayerColor(Color color)
        {
            _playerColor = color;

            foreach (WordPatternRowUI row in _rows)
            {
                if (row != null)
                {
                    row.SetPlayerColor(color);
                }
            }
        }

        // ============================================================
        // EVENT SUBSCRIPTION
        // ============================================================

        private void SubscribeToRow(WordPatternRowUI row)
        {
            if (row == null) return;

            row.OnLetterCellClicked += HandleLetterCellClicked;
            row.OnSelectClicked += HandleSelectClicked;
            row.OnPlaceClicked += HandlePlaceClicked;
            row.OnDeleteClicked += HandleDeleteClicked;
            row.OnGuessWordClicked += HandleGuessWordClicked;
        }

        private void UnsubscribeFromRows()
        {
            foreach (WordPatternRowUI row in _rows)
            {
                if (row == null) continue;

                row.OnLetterCellClicked -= HandleLetterCellClicked;
                row.OnSelectClicked -= HandleSelectClicked;
                row.OnPlaceClicked -= HandlePlaceClicked;
                row.OnDeleteClicked -= HandleDeleteClicked;
                row.OnGuessWordClicked -= HandleGuessWordClicked;
            }
        }

        // ============================================================
        // EVENT HANDLERS
        // ============================================================

        private void HandleLetterCellClicked(int rowIndex, int letterIndex)
        {
            OnLetterCellClicked?.Invoke(rowIndex, letterIndex);
        }

        private void HandleSelectClicked(int rowIndex)
        {
            OnSelectClicked?.Invoke(rowIndex);
        }

        private void HandlePlaceClicked(int rowIndex)
        {
            OnPlaceClicked?.Invoke(rowIndex);
        }

        private void HandleDeleteClicked(int rowIndex)
        {
            OnDeleteClicked?.Invoke(rowIndex);
        }

        private void HandleGuessWordClicked(int rowIndex)
        {
            OnGuessWordClicked?.Invoke(rowIndex);
        }
    }
}
