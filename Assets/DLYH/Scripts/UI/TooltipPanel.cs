// TooltipPanel.cs
// Self-registering tooltip panel
// Created: December 16, 2025

using UnityEngine;
using TMPro;

namespace DLYH.UI
{
    /// <summary>
    /// Attach to the tooltip panel GameObject. Auto-registers on Awake.
    /// </summary>
    public class TooltipPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _tooltipText;

        private void Awake()
        {
            if (_tooltipText == null)
            {
                _tooltipText = GetComponentInChildren<TMP_Text>();
            }

            ButtonTooltip.RegisterTooltipPanel(gameObject, _tooltipText);
        }
    }
}
