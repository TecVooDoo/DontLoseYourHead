using UnityEngine;
using Sirenix.OdinInspector;

namespace TecVooDoo.DontLoseYourHead.Core
{
    /// <summary>
    /// Test script to verify turn validation works correctly
    /// </summary>
    public class TurnValidationTest : MonoBehaviour
    {
        [Title("Dependencies")]
        [Required]
        [SerializeField] private GameManager _gameManager;
        
        [Title("Test Configuration")]
        [SerializeField] private char _testLetter = 'A';
        [SerializeField] private Vector2Int _testCoordinate = new Vector2Int(0, 0);
        
        [Title("Test Controls")]
        [Button("Player 0 Guess Letter")]
        private void TestPlayer0GuessLetter()
        {
            Debug.Log($"=== Testing Player 0 Letter Guess ===");
            bool result = _gameManager.ProcessLetterGuess(0, _gameManager.OpponentGrid, _testLetter);
            Debug.Log($"Player 0 guessed '{_testLetter}': {(result ? "HIT" : "MISS")}");
        }
        
        [Button("Player 1 Guess Letter")]
        private void TestPlayer1GuessLetter()
        {
            Debug.Log($"=== Testing Player 1 Letter Guess ===");
            bool result = _gameManager.ProcessLetterGuess(1, _gameManager.PlayerGrid, _testLetter);
            Debug.Log($"Player 1 guessed '{_testLetter}': {(result ? "HIT" : "MISS")}");
        }
        
        [Button("Player 0 Guess Coordinate")]
        private void TestPlayer0GuessCoordinate()
        {
            Debug.Log($"=== Testing Player 0 Coordinate Guess ===");
            bool result = _gameManager.ProcessCoordinateGuess(0, _gameManager.OpponentGrid, _testCoordinate);
            Debug.Log($"Player 0 guessed {_testCoordinate}: {(result ? "HIT" : "MISS")}");
        }
        
        [Button("Player 1 Guess Coordinate")]
        private void TestPlayer1GuessCoordinate()
        {
            Debug.Log($"=== Testing Player 1 Coordinate Guess ===");
            bool result = _gameManager.ProcessCoordinateGuess(1, _gameManager.PlayerGrid, _testCoordinate);
            Debug.Log($"Player 1 guessed {_testCoordinate}: {(result ? "HIT" : "MISS")}");
        }
        
        [Title("Info")]
        [ShowInInspector]
        [ReadOnly]
        private string CurrentTurnInfo => $"Current Turn: Player {_gameManager.CurrentPlayerIndex}";
        
        [ShowInInspector]
        [ReadOnly]
        private string MissCountInfo => $"Misses: {GetMissCount()} / {_gameManager.MaxMisses}";
        
        private int GetMissCount()
        {
            // Try to get miss count from GameManager's ScriptableObject
            // This is a bit hacky for testing, but works
            return 0; // Will show in logs anyway
        }
    }
}
