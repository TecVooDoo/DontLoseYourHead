using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace TecVooDoo.DontLoseYourHead.Core
{
    /// <summary>
    /// ScriptableObject that holds a filtered list of words of a specific length
    /// </summary>
    [CreateAssetMenu(fileName = "NewWordList", menuName = "DLYH/Word List")]
    public class WordListSO : ScriptableObject
    {
        [Title("Word List Configuration")]
        [Tooltip("The length of words in this list (e.g., 3, 4, or 5)")]
        [SerializeField] private int _wordLength;

        [Title("Words")]
        [InfoBox("This list is populated by the Word Bank Importer")]
        [SerializeField] private List<string> _words = new List<string>();

        public int WordLength => _wordLength;
        public List<string> Words => _words;
        public int Count => _words.Count;

        /// <summary>
        /// Get a random word from this list
        /// </summary>
        public string GetRandomWord()
        {
            if (_words.Count == 0)
            {
                Debug.LogWarning("[WordListSO] No words available in " + name + "!");
                return string.Empty;
            }

            int randomIndex = Random.Range(0, _words.Count);
            return _words[randomIndex];
        }

        /// <summary>
        /// Check if a word exists in this list (case-insensitive)
        /// </summary>
        public bool Contains(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return false;

            string normalizedWord = word.Trim().ToUpper();
            foreach (var w in _words)
            {
                if (w.ToUpper() == normalizedWord)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Clear and set the word list (used by importer)
        /// </summary>
        public void SetWords(List<string> words, int wordLength)
        {
            _words.Clear();
            _words.AddRange(words);
            _wordLength = wordLength;
        }

#if UNITY_EDITOR
        [Button("Show Stats", ButtonSizes.Medium)]
        [PropertyOrder(100)]
        private void ShowStats()
        {
            Debug.Log("[" + name + "] Contains " + _words.Count + " words of length " + _wordLength);

            if (_words.Count > 0)
            {
                int sampleCount = Mathf.Min(10, _words.Count);
                string samples = string.Join(", ", _words.GetRange(0, sampleCount));
                Debug.Log("Sample words: " + samples);
            }
        }
#endif
    }
} 