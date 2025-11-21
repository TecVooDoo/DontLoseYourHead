using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UIColorSystem
{
    /// <summary>
    /// Example script showing different ways to use the color system
    /// </summary>
    public class ColorSystemTest : MonoBehaviour
    {
        [Header("Test UI Components")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image primaryButton;
        [SerializeField] private Image secondaryButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Image customColorImage;
        [SerializeField] private TextMeshProUGUI customColorText;

        [Header("Custom Color Settings")]
        [SerializeField] private string customColorName = "";

        private void Start()
        {
            // Add some example custom colors
            InitializeCustomColors();
            
            // Examples of using different ways to access colors
            DemonstrateColorUsage();
        }

        private void InitializeCustomColors()
        {
            // Add example custom colors to active palette
            ColorManager.AddCustomColor("BrandBlue", new Color(0.1f, 0.5f, 0.9f), "Main brand color");
            ColorManager.AddCustomColor("BrandOrange", new Color(1f, 0.6f, 0.1f), "Secondary brand color");
            ColorManager.AddCustomColor("SpecialGreen", new Color(0.2f, 0.8f, 0.3f), "Color for special actions");
            ColorManager.AddCustomColor("WarningRed", new Color(0.9f, 0.2f, 0.2f), "Warning color");
        }

                private void DemonstrateColorUsage()
        {
            // 1. Using ColorManager (static properties)
            if (backgroundImage != null)
                backgroundImage.color = ColorManager.Background;
            
            // 2. Using ColorManager with name (string)
            if (primaryButton != null)
                primaryButton.color = ColorManager.GetColor("primary");
            
            // 3. Using custom colors by name
            if (secondaryButton != null)
                secondaryButton.color = ColorManager.GetColor("BrandBlue");
            
            // 4. Using ColorConstants for type safety
            if (titleText != null)
                titleText.color = ColorManager.GetColor(ColorConstants.TEXT_PRIMARY);
            
            // 5. Using custom color with check if it exists
            if (bodyText != null)
            {
                if (ColorManager.HasCustomColor("BrandOrange"))
                {
                    bodyText.color = ColorManager.GetCustomColor("BrandOrange");
                }
                else
                {
                    bodyText.color = ColorManager.TextSecondary;
                }
            }
            
            // 6. Example of using custom color from component settings
            if (!string.IsNullOrEmpty(customColorName))
            {
                if (customColorImage != null)
                    customColorImage.color = ColorManager.GetColor(customColorName);
                
                if (customColorText != null)
                    customColorText.color = ColorManager.GetColor(customColorName);
            }
        }

        // Methods for runtime testing
        [ContextMenu("Test Color Changes")]
        public void TestColorChanges()
        {
            // Change custom color randomly
            if (ColorManager.HasCustomColor("BrandBlue"))
            {
                Color randomColor = new Color(
                    Random.Range(0f, 1f),
                    Random.Range(0f, 1f),
                    Random.Range(0f, 1f),
                    1f
                );
                
                ColorManager.AddCustomColor("BrandBlue", randomColor, "Randomly changed brand color");
            }
        }

        [ContextMenu("Switch to Different Palette")]
        public void SwitchPalette()
        {
            // Find available palettes and switch to different one
            var availablePalettes = ColorManager.GetAllPalettes();
            if (availablePalettes.Count > 1)
            {
                var currentPalette = ColorManager.GetActivePalette();
                var newPalette = availablePalettes.Find(p => p != currentPalette);
                
                if (newPalette != null)
                {
                    ColorManager.SetColorPalette(newPalette);
                }
            }
        }

        [ContextMenu("List All Available Colors")]
        public void ListAllColors()
        {
            var allColors = ColorManager.GetAllColorNames();
        }

        [ContextMenu("Remove Custom Color")]
        public void RemoveCustomColor()
        {
            if (ColorManager.HasCustomColor("BrandBlue"))
            {
                ColorManager.RemoveCustomColor("BrandBlue");
            }
        }

        // Subscribe to palette changes
        private void OnEnable()
        {
            ColorManager.OnPaletteChanged += OnPaletteChanged;
        }

        private void OnDisable()
        {
            ColorManager.OnPaletteChanged -= OnPaletteChanged;
        }

        private void OnPaletteChanged(ColorPalette newPalette)
        {
            // Re-apply colors after palette change
            DemonstrateColorUsage();
        }

        // Example of creating gradients with system colors
        public Gradient CreateBrandGradient()
        {
            Gradient gradient = new Gradient();
            
            var colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(ColorManager.GetColor("BrandBlue"), 0f),
                new GradientColorKey(ColorManager.GetColor("BrandOrange"), 1f)
            };
            
            var alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };
            
            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        // Example of using color utility methods
        public void DemonstrateColorUtilities()
        {
            Color baseColor = ColorManager.Primary;
            
            // Creating color variants
            Color lighterColor = ColorManager.Lighten(baseColor, 0.2f);
            Color darkerColor = ColorManager.Darken(baseColor, 0.2f);
            Color transparentColor = ColorManager.WithAlpha(baseColor, 0.5f);
            
            // Conversion to hex
            string hexColor = ColorManager.ToHex(baseColor);
            
            // Conversion from hex
            Color colorFromHex = ColorManager.FromHex("#FF5733");
        }
    }
} 