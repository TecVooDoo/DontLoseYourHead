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

        // Internal tracking of guessed words
        private List<GuessedWordData> _guessedWords = new List<GuessedWordData>();
        private List<GameObject> _instantiatedEntries = new List<GameObject>();

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

            foreach (var wordData in _guessedWords)
            {
                CreateWordEntry(wordData);
            }
        }

        /// <summary>
        /// Creates a single word entry UI element.
        /// </summary>
        private void CreateWordEntry(GuessedWordData wordData)
        {
            if (_guessedWordPrefab == null)
            {
                Debug.LogError("[GuessedWordListController] GuessedWord prefab not assigned!");
                return;
            }

            GameObject entry = Instantiate(_guessedWordPrefab, transform);
            _instantiatedEntries.Add(entry);

            // Set background color
            Image backgroundImage = entry.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = wordData.IsHit ? _hitColor : _missColor;
            }

            // Set text
            TMP_Text textComponent = entry.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
            {
                textComponent.text = wordData.Word;
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
