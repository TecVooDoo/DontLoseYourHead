using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace UIColorSystem
{
    [CustomEditor(typeof(ColoredUI))]
    public class ColoredUIEditor : UnityEditor.Editor
    {
        private ColoredUI coloredUI;
        private GUIStyle colorPreviewStyle;
        private Color lastPreviewColor;

        private SerializedProperty colorSourceTypeProp;
        private SerializedProperty colorTypeProp;
        private SerializedProperty customColorNameProp;
        private SerializedProperty previewColorProp;
        private SerializedProperty applyOnStartProp;
        private SerializedProperty updateInEditorProp;
        private SerializedProperty realTimeUpdateProp;
        private SerializedProperty targetImageProp;
        private SerializedProperty targetTextProp;
        private SerializedProperty targetButtonProp;

        private void OnEnable()
        {
            coloredUI = (ColoredUI)target;
            
            // Get serialized properties
            colorSourceTypeProp = serializedObject.FindProperty("_colorSourceType");
            colorTypeProp = serializedObject.FindProperty("_colorType");
            customColorNameProp = serializedObject.FindProperty("_customColorName");
            previewColorProp = serializedObject.FindProperty("_previewColor");
            applyOnStartProp = serializedObject.FindProperty("_applyOnStart");
            updateInEditorProp = serializedObject.FindProperty("_updateInEditor");
            realTimeUpdateProp = serializedObject.FindProperty("_realTimeUpdate");
            targetImageProp = serializedObject.FindProperty("_targetImage");
            targetTextProp = serializedObject.FindProperty("_targetText");
            targetButtonProp = serializedObject.FindProperty("_targetButton");

            // Store initial preview color
            lastPreviewColor = previewColorProp.colorValue;
            
            // Subscribe to editor updates for real-time preview
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            // Update preview color in real-time
            if (coloredUI != null && !Application.isPlaying)
            {
                Color currentColor = GetColorFromType((ColoredUI.ColorType)colorTypeProp.enumValueIndex);
                if (currentColor != lastPreviewColor)
                {
                    lastPreviewColor = currentColor;
                    previewColorProp.colorValue = currentColor;
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    Repaint();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (colorPreviewStyle == null)
            {
                colorPreviewStyle = new GUIStyle(GUI.skin.box);
                colorPreviewStyle.fixedHeight = 40;
                colorPreviewStyle.margin = new RectOffset(4, 4, 4, 4);
            }

            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ColoredUI Component", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This component automatically applies colors from the active ColorPalette. Color changes in the palette are immediately visible.", MessageType.Info);
            EditorGUILayout.Space();

            // Color Settings Section
            EditorGUILayout.LabelField("Color Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            // Color Source Type dropdown
            EditorGUILayout.PropertyField(colorSourceTypeProp, new GUIContent("Color Source"));

            var colorSourceType = (ColoredUI.ColorSourceType)colorSourceTypeProp.enumValueIndex;

            if (colorSourceType == ColoredUI.ColorSourceType.PredefinedType)
            {
                // Predefined color type dropdown
                EditorGUILayout.PropertyField(colorTypeProp, new GUIContent("Color Type", "Choose color type from palette"));
            }
            else
            {
                // Custom color name with dropdown
                DrawCustomColorNameField();
            }

            // Color preview
            Color previewColor = previewColorProp.colorValue;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Color Preview:", GUILayout.Width(100));
            
            var rect = GUILayoutUtility.GetRect(60, 30, colorPreviewStyle);
            EditorGUI.DrawRect(rect, previewColor);
            
            EditorGUILayout.LabelField($"#{ColorUtility.ToHtmlStringRGBA(previewColor)}", EditorStyles.miniLabel, GUILayout.Width(80));
            
            if (GUILayout.Button("Copy", GUILayout.Width(50)))
            {
                EditorGUIUtility.systemCopyBuffer = $"#{ColorUtility.ToHtmlStringRGBA(previewColor)}";
                Debug.Log($"Copied color to clipboard: #{ColorUtility.ToHtmlStringRGBA(previewColor)}");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(previewColorProp, new GUIContent("Preview Color (Read-Only)", "Current color from palette"));
            GUI.enabled = false;
            EditorGUILayout.PropertyField(previewColorProp, new GUIContent("", ""));
            GUI.enabled = true;

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            // Behavior Settings Section
            EditorGUILayout.LabelField("Behavior Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(applyOnStartProp, new GUIContent("Apply On Start", "Whether to apply color during Start()"));
            EditorGUILayout.PropertyField(updateInEditorProp, new GUIContent("Update In Editor", "Whether to update colors in editor"));
            EditorGUILayout.PropertyField(realTimeUpdateProp, new GUIContent("Real Time Update", "Whether to update colors immediately"));
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            // Target Components Section
            EditorGUILayout.LabelField("Target Components", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("If left empty, components will be automatically detected.", MessageType.Info);
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(targetImageProp, new GUIContent("Target Image", "Image component to colorize"));
            EditorGUILayout.PropertyField(targetTextProp, new GUIContent("Target Text", "TextMeshPro component to colorize"));
            EditorGUILayout.PropertyField(targetButtonProp, new GUIContent("Target Button", "Button component to colorize"));
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            // Component Status Section
            DrawComponentStatus();

            EditorGUILayout.Space();

            // Action Buttons
            DrawActionButtons(coloredUI);

            EditorGUILayout.Space();

            // Info section
            DrawInfoSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCustomColorNameField()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Get available custom color names
            var availableColorNames = ColoredUI.GetAvailableCustomColorNames();
            var allColorNames = ColoredUI.GetAllAvailableColorNames();
            
            if (allColorNames.Length > 0)
            {
                // Create options for dropdown (all colors + "Custom...")
                var dropdownOptions = allColorNames.Concat(new[] { "Custom..." }).ToArray();
                
                // Find current selection index
                string currentValue = customColorNameProp.stringValue;
                int selectedIndex = System.Array.IndexOf(allColorNames, currentValue);
                if (selectedIndex < 0) selectedIndex = allColorNames.Length; // "Custom..." option
                
                // Show dropdown
                int newIndex = EditorGUILayout.Popup("Custom Color", selectedIndex, dropdownOptions);
                
                if (newIndex != selectedIndex)
                {
                    if (newIndex < allColorNames.Length)
                    {
                        // Selected an existing color
                        customColorNameProp.stringValue = allColorNames[newIndex];
                    }
                    else
                    {
                        // Selected "Custom..." - keep current value but allow manual editing
                    }
                }
                
                // If "Custom..." is selected, show text field
                if (selectedIndex >= allColorNames.Length)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(customColorNameProp, new GUIContent("Color Name"));
                    EditorGUI.indentLevel--;
                }
                else
                {
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                // No colors available, show text field
                EditorGUILayout.PropertyField(customColorNameProp, new GUIContent("Custom Color Name"));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.HelpBox("No custom colors available. Create some in the Color Palette Window.", MessageType.Info);
            }
        }

        private void DrawActionButtons(ColoredUI coloredUI)
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Apply Color Now"))
            {
                coloredUI.ApplyColor();
                SceneView.RepaintAll();
                EditorUtility.SetDirty(coloredUI);
                Debug.Log("Color was applied manually");
            }
            
            if (GUILayout.Button("Detect Components"))
            {
                coloredUI.RefreshComponents();
                serializedObject.Update(); // Refresh to show detected components
                Debug.Log("Components were automatically detected");
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Reset to Primary"))
            {
                coloredUI.ResetToPrimary();
                serializedObject.Update();
                Debug.Log("Color type was reset to Primary");
            }
            
            if (GUILayout.Button("Refresh Scene"))
            {
                SceneView.RepaintAll();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawInfoSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Info", EditorStyles.boldLabel);
            
            var palette = ColorManager.GetActivePalette();
            if (palette != null)
            {
                var infoText = $"Active Palette: {palette.name}\n" +
                              $"Built-in Colors: 24\n" +
                              $"Custom Colors: {palette.CustomColors.Count}";
                
                EditorGUILayout.HelpBox(infoText, MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("No active color palette found.", MessageType.Warning);
            }
        }

        private void DrawComponentStatus()
        {
            EditorGUILayout.LabelField("Component Status", EditorStyles.boldLabel);
            
            // Check for components and show status
            var image = coloredUI.GetComponent<Image>();
            var text = coloredUI.GetComponent<TextMeshProUGUI>();
            var button = coloredUI.GetComponent<Button>();
            
            EditorGUI.indentLevel++;
            
            // Image status
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Image:", GUILayout.Width(80));
            if (image != null)
            {
                EditorGUILayout.LabelField("✓ Found", EditorStyles.miniLabel);
                EditorGUILayout.ColorField(GUIContent.none, image.color, false, false, false, GUILayout.Width(30));
            }
            else
            {
                EditorGUILayout.LabelField("✗ Not found", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
            
            // Text status
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Text:", GUILayout.Width(80));
            if (text != null)
            {
                EditorGUILayout.LabelField("✓ Found", EditorStyles.miniLabel);
                EditorGUILayout.ColorField(GUIContent.none, text.color, false, false, false, GUILayout.Width(30));
            }
            else
            {
                EditorGUILayout.LabelField("✗ Not found", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
            
            // Button status
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Button:", GUILayout.Width(80));
            if (button != null)
            {
                EditorGUILayout.LabelField("✓ Found", EditorStyles.miniLabel);
                EditorGUILayout.ColorField(GUIContent.none, button.colors.normalColor, false, false, false, GUILayout.Width(30));
            }
            else
            {
                EditorGUILayout.LabelField("✗ Not found", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.indentLevel--;
        }

        private Color GetColorFromType(ColoredUI.ColorType colorType)
        {
            switch (colorType)
            {
                case ColoredUI.ColorType.Primary: return ColorManager.Primary;
                case ColoredUI.ColorType.PrimaryLight: return ColorManager.PrimaryLight;
                case ColoredUI.ColorType.PrimaryDark: return ColorManager.PrimaryDark;
                case ColoredUI.ColorType.Secondary: return ColorManager.Secondary;
                case ColoredUI.ColorType.SecondaryLight: return ColorManager.SecondaryLight;
                case ColoredUI.ColorType.SecondaryDark: return ColorManager.SecondaryDark;
                case ColoredUI.ColorType.Success: return ColorManager.Success;
                case ColoredUI.ColorType.Warning: return ColorManager.Warning;
                case ColoredUI.ColorType.Error: return ColorManager.Error;
                case ColoredUI.ColorType.Info: return ColorManager.Info;
                case ColoredUI.ColorType.TextPrimary: return ColorManager.TextPrimary;
                case ColoredUI.ColorType.TextSecondary: return ColorManager.TextSecondary;
                case ColoredUI.ColorType.TextDisabled: return ColorManager.TextDisabled;
                case ColoredUI.ColorType.TextOnPrimary: return ColorManager.TextOnPrimary;
                case ColoredUI.ColorType.TextOnSecondary: return ColorManager.TextOnSecondary;
                case ColoredUI.ColorType.Background: return ColorManager.Background;
                case ColoredUI.ColorType.Surface: return ColorManager.Surface;
                case ColoredUI.ColorType.SurfaceAlt: return ColorManager.SurfaceAlt;
                case ColoredUI.ColorType.Divider: return ColorManager.Divider;
                case ColoredUI.ColorType.Accent: return ColorManager.Accent;
                case ColoredUI.ColorType.Selection: return ColorManager.Selection;
                case ColoredUI.ColorType.Hover: return ColorManager.Hover;
                case ColoredUI.ColorType.Shadow: return ColorManager.Shadow;
                default: return Color.white;
            }
        }
    }
}