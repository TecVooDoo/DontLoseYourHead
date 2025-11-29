using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Represents a single cell in the grid UI
    /// Handles visual states and click interactions
    /// </summary>
    public class GridCellUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Serialized Fields
        [SerializeField] private Image _background;
        [SerializeField] private TextMeshProUGUI _letterText;
        [SerializeField] private Button _button;
        #endregion

        #region Private Fields
        private int _column;
        private int _row;
        private CellState _currentState = CellState.Empty;
        private bool _isHovered;
        private bool _isHighlighted;
        private Color _highlightColor;
        private char _currentLetter;
        #endregion

        #region Events
        public System.Action OnCellClicked;
        public System.Action OnCellHoverEnter;
        public System.Action OnCellHoverExit;
        #endregion

        #region Properties
        public int Column => _column;
        public int Row => _row;
        public CellState CurrentState => _currentState;
        public bool IsHighlighted => _isHighlighted;
        #endregion

        #region Initialization
        public void Initialize(int column, int row)
        {
            _column = column;
            _row = row;
            _currentLetter = '\0';
            
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

        #region Event Handlers
        private void HandleClick()
        {
            OnCellClicked?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            OnCellHoverEnter?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            OnCellHoverExit?.Invoke();
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
