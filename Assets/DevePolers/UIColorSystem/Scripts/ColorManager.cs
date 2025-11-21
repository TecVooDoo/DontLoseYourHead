using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UIColorSystem
{
    #if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
    #endif
    public class ColorManager : Singleton<ColorManager>
    {
        [Header("Color Configuration")]
        [SerializeField] private ColorPalette _colorPalette;

        // Event triggered when color palette changes
        public static event Action<ColorPalette> OnPaletteChanged;

        private static ColorPalette ActivePalette => Instance._colorPalette;

        // Fallback default palette if none is assigned
        private static ColorPalette _defaultPalette;
        
        // Key for storing last selected palette in EditorPrefs
        private const string LAST_SELECTED_PALETTE_KEY = "UIColorSystem_LastSelectedPalette";

        #if UNITY_EDITOR
        // Static constructor for editor initialization
        static ColorManager()
        {
            EditorApplication.delayCall += InitializeForEditor;
        }

        private static void InitializeForEditor()
        {
            // This ensures ColorManager gets initialized properly in the editor
            if (Instance == null)
            {
                // Create a temporary instance to trigger initialization
                var temp = FindAnyObjectByType<ColorManager>();
                if (temp == null)
                {
                    // If no ColorManager exists in scene, the palette loading will happen when one is created
                }
            }
        }
        #endif

        protected override void Awake()
        {
            base.Awake();
            
            // Try to load the last selected palette first (editor only)
            #if UNITY_EDITOR
            LoadLastSelectedPalette();
            #endif
            
            // Create default palette if none assigned
            if (_colorPalette == null)
            {
                if (_defaultPalette == null)
                {
                    _defaultPalette = ScriptableObject.CreateInstance<ColorPalette>();
                }
                _colorPalette = _defaultPalette;
            }

            // Subscribe to ColorPalette events
            ColorPalette.OnColorsChanged += OnColorsChanged;
        }

        private void OnEnable()
        {
            // Subscribe to ColorPalette events in editor as well
            ColorPalette.OnColorsChanged += OnColorsChanged;
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            ColorPalette.OnColorsChanged -= OnColorsChanged;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            // Unsubscribe from events
            ColorPalette.OnColorsChanged -= OnColorsChanged;
        }

        // Static convenience methods for easy access
        public static Color Primary => GetPalette().Primary;
        public static Color PrimaryLight => GetPalette().PrimaryLight;
        public static Color PrimaryDark => GetPalette().PrimaryDark;

        public static Color Secondary => GetPalette().Secondary;
        public static Color SecondaryLight => GetPalette().SecondaryLight;
        public static Color SecondaryDark => GetPalette().SecondaryDark;

        public static Color Success => GetPalette().Success;
        public static Color Warning => GetPalette().Warning;
        public static Color Error => GetPalette().Error;
        public static Color Info => GetPalette().Info;

        public static Color TextPrimary => GetPalette().TextPrimary;
        public static Color TextSecondary => GetPalette().TextSecondary;
        public static Color TextDisabled => GetPalette().TextDisabled;
        public static Color TextOnPrimary => GetPalette().TextOnPrimary;
        public static Color TextOnSecondary => GetPalette().TextOnSecondary;

        public static Color Background => GetPalette().Background;
        public static Color Surface => GetPalette().Surface;
        public static Color SurfaceAlt => GetPalette().SurfaceAlt;
        public static Color Divider => GetPalette().Divider;

        public static Color Accent => GetPalette().Accent;
        public static Color Selection => GetPalette().Selection;
        public static Color Hover => GetPalette().Hover;
        public static Color Shadow => GetPalette().Shadow;

        // Method to get color by string name
        public static Color GetColor(string colorName)
        {
            return GetPalette().GetColor(colorName);
        }

        // Custom colors management
        public static void AddCustomColor(string colorName, Color color, string description = "")
        {
            GetPalette().AddCustomColor(colorName, color, description);
        }

        public static bool RemoveCustomColor(string colorName)
        {
            return GetPalette().RemoveCustomColor(colorName);
        }

        public static bool HasCustomColor(string colorName)
        {
            return GetPalette().HasCustomColor(colorName);
        }

        public static Color GetCustomColor(string colorName)
        {
            return GetPalette().GetCustomColor(colorName);
        }

        public static List<string> GetAllColorNames()
        {
            return GetPalette().GetAllColorNames();
        }

        public static List<ColorPalette.CustomColorEntry> GetCustomColors()
        {
            return GetPalette().CustomColors;
        }

        // Active palette management
        public static ColorPalette GetActivePalette()
        {
            return GetPalette();
        }

        // Method to change color palette at runtime
        public static void SetColorPalette(ColorPalette newPalette)
        {
            if (Instance != null && newPalette != null)
            {
                var oldPalette = Instance._colorPalette;
                
                // Only change if it's actually a different palette
                if (oldPalette != newPalette)
                {
                    Instance._colorPalette = newPalette;
                    
                    // Save the selected palette for persistence
                    #if UNITY_EDITOR
                    SaveLastSelectedPalette(newPalette);
                    #endif
                    
                    // Notify about palette change first
                    OnPaletteChanged?.Invoke(newPalette);
                    
                    // Force refresh of all ColoredUI components
                    RefreshAllColoredUI();
                    
                    // Additional force refresh using ForceRefreshColor method
                    ForceRefreshAllColoredUIComponents();
                    
                    // Force refresh all scenes and screens
                    ForceRefreshAllScenesAndScreens();
                }
            }
            else
            {
                Debug.LogWarning("[ColorManager] Cannot set color palette - Instance is null or newPalette is null");
            }
        }

        // Find and switch to palette by name
        public static bool SwitchToPalette(string paletteName)
        {
            #if UNITY_EDITOR
            var palettes = AssetDatabase.FindAssets("t:ColorPalette");
            foreach (var paletteGUID in palettes)
            {
                var path = AssetDatabase.GUIDToAssetPath(paletteGUID);
                var palette = AssetDatabase.LoadAssetAtPath<ColorPalette>(path);
                if (palette != null && palette.name == paletteName)
                {
                    SetColorPalette(palette);
                    return true;
                }
            }
            #endif
            Debug.LogWarning($"[ColorManager] Palette named '{paletteName}' not found");
            return false;
        }

        // Get all available palettes in project
        public static List<ColorPalette> GetAllPalettes()
        {
            var palettes = new List<ColorPalette>();
            
            #if UNITY_EDITOR
            var paletteGUIDs = AssetDatabase.FindAssets("t:ColorPalette");
            foreach (var paletteGUID in paletteGUIDs)
            {
                var path = AssetDatabase.GUIDToAssetPath(paletteGUID);
                var palette = AssetDatabase.LoadAssetAtPath<ColorPalette>(path);
                if (palette != null)
                {
                    palettes.Add(palette);
                }
            }
            #endif
            
            return palettes;
        }

        // Helper method to get the current palette
        private static ColorPalette GetPalette()
        {
            // First check if we have an instance in scene
            if (Instance != null && Instance._colorPalette != null)
            {
                return Instance._colorPalette;
            }

            #if UNITY_EDITOR
            // In editor, try to find ColorPalette in project
            if (!Application.isPlaying)
            {
                var palettes = AssetDatabase.FindAssets("t:ColorPalette");
                if (palettes.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(palettes[0]);
                    var palette = AssetDatabase.LoadAssetAtPath<ColorPalette>(path);
                    if (palette != null)
                    {
                        return palette;
                    }
                }
            }
            #endif

            // Fallback to default palette
            if (_defaultPalette == null)
            {
                _defaultPalette = ScriptableObject.CreateInstance<ColorPalette>();
            }
            return _defaultPalette;
        }

        // Convenience methods for common color operations
        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        public static Color Darken(Color color, float amount = 0.1f)
        {
            return new Color(
                Mathf.Max(0, color.r - amount),
                Mathf.Max(0, color.g - amount),
                Mathf.Max(0, color.b - amount),
                color.a
            );
        }

        public static Color Lighten(Color color, float amount = 0.1f)
        {
            return new Color(
                Mathf.Min(1, color.r + amount),
                Mathf.Min(1, color.g + amount),
                Mathf.Min(1, color.b + amount),
                color.a
            );
        }

        public static string ToHex(Color color)
        {
            return ColorUtility.ToHtmlStringRGBA(color);
        }

        public static Color FromHex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                return color;
            }
            Debug.LogWarning($"Invalid hex color: {hex}");
            return Color.white;
        }

        private void OnColorsChanged(ColorPalette palette)
        {
            // Notify all subscribers about palette change
            OnPaletteChanged?.Invoke(palette);
        }

        // Method to notify about color changes (called from editor)
        public static void NotifyPaletteChanged()
        {
            var palette = GetPalette();
            if (palette != null)
            {
                OnPaletteChanged?.Invoke(palette);
                
                // Force refresh all scenes and screens
                ForceRefreshAllScenesAndScreens();
            }
        }

        /// <summary>
        /// Force refresh all UI colors - useful when switching palettes or debugging
        /// Can be called from editor menu or external scripts
        /// </summary>
        [ContextMenu("Force Refresh All Colors")]
        public static void ForceRefreshAllColors()
        {
            // Notify about palette change
            var currentPalette = GetPalette();
            if (currentPalette != null)
            {
                OnPaletteChanged?.Invoke(currentPalette);
            }
            
            // Refresh using both methods for maximum compatibility
            RefreshAllColoredUI();
            ForceRefreshAllColoredUIComponents();
            
            // Force refresh all scenes and screens
            ForceRefreshAllScenesAndScreens();
        }

        /// <summary>
        /// Force refresh all scenes and screens - useful for debugging or manual refresh
        /// </summary>
        [ContextMenu("Force Refresh All Scenes")]
        public static void ForceRefreshAllScenes()
        {
            ForceRefreshAllScenesAndScreens();
        }

        // Force refresh all ColoredUI components
        public static void RefreshAllColoredUI()
        {
            #if UNITY_EDITOR
            // Find all ColoredUI components in scene (including inactive ones)
            var coloredUIComponents = UnityEngine.Object.FindObjectsByType<ColoredUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            int refreshedCount = 0;
            foreach (var coloredUI in coloredUIComponents)
            {
                if (coloredUI != null)
                {
                    try
                    {
                        coloredUI.ApplyColor();
                        EditorUtility.SetDirty(coloredUI);
                        
                        // Also mark the GameObject as dirty for scene refresh
                        if (coloredUI.gameObject != null)
                        {
                            EditorUtility.SetDirty(coloredUI.gameObject);
                        }
                        refreshedCount++;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[ColorManager] Failed to refresh ColoredUI on {coloredUI.gameObject.name}: {ex.Message}");
                    }
                }
            }
            
            // Force scene refresh after updating all components
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
            
            // Repaint all editor windows for immediate feedback
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            #else
            // In runtime, also refresh all components
            var coloredUIComponents = UnityEngine.Object.FindObjectsByType<ColoredUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            int refreshedCount = 0;
            foreach (var coloredUI in coloredUIComponents)
            {
                if (coloredUI != null)
                {
                    try
                    {
                        coloredUI.ApplyColor();
                        refreshedCount++;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[ColorManager] Failed to refresh ColoredUI on {coloredUI.gameObject.name}: {ex.Message}");
                    }
                }
            }
            #endif
        }

        // Additional force refresh method using ForceRefreshColor
        public static void ForceRefreshAllColoredUIComponents()
        {
            #if UNITY_EDITOR
            // Find all ColoredUI components in scene (including inactive ones)
            var coloredUIComponents = UnityEngine.Object.FindObjectsByType<ColoredUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            int refreshedCount = 0;
            foreach (var coloredUI in coloredUIComponents)
            {
                if (coloredUI != null)
                {
                    try
                    {
                        coloredUI.ForceRefreshColor();
                        refreshedCount++;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[ColorManager] Failed to force refresh ColoredUI on {coloredUI.gameObject.name}: {ex.Message}");
                    }
                }
            }
            #else
            // In runtime, also force refresh all components
            var coloredUIComponents = UnityEngine.Object.FindObjectsByType<ColoredUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            int refreshedCount = 0;
            foreach (var coloredUI in coloredUIComponents)
            {
                if (coloredUI != null)
                {
                    try
                    {
                        coloredUI.ForceRefreshColor();
                        refreshedCount++;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[ColorManager] Failed to force refresh ColoredUI on {coloredUI.gameObject.name}: {ex.Message}");
                    }
                }
            }
            #endif
        }

        /// <summary>
        /// Force refresh all scenes and screens - ensures all UI elements update across all scenes
        /// </summary>
        public static void ForceRefreshAllScenesAndScreens()
        {
            #if UNITY_EDITOR
            // Force refresh all ColoredUI components across all scenes
            var coloredUIComponents = UnityEngine.Object.FindObjectsByType<ColoredUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            int refreshedCount = 0;
            foreach (var coloredUI in coloredUIComponents)
            {
                if (coloredUI != null)
                {
                    try
                    {
                        // Apply color immediately
                        coloredUI.ApplyColor();
                        
                        // Force refresh color
                        coloredUI.ForceRefreshColor();
                        
                        // Mark as dirty for scene refresh
                        EditorUtility.SetDirty(coloredUI);
                        
                        // Also mark the GameObject as dirty
                        if (coloredUI.gameObject != null)
                        {
                            EditorUtility.SetDirty(coloredUI.gameObject);
                        }
                        
                        refreshedCount++;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[ColorManager] Failed to refresh ColoredUI on {coloredUI.gameObject.name}: {ex.Message}");
                    }
                }
            }
            
            // Force scene refresh for all scenes
            SceneView.RepaintAll();
            
            // Force all editor windows to repaint
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            
            // Queue player loop update for complete refresh
            EditorApplication.QueuePlayerLoopUpdate();
            
            // Force refresh of all SceneViews
            if (UnityEditor.EditorWindow.HasOpenInstances<UnityEditor.SceneView>())
            {
                var sceneView = UnityEditor.EditorWindow.GetWindow<UnityEditor.SceneView>();
                sceneView.Repaint();
            }
            
            Debug.Log($"[ColorManager] Refreshed {refreshedCount} UI components across all scenes");
            #else
            // In runtime, refresh all components
            var coloredUIComponents = UnityEngine.Object.FindObjectsByType<ColoredUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            int refreshedCount = 0;
            foreach (var coloredUI in coloredUIComponents)
            {
                if (coloredUI != null)
                {
                    try
                    {
                        coloredUI.ApplyColor();
                        coloredUI.ForceRefreshColor();
                        refreshedCount++;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[ColorManager] Failed to refresh ColoredUI on {coloredUI.gameObject.name}: {ex.Message}");
                    }
                }
            }
            
            Debug.Log($"[ColorManager] Refreshed {refreshedCount} UI components in runtime");
            #endif
        }

        #if UNITY_EDITOR
        // Method to save the last selected palette
        private static void SaveLastSelectedPalette(ColorPalette palette)
        {
            if (palette != null)
            {
                string palettePath = AssetDatabase.GetAssetPath(palette);
                EditorPrefs.SetString(LAST_SELECTED_PALETTE_KEY, palettePath);
            }
        }

        // Method to load the last selected palette
        private static void LoadLastSelectedPalette()
        {
            if (Instance == null) return;
            
            string savedPalettePath = EditorPrefs.GetString(LAST_SELECTED_PALETTE_KEY, "");
            
            if (!string.IsNullOrEmpty(savedPalettePath))
            {
                ColorPalette savedPalette = AssetDatabase.LoadAssetAtPath<ColorPalette>(savedPalettePath);
                
                if (savedPalette != null)
                {
                    Instance._colorPalette = savedPalette;
                }
                else
                {
                    Debug.LogWarning($"[ColorManager] Could not load saved palette from path: {savedPalettePath}. Palette may have been moved or deleted.");
                    // Clear the invalid preference
                    EditorPrefs.DeleteKey(LAST_SELECTED_PALETTE_KEY);
                }
            }
        }

        // Method to clear saved palette preference (useful for debugging or reset)
        public static void ClearSavedPalettePreference()
        {
            EditorPrefs.DeleteKey(LAST_SELECTED_PALETTE_KEY);
        }

        // Diagnostic method to find potential issues with ColoredUI components
        [ContextMenu("Diagnose ColoredUI Components")]
        public static void DiagnoseColoredUIComponents()
        {
            var coloredUIComponents = UnityEngine.Object.FindObjectsByType<ColoredUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            int validComponents = 0;
            int componentsWithIssues = 0;
            
            foreach (var coloredUI in coloredUIComponents)
            {
                if (coloredUI != null)
                {
                    bool hasIssues = false;
                    string issues = "";
                    
                    // Check if it has target components
                    if (coloredUI.GetComponent<UnityEngine.UI.Image>() == null && 
                        coloredUI.GetComponent<TMPro.TextMeshProUGUI>() == null && 
                        coloredUI.GetComponent<UnityEngine.UI.Button>() == null)
                    {
                        hasIssues = true;
                        issues += "No target UI components found; ";
                    }
                    
                    // Check if custom color name is valid
                    if (coloredUI.ColorSource == ColoredUI.ColorSourceType.CustomName && 
                        !HasCustomColor(coloredUI.CustomColorName))
                    {
                        hasIssues = true;
                        issues += $"Invalid custom color name '{coloredUI.CustomColorName}'; ";
                    }
                    
                    if (hasIssues)
                    {
                        Debug.LogWarning($"[ColorManager] Issues with {coloredUI.gameObject.name}: {issues}", coloredUI);
                        componentsWithIssues++;
                    }
                    else
                    {
                        validComponents++;
                    }
                }
            }
        }

        // Helper method to automatically add ColoredUI components to UI elements that don't have them
        [ContextMenu("Auto-Add ColoredUI to UI Elements")]
        public static void AutoAddColoredUIToUIElements()
        {
            var images = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var texts = UnityEngine.Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var buttons = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            int addedCount = 0;
            
            // Add to Images
            foreach (var image in images)
            {
                if (image.GetComponent<ColoredUI>() == null)
                {
                    var coloredUI = image.gameObject.AddComponent<ColoredUI>();
                    coloredUI.SetColorType(ColoredUI.ColorType.Primary);
                    addedCount++;
                    EditorUtility.SetDirty(image.gameObject);
                }
            }
            
            // Add to Texts
            foreach (var text in texts)
            {
                if (text.GetComponent<ColoredUI>() == null)
                {
                    var coloredUI = text.gameObject.AddComponent<ColoredUI>();
                    coloredUI.SetColorType(ColoredUI.ColorType.TextPrimary);
                    addedCount++;
                    EditorUtility.SetDirty(text.gameObject);
                }
            }
            
            // Add to Buttons (if they don't already have ColoredUI from their Image)
            foreach (var button in buttons)
            {
                if (button.GetComponent<ColoredUI>() == null)
                {
                    var coloredUI = button.gameObject.AddComponent<ColoredUI>();
                    coloredUI.SetColorType(ColoredUI.ColorType.Primary);
                    addedCount++;
                    EditorUtility.SetDirty(button.gameObject);
                }
            }
            
            if (addedCount > 0)
            {
                // Refresh all colors after adding new components
                ForceRefreshAllColors();
            }
        }
        #endif
    }
}