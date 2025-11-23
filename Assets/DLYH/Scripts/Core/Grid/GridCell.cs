using UnityEngine;
using Sirenix.OdinInspector;

namespace TecVooDoo.DontLoseYourHead.Core
{
    // REMOVED [System.Serializable] - this is runtime-only data
    public class GridCell
    {
        [ReadOnly]
        [ShowInInspector]
        private Vector2Int _coordinate;

        [ReadOnly]
        [ShowInInspector]
        private char? _letter;

        [ReadOnly]
        [ShowInInspector]
        private CellState _state = CellState.Hidden;

        private Word _belongsToWord;

        public Vector2Int Coordinate => _coordinate;
        public char? Letter => _letter;
        public CellState State => _state;
        public Word BelongsToWord => _belongsToWord;
        public bool IsEmpty => _letter == null;

        public GridCell(Vector2Int coordinate)
        {
            _coordinate = coordinate;
            _letter = null;
            _state = CellState.Hidden;
        }

        public void SetLetter(char letter, Word word)
        {
            _letter = letter;
            _belongsToWord = word;
        }

        public void SetState(CellState newState)
        {
            _state = newState;
        }
    }
}