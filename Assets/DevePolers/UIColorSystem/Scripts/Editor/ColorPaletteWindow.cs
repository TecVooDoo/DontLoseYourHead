using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace UIColorSystem
{
    public class ColorPaletteWindow : EditorWindow
    {
        private ColorPalette currentPalette;
        private Vector2 scrollPosition;
        private GUIStyle colorBlockStyle;
        private GUIStyle headerStyle;
        private GUIStyle sectionStyle;
        private string searchFilter = "";
        
        // Custom colors section
        private bool showCustomColorsSection = true;
        private string newCustomColorName = "";
        private Color newCustomColor = Color.white;
        private string newCustomColorDescription = "";
        
        // Palette management
        private List<ColorPalette> availablePalettes = new List<ColorPalette>();
        private string[] paletteNames = new string[0];
        private int selectedPaletteIndex = 0;
        
        [MenuItem("Window/UIColorSystem/Color Palette")]
        public static void ShowWindow()
        {
            var window = GetWindow<ColorPaletteWindow>("UIColorSystem Color Palette");
            window.minSize = new Vector2(450, 700);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshAvailablePalettes();
            
            // Automatycznie ustaw aktywną paletę z ColorManager
            currentPalette = ColorManager.GetActivePalette();
            UpdateSelectedPaletteIndex();
            
            // Subskrybuj się na zmiany palety
            ColorManager.OnPaletteChanged += OnPaletteChanged;
        }

        private void OnDisable()
        {
            // Odsubskrybuj się z eventów
            ColorManager.OnPaletteChanged -= OnPaletteChanged;
        }

        private void OnPaletteChanged(ColorPalette newPalette)
        {
            currentPalette = newPalette;
            UpdateSelectedPaletteIndex();
            Repaint();
        }

        private void RefreshAvailablePalettes()
        {
            availablePalettes = ColorManager.GetAllPalettes();
            paletteNames = availablePalettes.Select(p => p.name).ToArray();
        }

        private void UpdateSelectedPaletteIndex()
        {
            if (currentPalette != null)
            {
                selectedPaletteIndex = availablePalettes.FindIndex(p => p == currentPalette);
                if (selectedPaletteIndex < 0) selectedPaletteIndex = 0;
            }
        }

        private void InitializeStyles()
        {
            if (colorBlockStyle == null)
            {
                colorBlockStyle = new GUIStyle(GUI.skin.box);
                colorBlockStyle.fixedHeight = 25;
                colorBlockStyle.margin = new RectOffset(4, 4, 2, 2);
                colorBlockStyle.border = new RectOffset(1, 1, 1, 1);
            }

            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel);
                headerStyle.fontSize = 16;
                headerStyle.margin = new RectOffset(0, 0, 10, 10);
            }

            if (sectionStyle == null)
            {
                sectionStyle = new GUIStyle(EditorStyles.boldLabel);
                sectionStyle.fontSize = 12;
                sectionStyle.margin = new RectOffset(0, 0, 5, 5);
            }
        }

        private void OnGUI()
        {
            InitializeStyles();

            EditorGUILayout.Space(10);
            
            // Active palette in title
            string paletteTitle = currentPalette ? $"Color Palette Manager - {currentPalette.name}" : "Color Palette Manager - No Palette";
            EditorGUILayout.LabelField(paletteTitle, headerStyle);
            EditorGUILayout.Space(5);

            // Palette Selection and Management
            DrawPaletteManagement();

            if (currentPalette == null)
            {
                EditorGUILayout.HelpBox("No ColorPalette found in project. Create a new ColorPalette asset.", MessageType.Warning);
                if (GUILayout.Button("Create New ColorPalette"))
                {
                    CreateNewColorPalette();
                }
                return;
            }

            EditorGUILayout.Space(10);

            // Search Filter
            DrawSearchFilter();

            EditorGUILayout.Space(5);

            // Main Content
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Color Categories
            DrawColorCategories();
            
            // Custom Colors Section
            DrawCustomColorsSection();

            EditorGUILayout.EndScrollView();

            // Tools Section
            DrawToolsSection();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(currentPalette);
            }
        }

        private void DrawPaletteManagement()
        {
            EditorGUILayout.LabelField("Palette Management", sectionStyle);
            
            EditorGUILayout.BeginHorizontal();
            
            // Palette selection dropdown
            if (paletteNames.Length > 0)
            {
                int newSelectedIndex = EditorGUILayout.Popup("Active Palette:", selectedPaletteIndex, paletteNames);
                if (newSelectedIndex != selectedPaletteIndex && newSelectedIndex >= 0 && newSelectedIndex < availablePalettes.Count)
                {
                    selectedPaletteIndex = newSelectedIndex;
                    var newPalette = availablePalettes[selectedPaletteIndex];
                    ColorManager.SetColorPalette(newPalette);
                    currentPalette = newPalette;
                }
            }
            else
            {
                EditorGUILayout.LabelField("No available palettes");
            }
            
            if (GUILayout.Button("Refresh", GUILayout.Width(70)))
            {
                RefreshAvailablePalettes();
                UpdateSelectedPaletteIndex();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Create New Palette"))
            {
                CreateNewColorPalette();
            }
            
            if (GUILayout.Button("Duplicate Palette") && currentPalette != null)
            {
                DuplicateCurrentPalette();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Status info
            if (currentPalette != null)
            {
                EditorGUILayout.HelpBox($"Current palette: {currentPalette.name}\nCustom colors count: {currentPalette.CustomColors.Count}\n✓ This palette selection will be remembered", MessageType.Info);
            }
        }

        private void DrawSearchFilter()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔍", GUILayout.Width(20));
            searchFilter = EditorGUILayout.TextField("Search colors:", searchFilter);
            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                searchFilter = "";
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawColorCategories()
        {
            // Primary Colors
            DrawColorSection("Primary Colors", new[]
            {
                ("Primary", "_primary"),
                ("Primary Light", "_primaryLight"),
                ("Primary Dark", "_primaryDark")
            });

            // Secondary Colors
            DrawColorSection("Secondary Colors", new[]
            {
                ("Secondary", "_secondary"),
                ("Secondary Light", "_secondaryLight"),
                ("Secondary Dark", "_secondaryDark")
            });

            // Feedback Colors
            DrawColorSection("Feedback Colors", new[]
            {
                ("Success", "_success"),
                ("Warning", "_warning"),
                ("Error", "_error"),
                ("Info", "_info")
            });

            // Text Colors
            DrawColorSection("Text Colors", new[]
            {
                ("Text Primary", "_textPrimary"),
                ("Text Secondary", "_textSecondary"),
                ("Text Disabled", "_textDisabled"),
                ("Text On Primary", "_textOnPrimary"),
                ("Text On Secondary", "_textOnSecondary")
            });

            // Background Colors
            DrawColorSection("Background Colors", new[]
            {
                ("Background", "_background"),
                ("Surface", "_surface"),
                ("Surface Alt", "_surfaceAlt"),
                ("Divider", "_divider")
            });

            // Additional Colors
            DrawColorSection("Additional Colors", new[]
            {
                ("Accent", "_accent"),
                ("Selection", "_selection"),
                ("Hover", "_hover"),
                ("Shadow", "_shadow")
            });
        }

        private void DrawCustomColorsSection()
        {
            EditorGUILayout.Space(10);
            
            // Custom Colors Header
            EditorGUILayout.BeginHorizontal();
            showCustomColorsSection = EditorGUILayout.Foldout(showCustomColorsSection, "Custom Colors", true);
            EditorGUILayout.LabelField($"({currentPalette.CustomColors.Count})", GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();
            
            if (showCustomColorsSection)
            {
                EditorGUI.indentLevel++;
                
                // Add new custom color
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Add New Color", EditorStyles.boldLabel);
                
                newCustomColorName = EditorGUILayout.TextField("Name:", newCustomColorName);
                newCustomColor = EditorGUILayout.ColorField("Color:", newCustomColor);
                newCustomColorDescription = EditorGUILayout.TextField("Description (optional):", newCustomColorDescription);
                
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = !string.IsNullOrEmpty(newCustomColorName);
                if (GUILayout.Button("Add Color"))
                {
                    currentPalette.AddCustomColor(newCustomColorName, newCustomColor, newCustomColorDescription);
                    newCustomColorName = "";
                    newCustomColor = Color.white;
                    newCustomColorDescription = "";
                    EditorUtility.SetDirty(currentPalette);
                }
                GUI.enabled = true;
                
                if (GUILayout.Button("Clear", GUILayout.Width(60)))
                {
                    newCustomColorName = "";
                    newCustomColor = Color.white;
                    newCustomColorDescription = "";
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(5);
                
                // Display existing custom colors
                var customColors = currentPalette.CustomColors;
                if (customColors.Count > 0)
                {
                    for (int i = 0; i < customColors.Count; i++)
                    {
                        var customColor = customColors[i];
                        
                        // Check filter
                        if (!string.IsNullOrEmpty(searchFilter))
                        {
                            if (!customColor.colorName.ToLower().Contains(searchFilter.ToLower()) &&
                                !customColor.description.ToLower().Contains(searchFilter.ToLower()))
                            {
                                continue;
                            }
                        }
                        
                        DrawCustomColorField(customColor, i);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No custom colors. Add the first one above!", EditorStyles.miniLabel);
                }
                
                EditorGUI.indentLevel--;
            }
        }

        private void DrawCustomColorField(ColorPalette.CustomColorEntry customColor, int index)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Color preview block
            var rect = GUILayoutUtility.GetRect(40, 25, colorBlockStyle);
            EditorGUI.DrawRect(rect, customColor.color);
            
            // Color picker
            Color newColor = EditorGUILayout.ColorField(customColor.colorName, customColor.color);
            
            if (newColor != customColor.color)
            {
                Undo.RecordObject(currentPalette, $"Change Custom Color {customColor.colorName}");
                customColor.color = newColor;
                EditorUtility.SetDirty(currentPalette);
            }
            
            // Hex value display
            string hexValue = $"#{ColorUtility.ToHtmlStringRGBA(customColor.color)}";
            EditorGUILayout.LabelField(hexValue, EditorStyles.miniLabel, GUILayout.Width(80));
            
            // Copy button
            if (GUILayout.Button("Copy", GUILayout.Width(50)))
            {
                EditorGUIUtility.systemCopyBuffer = hexValue;
            }
            
            // Remove button
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                if (EditorUtility.DisplayDialog("Remove Color", $"Are you sure you want to remove color '{customColor.colorName}'?", "Remove", "Cancel"))
                {
                    Undo.RecordObject(currentPalette, $"Remove Custom Color {customColor.colorName}");
                    currentPalette.CustomColors.RemoveAt(index);
                    EditorUtility.SetDirty(currentPalette);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Description
            if (!string.IsNullOrEmpty(customColor.description))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(customColor.description, EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawColorSection(string sectionName, (string displayName, string fieldName)[] colors)
        {
            // Check if section contains colors matching the filter
            if (!string.IsNullOrEmpty(searchFilter))
            {
                bool hasMatchingColors = colors.Any(c => 
                    c.displayName.ToLower().Contains(searchFilter.ToLower()) ||
                    c.fieldName.ToLower().Contains(searchFilter.ToLower()));
                
                if (!hasMatchingColors) return;
            }

            EditorGUILayout.Space(10);
            
            // Section Header with toggle
            EditorGUILayout.BeginHorizontal();
            bool sectionExpanded = EditorGUILayout.Foldout(true, sectionName, true);
            EditorGUILayout.EndHorizontal();

            if (sectionExpanded)
            {
                EditorGUI.indentLevel++;
                
                foreach (var (displayName, fieldName) in colors)
                {
                    // Check filter for individual color
                    if (!string.IsNullOrEmpty(searchFilter))
                    {
                        if (!displayName.ToLower().Contains(searchFilter.ToLower()) &&
                            !fieldName.ToLower().Contains(searchFilter.ToLower()))
                        {
                            continue;
                        }
                    }

                    DrawColorField(displayName, fieldName);
                }
                
                EditorGUI.indentLevel--;
            }
        }

        private void DrawColorField(string displayName, string fieldName)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Get the color value using reflection
            var field = typeof(ColorPalette).GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                Color currentColor = (Color)field.GetValue(currentPalette);
                
                // Color preview block
                var rect = GUILayoutUtility.GetRect(40, 25, colorBlockStyle);
                EditorGUI.DrawRect(rect, currentColor);
                
                // Color picker
                Color newColor = EditorGUILayout.ColorField(displayName, currentColor);
                
                if (newColor != currentColor)
                {
                    Undo.RecordObject(currentPalette, $"Change {displayName} Color");
                    field.SetValue(currentPalette, newColor);
                }
                
                // Hex value display
                string hexValue = $"#{ColorUtility.ToHtmlStringRGBA(currentColor)}";
                EditorGUILayout.LabelField(hexValue, EditorStyles.miniLabel, GUILayout.Width(80));
                
                // Copy button
                if (GUILayout.Button("Copy", GUILayout.Width(50)))
                {
                    EditorGUIUtility.systemCopyBuffer = hexValue;
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolsSection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Tools", sectionStyle);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Reset to Defaults"))
            {
                if (EditorUtility.DisplayDialog("Confirm Reset", 
                    "Are you sure you want to reset all colors to default values? (Custom colors will be preserved)", 
                    "Yes", "Cancel"))
                {
                    ResetToDefaults();
                }
            }
            
            if (GUILayout.Button("Clear Custom Colors"))
            {
                if (EditorUtility.DisplayDialog("Confirm Deletion", 
                    "Are you sure you want to delete all custom colors?", 
                    "Yes", "Cancel"))
                {
                    ClearCustomColors();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Export JSON"))
            {
                ExportToJSON();
            }
            
            if (GUILayout.Button("Import JSON"))
            {
                ImportFromJSON();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Copy All Hex"))
            {
                CopyAllHexValues();
            }
            
            if (GUILayout.Button("Refresh UI"))
            {
                ColorManager.RefreshAllColoredUI();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Reset Palette Memory"))
            {
                if (EditorUtility.DisplayDialog("Reset Palette Memory", 
                    "This will clear the saved palette preference. Next time Unity starts, it will revert to the default palette selection.", 
                    "Clear", "Cancel"))
                {
                    ColorManager.ClearSavedPalettePreference();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void CreateNewColorPalette()
        {
            var palette = ScriptableObject.CreateInstance<ColorPalette>();
            var path = EditorUtility.SaveFilePanelInProject("Create ColorPalette", "ColorPalette", "asset", "Choose location");
            
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(palette, path);
                AssetDatabase.SaveAssets();
                
                RefreshAvailablePalettes();
                ColorManager.SetColorPalette(palette);
                currentPalette = palette;
                UpdateSelectedPaletteIndex();
                
                EditorGUIUtility.PingObject(palette);
            }
        }

        private void DuplicateCurrentPalette()
        {
            if (currentPalette == null) return;
            
            var duplicate = Object.Instantiate(currentPalette);
            var path = EditorUtility.SaveFilePanelInProject("Duplicate ColorPalette", $"{currentPalette.name}_Copy", "asset", "Choose location");
            
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(duplicate, path);
                AssetDatabase.SaveAssets();
                
                RefreshAvailablePalettes();
                ColorManager.SetColorPalette(duplicate);
                currentPalette = duplicate;
                UpdateSelectedPaletteIndex();
                
                EditorGUIUtility.PingObject(duplicate);
            }
        }

        private void ResetToDefaults()
        {
            if (currentPalette == null) return;
            
            Undo.RecordObject(currentPalette, "Reset Color Palette");
            currentPalette.ResetAllColors();
            EditorUtility.SetDirty(currentPalette);
        }

        private void ClearCustomColors()
        {
            if (currentPalette == null) return;
            
            Undo.RecordObject(currentPalette, "Clear Custom Colors");
            currentPalette.CustomColors.Clear();
            EditorUtility.SetDirty(currentPalette);
        }

        private void ExportToJSON()
        {
            if (currentPalette == null) return;
            
            var json = JsonUtility.ToJson(currentPalette, true);
            var path = EditorUtility.SaveFilePanel("Eksportuj paletę kolorów", "", $"{currentPalette.name}.json", "json");
            
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, json);
            }
        }

        private void ImportFromJSON()
        {
            if (currentPalette == null) return;
            
            var path = EditorUtility.OpenFilePanel("Importuj paletę kolorów", "", "json");
            
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(path);
                    JsonUtility.FromJsonOverwrite(json, currentPalette);
                    EditorUtility.SetDirty(currentPalette);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Błąd podczas importowania: {e.Message}");
                }
            }
        }

        private void CopyAllHexValues()
        {
            if (currentPalette == null) return;
            
            var fields = typeof(ColorPalette).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hexValues = new System.Text.StringBuilder();
            
            hexValues.AppendLine($"=== {currentPalette.name} Color Palette ===");
            hexValues.AppendLine();
            
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(Color))
                {
                    var color = (Color)field.GetValue(currentPalette);
                    var hexValue = $"#{ColorUtility.ToHtmlStringRGBA(color)}";
                    hexValues.AppendLine($"{field.Name}: {hexValue}");
                }
            }
            
            // Add custom colors
            if (currentPalette.CustomColors.Count > 0)
            {
                hexValues.AppendLine();
                hexValues.AppendLine("=== Custom Colors ===");
                foreach (var customColor in currentPalette.CustomColors)
                {
                    var hexValue = $"#{ColorUtility.ToHtmlStringRGBA(customColor.color)}";
                    hexValues.AppendLine($"{customColor.colorName}: {hexValue} ({customColor.description})");
                }
            }
            
            EditorGUIUtility.systemCopyBuffer = hexValues.ToString();
        }
    }
} 