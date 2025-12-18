using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Manages a list of guessed words, displaying them sorted by length then alphabetically.
    /// Attach to the GuessedWordList container (e.g., Player1GuessedWordList).
    /// </summary>
    public class GuessedWordListController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _guessedWordPrefab;

        [Header("Colors")]
        [SerializeField] private Color _hitColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
        [SerializeField] private Color _missColor = new Color(0.8f, 0.2f, 0.2f, 1f); // Red

        [Header("Sizing")]
        [SerializeField, Tooltip("Default height for word entries")]
        private float _defaultEntryHeight = 25f;
        [SerializeField, Tooltip("Minimum height for word entries when list is long")]
        private float _minEntryHeight = 14f;
        [SerializeField, Tooltip("Number of entries before scaling begins")]
        private int _scaleThreshold = 4;

        // Internal tracking of guessed words
        private List<GuessedWordData> _guessedWords = new List<GuessedWordData>();
        private List<GameObject> _instantiatedEntries = new List<GameObject>();
        private RectTransform _containerRect;
        private float _defaultFontSize = 14f; // Cached from prefab

        /// <summary>
        /// Data structure to track a guessed word and its result.
        /// </summary>
        private struct GuessedWordData
        {
            public string Word;
            public bool IsHit;

            public GuessedWordData(string word, bool isHit)
            {
                Word = word.ToUpper();
                IsHit = isHit;
            }
        }

        /// <summary>
        /// Adds a guessed word to the list and refreshes the display.
        /// </summary>
        /// <param name="word">The word that was guessed</param>
        /// <param name="isHit">True if the guess was correct, false if miss</param>
        public void AddGuessedWord(string word, bool isHit)
        {
            if (string.IsNullOrEmpty(word))
            {
                Debug.LogWarning("[GuessedWordListController] Attempted to add empty word.");
                return;
            }

            // Check for duplicate
            string upperWord = word.ToUpper();
            foreach (var existing in _guessedWords)
            {
                if (existing.Word == upperWord)
                {
                    Debug.LogWarning($"[GuessedWordListController] Word '{upperWord}' already in list.");
                    return;
                }
            }

            _guessedWords.Add(new GuessedWordData(word, isHit));
            SortAndRefreshDisplay();
        }

        /// <summary>
        /// Clears all guessed words from the list.
        /// </summary>
        public void ClearAllWords()
        {
            _guessedWords.Clear();
            DestroyAllEntries();
        }

        /// <summary>
        /// Gets the count of guessed words.
        /// </summary>
        public int WordCount => _guessedWords.Count;

        private void Awake()
        {
            _containerRect = GetComponent<RectTransform>();

            // Configure VerticalLayoutGroup for proper sizing control
            VerticalLayoutGroup layoutGroup = GetComponent<VerticalLayoutGroup>();
            if (layoutGroup != null)
            {
                // Disable child force expand so our heights are respected
                layoutGroup.childForceExpandHeight = false;
                layoutGroup.childControlHeight = false;
            }

            // Cache default font size from prefab
            if (_guessedWordPrefab != null)
            {
                TMP_Text prefabText = _guessedWordPrefab.GetComponentInChildren<TMP_Text>();
                if (prefabText != null)
                {
                    _defaultFontSize = prefabText.fontSize;
                }
            }
        }

        /// <summary>
        /// Sets the hit color used for correctly guessed words.
        /// Called during panel setup to apply the player's chosen color.
        /// </summary>
        public void SetHitColor(Color color)
        {
            _hitColor = color;
        }

        /// <summary>
        /// Calculates the entry height based on number of words and available space.
        /// </summary>
        private float CalculateEntryHeight()
        {
            int wordCount = _guessedWords.Count;
            if (wordCount <= _scaleThreshold)
            {
                return _defaultEntryHeight;
            }

            // Calculate available height from container
            float availableHeight = 120f; // Default fallback
            if (_containerRect != null)
            {
                // Force layout rebuild to get accurate rect size
                Canvas.ForceUpdateCanvases();
                availableHeight = _containerRect.rect.height;

                // Fallback if height is still 0 or invalid
                if (availableHeight <= 0f)
                {
                    availableHeight = 120f;
                }
            }

            // Get actual spacing from VerticalLayoutGroup if present
            float spacing = 2f;
            VerticalLayoutGroup layoutGroup = GetComponent<VerticalLayoutGroup>();
            if (layoutGroup != null)
            {
                spacing = layoutGroup.spacing;
            }

            // Account for spacing
            float totalSpacing = (wordCount - 1) * spacing;
            float heightPerEntry = (availableHeight - totalSpacing) / wordCount;

            // Clamp between min and default
            return Mathf.Clamp(heightPerEntry, _minEntryHeight, _defaultEntryHeight);
        }

        /// <summary>
        /// Sorts the word list and rebuilds the UI.
        /// </summary>
        private void SortAndRefreshDisplay()
        {
            // Sort by length first, then alphabetically
            _guessedWords.Sort((a, b) =>
            {
                int lengthCompare = a.Word.Length.CompareTo(b.Word.Length);
                if (lengthCompare != 0)
                    return lengthCompare;
                return string.Compare(a.Word, b.Word, System.StringComparison.Ordinal);
            });

            RebuildDisplay();
        }

        /// <summary>
        /// Destroys all instantiated entries and recreates them in sorted order.
        /// </summary>
        private void RebuildDisplay()
        {
            DestroyAllEntries();

            float entryHeight = CalculateEntryHeight();
            foreach (var wordData in _guessedWords)
            {
                CreateWordEntry(wordData, entryHeight);
            }
        }

        /// <summary>
        /// Creates a single word entry UI element.
        /// </summary>
        private void CreateWordEntry(GuessedWordData wordData, float entryHeight)
        {
            if (_guessedWordPrefab == null)
            {
                Debug.LogError("[GuessedWordListController] GuessedWord prefab not assigned!");
                return;
            }

            GameObject entry = Instantiate(_guessedWordPrefab, transform);
            _instantiatedEntries.Add(entry);

            // Set entry height
            RectTransform entryRect = entry.GetComponent<RectTransform>();
            if (entryRect != null)
            {
                entryRect.sizeDelta = new Vector2(entryRect.sizeDelta.x, entryHeight);
            }

            // Also set LayoutElement if present for proper layout group behavior
            LayoutElement layoutElement = entry.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.preferredHeight = entryHeight;
                layoutElement.minHeight = entryHeight;
            }

            // Set background color
            Image backgroundImage = entry.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = wordData.IsHit ? _hitColor : _missColor;
            }

            // Set text and adjust font size if needed
            TMP_Text textComponent = entry.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
            {
                textComponent.text = wordData.Word;
                // Scale font size proportionally with entry height
                float fontScale = entryHeight / _defaultEntryHeight;
                textComponent.fontSize = Mathf.Max(8f, _defaultFontSize * fontScale);
            }
        }

        /// <summary>
        /// Destroys all instantiated word entry GameObjects.
        /// </summary>
        private void DestroyAllEntries()
        {
            foreach (var entry in _instantiatedEntries)
            {
                if (entry != null)
                {
                    Destroy(entry);
                }
            }
            _instantiatedEntries.Clear();
        }
    }
}
