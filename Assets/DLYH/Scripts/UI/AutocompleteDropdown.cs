using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Displays filtered word suggestions from the word bank as the player types.
    /// Filters by required word length and current input prefix.
    /// </summary>
    public class AutocompleteDropdown : MonoBehaviour
    {
        #region Serialized Fields - References
        [TitleGroup("References")]
        [SerializeField, Required]
        private Transform _itemContainer;

        [SerializeField, Required]
        private GameObject _itemPrefab;

        [SerializeField]
        private ScrollRect _scrollRect;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private RectTransform _dropdownRect;
        #endregion

        #region Serialized Fields - Configuration
        [TitleGroup("Configuration")]
        [SerializeField, Range(1, 20)]
        private int _maxVisibleItems = 8;

        [SerializeField]
        private float _itemHeight = 30f;

        [SerializeField, Range(1, 3)]
        private int _minCharsToShow = 2;

        [SerializeField]
        private bool _hideOnEmptyResults = true;

        [TitleGroup("Animation")]
        [SerializeField]
        private float _fadeInDuration = 0.15f;

        [SerializeField]
        private float _fadeOutDuration = 0.1f;
        #endregion

        #region Private Fields
        private List<string> _currentWordList = new List<string>();
        private List<string> _filteredWords = new List<string>();
        private List<AutocompleteItem> _itemInstances = new List<AutocompleteItem>();
        private string _currentFilter = "";
        private int _currentWordLength;
        private bool _isVisible;
        private int _selectedIndex = -1;
        #endregion

        #region Events
        /// <summary>
        /// Fired when a word is selected from the dropdown. Parameter: selected word
        /// </summary>
        public event Action<string> OnWordSelected;

        /// <summary>
        /// Fired when the dropdown is shown
        /// </summary>
        public event Action OnDropdownShown;

        /// <summary>
        /// Fired when the dropdown is hidden
        /// </summary>
        public event Action OnDropdownHidden;
        #endregion

        #region Properties
        /// <summary>
        /// Whether the dropdown is currently visible
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Number of filtered results currently shown
        /// </summary>
        public int FilteredCount => _filteredWords.Count;

        /// <summary>
        /// Currently selected index (-1 if none)
        /// </summary>
        public int SelectedIndex => _selectedIndex;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_dropdownRect == null)
            {
                _dropdownRect = GetComponent<RectTransform>();
            }

            // Start hidden
            SetVisible(false, immediate: true);
        }
        #endregion

        #region Public Methods - Word List
        /// <summary>
        /// Sets the word list to use for autocomplete suggestions.
        /// </summary>
        /// <param name="words">List of valid words</param>
        public void SetWordList(List<string> words)
        {
            _currentWordList = words ?? new List<string>();
            RefreshFilter();
        }

        /// <summary>
        /// Sets the word list from a WordListSO ScriptableObject.
        /// </summary>
        /// <param name="wordListSO">The word list ScriptableObject</param>
        public void SetWordListFromSO(ScriptableObject wordListSO)
        {
            // Use reflection to get the words list to avoid hard dependency
            var wordsField = wordListSO.GetType().GetField("words");
            if (wordsField != null)
            {
                var words = wordsField.GetValue(wordListSO) as List<string>;
                SetWordList(words);
            }
            else
            {
                var wordsProperty = wordListSO.GetType().GetProperty("Words");
                if (wordsProperty != null)
                {
                    var words = wordsProperty.GetValue(wordListSO) as List<string>;
                    SetWordList(words);
                }
            }
        }

        /// <summary>
        /// Sets the required word length for filtering.
        /// </summary>
        /// <param name="length">Required word length (3-6)</param>
        public void SetRequiredWordLength(int length)
        {
            _currentWordLength = Mathf.Clamp(length, 3, 6);
            RefreshFilter();
        }
        #endregion

        #region Public Methods - Filtering
        /// <summary>
        /// Updates the filter based on current input text.
        /// </summary>
        /// <param name="inputText">Current text the user has typed</param>
        public void UpdateFilter(string inputText)
        {
            _currentFilter = inputText?.ToUpper() ?? "";
            RefreshFilter();

            // Show/hide based on input length and results
            bool shouldShow = _currentFilter.Length >= _minCharsToShow;

            if (_hideOnEmptyResults && _filteredWords.Count == 0)
            {
                shouldShow = false;
            }

            if (shouldShow && !_isVisible)
            {
                Show();
            }
            else if (!shouldShow && _isVisible)
            {
                Hide();
            }
        }

        /// <summary>
        /// Clears the current filter and hides the dropdown.
        /// </summary>
        public void ClearFilter()
        {
            _currentFilter = "";
            _filteredWords.Clear();
            _selectedIndex = -1;
            Hide();
        }
        #endregion

        #region Public Methods - Visibility
        /// <summary>
        /// Shows the dropdown.
        /// </summary>
        public void Show()
        {
            if (_isVisible) return;

            SetVisible(true);
            OnDropdownShown?.Invoke();
        }

        /// <summary>
        /// Hides the dropdown.
        /// </summary>
        public void Hide()
        {
            if (!_isVisible) return;

            _selectedIndex = -1;
            SetVisible(false);
            OnDropdownHidden?.Invoke();
        }
        #endregion

        #region Public Methods - Navigation
        /// <summary>
        /// Moves selection up in the list.
        /// </summary>
        public void SelectPrevious()
        {
            if (_filteredWords.Count == 0) return;

            _selectedIndex--;
            if (_selectedIndex < 0)
            {
                _selectedIndex = _filteredWords.Count - 1;
            }

            UpdateSelectionVisuals();
            ScrollToSelected();
        }

        /// <summary>
        /// Moves selection down in the list.
        /// </summary>
        public void SelectNext()
        {
            if (_filteredWords.Count == 0) return;

            _selectedIndex++;
            if (_selectedIndex >= _filteredWords.Count)
            {
                _selectedIndex = 0;
            }

            UpdateSelectionVisuals();
            ScrollToSelected();
        }

        /// <summary>
        /// Confirms the current selection.
        /// </summary>
        /// <returns>True if a word was selected</returns>
        public bool ConfirmSelection()
        {
            if (_selectedIndex >= 0 && _selectedIndex < _filteredWords.Count)
            {
                string selectedWord = _filteredWords[_selectedIndex];
                OnWordSelected?.Invoke(selectedWord);
                Hide();
                return true;
            }

            // If no selection but only one result, select it
            if (_filteredWords.Count == 1)
            {
                OnWordSelected?.Invoke(_filteredWords[0]);
                Hide();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the currently selected word, or null if none.
        /// </summary>
        public string GetSelectedWord()
        {
            if (_selectedIndex >= 0 && _selectedIndex < _filteredWords.Count)
            {
                return _filteredWords[_selectedIndex];
            }
            return null;
        }
        #endregion

        #region Private Methods - Filtering
        private void RefreshFilter()
        {
            _filteredWords.Clear();
            _selectedIndex = -1;

            if (string.IsNullOrEmpty(_currentFilter) || _currentWordList == null)
            {
                UpdateItemDisplay();
                return;
            }

            // Filter words by length and prefix
            foreach (string word in _currentWordList)
            {
                // Check length if specified
                if (_currentWordLength > 0 && word.Length != _currentWordLength)
                {
                    continue;
                }

                // Check prefix match
                if (word.StartsWith(_currentFilter, StringComparison.OrdinalIgnoreCase))
                {
                    _filteredWords.Add(word);

                    // Limit results for performance
                    if (_filteredWords.Count >= 50)
                    {
                        break;
                    }
                }
            }

            UpdateItemDisplay();
        }
        #endregion

        #region Private Methods - Display
        private void UpdateItemDisplay()
        {
            // Ensure we have enough item instances
            EnsureItemInstances(_filteredWords.Count);

            // Update items
            for (int i = 0; i < _itemInstances.Count; i++)
            {
                if (i < _filteredWords.Count)
                {
                    _itemInstances[i].gameObject.SetActive(true);
                    _itemInstances[i].SetWord(_filteredWords[i], _currentFilter);
                    _itemInstances[i].SetSelected(i == _selectedIndex);
                }
                else
                {
                    _itemInstances[i].gameObject.SetActive(false);
                }
            }

            // Update dropdown height
            UpdateDropdownHeight();
        }

        private void EnsureItemInstances(int count)
        {
            // Create more instances if needed
            while (_itemInstances.Count < count && _itemInstances.Count < _maxVisibleItems * 2)
            {
                CreateItemInstance();
            }
        }

        private void CreateItemInstance()
        {
            if (_itemPrefab == null || _itemContainer == null) return;

            GameObject itemGO = Instantiate(_itemPrefab, _itemContainer);
            AutocompleteItem item = itemGO.GetComponent<AutocompleteItem>();

            if (item == null)
            {
                item = itemGO.AddComponent<AutocompleteItem>();
            }

            int index = _itemInstances.Count;
            item.OnItemClicked += () => HandleItemClicked(index);

            _itemInstances.Add(item);
        }

        private void UpdateDropdownHeight()
        {
            if (_dropdownRect == null) return;

            int visibleCount = Mathf.Min(_filteredWords.Count, _maxVisibleItems);
            float height = visibleCount * _itemHeight;

            Vector2 sizeDelta = _dropdownRect.sizeDelta;
            sizeDelta.y = height;
            _dropdownRect.sizeDelta = sizeDelta;
        }

        private void UpdateSelectionVisuals()
        {
            for (int i = 0; i < _itemInstances.Count; i++)
            {
                if (i < _filteredWords.Count)
                {
                    _itemInstances[i].SetSelected(i == _selectedIndex);
                }
            }
        }

        private void ScrollToSelected()
        {
            if (_scrollRect == null || _selectedIndex < 0) return;

            // Calculate normalized scroll position
            float normalizedPos = 1f - ((float)_selectedIndex / Mathf.Max(1, _filteredWords.Count - 1));
            _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPos);
        }
        #endregion

        #region Private Methods - Visibility
        private void SetVisible(bool visible, bool immediate = false)
        {
            _isVisible = visible;

            if (_canvasGroup != null)
            {
                if (immediate)
                {
                    _canvasGroup.alpha = visible ? 1f : 0f;
                    _canvasGroup.interactable = visible;
                    _canvasGroup.blocksRaycasts = visible;
                }
                else
                {
                    // Use DOTween if available, otherwise immediate
                    _canvasGroup.alpha = visible ? 1f : 0f;
                    _canvasGroup.interactable = visible;
                    _canvasGroup.blocksRaycasts = visible;
                }
            }

            gameObject.SetActive(visible);
        }
        #endregion

        #region Private Methods - Event Handlers
        private void HandleItemClicked(int index)
        {
            // Find the actual index based on which items are visible
            int actualIndex = 0;
            for (int i = 0; i < _itemInstances.Count; i++)
            {
                if (_itemInstances[i].gameObject.activeSelf)
                {
                    if (i == index)
                    {
                        break;
                    }
                    actualIndex++;
                }
            }

            if (actualIndex < _filteredWords.Count)
            {
                OnWordSelected?.Invoke(_filteredWords[actualIndex]);
                Hide();
            }
        }
        #endregion

        #region Editor Helpers
#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Test with Sample Words")]
        private void TestWithSampleWords()
        {
            List<string> testWords = new List<string>
            {
                "CAT", "CAR", "CAP", "CAN", "CAB",
                "RAT", "RAW", "RAM", "RAN", "RAG",
                "BAT", "BAR", "BAD", "BAG", "BAN"
            };
            SetWordList(testWords);
            SetRequiredWordLength(3);
            UpdateFilter("CA");
        }

        [Button("Test Show")]
        private void TestShow()
        {
            Show();
        }

        [Button("Test Hide")]
        private void TestHide()
        {
            Hide();
        }

        [Button("Log Filtered Words")]
        private void LogFilteredWords()
        {
            Debug.Log($"[AutocompleteDropdown] Filter: '{_currentFilter}', Length: {_currentWordLength}");
            Debug.Log($"[AutocompleteDropdown] Results: {_filteredWords.Count}");
            for (int i = 0; i < Mathf.Min(10, _filteredWords.Count); i++)
            {
                Debug.Log($"  {i}: {_filteredWords[i]}");
            }
        }
#endif
        #endregion
    }

    /// <summary>
    /// Individual item in the autocomplete dropdown.
    /// </summary>
    public class AutocompleteItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Serialized Fields
        [SerializeField]
        private TextMeshProUGUI _wordText;

        [SerializeField]
        private Image _backgroundImage;

        [SerializeField]
        private Button _button;

        [SerializeField]
        private Color _normalColor = new Color(1f, 1f, 1f, 1f);

        [SerializeField]
        private Color _hoverColor = new Color(0.9f, 0.95f, 1f, 1f);

        [SerializeField]
        private Color _selectedColor = new Color(0.8f, 0.9f, 1f, 1f);

        [SerializeField]
        private Color _matchedTextColor = new Color(0.2f, 0.4f, 0.8f, 1f);
        #endregion

        #region Private Fields
        private string _word;
        private bool _isSelected;
        private bool _isHovered;
        #endregion

        #region Events
        public event Action OnItemClicked;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(HandleClick);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClick);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the word to display, optionally highlighting the matched prefix.
        /// </summary>
        /// <param name="word">The word to display</param>
        /// <param name="matchedPrefix">The prefix that was matched (for highlighting)</param>
        public void SetWord(string word, string matchedPrefix = "")
        {
            _word = word;

            if (_wordText != null)
            {
                if (!string.IsNullOrEmpty(matchedPrefix) && word.StartsWith(matchedPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    // Highlight the matched portion
                    string matched = word.Substring(0, matchedPrefix.Length);
                    string remainder = word.Substring(matchedPrefix.Length);
                    string hexColor = ColorUtility.ToHtmlStringRGB(_matchedTextColor);
                    _wordText.text = $"<color=#{hexColor}>{matched}</color>{remainder}";
                }
                else
                {
                    _wordText.text = word;
                }
            }
        }

        /// <summary>
        /// Sets whether this item is selected.
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateVisuals();
        }
        #endregion

        #region IPointerEnterHandler / IPointerExitHandler
        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            UpdateVisuals();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            UpdateVisuals();
        }
        #endregion

        #region Private Methods
        private void CacheReferences()
        {
            if (_wordText == null)
            {
                _wordText = GetComponentInChildren<TextMeshProUGUI>();
            }

            if (_backgroundImage == null)
            {
                _backgroundImage = GetComponent<Image>();
            }

            if (_button == null)
            {
                _button = GetComponent<Button>();
            }
        }

        private void HandleClick()
        {
            OnItemClicked?.Invoke();
        }

        private void UpdateVisuals()
        {
            if (_backgroundImage == null) return;

            Color bgColor;

            if (_isSelected)
            {
                bgColor = _selectedColor;
            }
            else if (_isHovered)
            {
                bgColor = _hoverColor;
            }
            else
            {
                bgColor = _normalColor;
            }

            _backgroundImage.color = bgColor;
        }
        #endregion
    }
}
