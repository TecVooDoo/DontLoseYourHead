using UnityEngine;
using Sirenix.OdinInspector;

namespace TecVooDoo.DontLoseYourHead.Core
{
    public class WordPlacementTest : MonoBehaviour
    {
        [Title("Dependencies")]
        [Required]
        [SerializeField] private GameManager _gameManager;

        [Title("Test Words")]
        [SerializeField] private string _word1 = "CAT";
        [SerializeField] private string _word2 = "DOGS";
        [SerializeField] private string _word3 = "BIRDS";

        [Button("Test Word Placement", ButtonSizes.Large)]
        private void TestWordPlacement()
        {
            if (_gameManager == null)
            {
                Debug.LogError("GameManager reference is missing!");
                return;
            }

            Grid testGrid = _gameManager.OpponentGrid;

            if (testGrid == null)
            {
                Debug.LogError("Player grid not initialized! Enter Play Mode and click Initialize Game first.");
                return;
            }

            Debug.Log("=== Testing Word Placement ===");

            bool success1 = WordPlacer.TryPlaceWord(testGrid, _word1, new Vector2Int(0, 0), WordDirection.Horizontal, out Word placedWord1);
            Debug.Log($"Placing '{_word1}' at (0,0) Horizontal: {(success1 ? "SUCCESS" : "FAILED")}");

            bool success2 = WordPlacer.TryPlaceWord(testGrid, _word2, new Vector2Int(0, 1), WordDirection.Horizontal, out Word placedWord2);
            Debug.Log($"Placing '{_word2}' at (0,1) Horizontal: {(success2 ? "SUCCESS" : "FAILED")}");

            bool success3 = WordPlacer.TryPlaceWord(testGrid, _word3, new Vector2Int(0, 2), WordDirection.Horizontal, out Word placedWord3);
            Debug.Log($"Placing '{_word3}' at (0,2) Horizontal: {(success3 ? "SUCCESS" : "FAILED")}");

            Debug.Log($"Total words placed: {testGrid.PlacedWords.Count}");

            foreach (var word in testGrid.PlacedWords)
            {
                Debug.Log($"Word: {word.Text}, Position: {word.StartPosition}, Direction: {word.Direction}, Cells: {word.OccupiedCells.Count}");
            }
        }
    }
}