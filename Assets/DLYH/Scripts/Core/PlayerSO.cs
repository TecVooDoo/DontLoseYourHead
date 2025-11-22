using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace TecVooDoo.DontLoseYourHead.Core
{
    /// <summary>
    /// ScriptableObject that holds all data for a single player
    /// </summary>
    [CreateAssetMenu(fileName = "NewPlayer", menuName = "Game/Player")]
    public class PlayerSO : ScriptableObject
    {
        #region Configuration
        [Title("Player Identity")]
        [SerializeField] private string _playerName = "Player";
        [SerializeField] private int _playerIndex = 0;
        [SerializeField] private bool _isAI = false;
        
        [Title("Dependencies")]
        [Required]
        [SerializeField] private IntVariableSO _missCount;
        #endregion
        
        #region Runtime Data
        [Title("Runtime State")]
        [ReadOnly]
        [ShowInInspector]
        private Grid _grid;
        
        [ReadOnly]
        [ShowInInspector]
        private HashSet<char> _knownLetters = new HashSet<char>();
        
        [ReadOnly]
        [ShowInInspector]
        private HashSet<string> _guessedWords = new HashSet<string>();
        
        [ReadOnly]
        [ShowInInspector]
        private List<Word> _foundWords = new List<Word>();
        #endregion
        
        #region Properties
        public string PlayerName => _playerName;
        public int PlayerIndex => _playerIndex;
        public bool IsAI => _isAI;
        public Grid Grid => _grid;
        public HashSet<char> KnownLetters => _knownLetters;
        public HashSet<string> GuessedWords => _guessedWords;
        public List<Word> FoundWords => _foundWords;
        public int MissCount => _missCount.Value;
        public IntVariableSO MissCountVariable => _missCount;
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Initialize player with a new grid
        /// </summary>
        public void Initialize(int gridSize)
        {
            _grid = new Grid(gridSize);
            _knownLetters.Clear();
            _guessedWords.Clear();
            _foundWords.Clear();
            _missCount.Value = 0;
            
            Debug.Log($"[PlayerSO] {_playerName} initialized with {gridSize}x{gridSize} grid");
        }
        
        /// <summary>
        /// Add a letter to the known letters list
        /// </summary>
        public void AddKnownLetter(char letter)
        {
            letter = char.ToUpper(letter);
            
            if (_knownLetters.Add(letter))
            {
                Debug.Log($"[PlayerSO] {_playerName} learned letter: {letter}");
            }
        }
        
        /// <summary>
        /// Check if a letter is already known
        /// </summary>
        public bool IsLetterKnown(char letter)
        {
            return _knownLetters.Contains(char.ToUpper(letter));
        }
        
        /// <summary>
        /// Add a word to the guessed words list
        /// </summary>
        public void AddGuessedWord(string word)
        {
            word = word.ToUpper();
            
            if (_guessedWords.Add(word))
            {
                Debug.Log($"[PlayerSO] {_playerName} guessed word: {word}");
            }
        }
        
        /// <summary>
        /// Check if a word has already been guessed
        /// </summary>
        public bool HasGuessedWord(string word)
        {
            return _guessedWords.Contains(word.ToUpper());
        }
        
        /// <summary>
        /// Add a word to the found words list (correctly guessed complete word)
        /// </summary>
        public void AddFoundWord(Word word)
        {
            if (!_foundWords.Contains(word))
            {
                _foundWords.Add(word);
                Debug.Log($"[PlayerSO] {_playerName} found word: {word.Text}");
            }
        }
        
        /// <summary>
        /// Check if all words on opponent's grid have been found
        /// </summary>
        public bool HasFoundAllWords(Grid opponentGrid)
        {
            if (opponentGrid == null || opponentGrid.PlacedWords.Count == 0)
            {
                return false;
            }
            
            foreach (var word in opponentGrid.PlacedWords)
            {
                word.CheckIfFullyRevealed();
                
                if (!word.IsFullyRevealed)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Reset player state (for new game)
        /// </summary>
        [Button("Reset Player State")]
        public void ResetState()
        {
            _grid = null;
            _knownLetters.Clear();
            _guessedWords.Clear();
            _foundWords.Clear();
            _missCount.Value = 0;
            
            Debug.Log($"[PlayerSO] {_playerName} state reset");
        }
        #endregion
        
        #region Editor Helpers
        private void OnEnable()
        {
            // Clear runtime data when asset loads
            _knownLetters = new HashSet<char>();
            _guessedWords = new HashSet<string>();
            _foundWords = new List<Word>();
        }
        #endregion
    }
}
