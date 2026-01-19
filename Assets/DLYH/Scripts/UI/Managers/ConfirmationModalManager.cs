// ConfirmationModalManager.cs
// Manages confirmation modal dialogs with title, message, and confirm/cancel actions
// Extracted from UIFlowController during Phase 3 refactoring (Session 3)
// Created: January 19, 2026
// Developer: TecVooDoo LLC

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DLYH.UI.Managers
{
    /// <summary>
    /// Manages generic confirmation modal dialogs.
    /// Creates UI programmatically and handles show/hide with callbacks.
    /// </summary>
    public class ConfirmationModalManager
    {
        // ============================================================
        // UI ELEMENTS
        // ============================================================

        private VisualElement _container;
        private Label _titleLabel;
        private Label _messageLabel;
        private Action _confirmAction;
        private VisualElement _root;

        // ============================================================
        // INITIALIZATION
        // ============================================================

        /// <summary>
        /// Initializes the confirmation modal manager with a root element.
        /// </summary>
        /// <param name="root">The root VisualElement to add the modal to</param>
        public void Initialize(VisualElement root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
        }

        // ============================================================
        // CREATE MODAL
        // ============================================================

        /// <summary>
        /// Creates the confirmation modal UI elements.
        /// Called lazily on first Show() if not already created.
        /// </summary>
        private void CreateModal()
        {
            if (_container != null) return;

            _container = new VisualElement();
            _container.name = "confirm-modal-container";
            _container.style.position = Position.Absolute;
            _container.style.left = 0;
            _container.style.right = 0;
            _container.style.top = 0;
            _container.style.bottom = 0;
            _container.style.alignItems = Align.Center;
            _container.style.justifyContent = Justify.Center;
            _container.style.backgroundColor = new Color(0, 0, 0, 0.7f);

            // Modal panel
            VisualElement panel = new VisualElement();
            panel.name = "confirm-panel";
            panel.style.backgroundColor = new Color(0.15f, 0.14f, 0.16f, 1f);
            panel.style.borderTopLeftRadius = 16;
            panel.style.borderTopRightRadius = 16;
            panel.style.borderBottomLeftRadius = 16;
            panel.style.borderBottomRightRadius = 16;
            panel.style.paddingTop = 24;
            panel.style.paddingBottom = 24;
            panel.style.paddingLeft = 32;
            panel.style.paddingRight = 32;
            panel.style.minWidth = 300;
            panel.style.maxWidth = 400;

            // Title
            _titleLabel = new Label("Confirm");
            _titleLabel.name = "confirm-title";
            _titleLabel.style.fontSize = 24;
            _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _titleLabel.style.color = Color.white;
            _titleLabel.style.marginBottom = 16;
            _titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(_titleLabel);

            // Message
            _messageLabel = new Label("Are you sure?");
            _messageLabel.name = "confirm-message";
            _messageLabel.style.fontSize = 16;
            _messageLabel.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            _messageLabel.style.marginBottom = 24;
            _messageLabel.style.whiteSpace = WhiteSpace.Normal;
            _messageLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(_messageLabel);

            // Button container
            VisualElement buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.Center;

            // Cancel button
            Button cancelBtn = new Button(() => Hide());
            cancelBtn.text = "Cancel";
            cancelBtn.style.marginRight = 16;
            cancelBtn.style.paddingLeft = 24;
            cancelBtn.style.paddingRight = 24;
            cancelBtn.style.paddingTop = 12;
            cancelBtn.style.paddingBottom = 12;
            buttonRow.Add(cancelBtn);

            // Confirm button
            Button confirmBtn = new Button();
            confirmBtn.clicked += () =>
            {
                Action action = _confirmAction;
                Hide();
                action?.Invoke();
            };
            confirmBtn.text = "Yes";
            confirmBtn.style.paddingLeft = 24;
            confirmBtn.style.paddingRight = 24;
            confirmBtn.style.paddingTop = 12;
            confirmBtn.style.paddingBottom = 12;
            confirmBtn.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f, 1f);
            buttonRow.Add(confirmBtn);

            panel.Add(buttonRow);
            _container.Add(panel);

            // Click outside to close
            _container.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == _container)
                {
                    Hide();
                }
            });

            _root.Add(_container);
            _container.AddToClassList("hidden");
        }

        // ============================================================
        // SHOW / HIDE
        // ============================================================

        /// <summary>
        /// Shows the confirmation modal with the specified title, message, and confirm action.
        /// </summary>
        /// <param name="title">Modal title text</param>
        /// <param name="message">Modal message text</param>
        /// <param name="onConfirm">Action to invoke when user confirms</param>
        public void Show(string title, string message, Action onConfirm)
        {
            if (_container == null)
            {
                CreateModal();
            }

            DLYH.Audio.UIAudioManager.PopupOpen();

            _titleLabel.text = title;
            _messageLabel.text = message;
            _confirmAction = onConfirm;
            _container.RemoveFromClassList("hidden");
        }

        /// <summary>
        /// Hides the confirmation modal.
        /// </summary>
        public void Hide()
        {
            DLYH.Audio.UIAudioManager.PopupClose();

            if (_container != null)
            {
                _container.AddToClassList("hidden");
            }
            _confirmAction = null;
        }

        /// <summary>
        /// Returns whether the modal is currently visible.
        /// </summary>
        public bool IsVisible => _container != null && !_container.ClassListContains("hidden");
    }
}
