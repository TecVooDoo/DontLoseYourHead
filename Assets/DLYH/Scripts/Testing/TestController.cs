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

        [Required]
        [SerializeField] private PlayerManager _playerManager;

        [Title("Test Configuration")]
        [SerializeField] private string _word1 = "CAT";
        [SerializeField] private string _word2 = "DOGS";
        [SerializeField] private string _word3 = "BIRDS";
        [SerializeField] private string _customWordGuess = "CAT";
        [SerializeField] private char _testLetter = 'A';
        [SerializeField] private Vector2Int _testCoordinate = new Vector2Int(0, 0);

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

            // Reset player states first
            if (_playerManager != null)
            {
                _playerManager.ResetPlayers();
            }

            // Call GameManager's initialization
            _gameManager.InitializeGame();
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
            Debug.Log(string.Format("[TestController] '{0}' at (0,0): {1}", _word1, success1 ? "[SUCCESS]" : "[FAILED]"));

            bool success2 = WordPlacer.TryPlaceWord(opponentGrid, _word2, new Vector2Int(0, 1), WordDirection.Horizontal, out Word placedWord2);
            Debug.Log(string.Format("[TestController] '{0}' at (0,1): {1}", _word2, success2 ? "[SUCCESS]" : "[FAILED]"));

            bool success3 = WordPlacer.TryPlaceWord(opponentGrid, _word3, new Vector2Int(0, 2), WordDirection.Horizontal, out Word placedWord3);
            Debug.Log(string.Format("[TestController] '{0}' at (0,2): {1}", _word3, success3 ? "[SUCCESS]" : "[FAILED]"));

            Debug.Log(string.Format("[TestController] Total words placed: {0}", opponentGrid.PlacedWords.Count));
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
                Debug.Log(string.Format("  {0}. {1} - Revealed: {2}", i + 1, words[i].Text, words[i].IsFullyRevealed));
            }
        }

        [Button("Test Letter Guess (Hit)", ButtonSizes.Medium)]
        [GUIColor(0.3f, 1f, 0.3f)]
        private void TestLetterGuessHit()
        {
            char testLetter = 'A'; // Should hit CAT, DOGS, BIRDS
            Debug.Log(string.Format("[TestController] Testing letter '{0}'...", testLetter));
            bool result = _gameManager.ProcessLetterGuess(_turnManager.CurrentPlayerIndex, _gameManager.OpponentGrid, testLetter);
            Debug.Log(string.Format("[TestController] Result: {0}", result ? "[HIT]" : "[MISS]"));
        }

        [Button("Test Letter Guess (Miss)", ButtonSizes.Medium)]
        [GUIColor(1f, 0.6f, 0.3f)]
        private void TestLetterGuessMiss()
        {
            char testLetter = 'Z'; // Should miss
            Debug.Log(string.Format("[TestController] Testing letter '{0}'...", testLetter));
            bool result = _gameManager.ProcessLetterGuess(_turnManager.CurrentPlayerIndex, _gameManager.OpponentGrid, testLetter);
            Debug.Log(string.Format("[TestController] Result: {0}", result ? "[HIT]" : "[MISS]"));
        }

        [Button("Test Custom Letter Guess")]
        private void TestCustomLetterGuess()
        {
            Debug.Log(string.Format("[TestController] Testing letter '{0}'...", _testLetter));
            bool result = _gameManager.ProcessLetterGuess(_turnManager.CurrentPlayerIndex, _gameManager.OpponentGrid, _testLetter);
            Debug.Log(string.Format("[TestController] Result: {0}", result ? "[HIT]" : "[MISS]"));
        }

        [Button("Test Coordinate Guess (Hit)", ButtonSizes.Medium)]
        [GUIColor(0.3f, 1f, 0.3f)]
        private void TestCoordinateGuessHit()
        {
            Vector2Int coord = new Vector2Int(0, 0); // Should hit first letter of CAT
            Debug.Log(string.Format("[TestController] Testing coordinate {0}...", coord));
            bool result = _gameManager.ProcessCoordinateGuess(_turnManager.CurrentPlayerIndex, _gameManager.OpponentGrid, coord);
            Debug.Log(string.Format("[TestController] Result: {0}", result ? "[HIT]" : "[MISS]"));
        }

        [Button("Test Coordinate Guess (Miss)", ButtonSizes.Medium)]
        [GUIColor(1f, 0.6f, 0.3f)]
        private void TestCoordinateGuessMiss()
        {
            Vector2Int coord = new Vector2Int(5, 5); // Should miss (empty cell)
            Debug.Log(string.Format("[TestController] Testing coordinate {0}...", coord));
            bool result = _gameManager.ProcessCoordinateGuess(_turnManager.CurrentPlayerIndex, _gameManager.OpponentGrid, coord);
            Debug.Log(string.Format("[TestController] Result: {0}", result ? "[HIT]" : "[MISS]"));
        }

        [Button("Test Custom Coordinate Guess")]
        private void TestCustomCoordinateGuess()
        {
            Debug.Log(string.Format("[TestController] Testing coordinate {0}...", _testCoordinate));
            bool result = _gameManager.ProcessCoordinateGuess(_turnManager.CurrentPlayerIndex, _gameManager.OpponentGrid, _testCoordinate);
            Debug.Log(string.Format("[TestController] Result: {0}", result ? "[HIT]" : "[MISS]"));
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
            Debug.Log(string.Format("[TestController] Testing CORRECT word guess: '{0}'", firstWord.Text));
            bool result = _gameManager.ProcessWordGuess(_turnManager.CurrentPlayerIndex, _gameManager.OpponentGrid, firstWord.Text);
            Debug.Log(string.Format("[TestController] Result: {0}", result ? "[CORRECT]" : "[WRONG]"));
        }

        [Button("Test Wrong Word Guess (+2 Misses)", ButtonSizes.Large)]
        [GUIColor(1f, 0.3f, 0.3f)]
        private void TestWrongWordGuess()
        {
            string wrongWord = "ZZZZZ";
            Debug.Log(string.Format("[TestController] Testing WRONG word guess: '{0}'", wrongWord));
            bool result = _gameManager.ProcessWordGuess(_turnManager.CurrentPlayerIndex, _gameManager.OpponentGrid, wrongWord);
            Debug.Log(string.Format("[TestController] Result: {0}", result ? "[UNEXPECTED SUCCESS]" : "[CORRECTLY FAILED]"));
        }

        [Button("Test Custom Word Guess")]
        private void TestCustomWordGuess()
        {
            if (string.IsNullOrWhiteSpace(_customWordGuess))
            {
                Debug.LogError("[TestController] Enter a word in Custom Word Guess field!");
                return;
            }

            Debug.Log(string.Format("[TestController] Testing custom word: '{0}'", _customWordGuess));
            bool result = _gameManager.ProcessWordGuess(_turnManager.CurrentPlayerIndex, _gameManager.OpponentGrid, _customWordGuess);
            Debug.Log(string.Format("[TestController] Result: {0}", result ? "[CORRECT]" : "[WRONG]"));
        }

        #endregion

        #region Step 5: Test Reveal Mechanics

        [Title("STEP 5: Test Letter Reveal Mechanics")]
        [InfoBox("These tests verify that * symbols upgrade to letters when the letter becomes known")]

        [Button("Scenario A: Coord First, Then Letter", ButtonSizes.Large)]
        [GUIColor(0.8f, 0.5f, 1f)]
        private void TestScenarioA_CoordThenLetter()
        {
            Debug.Log("[TestController] === SCENARIO A: Coordinate first, then Letter ===");
            Debug.Log("[TestController] Expected: Coord hit shows *, then letter guess upgrades * to letter");
            Debug.Log("[TestController] ");
            Debug.Log("[TestController] Step 1: Guess coordinate (0,0) - should be C from CAT");
            Debug.Log("[TestController] Step 2: Guess letter 'C' - should upgrade the * at (0,0) to 'C'");
            Debug.Log("[TestController] ");
            Debug.Log("[TestController] Use the custom coordinate/letter fields and Print Grid between steps to verify.");
        }

        [Button("Scenario B: Letter First, Then Coord", ButtonSizes.Large)]
        [GUIColor(0.8f, 0.5f, 1f)]
        private void TestScenarioB_LetterThenCoord()
        {
            Debug.Log("[TestController] === SCENARIO B: Letter first, then Coordinate ===");
            Debug.Log("[TestController] Expected: Letter guess adds to known, coord guess shows letter immediately");
            Debug.Log("[TestController] ");
            Debug.Log("[TestController] Step 1: Guess letter 'A' - should HIT and add 'A' to known letters");
            Debug.Log("[TestController] Step 2: Guess coordinate (1,0) - should immediately show 'A' (not *)");
            Debug.Log("[TestController] ");
            Debug.Log("[TestController] Use the custom coordinate/letter fields and Print Grid between steps to verify.");
        }

        [Button("Scenario C: Word Guess Reveals All", ButtonSizes.Large)]
        [GUIColor(0.8f, 0.5f, 1f)]
        private void TestScenarioC_WordRevealsAll()
        {
            Debug.Log("[TestController] === SCENARIO C: Word guess reveals letters ===");
            Debug.Log("[TestController] Expected: Correct word guess reveals word AND upgrades other * cells with same letters");
            Debug.Log("[TestController] ");
            Debug.Log("[TestController] Step 1: Guess coordinate (0,1) - D from DOGS, shows as *");
            Debug.Log("[TestController] Step 2: Guess word 'HOLD' (wrong) - +2 misses, no reveal");
            Debug.Log("[TestController] Step 3: Guess word 'DOGS' (correct) - reveals DOGS and upgrades D at (0,1)");
            Debug.Log("[TestController] ");
            Debug.Log("[TestController] Use Print Grid between steps to verify state changes.");
        }

        #endregion

        #region Utilities

        [Title("Utilities")]

        [Button("Force Next Turn")]
        private void ForceNextTurn()
        {
            _turnManager.EndTurn();
            Debug.Log(string.Format("[TestController] Turn ended. Current player: {0}", _turnManager.CurrentPlayerIndex));
        }

        [Button("Show Known Letters")]
        private void ShowKnownLetters()
        {
            if (_playerManager == null)
            {
                Debug.LogWarning("[TestController] PlayerManager not assigned!");
                return;
            }

            PlayerSO player = _playerManager.GetPlayer(0);
            if (player == null)
            {
                Debug.LogWarning("[TestController] Player 0 not found!");
                return;
            }

            if (player.KnownLetters.Count == 0)
            {
                Debug.Log("[TestController] Player has no known letters yet.");
            }
            else
            {
                Debug.Log(string.Format("[TestController] Known Letters: {0}", string.Join(", ", player.KnownLetters)));
            }
        }

        [Button("Show Found Words")]
        private void ShowFoundWords()
        {
            if (_playerManager == null)
            {
                Debug.LogWarning("[TestController] PlayerManager not assigned!");
                return;
            }

            PlayerSO player = _playerManager.GetPlayer(0);
            if (player == null)
            {
                Debug.LogWarning("[TestController] Player 0 not found!");
                return;
            }

            if (player.FoundWords.Count == 0)
            {
                Debug.Log("[TestController] Player has not found any words yet.");
            }
            else
            {
                Debug.Log("[TestController] Found Words:");
                foreach (var word in player.FoundWords)
                {
                    Debug.Log(string.Format("  - {0}", word.Text));
                }
            }
        }

        [Button("Print Grid State", ButtonSizes.Large)]
        [GUIColor(0.5f, 0.5f, 1f)]
        private void PrintGridState()
        {
            _gameManager.DebugPrintOpponentGrid();
        }

        [Button("Show Game State")]
        private void ShowGameState()
        {
            Debug.Log("[TestController] === Current Game State ===");
            Debug.Log(string.Format("  Phase: {0}", _stateMachine.CurrentPhase));
            Debug.Log(string.Format("  Current Player: {0}", _turnManager.CurrentPlayerIndex));
            Debug.Log(string.Format("  Player Grid Words: {0}", _gameManager.PlayerGrid != null ? _gameManager.PlayerGrid.PlacedWords.Count : 0));
            Debug.Log(string.Format("  Opponent Grid Words: {0}", _gameManager.OpponentGrid != null ? _gameManager.OpponentGrid.PlacedWords.Count : 0));
            
            PlayerSO player = _playerManager.GetPlayer(0);
            if (player != null)
            {
                Debug.Log(string.Format("  Player 0 Misses: {0}/{1}", player.MissCount, _gameManager.MaxMisses));
                Debug.Log(string.Format("  Player 0 Known Letters: {0}", player.KnownLetters.Count));
                Debug.Log(string.Format("  Player 0 Found Words: {0}", player.FoundWords.Count));
            }
        }

        [Button("Full Reset", ButtonSizes.Large)]
        [GUIColor(1f, 0.3f, 0.3f)]
        private void FullReset()
        {
            Debug.Log("[TestController] === FULL RESET ===");
            
            // Reset players
            if (_playerManager != null)
            {
                _playerManager.ResetPlayers();
            }
            
            // Re-initialize game
            Step1_InitializeGame();
            
            Debug.Log("[TestController] Reset complete. Run steps 2 and 3 to set up again.");
        }

        #endregion
    }
}
