using UnityEngine;

namespace TecVooDoo.DontLoseYourHead.Core
{
    public static class WordPlacementValidator
    {
        public static bool CanPlaceWord(Grid grid, string word, Vector2Int startPosition, WordDirection direction)
        {
            Vector2Int[] cellPositions = GetWordCellPositions(startPosition, word.Length, direction);

            foreach (var position in cellPositions)
            {
                if (!grid.IsValidCoordinate(position))
                {
                    return false;
                }
            }

            for (int i = 0; i < cellPositions.Length; i++)
            {
                GridCell cell = grid.GetCell(cellPositions[i]);
                char currentLetter = word[i];

                if (!cell.IsEmpty)
                {
                    if (cell.Letter != currentLetter)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static Vector2Int[] GetWordCellPositions(Vector2Int startPosition, int wordLength, WordDirection direction)
        {
            Vector2Int[] positions = new Vector2Int[wordLength];
            Vector2Int directionVector = GetDirectionVector(direction);

            for (int i = 0; i < wordLength; i++)
            {
                positions[i] = startPosition + (directionVector * i);
            }

            return positions;
        }

        public static Vector2Int GetDirectionVector(WordDirection direction)
        {
            return direction switch
            {
                WordDirection.Horizontal => new Vector2Int(1, 0),
                WordDirection.Vertical => new Vector2Int(0, 1),
                WordDirection.DiagonalDownRight => new Vector2Int(1, 1),
                WordDirection.DiagonalDownLeft => new Vector2Int(-1, 1),
                WordDirection.HorizontalReverse => new Vector2Int(-1, 0),
                WordDirection.VerticalReverse => new Vector2Int(0, -1),
                WordDirection.DiagonalUpRight => new Vector2Int(1, -1),
                WordDirection.DiagonalUpLeft => new Vector2Int(-1, -1),
                _ => Vector2Int.zero
            };
        }
    }
}