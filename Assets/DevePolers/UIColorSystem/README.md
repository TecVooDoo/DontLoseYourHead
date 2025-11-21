# UI Color System

An automatic UI color management system for Unity that instantly refreshes all screens when the color palette changes.

## Features

- ✅ **Automatic refresh of all screens** - changing the ColorPalette immediately updates all UI elements
- ✅ **Multi-scene support** - works across the entire project, not just the current scene
- ✅ **Real-time preview** - color changes are visible instantly in the editor
- ✅ **Support for various UI types** - Image, TextMeshPro, Button
- ✅ **Custom colors** - ability to add your own colors
- ✅ **Event system** - notifications about color changes
- ✅ **Editor tools** - tools for debugging and management

## Quick start

### 1. ColorManager setup

1. Create a GameObject in the scene
2. Add the `ColorManager` component
3. Assign a `ColorPalette` to the `Color Palette` field

### 2. Adding colors to UI elements

1. Select a UI element (Image, Text, Button)
2. Add the `ColoredUI` component
3. Choose a color type (Primary, Secondary, TextPrimary, etc.)
4. The color will be applied automatically

### 3. Changing the color palette

```csharp
// Change the palette programmatically
ColorManager.SetColorPalette(newPalette);

// Or by name
ColorManager.SwitchToPalette("DarkTheme");
```

## Automatic refresh

The system automatically refreshes all screens in the following situations:

- Changing the ColorPalette in ColorManager
- Editing colors in the ColorPalette asset
- Changing the palette programmatically
- Resetting colors to defaults

### Refresh methods

```csharp
// Refresh all colors
ColorManager.ForceRefreshAllColors();

// Refresh all scenes and screens
ColorManager.ForceRefreshAllScenesAndScreens();

// Refresh a specific component
coloredUIComponent.ForceRefreshColor();
```

## Color types

### Basic colors
- `Primary` - the main color of the application
- `PrimaryLight` - a lighter version of primary
- `PrimaryDark` - a darker version of primary
- `Secondary` - the secondary color
- `SecondaryLight` - a lighter version of secondary
- `SecondaryDark` - a darker version of secondary

### Feedback colors
- `Success` - success color
- `Warning` - warning color
- `Error` - error color
- `Info` - information color

### Text colors
- `TextPrimary` - primary text color
- `TextSecondary` - secondary text color
- `TextDisabled` - disabled text color
- `TextOnPrimary` - text on primary background
- `TextOnSecondary` - text on secondary background

### Background colors
- `Background` - background color
- `Surface` - surface color
- `SurfaceAlt` - alternate surface color
- `Divider` - divider color

### Additional colors
- `Accent` - accent color
- `Selection` - selection color
- `Hover` - hover color
- `Shadow` - shadow color

## Custom colors

You can add your own colors to the ColorPalette:

```csharp
// Add a custom color
ColorManager.AddCustomColor("MyCustomColor", Color.red, "Color description");

// Use the custom color in ColoredUI
coloredUI.SetCustomColor("MyCustomColor");
```

## Event system

The system uses events to notify about changes:

```csharp
// Subscribe to palette changes
ColorManager.OnPaletteChanged += OnPaletteChanged;

private void OnPaletteChanged(ColorPalette newPalette)
{
    Debug.Log($"Palette changed to: {newPalette.name}");
}
```

## Debugging tools

### ColorSystemTester

Add the `ColorSystemTester` component to a GameObject to test the system:

- Automatic refresh at a specified interval
- Switching between palettes
- Counting UI components
- System diagnostics

### Context menu

Right-click on ColorManager in the Inspector to access:

- `Force Refresh All Colors` - refresh all colors
- `Force Refresh All Scenes` - refresh all scenes
- `Diagnose ColoredUI Components` - check components
- `Auto-Add ColoredUI to UI Elements` - automatically add components

## Troubleshooting

### Colors are not refreshing

1. Check that ColorManager has a ColorPalette assigned
2. Use `ColorManager.ForceRefreshAllColors()`
3. Check that UI elements have the `ColoredUI` component
4. Use `ColorSystemTester.DiagnoseSystem()`

### Components are not found

1. Make sure `ColoredUI` components are active
2. Check whether they are in inactive scenes
3. Use `FindObjectsByType<ColoredUI>(FindObjectsInactive.Include)`

### Editor issues

1. Check that `Update In Editor` is enabled in ColoredUI
2. Use `SceneView.RepaintAll()` to force a repaint
3. Check that there are no errors in the Console

## Usage examples

### Switching themes at runtime

```csharp
public class ThemeSwitcher : MonoBehaviour
{
    [SerializeField] private ColorPalette lightTheme;
    [SerializeField] private ColorPalette darkTheme;
    
    public void SwitchToLightTheme()
    {
        ColorManager.SetColorPalette(lightTheme);
    }
    
    public void SwitchToDarkTheme()
    {
        ColorManager.SetColorPalette(darkTheme);
    }
}
```

### Dynamic colors

```csharp
public class DynamicColorUI : MonoBehaviour
{
    private ColoredUI coloredUI;
    
    private void Start()
    {
        coloredUI = GetComponent<ColoredUI>();
    }
    
    public void SetToSuccessColor()
    {
        coloredUI.SetColorType(ColoredUI.ColorType.Success);
    }
    
    public void SetToErrorColor()
    {
        coloredUI.SetColorType(ColoredUI.ColorType.Error);
    }
}
```

## Requirements

- Unity 2021.3 LTS or newer
- TextMeshPro (for text support)
- URP (Universal Render Pipeline) - optional

## License

This system is part of the DevePolers UI Color System package. 