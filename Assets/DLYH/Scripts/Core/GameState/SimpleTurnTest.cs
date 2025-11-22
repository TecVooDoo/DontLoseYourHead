using UnityEngine;
using Sirenix.OdinInspector;

namespace TecVooDoo.DontLoseYourHead.Core
{
    /// <summary>
    /// Simple test script to verify turn validation
    /// </summary>
    public class SimpleTurnTest : MonoBehaviour
    {
        [Title("Dependencies")]
        [Required]
        [SerializeField] private GameManager _gameManager;

        [Title("Test Controls")]
        [Button("Player 0 Guess Letter 'A'", ButtonSizes.Large)]
        [GUIColor(0.3f, 0.8f, 1f)]
        private void TestPlayer0Guess()
        {
            if (_gameManager == null)
            {
                Debug.LogError("GameManager not assigned!");
                return;
            }

            Debug.Log("=== Player 0 attempting to guess letter 'A' ===");
            _gameManager.ProcessLetterGuess(0, _gameManager.OpponentGrid, 'A');
        }

        [Button("Player 1 Guess Letter 'B'", ButtonSizes.Large)]
        [GUIColor(0.8f, 1f, 0.3f)]
        private void TestPlayer1Guess()
        {
            if (_gameManager == null)
            {
                Debug.LogError("GameManager not assigned!");
                return;
            }

            Debug.Log("=== Player 1 attempting to guess letter 'B' ===");
            _gameManager.ProcessLetterGuess(1, _gameManager.PlayerGrid, 'B');
        }
    }
}