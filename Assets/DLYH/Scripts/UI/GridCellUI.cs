using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Represents a single cell in the grid UI
    /// Handles visual states and click interactions
    /// Supports hidden letters for opponent grids in gameplay mode.
    /// </summary>
    public class GridCellUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Serialized Fields
        [SerializeField] private Image _background;
        [SerializeField] private TextMeshProUGUI _letterText;
        [SerializeField] private Button _button;

        [Header("Gameplay Colors")]
        [SerializeField] private Color _hitColor = new Color(0.5f, 1f, 0.5f, 1f);
        [SerializeField] private Color _missColor = new Color(1f, 0.5f, 0.5f, 1f);
        #endregion

        #region Private Fields
        private int _column;
        private int _row;
        private CellState _currentState = CellState.Empty;
        private bool _isHovered;
        private bool _isHighlighted;
        private Color _highlightColor;
        private char _currentLetter;

        // Gameplay mode fields
        private char _hiddenLetter = '\0';
        private bool _isLetterHidden = false;
        private bool _hasBeenGuessed = false;
        #endregion

        #region Events
        /// <summary>
        /// Fired when the cell is clicked. Parameters: column, row
        /// </summary>
        public System.Action<int, int> OnCellClicked;

        /// <summary>
        /// Fired when mouse enters the cell. Parameters: column, row
        /// </summary>
        public System.Action<int, int> OnCellHoverEnter;

        /// <summary>
        /// Fired when mouse exits the cell. Parameters: column, row
        /// </summary>
        public System.Action<int, int> OnCellHoverExit;
        #endregion

        #region Properties
        public int Column => _column;
        public int Row => _row;
        public CellState CurrentState => _currentState;
        public bool IsHighlighted => _isHighlighted;

        /// <summary>
        /// Returns true if this cell has a hidden (unrevealed) letter.
        /// </summary>
        public bool HasHiddenLetter => _isLetterHidden && _hiddenLetter != '\0';

        /// <summary>
        /// Returns true if this cell has been guessed in gameplay.
        /// </summary>
        public bool HasBeenGuessed => _hasBeenGuessed;
        #endregion

        #region Initialization
        public void Initialize(int column, int row)
        {
            _column = column;
            _row = row;
            _currentLetter = '\0';
            _hiddenLetter = '\0';
            _isLetterHidden = false;
            _hasBeenGuessed = false;

            if (_button != null)
            {
                _button.onClick.AddListener(HandleClick);
            }

            SetState(CellState.Empty);
        }
        #endregion

        #region State Management
        public void SetState(CellState state)
        {
            _currentState = state;
            _isHighlighted = false;
            UpdateVisuals();
        }

        public void SetLetter(char letter)
        {
            _currentLetter = letter;
            _isLetterHidden = false;
            if (_letterText != null)
            {
                _letterText.text = letter.ToString();
            }
        }

        public char GetLetter()
        {
            return _currentLetter;
        }

        public void ClearLetter()
        {
            _currentLetter = '\0';
            _hiddenLetter = '\0';
            _isLetterHidden = false;
            if (_letterText != null)
            {
                _letterText.text = "";
            }
        }

        public void SetColor(Color color)
        {
            if (_background != null)
            {
                _background.color = color;
            }
        }

        /// <summary>
        /// Sets a highlight color overlay on the cell.
        /// Used for placement mode preview (yellow/green/red).
        /// </summary>
        /// <param name="color">The highlight color to apply</param>
        public void SetHighlightColor(Color color)
        {
            _isHighlighted = true;
            _highlightColor = color;

            if (_background != null)
            {
                _background.color = color;
            }
        }

        /// <summary>
        /// Clears any highlight and returns to the current state's default color.
        /// </summary>
        public void ClearHighlight()
        {
            _isHighlighted = false;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_background == null) return;

            // If highlighted, don't override with state colors
            if (_isHighlighted)
            {
                _background.color = _highlightColor;
                return;
            }

            switch (_currentState)
            {
                case CellState.Empty:
                    _background.color = Color.white;
                    break;

                case CellState.Filled:
                    _background.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                    break;

                case CellState.Active:
                    _background.color = Color.yellow;
                    break;

                case CellState.ValidPlacement:
                    _background.color = Color.green;
                    break;

                case CellState.InvalidPlacement:
                    _background.color = Color.red;
                    break;

                case CellState.Hover:
                    _background.color = Color.cyan;
                    break;
            }
        }
        #endregion

        #region Gameplay Mode Methods
        /// <summary>
        /// Sets a hidden letter that wont be displayed until revealed.
        /// Used for opponent grids in gameplay mode.
        /// </summary>
        public void SetHiddenLetter(char letter)
        {
            _hiddenLetter = char.ToUpper(letter);
            _isLetterHidden = true;
            _currentLetter = '\0';

            if (_letterText != null)
            {
                _letterText.text = "";
            }
        }

        /// <summary>
        /// Reveals the hidden letter if one exists.
        /// Returns the revealed letter or null char if none.
        /// </summary>
        public char RevealHiddenLetter()
        {
            if (_isLetterHidden && _hiddenLetter != '\0')
            {
                char revealed = _hiddenLetter;
                SetLetter(_hiddenLetter);
                _isLetterHidden = false;
                _hasBeenGuessed = true;
                return revealed;
            }
            return '\0';
        }

        /// <summary>
        /// Marks this cell as guessed (for gameplay tracking).
        /// </summary>
        public void MarkAsGuessed(bool isHit)
        {
            _hasBeenGuessed = true;

            if (_background != null)
            {
                _background.color = isHit ? _hitColor : _missColor;
            }
        }

        /// <summary>
        /// Resets the cell to its initial state (including gameplay state).
        /// </summary>
        public void Reset()
        {
            ClearLetter();
            SetState(CellState.Empty);
            _hasBeenGuessed = false;
        }
        #endregion

        #region Event Handlers
        private void HandleClick()
        {
            OnCellClicked?.Invoke(_column, _row);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            OnCellHoverEnter?.Invoke(_column, _row);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            OnCellHoverExit?.Invoke(_column, _row);
        }
        #endregion

        #region Cleanup
        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClick);
            }
        }
        #endregion
    }

    /// <summary>
    /// Visual states for grid cells
    /// </summary>
    public enum CellState
    {
        Empty,              // Default empty cell
        Filled,             // Cell contains a letter
        Active,             // Currently selected cell (yellow)
        ValidPlacement,     // Valid position for next letter (green)
        InvalidPlacement,   // Invalid position for next letter (red)
        Hover               // Mouse hovering over cell
    }
}