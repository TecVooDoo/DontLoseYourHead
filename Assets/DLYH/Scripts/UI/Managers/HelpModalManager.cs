// HelpModalManager.cs
// Manages the How to Play help modal with scrollable content
// Extracted from UIFlowController during Phase 3 refactoring (Session 3)
// Created: January 19, 2026
// Developer: TecVooDoo LLC

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DLYH.UI.Managers
{
    /// <summary>
    /// Manages the How to Play help modal.
    /// Creates UI programmatically with scrollable help content.
    /// </summary>
    public class HelpModalManager
    {
        // ============================================================
        // CONSTANTS
        // ============================================================

        private const string HELP_CONTENT = @"<b>Your Goal:</b>
Discover all of your opponent's hidden words AND their grid positions before they find yours!

<b>Color Rules:</b>
<color=#F44336>RED</color> = Miss (empty cell)
<color=#FFC107>YELLOW</color> = Hit, but incomplete (letter OR position still unknown)
<b>YOUR COLOR</b> = Fully known (both letter AND position confirmed)

<b>Three Ways to Guess:</b>

<b>1. Letter Guess</b>
Click a letter on the keyboard (A-Z)
- Hit: Letter appears in word rows (yellow until all positions known)
- Upgrades to your color when ALL positions of that letter are found
- Miss: Opponent has no words with that letter

<b>2. Coordinate Guess</b>
Click a cell on the opponent's grid
- Hit: Cell turns yellow (you found a letter position)
- Upgrades to your color when you also know the letter
- Miss: Empty cell (red)

<b>3. Word Guess</b>
Click the GUESS button next to a word row
- Type the full word you think it is
- Correct: Word is revealed + EXTRA TURN!
- <color=#F44336>WRONG = 2 misses!</color>

<b>Extra Turns:</b>
Complete a word = EXTRA TURN!
- Letter guess that completes a word = extra turn
- Correct word guess = extra turn
- Multiple words completed = multiple extra turns (queued)

<b>Win Conditions:</b>
- Find ALL opponent's letters AND all their grid positions
- OR opponent reaches their miss limit

<b>Tips:</b>
- Start with common letters (E, T, A, O, I, N)
- Coordinate guesses near hits often find adjacent letters
- Words can be placed in any of 8 directions!
- Save word guesses for when you're confident - wrong guesses hurt!";

        // ============================================================
        // UI ELEMENTS
        // ============================================================

        private VisualElement _container;
        private ScrollView _scrollView;
        private VisualElement _root;

        // ============================================================
        // INITIALIZATION
        // ============================================================

        /// <summary>
        /// Initializes the help modal manager with a root element.
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
        /// Creates the help modal UI elements.
        /// Called lazily on first Show() if not already created.
        /// </summary>
        private void CreateModal()
        {
            if (_container != null) return;

            _container = new VisualElement();
            _container.name = "help-modal-container";
            _container.style.position = Position.Absolute;
            _container.style.left = 0;
            _container.style.right = 0;
            _container.style.top = 0;
            _container.style.bottom = 0;
            _container.style.alignItems = Align.Center;
            _container.style.justifyContent = Justify.Center;
            _container.style.backgroundColor = new Color(0f, 0f, 0f, 0.7f);
            _container.pickingMode = PickingMode.Position;

            // Panel
            VisualElement panel = new VisualElement();
            panel.name = "help-modal-panel";
            panel.style.backgroundColor = new Color(0.15f, 0.15f, 0.2f, 1f);
            panel.style.borderTopLeftRadius = 12;
            panel.style.borderTopRightRadius = 12;
            panel.style.borderBottomLeftRadius = 12;
            panel.style.borderBottomRightRadius = 12;
            panel.style.paddingLeft = 24;
            panel.style.paddingRight = 24;
            panel.style.paddingTop = 20;
            panel.style.paddingBottom = 20;
            panel.style.minWidth = 340;
            panel.style.maxWidth = 500;
            panel.style.maxHeight = new StyleLength(new Length(80, LengthUnit.Percent));
            panel.pickingMode = PickingMode.Position;
            _container.Add(panel);

            // Header row with title and close button
            VisualElement headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 16;
            panel.Add(headerRow);

            // Title
            Label title = new Label("How to Play");
            title.style.fontSize = 22;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            headerRow.Add(title);

            // Close button (X)
            Button closeBtn = new Button(() => Hide());
            closeBtn.text = "X";
            closeBtn.style.fontSize = 18;
            closeBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            closeBtn.style.width = 32;
            closeBtn.style.height = 32;
            closeBtn.style.paddingLeft = 0;
            closeBtn.style.paddingRight = 0;
            closeBtn.style.paddingTop = 0;
            closeBtn.style.paddingBottom = 0;
            closeBtn.style.backgroundColor = new Color(0.4f, 0.2f, 0.2f, 1f);
            closeBtn.style.borderTopLeftRadius = 4;
            closeBtn.style.borderTopRightRadius = 4;
            closeBtn.style.borderBottomLeftRadius = 4;
            closeBtn.style.borderBottomRightRadius = 4;
            headerRow.Add(closeBtn);

            // Scrollable content area
            _scrollView = new ScrollView(ScrollViewMode.Vertical);
            _scrollView.style.flexGrow = 1;
            _scrollView.style.maxHeight = new StyleLength(new Length(100, LengthUnit.Percent));
            panel.Add(_scrollView);

            // Help content
            Label contentLabel = new Label(HELP_CONTENT);
            contentLabel.style.fontSize = 18;
            contentLabel.style.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            contentLabel.style.whiteSpace = WhiteSpace.Normal;
            contentLabel.enableRichText = true;
            _scrollView.Add(contentLabel);

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
        /// Shows the help modal.
        /// </summary>
        public void Show()
        {
            if (_container == null)
            {
                CreateModal();
            }

            DLYH.Audio.UIAudioManager.PopupOpen();

            // Reset scroll position to top
            if (_scrollView != null)
            {
                _scrollView.scrollOffset = Vector2.zero;
            }

            _container.RemoveFromClassList("hidden");
        }

        /// <summary>
        /// Hides the help modal.
        /// </summary>
        public void Hide()
        {
            DLYH.Audio.UIAudioManager.PopupClose();

            if (_container != null)
            {
                _container.AddToClassList("hidden");
            }
        }

        /// <summary>
        /// Returns whether the modal is currently visible.
        /// </summary>
        public bool IsVisible => _container != null && !_container.ClassListContains("hidden");
    }
}
