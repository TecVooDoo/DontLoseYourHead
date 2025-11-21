using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace TecVooDoo.DontLoseYourHead.Core
{
    [System.Serializable]
    public class Word
    {
        [ReadOnly]
        [SerializeField] private string _text;

        [ReadOnly]
        [SerializeField] private Vector2Int _startPosition;

        [ReadOnly]
        [SerializeField] private WordDirection _direction;

        [ReadOnly]
        [SerializeField] private bool _isFullyRevealed;

        private List<GridCell> _occupiedCells = new List<GridCell>();

        public string Text => _text;
        public Vector2Int StartPosition => _startPosition;
        public WordDirection Direction => _direction;
        public bool IsFullyRevealed => _isFullyRevealed;
        public int Length => _text.Length;
        public IReadOnlyList<GridCell> OccupiedCells => _occupiedCells;

        public Word(string text, Vector2Int startPosition, WordDirection direction)
        {
            _text = text.ToUpper();
            _startPosition = startPosition;
            _direction = direction;
            _isFullyRevealed = false;
        }

        public void AddOccupiedCell(GridCell cell)
        {
            if (!_occupiedCells.Contains(cell))
            {
                _occupiedCells.Add(cell);
            }
        }

        public void CheckIfFullyRevealed()
        {
            _isFullyRevealed = true;

            foreach (var cell in _occupiedCells)
            {
                if (cell.State == CellState.Hidden || cell.State == CellState.PartiallyKnown)
                {
                    _isFullyRevealed = false;
                    break;
                }
            }
        }
    }
}
