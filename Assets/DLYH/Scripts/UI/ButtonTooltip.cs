// ButtonTooltip.cs
// Shows a tooltip on hover for UI buttons
// Created: December 16, 2025

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

namespace DLYH.UI
{
    /// <summary>
    /// Attach to any UI element to show a tooltip on hover.
    /// Requires a shared tooltip panel in the scene.
    /// </summary>
    public class ButtonTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string _tooltipText;
        [SerializeField] private float _showDelay = 0.5f;

        private static GameObject _tooltipPanel;
        private static TMP_Text _tooltipLabel;
        private static RectTransform _tooltipRect;
        private static Canvas _canvas;

        private bool _isHovering;
        private float _hoverTimer;
        private RectTransform _buttonRect;

        /// <summary>
        /// Set the tooltip text at runtime
        /// </summary>
        public string TooltipText
        {
            get => _tooltipText;
            set => _tooltipText = value;
        }

        private void Awake()
        {
            _buttonRect = GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (_isHovering && _tooltipPanel != null)
            {
                _hoverTimer += Time.unscaledDeltaTime;

                if (_hoverTimer >= _showDelay && !_tooltipPanel.activeSelf)
                {
                    ShowTooltip();
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(_tooltipText)) return;

            _isHovering = true;
            _hoverTimer = 0f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
            _hoverTimer = 0f;
            HideTooltip();
        }

        private void OnDisable()
        {
            _isHovering = false;
            HideTooltip();
        }

        private void ShowTooltip()
        {
            if (_tooltipPanel == null || _tooltipLabel == null) return;

            _tooltipLabel.text = _tooltipText;

            // Force text to calculate its preferred size
            _tooltipLabel.ForceMeshUpdate();

            // Resize panel to fit text with padding
            Vector2 textSize = _tooltipLabel.GetPreferredValues(_tooltipText);
            float padding = 16f; // 8px on each side
            _tooltipRect.sizeDelta = new Vector2(textSize.x + padding, textSize.y + padding);

            _tooltipPanel.SetActive(true);
            PositionTooltipAboveButton();
        }

        private void PositionTooltipAboveButton()
        {
            if (_tooltipRect == null || _buttonRect == null) return;

            // Force layout rebuild to get correct size
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipRect);

            // Get button's world corners
            Vector3[] buttonCorners = new Vector3[4];
            _buttonRect.GetWorldCorners(buttonCorners);
            // corners: 0=bottom-left, 1=top-left, 2=top-right, 3=bottom-right

            // Position tooltip centered above the button
            float buttonCenterX = (buttonCorners[0].x + buttonCorners[2].x) / 2f;
            float buttonTop = buttonCorners[1].y;

            Vector2 tooltipSize = _tooltipRect.sizeDelta;

            // Position above button with small gap
            Vector2 targetPos = new Vector2(buttonCenterX, buttonTop + 10f);

            // Adjust for tooltip pivot (assuming pivot is at bottom-center or we need to offset)
            // Set pivot to bottom-center for easier positioning
            _tooltipRect.pivot = new Vector2(0.5f, 0f);

            // Keep tooltip on screen - check right edge
            float halfWidth = tooltipSize.x / 2f;
            if (targetPos.x + halfWidth > Screen.width)
            {
                targetPos.x = Screen.width - halfWidth - 5f;
            }
            if (targetPos.x - halfWidth < 0)
            {
                targetPos.x = halfWidth + 5f;
            }

            // Keep on screen - check top edge
            if (targetPos.y + tooltipSize.y > Screen.height)
            {
                // Put it below the button instead
                _tooltipRect.pivot = new Vector2(0.5f, 1f);
                targetPos.y = buttonCorners[0].y - 10f;
            }

            _tooltipRect.position = targetPos;
        }

        private static void HideTooltip()
        {
            if (_tooltipPanel != null)
            {
                _tooltipPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Register the shared tooltip panel. Call this once from a manager or the tooltip panel itself.
        /// </summary>
        public static void RegisterTooltipPanel(GameObject panel, TMP_Text label)
        {
            _tooltipPanel = panel;
            _tooltipLabel = label;
            _tooltipRect = panel.GetComponent<RectTransform>();
            _canvas = panel.GetComponentInParent<Canvas>();

            // Start hidden
            panel.SetActive(false);
        }
    }
}
