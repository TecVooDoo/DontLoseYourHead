# UI Color System - Documentation

## Overview

UI Color System is a comprehensive color management system for Unity projects that enables easy management of color palettes and automatic updating of UI components.

## ✨ New Features

### 🎨 Custom Colors
- **Define custom color names** - You can now create custom colors with arbitrary names
- **Preserve default colors** - All existing predefined colors are preserved
- **Dynamic management** - Add, edit and remove custom colors at runtime

### 🎛️ Palette Management
- **Active palette in editor window** - The `Windows -> UIColorSystem -> Color Palette` window shows the active palette
- **Palette switching** - Changing palette in the window automatically switches all colors in the project
- **Switch between palettes** - Dropdown for quick switching between available palettes

### 🔧 Extended API

#### Custom Color Management
```csharp
// Add custom color
ColorManager.AddCustomColor("BrandBlue", new Color(0.1f, 0.5f, 0.9f), "Main brand color");

// Check if custom color exists
if (ColorManager.HasCustomColor("BrandBlue"))
{
    Color brandColor = ColorManager.GetCustomColor("BrandBlue");
}

// Remove custom color
ColorManager.RemoveCustomColor("BrandBlue");

// Get all color names (built-in + custom)
var allColors = ColorManager.GetAllColorNames();
```

#### Palette Management
```csharp
// Change active palette
ColorManager.SetColorPalette(newPalette);

// Switch to palette by name
ColorManager.SwitchToPalette("DarkTheme");

// Get all available palettes
var availablePalettes = ColorManager.GetAllPalettes();

// Get active palette
var activePalette = ColorManager.GetActivePalette();
```

### 🎯 Extended ColoredUI Component

The `ColoredUI` component has been extended with custom color support:

```csharp
// Set predefined color type
coloredUI.SetColorType(ColoredUI.ColorType.Primary);

// Set custom color
coloredUI.SetCustomColor("BrandBlue");
```

#### New inspector options:
- **Color Source** - Choose between predefined types and custom colors
- **Custom Color dropdown** - List of available custom colors with manual entry option
- **Palette information** - Display current palette and custom color count

## 🚀 How to Use

### 1. Creating Custom Colors

#### In code:
```csharp
void Start()
{
    // Add brand colors
    ColorManager.AddCustomColor("BrandPrimary", new Color(0.2f, 0.4f, 1f), "Main brand color");
    ColorManager.AddCustomColor("BrandSecondary", new Color(1f, 0.6f, 0.1f), "Secondary brand color");
    
    // Use custom color
    myImage.color = ColorManager.GetColor("BrandPrimary");
}
```

#### In editor:
1. Open `Windows -> UIColorSystem -> Color Palette`
2. Expand "Custom Colors" section
3. Enter color name, choose color and optionally add description
4. Click "Add Color"

### 2. Using Custom Colors in ColoredUI

1. Add `ColoredUI` component to UI object
2. Change **Color Source** to "Custom Name"
3. Select custom color from dropdown or enter name manually
4. Color will be automatically applied

### 3. Switching Palettes

#### In editor:
1. Open `Windows -> UIColorSystem -> Color Palette`
2. Use "Active Palette" dropdown to select another palette
3. All `ColoredUI` components will be automatically updated

#### In code:
```csharp
// Switch to another palette
ColorManager.SwitchToPalette("DarkTheme");

// Or assign directly
ColorManager.SetColorPalette(myPalette);
```

### 4. Reacting to Palette Changes

```csharp
void OnEnable()
{
    ColorManager.OnPaletteChanged += OnPaletteChanged;
}

void OnDisable()
{
    ColorManager.OnPaletteChanged -= OnPaletteChanged;
}

void OnPaletteChanged(ColorPalette newPalette)
{
    Debug.Log($"Palette changed to: {newPalette.name}");
    // Your UI update logic
}
```

## 📋 Editor Window Features

### Palette Management
- **Palette selection dropdown** - Quick switching between palettes
- **Create new palette** - Button to create new palette
- **Duplicate palette** - Copy existing palette
- **Palette information** - Active palette status and custom color count

### Custom Colors
- **Custom Colors section** - Manage custom colors
- **Add colors** - Form for adding new custom colors
- **Edit colors** - Direct editing of existing custom colors
- **Remove colors** - "×" button to remove custom colors
- **Color descriptions** - Optional descriptions for better organization

### Tools
- **Reset to defaults** - Reset built-in colors (custom colors preserved)
- **Clear custom colors** - Remove all custom colors
- **Export/Import JSON** - Save and load palettes from JSON files
- **Copy hex** - Copy hex values of all colors
- **Refresh UI** - Force update of all ColoredUI components

## 🔄 Automatic Updates

The system automatically updates all `ColoredUI` components when:
- A color changes in the palette
- The active palette changes
- Custom colors are added/removed
- A new palette is imported

## 💡 Tips

1. **Color organization** - Use descriptions for custom colors for better organization
2. **Naming convention** - Use consistent naming for custom colors (e.g. "Brand", "UI", "Content")
3. **Theme palettes** - Create separate palettes for different themes (Light, Dark, HighContrast)
4. **Palette backup** - Regularly export palettes to JSON as backup
5. **Performance** - System is optimized, but avoid frequent adding/removing colors at runtime

## 🆕 Compatibility

All new features are fully compatible with existing code. You don't need to modify your code - you just gain new capabilities!

## Examples

Check `ColorSystemTest.cs` to see complete examples of using all system features.