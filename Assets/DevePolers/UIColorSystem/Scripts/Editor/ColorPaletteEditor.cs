using UnityEngine;
using UnityEditor;

namespace UIColorSystem
{
    [CustomEditor(typeof(ColorPalette))]
    public class ColorPaletteEditor : UnityEditor.Editor
    {
        private ColorPalette colorPalette;
        private GUIStyle colorBlockStyle;
        private SerializedProperty[] colorProperties;
        private Color[] previousColors;
        private bool needsRefresh = false;

        // Names for easier identification of changed colors
        private string[] colorNames = {
            "Primary", "PrimaryLight", "PrimaryDark",
            "Secondary", "SecondaryLight", "SecondaryDark", 
            "Success", "Warning", "Error", "Info",
            "TextPrimary", "TextSecondary", "TextDisabled", "TextOnPrimary", "TextOnSecondary",
            "Background", "Surface", "SurfaceAlt", "Divider",
            "Accent", "Selection", "Hover", "Shadow"
        };

        private void OnEnable()
        {
            colorPalette = (ColorPalette)target;
            InitializeColorProperties();
            StorePreviousColors();
            
            // Subscribe to editor update for better tracking
            EditorApplication.update += CheckForColorChanges;
        }

        private void OnDisable()
        {
            // Unsubscribe from editor update
            EditorApplication.update -= CheckForColorChanges;
        }

        private void CheckForColorChanges()
        {
            if (needsRefresh && !Application.isPlaying)
            {
                needsRefresh = false;
                PerformRefresh();
            }
        }

        private void InitializeColorProperties()
        {
            // Get all color properties
            colorProperties = new SerializedProperty[]
            {
                serializedObject.FindProperty("_primary"),
                serializedObject.FindProperty("_primaryLight"),
                serializedObject.FindProperty("_primaryDark"),
                serializedObject.FindProperty("_secondary"),
                serializedObject.FindProperty("_secondaryLight"),
                serializedObject.FindProperty("_secondaryDark"),
                serializedObject.FindProperty("_success"),
                serializedObject.FindProperty("_warning"),
                serializedObject.FindProperty("_error"),
                serializedObject.FindProperty("_info"),
                serializedObject.FindProperty("_textPrimary"),
                serializedObject.FindProperty("_textSecondary"),
                serializedObject.FindProperty("_textDisabled"),
                serializedObject.FindProperty("_textOnPrimary"),
                serializedObject.FindProperty("_textOnSecondary"),
                serializedObject.FindProperty("_background"),
                serializedObject.FindProperty("_surface"),
                serializedObject.FindProperty("_surfaceAlt"),
                serializedObject.FindProperty("_divider"),
                serializedObject.FindProperty("_accent"),
                serializedObject.FindProperty("_selection"),
                serializedObject.FindProperty("_hover"),
                serializedObject.FindProperty("_shadow")
            };
        }

