using UnityEngine;

namespace TecVooDoo.DontLoseYourHead.Core
{
    public static class WordPlacer
    {
        public static bool TryPlaceWord(Grid grid, string wordText, Vector2Int startPosition, WordDirection direction, out Word placedWord)
        {
            placedWord = null;

            if (!WordPlacementValidator.CanPlaceWord(grid, wordText, startPosition, direction))
            {
                return false;
            }

            Word word = new Word(wordText, startPosition, direction);
            Vector2Int[] cellPositions = WordPlacementValidator.GetWordCellPositions(startPosition, wordText.Length, direction);

            for (int i = 0; i < cellPositions.Length; i++)
            {
                GridCell cell = grid.GetCell(cellPositions[i]);
                char letter = wordText.ToUpper()[i];

                if (cell.IsEmpty)
                {
                    cell.SetLetter(letter, word);
                }

                word.AddOccupiedCell(cell);
            }

            grid.AddWord(word);
            placedWord = word;
            return true;
        }
    }
}