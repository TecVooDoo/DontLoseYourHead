using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace TecVooDoo.DontLoseYourHead.Core
{
    // REMOVED [System.Serializable] - this is runtime-only data
    public class Grid
    {
        [ReadOnly]
        [ShowInInspector]
        private int _size;

        private GridCell[,] _cells;
        private List<Word> _placedWords = new List<Word>();

        public int Size => _size;
        public GridCell[,] Cells => _cells;
        public IReadOnlyList<Word> PlacedWords => _placedWords;

        public Grid(int size)
        {
            _size = size;
            _cells = new GridCell[size, size];
            InitializeCells();
        }

        private void InitializeCells()
        {
            for (int x = 0; x < _size; x++)
            {
                for (int y = 0; y < _size; y++)
                {
                    _cells[x, y] = new GridCell(new Vector2Int(x, y));
                }
            }
        }

        public GridCell GetCell(int x, int y)
        {
            if (IsValidCoordinate(x, y))
            {
                return _cells[x, y];
            }
            return null;
        }

        public GridCell GetCell(Vector2Int coordinate)
        {
            return GetCell(coordinate.x, coordinate.y);
        }

        public bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < _size && y >= 0 && y < _size;
        }

        public bool IsValidCoordinate(Vector2Int coordinate)
        {
            return IsValidCoordinate(coordinate.x, coordinate.y);
        }

        public void AddWord(Word word)
        {
            if (!_placedWords.Contains(word))
            {
                _placedWords.Add(word);
            }
        }
    }
}