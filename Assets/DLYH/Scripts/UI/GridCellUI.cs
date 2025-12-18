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
        [SerializeField] private Color _hitUnknownLetterColor = new Color(1f, 0.85f, 0.4f, 1f); // Yellow/orange for hit but letter unknown
        #endregion

        #region Private Fields
        private int _column;
        private int _row;
        private CellState _currentState = CellState.Empty;
        private bool _isHighlighted;
        private Color _highlightColor;
        private char _currentLetter;

        // Gameplay mode fields
        private char _hiddenLetter = '\0';
        private bool _isLetterHidden = false;
        private bool _hasBeenGuessed = false;
        private bool _isHitButLetterUnknown = false; // Track if hit but letter not yet guessed
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

        /// <summary>
        /// Returns true if this cell was hit but the letter is not yet known.
        /// </summary>
        public bool IsHitButLetterUnknown => _isHitButLetterUnknown;
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
            _isHitButLetterUnknown = false;

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

        /// <summary>
        /// Sets the hit color used when marking cells as guessed hits.
        /// Called during panel setup to apply the player's chosen color.
        /// </summary>
        public void SetHitColor(Color color)
        {
            _hitColor = color;
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
        /// <param name="isHit">True if coordinate hit a letter, false if miss</param>
        public void MarkAsGuessed(bool isHit)
        {
            _hasBeenGuessed = true;
            _isHitButLetterUnknown = false; // Clear this flag when using standard marking

            if (_background != null)
            {
                _background.color = isHit ? _hitColor : _missColor;
            }
        }

        /// <summary>
        /// Marks this cell as hit but with letter unknown (yellow/orange color).
        /// Used when opponent guesses a coordinate hit but hasn't guessed the letter yet.
        /// </summary>
        public void MarkAsHitButLetterUnknown()
        {
            _hasBeenGuessed = true;
            _isHitButLetterUnknown = true;

            if (_background != null)
            {
                _background.color = _hitUnknownLetterColor;
            }
        }

        /// <summary>
        /// Upgrades a "hit but letter unknown" cell to fully known (green).
        /// Called when the letter is later guessed correctly.
        /// </summary>
        public void UpgradeToKnownHit()
        {
            if (_isHitButLetterUnknown)
            {
                _isHitButLetterUnknown = false;
                if (_background != null)
                {
                    _background.color = _hitColor;
                }
            }
        }

        /// <summary>
        /// Marks this cell as revealed at end of game (yellow color, shows letter).
        /// Only applies if the cell has not been guessed during gameplay.
        /// </summary>
        public void MarkAsRevealed()
        {
            if (_hasBeenGuessed) return; // Do not override gameplay results

            _hasBeenGuessed = true;
            _isHitButLetterUnknown = false;

            if (_background != null)
            {
                _background.color = _hitUnknownLetterColor; // Yellow
            }

            // Reveal the hidden letter
            RevealHiddenLetter();
        }

        /// <summary>
        /// Reveals the hidden letter on this cell at end of game without changing color.
        /// Works on any cell that has a hidden letter (yellow cells, unrevealed cells, etc.)
        /// </summary>
        public void RevealHiddenLetterKeepColor()
        {
            // Reveal the hidden letter without changing color
            if (_isLetterHidden && _hiddenLetter != '\0')
            {
                SetLetter(_hiddenLetter);
                _isLetterHidden = false;
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
            _isHitButLetterUnknown = false;
        }
        #endregion

        #region Event Handlers
        private void HandleClick()
        {
            // Play grid cell click sound
            DLYH.Audio.UIAudioManager.GridCellClick();

            OnCellClicked?.Invoke(_column, _row);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnCellHoverEnter?.Invoke(_column, _row);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
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
