using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UIColorSystem
{
    [CreateAssetMenu(fileName = "ColorPalette", menuName = "UIColorSystem/Color Palette")]
    public class ColorPalette : ScriptableObject
    {
        // Event triggered when any color in the palette changes
        public static event Action<ColorPalette> OnColorsChanged;

        // Event triggered when a specific color changes
        public static event Action<ColorPalette, string> OnColorChanged;

        [Header("Primary Colors")]
        [SerializeField] private Color _primary = new Color(0.2f, 0.4f, 1f, 1f); // #3366FF
        [SerializeField] private Color _primaryLight = new Color(0.35f, 0.55f, 1f, 1f); // #5A8DFF
        [SerializeField] private Color _primaryDark = new Color(0.15f, 0.31f, 0.86f, 1f); // #254EDB

        [Header("Secondary Colors")]
        [SerializeField] private Color _secondary = new Color(1f, 0.67f, 0.2f, 1f); // #FFAA33
        [SerializeField] private Color _secondaryLight = new Color(1f, 0.84f, 0.5f, 1f); // #FFD580
        [SerializeField] private Color _secondaryDark = new Color(0.86f, 0.54f, 0f, 1f); // #DB8900

        [Header("Feedback Colors")]
        [SerializeField] private Color _success = new Color(0.15f, 0.68f, 0.38f, 1f); // #27AE60
        [SerializeField] private Color _warning = new Color(1f, 0.75f, 0f, 1f); // #FFBF00
        [SerializeField] private Color _error = new Color(0.92f, 0.34f, 0.34f, 1f); // #EB5757
        [SerializeField] private Color _info = new Color(0.18f, 0.61f, 0.86f, 1f); // #2D9CDB

        [Header("Text Colors")]
        [SerializeField] private Color _textPrimary = new Color(0.13f, 0.13f, 0.13f, 1f); // #212121
        [SerializeField] private Color _textSecondary = new Color(0.38f, 0.38f, 0.38f, 1f); // #616161
        [SerializeField] private Color _textDisabled = new Color(0.74f, 0.74f, 0.74f, 1f); // #BDBDBD
        [SerializeField] private Color _textOnPrimary = Color.white; // #FFFFFF
        [SerializeField] private Color _textOnSecondary = new Color(0.13f, 0.13f, 0.13f, 1f); // #212121

        [Header("Background Colors")]
        [SerializeField] private Color _background = new Color(0.96f, 0.96f, 0.98f, 1f); // #F5F6FA
        [SerializeField] private Color _surface = Color.white; // #FFFFFF
        [SerializeField] private Color _surfaceAlt = new Color(0.95f, 0.95f, 0.95f, 1f); // #F1F1F1
        [SerializeField] private Color _divider = new Color(0.88f, 0.88f, 0.88f, 1f); // #E0E0E0

        [Header("Additional Colors")]
        [SerializeField] private Color _accent = new Color(0f, 0.75f, 0.68f, 1f); // #00BFAE
        [SerializeField] private Color _selection = new Color(0.89f, 0.95f, 0.99f, 1f); // #E3F2FD
        [SerializeField] private Color _hover = new Color(0.9f, 0.94f, 1f, 1f); // #E6F0FF
        [SerializeField] private Color _shadow = new Color(0.13f, 0.13f, 0.13f, 0.07f); // rgba(33, 33, 33, 0.07)

        [Header("Custom Colors")]
        [SerializeField] private List<CustomColorEntry> _customColors = new List<CustomColorEntry>();
        // Cache for tracking actual changes
        #if UNITY_EDITOR
        private Dictionary<string, Color> _previousColors = new Dictionary<string, Color>();
        #endif

        [System.Serializable]
        public class CustomColorEntry
        {
            public string colorName;
            public Color color;
            public string description;

            public CustomColorEntry(string name, Color col, string desc = "")
            {
                colorName = name;
                color = col;
                description = desc;
            }
        }

        // Primary Colors Properties
        public Color Primary => _primary;
        public Color PrimaryLight => _primaryLight;
        public Color PrimaryDark => _primaryDark;

        // Secondary Colors Properties
        public Color Secondary => _secondary;
        public Color SecondaryLight => _secondaryLight;
        public Color SecondaryDark => _secondaryDark;

        // Feedback Colors Properties
        public Color Success => _success;
        public Color Warning => _warning;
        public Color Error => _error;
        public Color Info => _info;

        // Text Colors Properties
        public Color TextPrimary => _textPrimary;
        public Color TextSecondary => _textSecondary;
        public Color TextDisabled => _textDisabled;
        public Color TextOnPrimary => _textOnPrimary;
        public Color TextOnSecondary => _textOnSecondary;

        // Background Colors Properties
        public Color Background => _background;
        public Color Surface => _surface;
        public Color SurfaceAlt => _surfaceAlt;
        public Color Divider => _divider;

        // Additional Colors Properties
        public Color Accent => _accent;
        public Color Selection => _selection;
        public Color Hover => _hover;
        public Color Shadow => _shadow;

        // Custom Colors Management
        public List<CustomColorEntry> CustomColors => _customColors;

        public void AddCustomColor(string colorName, Color color, string description = "")
        {
            if (string.IsNullOrEmpty(colorName))
            {
                Debug.LogWarning("Color name cannot be empty");
                return;
            }

            // Check if name already exists
            var existingEntry = _customColors.Find(c => c.colorName.ToLower() == colorName.ToLower());
            if (existingEntry != null)
            {
                Debug.LogWarning($"Color named '{colorName}' already exists. Updating existing one.");
                existingEntry.color = color;
                existingEntry.description = description;
            }
            else
            {
                _customColors.Add(new CustomColorEntry(colorName, color, description));
            }

            NotifyColorChanged(colorName);
            NotifyColorsChanged();

            #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            #endif
        }

        public bool RemoveCustomColor(string colorName)
        {
            var entry = _customColors.Find(c => c.colorName.ToLower() == colorName.ToLower());
            if (entry != null)
            {
                _customColors.Remove(entry);
                NotifyColorsChanged();

                #if UNITY_EDITOR
                EditorUtility.SetDirty(this);
                #endif

                return true;
            }
            return false;
        }

        public bool HasCustomColor(string colorName)
        {
            return _customColors.Exists(c => c.colorName.ToLower() == colorName.ToLower());
        }

        public Color GetCustomColor(string colorName)
        {
            var entry = _customColors.Find(c => c.colorName.ToLower() == colorName.ToLower());
            return entry?.color ?? Color.white;
        }

        // Method called automatically by Unity when values change in editor
        private void OnValidate()
        {
            #if UNITY_EDITOR
            // Only process changes in editor mode
            if (!Application.isPlaying)
            {
                // Use EditorApplication.delayCall to ensure all changes are processed
                EditorApplication.delayCall += () =>
                {
                    if (this != null) // Check if object still exists
                    {
                        CheckForActualChanges();
                    }
                };
            }
            #endif
        }

        #if UNITY_EDITOR
        private void CheckForActualChanges()
        {
            bool hasChanges = false;
            List<string> changedColors = new List<string>();

            // Check primary colors
            if (HasColorChanged("Primary", _primary)) { hasChanges = true; changedColors.Add("Primary"); }
            if (HasColorChanged("PrimaryLight", _primaryLight)) { hasChanges = true; changedColors.Add("PrimaryLight"); }
            if (HasColorChanged("PrimaryDark", _primaryDark)) { hasChanges = true; changedColors.Add("PrimaryDark"); }

            // Check secondary colors
            if (HasColorChanged("Secondary", _secondary)) { hasChanges = true; changedColors.Add("Secondary"); }
            if (HasColorChanged("SecondaryLight", _secondaryLight)) { hasChanges = true; changedColors.Add("SecondaryLight"); }
            if (HasColorChanged("SecondaryDark", _secondaryDark)) { hasChanges = true; changedColors.Add("SecondaryDark"); }

            // Check feedback colors
            if (HasColorChanged("Success", _success)) { hasChanges = true; changedColors.Add("Success"); }
            if (HasColorChanged("Warning", _warning)) { hasChanges = true; changedColors.Add("Warning"); }
            if (HasColorChanged("Error", _error)) { hasChanges = true; changedColors.Add("Error"); }
            if (HasColorChanged("Info", _info)) { hasChanges = true; changedColors.Add("Info"); }

            // Check text colors
            if (HasColorChanged("TextPrimary", _textPrimary)) { hasChanges = true; changedColors.Add("TextPrimary"); }
            if (HasColorChanged("TextSecondary", _textSecondary)) { hasChanges = true; changedColors.Add("TextSecondary"); }
            if (HasColorChanged("TextDisabled", _textDisabled)) { hasChanges = true; changedColors.Add("TextDisabled"); }
            if (HasColorChanged("TextOnPrimary", _textOnPrimary)) { hasChanges = true; changedColors.Add("TextOnPrimary"); }
            if (HasColorChanged("TextOnSecondary", _textOnSecondary)) { hasChanges = true; changedColors.Add("TextOnSecondary"); }

            // Check background colors
            if (HasColorChanged("Background", _background)) { hasChanges = true; changedColors.Add("Background"); }
            if (HasColorChanged("Surface", _surface)) { hasChanges = true; changedColors.Add("Surface"); }
            if (HasColorChanged("SurfaceAlt", _surfaceAlt)) { hasChanges = true; changedColors.Add("SurfaceAlt"); }
            if (HasColorChanged("Divider", _divider)) { hasChanges = true; changedColors.Add("Divider"); }

            // Check additional colors
            if (HasColorChanged("Accent", _accent)) { hasChanges = true; changedColors.Add("Accent"); }
            if (HasColorChanged("Selection", _selection)) { hasChanges = true; changedColors.Add("Selection"); }
            if (HasColorChanged("Hover", _hover)) { hasChanges = true; changedColors.Add("Hover"); }
            if (HasColorChanged("Shadow", _shadow)) { hasChanges = true; changedColors.Add("Shadow"); }

            // Only process if there are actual changes
            if (hasChanges)
            {
                ProcessColorChanges(changedColors);
            }
        }

        private bool HasColorChanged(string colorName, Color currentColor)
        {
            if (_previousColors.TryGetValue(colorName, out Color previousColor))
            {
                return !ColorApproximatelyEquals(currentColor, previousColor);
            }
            else
            {
                // First time seeing this color, store it
                _previousColors[colorName] = currentColor;
                return false; // No change on first load
            }
        }

        #endif

        // Utility method to compare colors approximately (available in both editor and runtime)
        private bool ColorApproximatelyEquals(Color a, Color b)
        {
            return Mathf.Approximately(a.r, b.r) && 
                   Mathf.Approximately(a.g, b.g) && 
                   Mathf.Approximately(a.b, b.b) && 
                   Mathf.Approximately(a.a, b.a);
        }

        #if UNITY_EDITOR
        private void ProcessColorChanges(List<string> changedColors)
        {
            // Update stored colors
            foreach (string colorName in changedColors)
            {
                Color currentColor = GetColorByName(colorName);
                _previousColors[colorName] = currentColor;
            }

            // Notify about specific color changes
            foreach (string colorName in changedColors)
            {
                NotifyColorChanged(colorName);
            }
            
            // Notify about general palette change
            NotifyColorsChanged();
            
            // Notify ColorManager about changes
            ColorManager.NotifyPaletteChanged();
            
            // Force refresh all scenes and screens
            ColorManager.ForceRefreshAllScenesAndScreens();
            
            // Force scene refresh in editor
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
            
            // Force refresh of all inspectors
            EditorUtility.SetDirty(this);
            
            // Refresh all ColoredUI components
            RefreshAllColoredUI();
            
            // Repaint all editor windows for immediate feedback
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private Color GetColorByName(string colorName)
        {
            switch (colorName.ToLower())
            {
                case "primary": return _primary;
                case "primarylight": return _primaryLight;
                case "primarydark": return _primaryDark;
                case "secondary": return _secondary;
                case "secondarylight": return _secondaryLight;
                case "secondarydark": return _secondaryDark;
                case "success": return _success;
                case "warning": return _warning;
                case "error": return _error;
                case "info": return _info;
                case "textprimary": return _textPrimary;
                case "textsecondary": return _textSecondary;
                case "textdisabled": return _textDisabled;
                case "textonprimary": return _textOnPrimary;
                case "textonsecondary": return _textOnSecondary;
                case "background": return _background;
                case "surface": return _surface;
                case "surfacealt": return _surfaceAlt;
                case "divider": return _divider;
                case "accent": return _accent;
                case "selection": return _selection;
                case "hover": return _hover;
                case "shadow": return _shadow;
                default: return Color.white;
            }
        }
        #endif

        // Utility methods for getting colors by string name
        public Color GetColor(string colorName)
        {
            switch (colorName.ToLower())
            {
                case "primary": return _primary;
                case "primarylight": return _primaryLight;
                case "primarydark": return _primaryDark;
                case "secondary": return _secondary;
                case "secondarylight": return _secondaryLight;
                case "secondarydark": return _secondaryDark;
                case "success": return _success;
                case "warning": return _warning;
                case "error": return _error;
                case "info": return _info;
                case "textprimary": return _textPrimary;
                case "textsecondary": return _textSecondary;
                case "textdisabled": return _textDisabled;
                case "textonprimary": return _textOnPrimary;
                case "textonsecondary": return _textOnSecondary;
                case "background": return _background;
                case "surface": return _surface;
                case "surfacealt": return _surfaceAlt;
                case "divider": return _divider;
                case "accent": return _accent;
                case "selection": return _selection;
                case "hover": return _hover;
                case "shadow": return _shadow;
                default:
                    // Check custom colors
                    return GetCustomColor(colorName);
            }
        }

        public List<string> GetAllColorNames()
        {
            var colorNames = new List<string>
            {
                "primary", "primarylight", "primarydark",
                "secondary", "secondarylight", "secondarydark",
                "success", "warning", "error", "info",
                "textprimary", "textsecondary", "textdisabled", "textonprimary", "textonsecondary",
                "background", "surface", "surfacealt", "divider",
                "accent", "selection", "hover", "shadow"
            };

            // Add custom colors
            foreach (var customColor in _customColors)
            {
                colorNames.Add(customColor.colorName.ToLower());
            }

            return colorNames;
        }

        // Methods for triggering events after color changes
        public void NotifyColorsChanged()
        {
            OnColorsChanged?.Invoke(this);
        }

        public void NotifyColorChanged(string colorName)
        {
            OnColorChanged?.Invoke(this, colorName);
        }

        // Method to reset all colors and notify about changes
        public void ResetAllColors()
        {
            // Reset all colors to default values
            _primary = new Color(0.2f, 0.4f, 1f, 1f);
            _primaryLight = new Color(0.35f, 0.55f, 1f, 1f);
            _primaryDark = new Color(0.15f, 0.31f, 0.86f, 1f);
            
            _secondary = new Color(1f, 0.67f, 0.2f, 1f);
            _secondaryLight = new Color(1f, 0.84f, 0.5f, 1f);
            _secondaryDark = new Color(0.86f, 0.54f, 0f, 1f);
            
            _success = new Color(0.15f, 0.68f, 0.38f, 1f);
            _warning = new Color(1f, 0.75f, 0f, 1f);
            _error = new Color(0.92f, 0.34f, 0.34f, 1f);
            _info = new Color(0.18f, 0.61f, 0.86f, 1f);
            
            _textPrimary = new Color(0.13f, 0.13f, 0.13f, 1f);
            _textSecondary = new Color(0.38f, 0.38f, 0.38f, 1f);
            _textDisabled = new Color(0.74f, 0.74f, 0.74f, 1f);
            _textOnPrimary = Color.white;
            _textOnSecondary = new Color(0.13f, 0.13f, 0.13f, 1f);
            
            _background = new Color(0.96f, 0.96f, 0.98f, 1f);
            _surface = Color.white;
            _surfaceAlt = new Color(0.95f, 0.95f, 0.95f, 1f);
            _divider = new Color(0.88f, 0.88f, 0.88f, 1f);
            
            _accent = new Color(0f, 0.75f, 0.68f, 1f);
            _selection = new Color(0.89f, 0.95f, 0.99f, 1f);
            _hover = new Color(0.9f, 0.94f, 1f, 1f);
            _shadow = new Color(0.13f, 0.13f, 0.13f, 0.07f);

            // Notify about all color changes
            NotifyColorsChanged();
            
            #if UNITY_EDITOR
            // Immediate refresh in editor
            if (!Application.isPlaying)
            {
                // Clear previous colors cache to force refresh
                _previousColors.Clear();
                ProcessColorChanges(new List<string> { "All" });
            }
            #endif
        }

        #if UNITY_EDITOR
        // Method to refresh all ColoredUI components in editor
        private void RefreshAllColoredUI()
        {
            var coloredUIComponents = UnityEngine.Object.FindObjectsByType<ColoredUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            foreach (var coloredUI in coloredUIComponents)
            {
                if (coloredUI != null)
                {
                    try
                    {
                        coloredUI.ApplyColor();
                        EditorUtility.SetDirty(coloredUI);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[ColorPalette] Failed to refresh ColoredUI on {coloredUI.gameObject.name}: {ex.Message}");
                    }
                }
            }
            
            // Force refresh all scenes and screens
            ColorManager.ForceRefreshAllScenesAndScreens();
        }
        #endif

        // Method to set a specific color by name
        public void SetColor(string colorName, Color newColor)
        {
            bool colorChanged = false;
            
            // Check if it's a custom color
            var customEntry = _customColors.Find(c => c.colorName.ToLower() == colorName.ToLower());
            if (customEntry != null)
            {
                if (!ColorApproximatelyEquals(customEntry.color, newColor))
                {
                    customEntry.color = newColor;
                    colorChanged = true;
                }
            }
            else
            {
                // Check default colors
                switch (colorName.ToLower())
                {
                    case "primary": 
                        if (!ColorApproximatelyEquals(_primary, newColor)) { _primary = newColor; colorChanged = true; }
                        break;
                    case "primarylight": 
                        if (!ColorApproximatelyEquals(_primaryLight, newColor)) { _primaryLight = newColor; colorChanged = true; }
                        break;
                    case "primarydark": 
                        if (!ColorApproximatelyEquals(_primaryDark, newColor)) { _primaryDark = newColor; colorChanged = true; }
                        break;
                    case "secondary": 
                        if (!ColorApproximatelyEquals(_secondary, newColor)) { _secondary = newColor; colorChanged = true; }
                        break;
                    case "secondarylight": 
                        if (!ColorApproximatelyEquals(_secondaryLight, newColor)) { _secondaryLight = newColor; colorChanged = true; }
                        break;
                    case "secondarydark": 
                        if (!ColorApproximatelyEquals(_secondaryDark, newColor)) { _secondaryDark = newColor; colorChanged = true; }
                        break;
                    case "success": 
                        if (!ColorApproximatelyEquals(_success, newColor)) { _success = newColor; colorChanged = true; }
                        break;
                    case "warning": 
                        if (!ColorApproximatelyEquals(_warning, newColor)) { _warning = newColor; colorChanged = true; }
                        break;
                    case "error": 
                        if (!ColorApproximatelyEquals(_error, newColor)) { _error = newColor; colorChanged = true; }
                        break;
                    case "info": 
                        if (!ColorApproximatelyEquals(_info, newColor)) { _info = newColor; colorChanged = true; }
                        break;
                    case "textprimary": 
                        if (!ColorApproximatelyEquals(_textPrimary, newColor)) { _textPrimary = newColor; colorChanged = true; }
                        break;
                    case "textsecondary": 
                        if (!ColorApproximatelyEquals(_textSecondary, newColor)) { _textSecondary = newColor; colorChanged = true; }
                        break;
                    case "textdisabled": 
                        if (!ColorApproximatelyEquals(_textDisabled, newColor)) { _textDisabled = newColor; colorChanged = true; }
                        break;
                    case "textonprimary": 
                        if (!ColorApproximatelyEquals(_textOnPrimary, newColor)) { _textOnPrimary = newColor; colorChanged = true; }
                        break;
                    case "textonsecondary": 
                        if (!ColorApproximatelyEquals(_textOnSecondary, newColor)) { _textOnSecondary = newColor; colorChanged = true; }
                        break;
                    case "background": 
                        if (!ColorApproximatelyEquals(_background, newColor)) { _background = newColor; colorChanged = true; }
                        break;
                    case "surface": 
                        if (!ColorApproximatelyEquals(_surface, newColor)) { _surface = newColor; colorChanged = true; }
                        break;
                    case "surfacealt": 
                        if (!ColorApproximatelyEquals(_surfaceAlt, newColor)) { _surfaceAlt = newColor; colorChanged = true; }
                        break;
                    case "divider": 
                        if (!ColorApproximatelyEquals(_divider, newColor)) { _divider = newColor; colorChanged = true; }
                        break;
                    case "accent": 
                        if (!ColorApproximatelyEquals(_accent, newColor)) { _accent = newColor; colorChanged = true; }
                        break;
                    case "selection": 
                        if (!ColorApproximatelyEquals(_selection, newColor)) { _selection = newColor; colorChanged = true; }
                        break;
                    case "hover": 
                        if (!ColorApproximatelyEquals(_hover, newColor)) { _hover = newColor; colorChanged = true; }
                        break;
                    case "shadow": 
                        if (!ColorApproximatelyEquals(_shadow, newColor)) { _shadow = newColor; colorChanged = true; }
                        break;
                    default:
                        Debug.LogWarning($"Unknown color name: {colorName}");
                        return;
                }
            }
            
            if (colorChanged)
            {
                NotifyColorChanged(colorName);
                NotifyColorsChanged();
                
                #if UNITY_EDITOR
                EditorUtility.SetDirty(this);
                if (!Application.isPlaying)
                {
                    ProcessColorChanges(new List<string> { colorName });
                }
                #endif
            }
        }
    }
}