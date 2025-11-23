using UnityEngine;
using Sirenix.OdinInspector;
using TecVooDoo.DontLoseYourHead.Core.GameState;

namespace TecVooDoo.DontLoseYourHead.Core
{
    /// <summary>
    /// Consolidated test controller - all testing buttons in one place, in logical order
    /// </summary>
    public class TestController : MonoBehaviour
    {
        [Title("Dependencies")]
        [Required]
        [SerializeField] private GameManager _gameManager;

        [Required]
        [SerializeField] private GameStateMachine _stateMachine;

        [Required]
        [SerializeField] private TurnManager _turnManager;

        [Title("Test Configuration")]
        [SerializeField] private string _word1 = "CAT";
        [SerializeField] private string _word2 = "DOGS";
        [SerializeField] private string _word3 = "BIRDS";
        [SerializeField] private string _customWordGuess = "CAT";

        #region Step 1: Initialize

        [Title("STEP 1: Initialize Game")]
        [Button("1. Initialize Game", ButtonSizes.Large)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void Step1_InitializeGame()
        {
            if (_gameManager == null)
            {
                Debug.LogError("[TestController] GameManager reference missing!");
                return;
            }

            // Call GameManager's initialization through reflection or make it public
            _gameManager.SendMessage("InitializeGame");
            Debug.Log("[TestController] [SUCCESS] Game initialized");
        }

        #endregion

        #region Step 2: Place Words

        [Title("STEP 2: Place Test Words")]
        [Button("2. Place Words on Opponent Grid", ButtonSizes.Large)]
        [GUIColor(0.5f, 1f, 0.8f)]
        private void Step2_PlaceWords()
        {
            if (_gameManager == null)
            {
                Debug.LogError("[TestController] GameManager reference missing!");
                return;
            }

            Grid opponentGrid = _gameManager.OpponentGrid;

            if (opponentGrid == null)
            {
                Debug.LogError("[TestController] Opponent grid not initialized! Run Step 1 first.");
                return;
            }

            Debug.Log("[TestController] === Placing Test Words ===");

            bool success1 = WordPlacer.TryPlaceWord(opponentGrid, _word1, new Vector2Int(0, 0), WordDirection.Horizontal, out Word placedWord1);
            Debug.Log($"[TestController] '{_word1}' at (0,0): {(success1 ? "[SUCCESS]" : "[FAILED]")}");

            bool success2 = WordPlacer.TryPlaceWord(opponentGrid, _word2, new Vector2Int(0, 1), WordDirection.Horizontal, out Word placedWord2);
            Debug.Log($"[TestController] '{_word2}' at (0,1): {(success2 ? "[SUCCESS]" : "[FAILED]")}");

            bool success3 = WordPlacer.TryPlaceWord(opponentGrid, _word3, new Vector2Int(0, 2), WordDirection.Horizontal, out Word placedWord3);
            Debug.Log($"[TestController] '{_word3}' at (0,2): {(success3 ? "[SUCCESS]" : "[FAILED]")}");

            Debug.Log($"[TestController] Total words placed: {opponentGrid.PlacedWords.Count}");
        }

        #endregion

        #region Step 3: Start Gameplay

        [Title("STEP 3: Enter Gameplay")]
        [Button("3. Complete Setup & Start Game", ButtonSizes.Large)]
        [GUIColor(1f, 0.8f, 0.5f)]
        private void Step3_StartGameplay()
        {
            if (_stateMachine == null)
            {
                Debug.LogError("[TestController] GameStateMachine reference missing!");
                return;
            }

            _stateMachine.CompleteSetup();
            Debug.Log("[TestController] [SUCCESS] Gameplay started");
        }

        #endregion

        #region Step 4: Test Guessing

        [Title("STEP 4: Test Guessing Mechanics")]

        [Button("Show Opponent Words")]
        private void ShowOpponentWords()
        {
            if (_gameManager == null || _gameManager.OpponentGrid == null)
            {
                Debug.LogWarning("[TestController] Grid not ready!");
                return;
            }

            var words = _gameManager.OpponentGrid.PlacedWords;
            if (words.Count == 0)
            {
                Debug.LogWarning("[TestController] No words on opponent grid!");
                return;
            }

            Debug.Log("[TestController] === Opponent's Words ===");
            for (int i = 0; i < words.Count; i++)
            {
                Debug.Log($"  {i + 1}. {words[i].Text} - Revealed: {words[i].IsFullyRevealed}");
            }
        }

        [Button("Test Letter Guess (Hit)", ButtonSizes.Medium)]
        [GUIColor(0.3f, 1f, 0.3f)]
        private void TestLetterGuessHit()
        {
            char testLetter = 'A'; // Should hit CAT, CATS, DOGS
            Debug.Log($"[TestController] Testing letter '{testLetter}'...");
            bool result = _gameManager.ProcessLetterGuess(_turnManager.CurrentPlayerIndex, _gameManager.OpponentGrid, testLetter);
            Debug.Log($"[TestController] Result: {(result ? "[HIT]" : "[MISS]")}");
        }

        [Button("Test Letter Guess (Miss)", ButtonSizes.Medium)]
        [GUIColor(1f, 0.6f, 0.3f)]
        private void TestLetterGuessMiss()
        {
            char testLetter = 'Z'; // Should miss
            Debug.Log($"[TestController] Testing letter '{testLetter}'...");
            bool result = _gameManager.ProcessLetterGuess(_turnManager.CurrentPlayerIndex, _gameManager.OpponentGrid, testLetter);
            Debug.Log($"[TestController] Result: {(result ? "[HIT]" : "[MISS]")}");
        }

        [Button("Test Coordinate Guess (Hit)", ButtonSizes.Medium)]
        [GUIColor(0.3f, 1f, 0.3f)]
        private void TestCoordinateGuessHit()
        {
            Vector2Int coord = new Vector2Int(0, 0); // Should hit first letter of CAT
            Debug.Log($"[TestController] Testing coordinate {coord}...");
            bool result = _gameManager.ProcessCoordinateGuess(_turnManager.CurrentPlayerIndex, _gameManager.OpponentGrid, coord);
            Debug.Log($"[TestController] Result: {(result ? "[HIT]" : "[MISS]")}");
        }

        [Button("Test Coordinate Guess (Miss)", ButtonSizes.Medium)]
        [GUIColor(1f, 0.6f, 0.3f)]
        private void TestCoordinateGuessMiss()
        {
            Vector2Int coord = new Vector2Int(5, 5); // Should miss (empty cell)
            Debug.Log($"[TestController] Testing coordinate {coord}...");
            bool result = _gameManager.ProcessCoordinateGuess(_turnManager.CurrentPlayerIndex, _gameManager.OpponentGrid, coord);
            Debug.Log($"[TestController] Result: {(result ? "[HIT]" : "[MISS]")}");
        }

        [Button("Test Correct Word Guess", ButtonSizes.Large)]
        [GUIColor(0.3f, 1f, 0.3f)]
        private void TestCorrectWordGuess()
        {
            if (_gameManager.OpponentGrid == null || _gameManager.OpponentGrid.PlacedWords.Count == 0)
            {
                Debug.LogError("[TestController] No words on opponent grid!");
                return;
            }

            Word firstWord = _gameManager.OpponentGrid.PlacedWords[0];
            Debug.Log($"[TestController] Testing CORRECT word guess: '{firstWord.Text}'");
            bool result = _gameManager.ProcessWordGuess(_turnManager.CurrentPlayerIndex, _gameManager.OpponentGrid, firstWord.Text);
            Debug.Log($"[TestController] Result: {(result ? "[CORRECT]" : "[WRONG]")}");
        }

        [Button("Test Wrong Word Guess (+2 Misses)", ButtonSizes.Large)]
        [GUIColor(1f, 0.3f, 0.3f)]
        private void TestWrongWordGuess()
        {
            string wrongWord = "ZZZZZ";
            Debug.Log($"[TestController] Testing WRONG word guess: '{wrongWord}'");
            bool result = _gameManager.ProcessWordGuess(_turnManager.CurrentPlayerIndex, _gameManager.OpponentGrid, wrongWord);
            Debug.Log($"[TestController] Result: {(result ? "[UNEXPECTED SUCCESS]" : "[CORRECTLY FAILED]")}");
        }

        [Button("Test Custom Word Guess")]
        private void TestCustomWordGuess()
        {
            if (string.IsNullOrWhiteSpace(_customWordGuess))
            {
                Debug.LogError("[TestController] Enter a word in Custom Word Guess field!");
                return;
            }

            Debug.Log($"[TestController] Testing custom word: '{_customWordGuess}'");
            bool result = _gameManager.ProcessWordGuess(_turnManager.CurrentPlayerIndex, _gameManager.OpponentGrid, _customWordGuess);
            Debug.Log($"[TestController] Result: {(result ? "[CORRECT]" : "[WRONG]")}");
        }

        #endregion

        #region Utilities

        [Title("Utilities")]

        [Button("Force Next Turn")]
        private void ForceNextTurn()
        {
            _turnManager.EndTurn();
            Debug.Log($"[TestController] Turn ended. Current player: {_turnManager.CurrentPlayerIndex}");
        }

        [Button("Reset Miss Count")]
        private void ResetMissCount()
        {
            // This would require access to the MissCount IntVariableSO
            Debug.Log("[TestController] Reset miss count (requires direct SO reference)");
        }

        [Button("Show Game State")]
        private void ShowGameState()
        {
            Debug.Log("[TestController] === Current Game State ===");
            Debug.Log($"  Phase: {_stateMachine.CurrentPhase}");
            Debug.Log($"  Current Player: {_turnManager.CurrentPlayerIndex}");
            Debug.Log($"  Player Grid Words: {_gameManager.PlayerGrid?.PlacedWords.Count ?? 0}");
            Debug.Log($"  Opponent Grid Words: {_gameManager.OpponentGrid?.PlacedWords.Count ?? 0}");
        }

        #endregion
    }
}