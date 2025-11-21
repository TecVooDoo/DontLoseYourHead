using UnityEngine;
using UnityEngine.UI;

namespace UIColorSystem
{
    /// <summary>
    /// Simple test script to check the color system
    /// </summary>
    public class ColorTest : MonoBehaviour
    {
        [Header("Test Components")]
        [SerializeField] private Image testImage;
        [SerializeField] private Text testText;
        [SerializeField] private Button testButton;

        [Header("Test Settings")]
        [SerializeField] private bool autoUpdate = true;
        [SerializeField] private float updateInterval = 1f;

        private float lastUpdateTime;

        private void Start()
        {
            // Auto-assign components if not set
            if (testImage == null) testImage = GetComponent<Image>();
            if (testText == null) testText = GetComponent<Text>();
            if (testButton == null) testButton = GetComponent<Button>();

            // Subscribe to color changes
            ColorManager.OnPaletteChanged += OnColorPaletteChanged;

            // Apply initial colors
            ApplyTestColors();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            ColorManager.OnPaletteChanged -= OnColorPaletteChanged;
        }

        private void Update()
        {
            if (autoUpdate && Time.time - lastUpdateTime > updateInterval)
            {
                ApplyTestColors();
                lastUpdateTime = Time.time;
            }
        }

        private void OnColorPaletteChanged(ColorPalette newPalette)
        {
            ApplyTestColors();
        }

        private void ApplyTestColors()
        {
            // Apply different colors to different components
            if (testImage != null)
            {
                testImage.color = ColorManager.Primary;
            }

            if (testText != null)
            {
                testText.color = ColorManager.TextPrimary;
            }

            if (testButton != null)
            {
                ColorBlock colorBlock = testButton.colors;
                colorBlock.normalColor = ColorManager.Secondary;
                colorBlock.highlightedColor = ColorManager.SecondaryLight;
                colorBlock.pressedColor = ColorManager.SecondaryDark;
                testButton.colors = colorBlock;
            }
        }

        // Public method for external access
        public void ApplyColors()
        {
            ApplyTestColors();
        }

        [ContextMenu("Test Color Change")]
        public void TestColorChange()
        {
            ApplyTestColors();
        }

        [ContextMenu("Print Current Colors")]
        public void PrintCurrentColors()
        {
            // Method kept for context menu compatibility
        }
    }
} 