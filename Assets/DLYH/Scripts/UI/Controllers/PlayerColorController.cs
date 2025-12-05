using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Manages player color selection UI including button setup,
    /// outline highlighting for selection state, and color change events.
    /// </summary>
    public class PlayerColorController
    {
        #region Private Fields

        private readonly Transform _colorButtonsContainer;
        private readonly List<Button> _colorButtons = new List<Button>();
        private readonly List<Outline> _colorButtonOutlines = new List<Outline>();
        
        private int _currentColorIndex = 0;
        private Color _currentColor;

        #endregion

        #region Events

        /// <summary>Fired when player selects a new color</summary>
        public event Action<Color> OnColorChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new PlayerColorController
        /// </summary>
        /// <param name="colorButtonsContainer">Transform containing color button children</param>
        public PlayerColorController(Transform colorButtonsContainer)
        {
            _colorButtonsContainer = colorButtonsContainer;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes color buttons from the container, sets up click handlers and outlines
        /// </summary>
        public void Initialize()
        {
            _colorButtons.Clear();
            _colorButtonOutlines.Clear();

            if (_colorButtonsContainer == null)
            {
                Debug.LogWarning("[PlayerColorController] Color buttons container not assigned!");
                return;
            }

            // Get all buttons in the container
            for (int i = 0; i < _colorButtonsContainer.childCount; i++)
            {
                var child = _colorButtonsContainer.GetChild(i);
                var button = child.GetComponent<Button>();

                if (button != null)
                {
                    _colorButtons.Add(button);

                    // Get or add outline component for selection highlight
                    var outline = child.GetComponent<Outline>();
                    if (outline == null)
                    {
                        outline = child.gameObject.AddComponent<Outline>();
                    }
                    outline.effectColor = Color.white;
                    outline.effectDistance = new Vector2(3, 3);
                    outline.enabled = false;
                    _colorButtonOutlines.Add(outline);

                    // Capture index for closure
                    int colorIndex = i;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => SelectColor(colorIndex));
                }
            }

            Debug.Log($"[PlayerColorController] Set up {_colorButtons.Count} color buttons");

            // Select initial color (index 0)
            if (_colorButtons.Count > 0)
            {
                SelectColor(0);
            }
        }

        /// <summary>
        /// Selects a color by index, updates outline highlights, and fires event
        /// </summary>
        /// <param name="colorIndex">Index of the color button to select</param>
        public void SelectColor(int colorIndex)
        {
            if (colorIndex < 0 || colorIndex >= _colorButtons.Count)
            {
                Debug.LogWarning($"[PlayerColorController] Invalid color index: {colorIndex}");
                return;
            }

            _currentColorIndex = colorIndex;

            // Get the color from the button's Image component
            var buttonImage = _colorButtons[colorIndex].GetComponent<Image>();
            if (buttonImage != null)
            {
                _currentColor = buttonImage.color;
            }

            // Update outline selection states - highlight only the selected button
            for (int i = 0; i < _colorButtonOutlines.Count; i++)
            {
                if (_colorButtonOutlines[i] != null)
                {
                    _colorButtonOutlines[i].enabled = (i == colorIndex);
                }
            }

            OnColorChanged?.Invoke(_currentColor);
            Debug.Log($"[PlayerColorController] Color selected: index={_currentColorIndex}, color={_currentColor}");
        }

        /// <summary>
        /// Gets the currently selected color
        /// </summary>
        public Color GetCurrentColor() => _currentColor;

        /// <summary>
        /// Gets the currently selected color index
        /// </summary>
        public int GetCurrentColorIndex() => _currentColorIndex;

        /// <summary>
        /// Updates the outline highlight to match current selection (for UI refresh)
        /// </summary>
        public void RefreshSelectionVisual()
        {
            for (int i = 0; i < _colorButtonOutlines.Count; i++)
            {
                if (_colorButtonOutlines[i] != null)
                {
                    _colorButtonOutlines[i].enabled = (i == _currentColorIndex);
                }
            }
        }

        /// <summary>
        /// Cleans up button listeners
        /// </summary>
        public void Cleanup()
        {
            foreach (var button in _colorButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                }
            }
        }

        #endregion
    }
}