        private void StorePreviousColors()
        {
            previousColors = new Color[colorProperties.Length];
            for (int i = 0; i < colorProperties.Length; i++)
            {
                if (colorProperties[i] != null)
                {
                    previousColors[i] = colorProperties[i].colorValue;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (colorBlockStyle == null)
            {
                colorBlockStyle = new GUIStyle(GUI.skin.box);
                colorBlockStyle.fixedHeight = 20;
                colorBlockStyle.margin = new RectOffset(4, 4, 2, 2);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Color Palette", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Edit colors below. Changes will be automatically visible throughout the application in real time. All ColoredUI components will be automatically refreshed.", MessageType.Info);
            EditorGUILayout.Space();

            // Begin checking for changes
            serializedObject.Update();
            
            // Use EditorGUI.BeginChangeCheck for more reliable change detection
            EditorGUI.BeginChangeCheck();

            // Draw color fields
            DrawColorFields();

            // Check if any changes were made
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                
                // Identify which colors changed and notify
                IdentifyAndNotifyChanges();
                
                // Schedule refresh for next frame to ensure all changes are applied
                needsRefresh = true;
                
                // Immediate refresh for better responsiveness
                PerformRefresh();
                
                // Store new previous colors
                StorePreviousColors();
                
                // Repaint this inspector to show updated preview
                Repaint();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Color Preview", EditorStyles.boldLabel);

            // Primary Colors Preview
            DrawColorGroup("Primary Colors", new[]
            {
                ("Primary", colorPalette.Primary),
                ("Primary Light", colorPalette.PrimaryLight),
                ("Primary Dark", colorPalette.PrimaryDark)
            });

            // Secondary Colors Preview
            DrawColorGroup("Secondary Colors", new[]
            {
                ("Secondary", colorPalette.Secondary),
                ("Secondary Light", colorPalette.SecondaryLight),
                ("Secondary Dark", colorPalette.SecondaryDark)
            });

            // Feedback Colors Preview
            DrawColorGroup("Feedback Colors", new[]
            {
                ("Success", colorPalette.Success),
                ("Warning", colorPalette.Warning),
                ("Error", colorPalette.Error),
                ("Info", colorPalette.Info)
            });

            // Text Colors Preview
            DrawColorGroup("Text Colors", new[]
            {
                ("Text Primary", colorPalette.TextPrimary),
                ("Text Secondary", colorPalette.TextSecondary),
                ("Text Disabled", colorPalette.TextDisabled),
                ("Text On Primary", colorPalette.TextOnPrimary),
                ("Text On Secondary", colorPalette.TextOnSecondary)
            });

            // Background Colors Preview
            DrawColorGroup("Background Colors", new[]
            {
                ("Background", colorPalette.Background),
                ("Surface", colorPalette.Surface),
                ("Surface Alt", colorPalette.SurfaceAlt),
                ("Divider", colorPalette.Divider)
            });

            // Additional Colors Preview
            DrawColorGroup("Additional Colors", new[]
            {
                ("Accent", colorPalette.Accent),
                ("Selection", colorPalette.Selection),
                ("Hover", colorPalette.Hover),
                ("Shadow", colorPalette.Shadow)
            });

            EditorGUILayout.Space();

            // Utility buttons
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export to JSON"))
            {
                ExportToJSON();
            }
            if (GUILayout.Button("Reset to Defaults"))
            {
                if (EditorUtility.DisplayDialog("Confirm Reset", 
                    "Are you sure you want to reset all colors to default values?", 
                    "Yes", "Cancel"))
                {
                    ResetToDefaults();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh All UI"))
            {
                RefreshAllColoredUI();
            }
            if (GUILayout.Button("Refresh Scene"))
            {
                SceneView.RepaintAll();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void IdentifyAndNotifyChanges()
        {
            for (int i = 0; i < colorProperties.Length && i < previousColors.Length && i < colorNames.Length; i++)
            {
                if (colorProperties[i] != null)
                {
                    Color currentColor = colorProperties[i].colorValue;
                    if (currentColor != previousColors[i])
                    {
                        // Notify about specific color change
                        colorPalette.NotifyColorChanged(colorNames[i]);
                        Debug.Log($"Changed color: {colorNames[i]} to {ColorUtility.ToHtmlStringRGBA(currentColor)}");
                    }
                }
            }
            
            // Notify about general palette change
            colorPalette.NotifyColorsChanged();
            ColorManager.NotifyPaletteChanged();
        }

        private void PerformRefresh()
        {
            // Mark palette as dirty
            EditorUtility.SetDirty(colorPalette);
            
            // Refresh all ColoredUI components
            RefreshAllColoredUI();
            
            // Force refresh all scenes and screens
            ColorManager.ForceRefreshAllScenesAndScreens();
            
            // Force scene view repaint for immediate visual feedback
            SceneView.RepaintAll();
            
            // Repaint all editor windows for complete refresh
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            
            // Queue player loop update
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private void DrawColorFields()
        {
            string[] sectionNames = { "Primary Colors", "Secondary Colors", "Feedback Colors", "Text Colors", "Background Colors", "Additional Colors" };
            int propertyIndex = 0;

            foreach (string sectionName in sectionNames)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(sectionName, EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                int propertiesInSection = GetPropertiesInSection(sectionName);
                for (int i = 0; i < propertiesInSection && propertyIndex < colorProperties.Length; i++)
                {
                    if (colorProperties[propertyIndex] != null)
                    {
                        EditorGUILayout.PropertyField(colorProperties[propertyIndex]);
                    }
                    propertyIndex++;
                }

                EditorGUI.indentLevel--;
            }
        }

        private int GetPropertiesInSection(string sectionName)
        {
            switch (sectionName)
            {
                case "Primary Colors": return 3;
                case "Secondary Colors": return 3;
                case "Feedback Colors": return 4;
                case "Text Colors": return 5;
                case "Background Colors": return 4;
                case "Additional Colors": return 4;
                default: return 0;
            }
        }

        private void DrawColorGroup(string groupName, (string name, Color color)[] colors)
        {
            EditorGUILayout.LabelField(groupName, EditorStyles.miniLabel);
            
            foreach (var (name, color) in colors)
            {
                EditorGUILayout.BeginHorizontal();
                
                // Color preview box
                var rect = GUILayoutUtility.GetRect(60, 20, colorBlockStyle);
                EditorGUI.DrawRect(rect, color);
                
                // Color name and hex value
                EditorGUILayout.LabelField($"{name}: #{ColorUtility.ToHtmlStringRGBA(color)}", EditorStyles.miniLabel);
                
                // Copy to clipboard button
                if (GUILayout.Button("Copy Hex", GUILayout.Width(70)))
                {
                    EditorGUIUtility.systemCopyBuffer = $"#{ColorUtility.ToHtmlStringRGBA(color)}";
                    Debug.Log($"Copied to clipboard: #{ColorUtility.ToHtmlStringRGBA(color)}");
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(5);
        }

        private void ExportToJSON()
        {
            var json = JsonUtility.ToJson(colorPalette, true);
            var path = EditorUtility.SaveFilePanel("Export Color Palette", "", "ColorPalette.json", "json");
            
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, json);
                Debug.Log($"Color palette exported to: {path}");
            }
        }

        private void ResetToDefaults()
        {
            Undo.RecordObject(colorPalette, "Reset Color Palette");
            
            // Use method from ColorPalette that automatically notifies about changes
            colorPalette.ResetAllColors();
            
            EditorUtility.SetDirty(colorPalette);
            
            // Notify ColorManager about change
            ColorManager.NotifyPaletteChanged();
            
            // Force refresh all scenes and screens
            ColorManager.ForceRefreshAllScenesAndScreens();
            
            // Immediate refresh
            RefreshAllColoredUI();
            SceneView.RepaintAll();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            EditorApplication.QueuePlayerLoopUpdate();
            
            // Update our tracking
            StorePreviousColors();
        }

        private void RefreshAllColoredUI()
        {
            // Find all ColoredUI components in scene
            var coloredUIComponents = FindObjectsByType<ColoredUI>(FindObjectsSortMode.None);
            
            foreach (var coloredUI in coloredUIComponents)
            {
                if (coloredUI != null)
                {
                    // Apply color immediately
                    coloredUI.ApplyColor();
                    
                    // Mark component as dirty for inspector refresh
                    EditorUtility.SetDirty(coloredUI);
                    
                    // Also try to refresh the specific gameobject in scene view
                    if (coloredUI.gameObject != null)
                    {
                        EditorUtility.SetDirty(coloredUI.gameObject);
                    }
                }
            }
            
            // Force refresh all scenes and screens
            ColorManager.ForceRefreshAllScenesAndScreens();
            
            // Force immediate scene refresh
            SceneView.RepaintAll();
            
            // Force all editor windows to repaint
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            
            // Queue player loop update for complete refresh
            EditorApplication.QueuePlayerLoopUpdate();
        }
    }

    // Custom property drawer for better color field visualization
    [CustomPropertyDrawer(typeof(Color))]
    public class ColorPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            // Split the rect into label and color field
            var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            var colorRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, 
                position.width - EditorGUIUtility.labelWidth - 80, position.height);
            var hexRect = new Rect(position.x + position.width - 75, position.y, 75, position.height);
            
            // Draw label
            EditorGUI.LabelField(labelRect, label);
            
            // Draw color field
            Color currentColor = property.colorValue;
            Color newColor = EditorGUI.ColorField(colorRect, currentColor);
            
            if (newColor != currentColor)
            {
                property.colorValue = newColor;
            }
            
            // Draw hex value
            string hexValue = $"#{ColorUtility.ToHtmlStringRGB(currentColor)}";
            EditorGUI.LabelField(hexRect, hexValue, EditorStyles.miniLabel);
            
            EditorGUI.EndProperty();
        }
    }
}