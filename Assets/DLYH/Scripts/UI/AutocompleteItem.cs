using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Individual item in the autocomplete dropdown.
    /// </summary>
    public class AutocompleteItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Serialized Fields
        [SerializeField]
        private TextMeshProUGUI _wordText;

        [SerializeField]
        private Image _backgroundImage;

        [SerializeField]
        private Button _button;

        [SerializeField]
        private Color _normalColor = new Color(1f, 1f, 1f, 1f);

        [SerializeField]
        private Color _hoverColor = new Color(0.9f, 0.95f, 1f, 1f);

        [SerializeField]
        private Color _selectedColor = new Color(0.8f, 0.9f, 1f, 1f);

        [SerializeField]
        private Color _matchedTextColor = new Color(0.2f, 0.4f, 0.8f, 1f);
        #endregion

        #region Private Fields
        private string _word;
        private bool _isSelected;
        private bool _isHovered;
        #endregion

        #region Events
        public event Action OnItemClicked;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(HandleClick);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClick);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the word to display, optionally highlighting the matched prefix.
        /// </summary>
        /// <param name="word">The word to display</param>
        /// <param name="matchedPrefix">The prefix that was matched (for highlighting)</param>
        public void SetWord(string word, string matchedPrefix = "")
        {
            _word = word;

            if (_wordText != null)
            {
                if (!string.IsNullOrEmpty(matchedPrefix) && word.StartsWith(matchedPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    // Highlight the matched portion
                    string matched = word.Substring(0, matchedPrefix.Length);
                    string remainder = word.Substring(matchedPrefix.Length);
                    string hexColor = ColorUtility.ToHtmlStringRGB(_matchedTextColor);
                    _wordText.text = string.Format("<color=#{0}>{1}</color>{2}", hexColor, matched, remainder);
                }
                else
                {
                    _wordText.text = word;
                }
            }
        }

        /// <summary>
        /// Sets whether this item is selected.
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateVisuals();
        }

        /// <summary>
        /// Gets the current word.
        /// </summary>
        public string Word => _word;
        #endregion

        #region IPointerEnterHandler / IPointerExitHandler
        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            UpdateVisuals();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            UpdateVisuals();
        }
        #endregion

        #region Private Methods
        private void CacheReferences()
        {
            if (_wordText == null)
            {
                _wordText = GetComponentInChildren<TextMeshProUGUI>();
            }

            if (_backgroundImage == null)
            {
                _backgroundImage = GetComponent<Image>();
            }

            if (_button == null)
            {
                _button = GetComponent<Button>();
            }
        }

        private void HandleClick()
        {
            OnItemClicked?.Invoke();
        }

        private void UpdateVisuals()
        {
            if (_backgroundImage == null) return;

            Color bgColor;

            if (_isSelected)
            {
                bgColor = _selectedColor;
            }
            else if (_isHovered)
            {
                bgColor = _hoverColor;
            }
            else
            {
                bgColor = _normalColor;
            }

            _backgroundImage.color = bgColor;
        }
        #endregion
    }
}
