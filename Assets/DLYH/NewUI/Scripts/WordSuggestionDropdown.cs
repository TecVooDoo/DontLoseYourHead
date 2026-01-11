using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TecVooDoo.DontLoseYourHead.Core;

namespace DLYH.TableUI
{
    /// <summary>
    /// A dropdown that shows filtered word suggestions as the user types.
    /// Built for UI Toolkit, displays below the active word row.
    /// </summary>
    public class WordSuggestionDropdown
    {
        private VisualElement _root;
        private VisualElement _itemsContainer;
        private List<string> _wordList = new List<string>();
        private List<string> _filteredWords = new List<string>();
        private List<Button> _itemButtons = new List<Button>();
        private int _requiredLength;
        private int _selectedIndex = -1;
        private bool _isVisible;

        private const int MAX_VISIBLE_ITEMS = 6;
        private const int MIN_CHARS_TO_SHOW = 1;

        // USS class names
        private static readonly string ClassDropdown = "word-suggestion-dropdown";
        private static readonly string ClassItemsContainer = "word-suggestion-items";
        private static readonly string ClassItem = "word-suggestion-item";
        private static readonly string ClassItemSelected = "word-suggestion-item-selected";
        private static readonly string ClassHidden = "hidden";

        /// <summary>
        /// Event fired when a word is selected from the dropdown.
        /// </summary>
        public event Action<string> OnWordSelected;

        /// <summary>
        /// The root VisualElement for the dropdown.
        /// </summary>
        public VisualElement Root => _root;

        /// <summary>
        /// Whether the dropdown is currently visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Number of filtered results.
        /// </summary>
        public int FilteredCount => _filteredWords.Count;

        /// <summary>
        /// Currently selected index (-1 if none).
        /// </summary>
        public int SelectedIndex => _selectedIndex;

        public WordSuggestionDropdown()
        {
            BuildUI();
            Hide();
        }

        private void BuildUI()
        {
            _root = new VisualElement();
            _root.AddToClassList(ClassDropdown);

            _itemsContainer = new VisualElement();
            _itemsContainer.AddToClassList(ClassItemsContainer);
            _root.Add(_itemsContainer);
        }

        /// <summary>
        /// Sets the word list for a specific length from a WordListSO.
        /// </summary>
        public void SetWordList(WordListSO wordListSO)
        {
            if (wordListSO == null)
            {
                _wordList.Clear();
                return;
            }

            _wordList = new List<string>(wordListSO.Words);
        }

        /// <summary>
        /// Sets the word list directly.
        /// </summary>
        public void SetWordList(List<string> words)
        {
            _wordList = words ?? new List<string>();
        }

        /// <summary>
        /// Sets the required word length for filtering.
        /// </summary>
        public void SetRequiredLength(int length)
        {
            _requiredLength = length;
        }

        /// <summary>
        /// Updates the filter based on current input text.
        /// </summary>
        public void UpdateFilter(string inputText)
        {
            string filter = inputText?.ToUpper() ?? "";

            _filteredWords.Clear();
            _selectedIndex = -1;

            // Only filter if we have enough characters
            if (filter.Length < MIN_CHARS_TO_SHOW)
            {
                UpdateDisplay();
                Hide();
                return;
            }

            // Filter words by prefix and length
            foreach (string word in _wordList)
            {
                // Check length if specified
                if (_requiredLength > 0 && word.Length != _requiredLength)
                {
                    continue;
                }

                // Check prefix match
                if (word.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
                {
                    _filteredWords.Add(word);

                    // Limit results
                    if (_filteredWords.Count >= MAX_VISIBLE_ITEMS * 2)
                    {
                        break;
                    }
                }
            }

            UpdateDisplay();

            if (_filteredWords.Count > 0)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        /// <summary>
        /// Clears the filter and hides the dropdown.
        /// </summary>
        public void ClearFilter()
        {
            _filteredWords.Clear();
            _selectedIndex = -1;
            Hide();
        }

        /// <summary>
        /// Shows the dropdown.
        /// </summary>
        public void Show()
        {
            if (_isVisible) return;
            _isVisible = true;
            _root.RemoveFromClassList(ClassHidden);
        }

        /// <summary>
        /// Hides the dropdown.
        /// </summary>
        public void Hide()
        {
            _isVisible = false;
            _selectedIndex = -1;
            _root.AddToClassList(ClassHidden);
        }

        /// <summary>
        /// Moves selection up.
        /// </summary>
        public void SelectPrevious()
        {
            if (_filteredWords.Count == 0) return;

            _selectedIndex--;
            if (_selectedIndex < 0)
            {
                _selectedIndex = Math.Min(_filteredWords.Count - 1, MAX_VISIBLE_ITEMS - 1);
            }

            UpdateSelectionVisuals();
        }

        /// <summary>
        /// Moves selection down.
        /// </summary>
        public void SelectNext()
        {
            if (_filteredWords.Count == 0) return;

            _selectedIndex++;
            int maxIndex = Math.Min(_filteredWords.Count - 1, MAX_VISIBLE_ITEMS - 1);
            if (_selectedIndex > maxIndex)
            {
                _selectedIndex = 0;
            }

            UpdateSelectionVisuals();
        }

        /// <summary>
        /// Confirms the current selection.
        /// </summary>
        /// <returns>True if a word was selected.</returns>
        public bool ConfirmSelection()
        {
            if (_selectedIndex >= 0 && _selectedIndex < _filteredWords.Count)
            {
                string word = _filteredWords[_selectedIndex];
                OnWordSelected?.Invoke(word);
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

        private void UpdateDisplay()
        {
            // Clear existing items
            _itemsContainer.Clear();
            _itemButtons.Clear();

            // Create items for filtered words (up to max)
            int itemsToShow = Math.Min(_filteredWords.Count, MAX_VISIBLE_ITEMS);

            for (int i = 0; i < itemsToShow; i++)
            {
                string word = _filteredWords[i];
                int capturedIndex = i;

                Button item = new Button(() => HandleItemClicked(capturedIndex));
                item.text = word;
                item.AddToClassList(ClassItem);

                if (i == _selectedIndex)
                {
                    item.AddToClassList(ClassItemSelected);
                }

                _itemsContainer.Add(item);
                _itemButtons.Add(item);
            }
        }

        private void UpdateSelectionVisuals()
        {
            for (int i = 0; i < _itemButtons.Count; i++)
            {
                if (i == _selectedIndex)
                {
                    _itemButtons[i].AddToClassList(ClassItemSelected);
                }
                else
                {
                    _itemButtons[i].RemoveFromClassList(ClassItemSelected);
                }
            }
        }

        private void HandleItemClicked(int index)
        {
            if (index >= 0 && index < _filteredWords.Count)
            {
                string word = _filteredWords[index];
                OnWordSelected?.Invoke(word);
                Hide();
            }
        }
    }
}
