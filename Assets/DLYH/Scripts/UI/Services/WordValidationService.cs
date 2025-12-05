using UnityEngine;
using TecVooDoo.DontLoseYourHead.Core;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Service that validates words against word bank lists.
    /// Extracted from SetupSettingsPanel to follow Single Responsibility Principle.
    /// </summary>
    public class WordValidationService
    {
        private readonly WordListSO _threeLetterWords;
        private readonly WordListSO _fourLetterWords;
        private readonly WordListSO _fiveLetterWords;
        private readonly WordListSO _sixLetterWords;

        public WordValidationService(
            WordListSO threeLetterWords,
            WordListSO fourLetterWords,
            WordListSO fiveLetterWords,
            WordListSO sixLetterWords)
        {
            _threeLetterWords = threeLetterWords;
            _fourLetterWords = fourLetterWords;
            _fiveLetterWords = fiveLetterWords;
            _sixLetterWords = sixLetterWords;
        }

        /// <summary>
        /// Validates a word against the appropriate word list based on length
        /// </summary>
        /// <param name="word">The word to validate</param>
        /// <param name="requiredLength">The required length for this word slot</param>
        /// <returns>True if word is valid, false otherwise</returns>
        public bool ValidateWord(string word, int requiredLength)
        {
            if (string.IsNullOrEmpty(word))
            {
                Debug.Log("[WordValidationService] ValidateWord: Empty word rejected");
                return false;
            }

            string upperWord = word.ToUpper();
            int length = upperWord.Length;

            // Check if word matches required length
            if (length != requiredLength)
            {
                Debug.Log($"[WordValidationService] ValidateWord: '{upperWord}' length {length} != required {requiredLength}");
                return false;
            }

            WordListSO wordList = GetWordListForLength(length);

            if (wordList == null)
            {
                Debug.LogWarning($"[WordValidationService] No word list found for length {length}");
                return false;
            }

            bool isValid = wordList.Contains(upperWord);
            Debug.Log($"[WordValidationService] ValidateWord '{upperWord}' (length {length}): {(isValid ? "VALID" : "INVALID")}");

            return isValid;
        }

        /// <summary>
        /// Gets a random word of the specified length from the word bank
        /// </summary>
        public string GetRandomWordOfLength(int length)
        {
            WordListSO wordList = GetWordListForLength(length);
            if (wordList == null || wordList.Words == null || wordList.Words.Count == 0)
            {
                Debug.LogWarning($"[WordValidationService] No word list found for length {length}");
                return null;
            }

            int randomIndex = Random.Range(0, wordList.Words.Count);
            return wordList.Words[randomIndex];
        }

        /// <summary>
        /// Returns the appropriate WordListSO for the given word length
        /// </summary>
        public WordListSO GetWordListForLength(int length)
        {
            return length switch
            {
                3 => _threeLetterWords,
                4 => _fourLetterWords,
                5 => _fiveLetterWords,
                6 => _sixLetterWords,
                _ => null
            };
        }
    }
}