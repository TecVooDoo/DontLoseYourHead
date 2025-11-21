using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Collections;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UIColorSystem
{
    /// <summary>
    /// Example component showing how to use the Color System with UI elements
    /// Attach this to UI elements to automatically apply colors from the ColorManager
    /// </summary>
    public class ColoredUI : MonoBehaviour
    {
        [Header("Color Settings")]
        [SerializeField] private ColorSourceType _colorSourceType = ColorSourceType.PredefinedType;
        [SerializeField] private ColorType _colorType = ColorType.Primary;
        [SerializeField] private string _customColorName = "";
        [SerializeField, ReadOnly] private Color _previewColor = Color.white;
        [SerializeField] private bool _applyOnStart = true;
        [SerializeField] private bool _updateInEditor = true;
        [SerializeField] private bool _realTimeUpdate = true;

        [Header("Target Components (Auto-detected if empty)")]
        [SerializeField] private Image _targetImage;
        [SerializeField] private TextMeshProUGUI _targetText;
        [SerializeField] private Button _targetButton;

        #if UNITY_EDITOR
        private Color _lastAppliedColor;
        #endif

        public enum ColorSourceType
        {
            PredefinedType,
            CustomName
        }

        public enum ColorType
        {
            Primary,
            PrimaryLight,
            PrimaryDark,
            Secondary,
            SecondaryLight,
            SecondaryDark,
            Success,
            Warning,
            Error,
            Info,
            TextPrimary,
            TextSecondary,
            TextDisabled,
            TextOnPrimary,
            TextOnSecondary,
            Background,
            Surface,
            SurfaceAlt,
            Divider,
            Accent,
            Selection,
            Hover,
            Shadow
        }

        // Properties for access from external scripts
        public ColorSourceType ColorSource => _colorSourceType;
        public ColorType CurrentColorType => _colorType;
        public string CustomColorName => _customColorName;

        private void Reset()
        {
            // Called when component is added to GameObject in editor
            AutoDetectComponents();
            ApplyColorInEditor();
        }

        private void OnEnable()
        {
            // Subscribe to color change events
            ColorManager.OnPaletteChanged += OnPaletteChanged;
            
            #if UNITY_EDITOR
            // Add callback for editor updates only when not playing
            if (!Application.isPlaying)
            {
                EditorApplication.update += OnEditorUpdate;
                Undo.undoRedoPerformed += OnUndoRedo;
            }
            #endif
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            ColorManager.OnPaletteChanged -= OnPaletteChanged;
            
            #if UNITY_EDITOR
            // Remove editor callbacks
            if (!Application.isPlaying)
            {
                EditorApplication.update -= OnEditorUpdate;
                Undo.undoRedoPerformed -= OnUndoRedo;
            }
            #endif
        }

        private void Start()
        {
            if (_applyOnStart)
            {
                AutoDetectComponents();
                ApplyColor();
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            ColorManager.OnPaletteChanged -= OnPaletteChanged;
            
            #if UNITY_EDITOR
            // Remove editor callbacks
            if (!Application.isPlaying)
            {
                EditorApplication.update -= OnEditorUpdate;
                Undo.undoRedoPerformed -= OnUndoRedo;
            }
            #endif
        }

        // Method called when color palette changes
        private void OnPaletteChanged(ColorPalette newPalette)
        {
            // Refresh color of this component
            ApplyColor();
            
            // Force refresh color to ensure immediate update
            ForceRefreshColor();
            
            #if UNITY_EDITOR
            // Force inspector refresh in editor
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                
                // Mark GameObject as dirty for scene refresh
                if (gameObject != null)
                {
                    EditorUtility.SetDirty(gameObject);
                }
                
                // Force scene refresh
                SceneView.RepaintAll();
                
                // Force all editor windows to repaint
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
            #endif
        }

        #if UNITY_EDITOR
        private void OnUndoRedo()
        {
            // Handle undo/redo operations
            if (!Application.isPlaying)
            {
                ApplyColor();
                EditorUtility.SetDirty(this);
            }
        }

        private void OnEditorUpdate()
        {
            // Check if colors have changed in editor
            if (!Application.isPlaying && (_updateInEditor || _realTimeUpdate))
            {
                Color currentColor = GetCurrentColor();
                
                // Only update if color actually changed (using approximate comparison)
                if (!ColorApproximatelyEquals(currentColor, _previewColor))
                {
                    _previewColor = currentColor;
                    _lastAppliedColor = currentColor;
                    
                    ApplyColorInEditor();
                    
                    // Force scene refresh for real-time preview
                    if (_realTimeUpdate)
                    {
                        SceneView.RepaintAll();
                    }
                    
                    // Mark as dirty for inspector refresh
                    EditorUtility.SetDirty(this);
                }
            }
        }

        #endif

        private bool ColorApproximatelyEquals(Color a, Color b)
        {
            return Mathf.Approximately(a.r, b.r) && 
                   Mathf.Approximately(a.g, b.g) && 
                   Mathf.Approximately(a.b, b.b) && 
                   Mathf.Approximately(a.a, b.a);
        }

        private void OnValidate()
        {
            // Update preview color in editor
            Color newPreviewColor = GetCurrentColor();
            
            // Only update if color actually changed
            if (!ColorApproximatelyEquals(newPreviewColor, _previewColor))
            {
                _previewColor = newPreviewColor;
                
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    // Auto-detect components if needed
                    AutoDetectComponents();
                    
                    // Apply color for immediate visual feedback
                    ApplyColorInEditor();
                    
                    // Force scene refresh
                    SceneView.RepaintAll();
                    EditorUtility.SetDirty(this);
                    
                    // Track last applied color
                    _lastAppliedColor = newPreviewColor;
                }
                else
                #endif
                {
                    // In play mode, apply normally
                    if (_updateInEditor || _realTimeUpdate)
                    {
                        ApplyColor();
                    }
                }
            }
        }

        private void AutoDetectComponents()
        {
            // Auto-detect components if not assigned
            if (_targetImage == null) _targetImage = GetComponent<Image>();
            if (_targetText == null) _targetText = GetComponent<TextMeshProUGUI>();
            if (_targetButton == null) _targetButton = GetComponent<Button>();
        }

        private void ApplyColorInEditor()
        {
            AutoDetectComponents();
            Color color = GetCurrentColor();

            // Apply to Image
            if (_targetImage != null)
            {
                _targetImage.color = color;
                #if UNITY_EDITOR
                EditorUtility.SetDirty(_targetImage);
                #endif
            }

            // Apply to Text
            if (_targetText != null)
            {
                _targetText.color = color;
                #if UNITY_EDITOR
                EditorUtility.SetDirty(_targetText);
                #endif
            }

            // Apply to Button (simplified for editor)
            if (_targetButton != null)
            {
                ColorBlock colorBlock = _targetButton.colors;
                colorBlock.normalColor = Color.white;
                colorBlock.highlightedColor = ColorManager.Lighten(color, 0.1f);
                colorBlock.pressedColor = ColorManager.Darken(color, 0.1f);
                colorBlock.disabledColor = ColorManager.WithAlpha(color, 0.5f);
                _targetButton.colors = colorBlock;
                
                #if UNITY_EDITOR
                EditorUtility.SetDirty(_targetButton);
                #endif
            }
        }

        public void ApplyColor()
        {
            AutoDetectComponents();
            Color color = GetCurrentColor();

            // Apply to Image
            if (_targetImage != null)
            {
                _targetImage.color = color;
            }

            // Apply to Text
            if (_targetText != null)
            {
                _targetText.color = color;
            }

            // Apply to Button
            if (_targetButton != null)
            {
                ColorBlock colorBlock = _targetButton.colors;
                colorBlock.normalColor = Color.white;
                colorBlock.highlightedColor = ColorManager.Lighten(color, 0.1f);
                colorBlock.pressedColor = ColorManager.Darken(color, 0.1f);
                colorBlock.disabledColor = ColorManager.WithAlpha(color, 0.5f);
                _targetButton.colors = colorBlock;
            }

            #if UNITY_EDITOR
            // Update tracking
            _lastAppliedColor = color;
            _previewColor = color;
            #endif
        }

        public void SetColorType(ColorType newColorType)
        {
            _colorSourceType = ColorSourceType.PredefinedType;
            _colorType = newColorType;
            _previewColor = GetCurrentColor();
            ApplyColor();
            
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                SceneView.RepaintAll();
            }
            #endif
        }

        public void SetCustomColor(string customColorName)
        {
            _colorSourceType = ColorSourceType.CustomName;
            _customColorName = customColorName;
            _previewColor = GetCurrentColor();
            ApplyColor();
            
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                SceneView.RepaintAll();
            }
            #endif
        }

        public void SetColor(Color customColor)
        {
            if (_targetImage != null)
                _targetImage.color = customColor;
            
            if (_targetText != null)
                _targetText.color = customColor;
            
            if (_targetButton != null)
            {
                ColorBlock colorBlock = _targetButton.colors;
                colorBlock.normalColor = Color.white;
                colorBlock.highlightedColor = ColorManager.Lighten(customColor, 0.1f);
                colorBlock.pressedColor = ColorManager.Darken(customColor, 0.1f);
                _targetButton.colors = colorBlock;
            }

            #if UNITY_EDITOR
            _lastAppliedColor = customColor;
            _previewColor = customColor;
            #endif
        }

        private Color GetCurrentColor()
        {
            if (_colorSourceType == ColorSourceType.CustomName)
            {
                return GetColorFromCustomName(_customColorName);
            }
            else
            {
                return GetColorFromType(_colorType);
            }
        }

        private Color GetColorFromCustomName(string colorName)
        {
            if (string.IsNullOrEmpty(colorName))
            {
                return Color.white;
            }

            return ColorManager.GetColor(colorName);
        }

        private Color GetColorFromType(ColorType colorType)
        {
            switch (colorType)
            {
                case ColorType.Primary: return ColorManager.Primary;
                case ColorType.PrimaryLight: return ColorManager.PrimaryLight;
                case ColorType.PrimaryDark: return ColorManager.PrimaryDark;
                case ColorType.Secondary: return ColorManager.Secondary;
                case ColorType.SecondaryLight: return ColorManager.SecondaryLight;
                case ColorType.SecondaryDark: return ColorManager.SecondaryDark;
                case ColorType.Success: return ColorManager.Success;
                case ColorType.Warning: return ColorManager.Warning;
                case ColorType.Error: return ColorManager.Error;
                case ColorType.Info: return ColorManager.Info;
                case ColorType.TextPrimary: return ColorManager.TextPrimary;
                case ColorType.TextSecondary: return ColorManager.TextSecondary;
                case ColorType.TextDisabled: return ColorManager.TextDisabled;
                case ColorType.TextOnPrimary: return ColorManager.TextOnPrimary;
                case ColorType.TextOnSecondary: return ColorManager.TextOnSecondary;
                case ColorType.Background: return ColorManager.Background;
                case ColorType.Surface: return ColorManager.Surface;
                case ColorType.SurfaceAlt: return ColorManager.SurfaceAlt;
                case ColorType.Divider: return ColorManager.Divider;
                case ColorType.Accent: return ColorManager.Accent;
                case ColorType.Selection: return ColorManager.Selection;
                case ColorType.Hover: return ColorManager.Hover;
                case ColorType.Shadow: return ColorManager.Shadow;
                default: return Color.white;
            }
        }

        // Editor convenience methods
        [ContextMenu("Apply Color Now")]
        public void ApplyColorNow()
        {
            ApplyColor();
            
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                SceneView.RepaintAll();
            }
            #endif
        }

        [ContextMenu("Reset to Primary")]
        public void ResetToPrimary()
        {
            _colorSourceType = ColorSourceType.PredefinedType;
            _colorType = ColorType.Primary;
            ApplyColor();
            
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                SceneView.RepaintAll();
            }
            #endif
        }

        [ContextMenu("Refresh Components")]
        public void RefreshComponents()
        {
            AutoDetectComponents();
            ApplyColor();
            
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                SceneView.RepaintAll();
            }
            #endif
        }

        /// <summary>
        /// Force refresh color from current palette - useful when palette changes
        /// </summary>
        public void ForceRefreshColor()
        {
            // Re-fetch the current color from palette
            Color newColor = GetCurrentColor();
            _previewColor = newColor;
            
            // Apply the color immediately
            ApplyColor();
            
            #if UNITY_EDITOR
            _lastAppliedColor = newColor;
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                SceneView.RepaintAll();
            }
            #endif
        }

        // Utility methods for getting available color names
        #if UNITY_EDITOR
        public static string[] GetAvailableCustomColorNames()
        {
            var palette = ColorManager.GetActivePalette();
            if (palette != null && palette.CustomColors.Count > 0)
            {
                return palette.CustomColors.Select(c => c.colorName).ToArray();
            }
            return new string[0];
        }

        public static string[] GetAllAvailableColorNames()
        {
            var palette = ColorManager.GetActivePalette();
            if (palette != null)
            {
                return palette.GetAllColorNames().ToArray();
            }
            return new string[0];
        }
        #endif
    }
}