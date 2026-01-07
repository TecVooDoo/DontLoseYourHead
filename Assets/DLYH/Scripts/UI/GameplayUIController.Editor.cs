#if UNITY_EDITOR
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace TecVooDoo.DontLoseYourHead.UI
{
    // Type alias for cleaner code
    using GuessResult = GuessProcessingManager.GuessResult;

    /// <summary>
    /// Editor-only testing functionality for GameplayUIController.
    /// Provides Odin Inspector buttons for debugging gameplay.
    /// </summary>
    public partial class GameplayUIController
    {
        #region Editor Testing Fields

        [FoldoutGroup("Editor Testing")]
        [SerializeField]
        [GUIColor(0.8f, 1f, 0.8f)]
        private string _testOpponentLetter = "E";

        [FoldoutGroup("Editor Testing")]
        [SerializeField]
        [GUIColor(0.8f, 1f, 0.8f)]
        private int _testOpponentCol = 0;

        [FoldoutGroup("Editor Testing")]
        [SerializeField]
        [GUIColor(0.8f, 1f, 0.8f)]
        private int _testOpponentRow = 0;

        [FoldoutGroup("Editor Testing")]
        [SerializeField]
        [GUIColor(0.8f, 1f, 0.8f)]
        private string _testOpponentWord = "RAW";

        #endregion

        #region Editor Testing Methods

        [FoldoutGroup("Editor Testing")]
        [Button("Switch to Opponent Turn")]
        [GUIColor(0.8f, 0.6f, 1f)]
        private void TestSwitchToOpponentTurn()
        {
            if (_isPlayerTurn)
            {
                _isPlayerTurn = false;
                Debug.Log("[GameplayUI] Switched to Opponent's turn (manual)");
            }
            else
            {
                Debug.Log("[GameplayUI] Already Opponent's turn!");
            }
        }

        [FoldoutGroup("Editor Testing")]
        [Button("Switch to Player Turn")]
        [GUIColor(0.8f, 0.6f, 1f)]
        private void TestSwitchToPlayerTurn()
        {
            if (!_isPlayerTurn)
            {
                _isPlayerTurn = true;
                Debug.Log("[GameplayUI] Switched to Player's turn (manual)");
            }
            else
            {
                Debug.Log("[GameplayUI] Already Player's turn!");
            }
        }

        [FoldoutGroup("Editor Testing")]
        [Button("Simulate Opponent Letter Guess")]
        [GUIColor(0.8f, 0.4f, 0.2f)]
        private void TestSimulateOpponentLetter()
        {
            if (_playerSetupData == null || _playerSetupData.PlacedWords.Count == 0)
            {
                Debug.LogWarning("[GameplayUI] No player data - start gameplay first!");
                return;
            }

            if (_isPlayerTurn)
            {
                Debug.LogWarning("[GameplayUI] It's player's turn! Use 'Switch to Opponent Turn' first.");
                return;
            }

            char letter = _testOpponentLetter.Length > 0 ? char.ToUpper(_testOpponentLetter[0]) : 'E';
            GuessResult result = ProcessOpponentLetterGuess(letter);

            if (result == GuessResult.AlreadyGuessed)
            {
                Debug.Log(string.Format("[GameplayUI] Opponent already guessed letter '{0}' - try a different letter!", letter));
                return;
            }

            Debug.Log(string.Format("[GameplayUI] Opponent guessed letter '{0}': {1}", letter, result == GuessResult.Hit ? "HIT" : "MISS"));
            EndOpponentTurn();
        }

        [FoldoutGroup("Editor Testing")]
        [Button("Simulate Opponent Coordinate Guess")]
        [GUIColor(0.8f, 0.4f, 0.2f)]
        private void TestSimulateOpponentCoordinate()
        {
            if (_playerSetupData == null || _playerSetupData.PlacedWords.Count == 0)
            {
                Debug.LogWarning("[GameplayUI] No player data - start gameplay first!");
                return;
            }

            if (_isPlayerTurn)
            {
                Debug.LogWarning("[GameplayUI] It's player's turn! Use 'Switch to Opponent Turn' first.");
                return;
            }

            string colLabel = ((char)('A' + _testOpponentCol)).ToString();
            string coordLabel = colLabel + (_testOpponentRow + 1);

            GuessResult result = ProcessOpponentCoordinateGuess(_testOpponentCol, _testOpponentRow);

            if (result == GuessResult.AlreadyGuessed)
            {
                Debug.Log(string.Format("[GameplayUI] Opponent already guessed coordinate {0} - try a different cell!", coordLabel));
                return;
            }

            Debug.Log(string.Format("[GameplayUI] Opponent guessed coordinate {0}: {1}", coordLabel, result == GuessResult.Hit ? "HIT" : "MISS"));
            EndOpponentTurn();
        }

        [FoldoutGroup("Editor Testing")]
        [Button("Simulate Opponent Word Guess")]
        [GUIColor(0.8f, 0.4f, 0.2f)]
        private void TestSimulateWordGuess()
        {
            if (_playerSetupData == null || _playerSetupData.PlacedWords.Count == 0)
            {
                Debug.LogWarning("[GameplayUI] No player data - start gameplay first!");
                return;
            }

            if (_isPlayerTurn)
            {
                Debug.LogWarning("[GameplayUI] It's player's turn! Use 'Switch to Opponent Turn' first.");
                return;
            }

            GuessResult result = ProcessOpponentWordGuess(_testOpponentWord, 0);

            if (result == GuessResult.InvalidWord)
            {
                Debug.Log(string.Format("[GameplayUI] Opponent word '{0}' is not a valid English word - rejected!", _testOpponentWord));
                return;
            }

            if (result == GuessResult.AlreadyGuessed)
            {
                Debug.Log(string.Format("[GameplayUI] Opponent already guessed word '{0}' - try a different word!", _testOpponentWord));
                return;
            }

            Debug.Log(string.Format("[GameplayUI] Opponent guessed word '{0}': {1}",
                _testOpponentWord, result == GuessResult.Hit ? "CORRECT" : "WRONG (+2 misses)"));
            EndOpponentTurn();
        }

        [Button("Show Player's Words (Targets)")]
        [GUIColor(0.5f, 0.7f, 1f)]
        private void TestShowPlayerWords()
        {
            if (_playerSetupData == null || _playerSetupData.PlacedWords.Count == 0)
            {
                Debug.LogWarning("[GameplayUI] No player data!");
                return;
            }

            Debug.Log("[GameplayUI] === Player's Words (Opponent's Targets) ===");
            foreach (WordPlacementData word in _playerSetupData.PlacedWords)
            {
                string colLabel = ((char)('A' + word.StartCol)).ToString();
                string coordLabel = colLabel + (word.StartRow + 1);
                string direction = word.DirCol == 1 ? "Horizontal" : (word.DirRow == 1 ? "Vertical" : "Diagonal");

                Debug.Log(string.Format("  {0}. {1} at {2} ({3})",
                    word.RowIndex + 1, word.Word, coordLabel, direction));
            }
        }

        [Button("Show Opponent's Words (Your Targets)")]
        [GUIColor(0.5f, 0.7f, 1f)]
        private void TestShowOpponentWords()
        {
            if (_opponentSetupData == null || _opponentSetupData.PlacedWords.Count == 0)
            {
                Debug.LogWarning("[GameplayUI] No opponent data!");
                return;
            }

            Debug.Log("[GameplayUI] === Opponent's Words (Your Targets) ===");
            foreach (WordPlacementData word in _opponentSetupData.PlacedWords)
            {
                string colLabel = ((char)('A' + word.StartCol)).ToString();
                string coordLabel = colLabel + (word.StartRow + 1);
                string direction = word.DirCol == 1 ? "Horizontal" : (word.DirRow == 1 ? "Vertical" : "Diagonal");

                Debug.Log(string.Format("  {0}. {1} at {2} ({3})",
                    word.RowIndex + 1, word.Word, coordLabel, direction));
            }
        }

        [Button("Show Known Letters")]
        [GUIColor(0.5f, 0.7f, 1f)]
        private void TestShowKnownLetters()
        {
            List<char> playerSorted = new List<char>(_playerKnownLetters);
            playerSorted.Sort();
            List<char> opponentSorted = new List<char>(_opponentKnownLetters);
            opponentSorted.Sort();

            Debug.Log(string.Format("[GameplayUI] Your Known Letters: {0}",
                playerSorted.Count > 0 ? string.Join(", ", playerSorted) : "(none)"));
            Debug.Log(string.Format("[GameplayUI] Opponent's Known Letters: {0}",
                opponentSorted.Count > 0 ? string.Join(", ", opponentSorted) : "(none)"));
        }

        [Button("Trigger AI Turn (Debug)")]
        [GUIColor(1f, 0.8f, 0.4f)]
        private void TestTriggerAITurn()
        {
            if (_opponentTurnManager == null || !_opponentTurnManager.IsOpponentInitialized)
            {
                Debug.LogWarning("[GameplayUI] Opponent not initialized!");
                return;
            }

            if (_isPlayerTurn)
            {
                Debug.LogWarning("[GameplayUI] It's player's turn! Switch to opponent turn first.");
                return;
            }

            _opponentTurnManager.TriggerOpponentTurn();
        }

        #endregion
    }
}
#endif
