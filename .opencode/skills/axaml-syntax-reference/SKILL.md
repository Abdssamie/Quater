---
name: axaml-syntax-reference
description: Quick reference for Avalonia XAML (AXAML) syntax including namespaces, property syntax, bindings, styles, and SukiUI patterns
license: MIT
compatibility: opencode
metadata:
  audience: avalonia-developers
  framework: avalonia
---

# AXAML Syntax Reference

Complete reference for Avalonia XAML (AXAML) syntax.

## What I do

- Provide quick reference for AXAML root element structure
- Show correct namespace declarations (especially SukiUI)
- Explain property syntax (attribute vs element)
- Demonstrate data binding syntax shortcuts
- Show style selector patterns
- List markup extensions
- Highlight common mistakes and corrections

## When to use me

Use this skill when you need to:

- Set up a new AXAML file with correct structure
- Add namespace declarations for SukiUI or custom types
- Choose between attribute and element property syntax
- Write style selectors for controls
- Use markup extensions like StaticResource or x:Static
- Fix common AXAML syntax errors
- Ensure proper SukiWindow setup

## Basic Structure

### Root Element

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             mc:Ignorable="d" d:DesignWidth="800" d:DesignHeight="600"
             x:Class="Namespace.Views.ViewName"
             x:DataType="vm:ViewModelName">
    <!-- Content -->
</UserControl>
```

**Attributes:**

- `x:Class`: Code-behind class (required)
- `x:DataType`: ViewModel type for compiled bindings (required)

## Namespace Declarations

**Standard:**

```xml
xmlns="https://github.com/avaloniaui"
xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
```

**SukiUI (Critical):**
Always use the URL-based namespace.

```xml
xmlns:suki="https://github.com/kikipoulet/SukiUI"
```

**Custom:**

```xml
xmlns:vm="using:YourApp.ViewModels"
xmlns:views="using:YourApp.Views"
xmlns:local="clr-namespace:YourApp"
```

## Property Syntax

**Attribute (Simple):**

```xml
<Button Content="Click" Width="100" />
```

**Element (Complex):**

```xml
<Button>
    <Button.Content>
        <StackPanel>
            <TextBlock Text="A" />
            <TextBlock Text="B" />
        </StackPanel>
    </Button.Content>
</Button>
```

## Data Binding

**Modes:** `OneWay` (default), `TwoWay` (input), `OneTime`, `OneWayToSource`

**Syntax:**

```xml
<!-- Property -->
<TextBlock Text="{Binding Name}" />

<!-- TwoWay (Input) -->
<TextBox Text="{Binding Name, Mode=TwoWay}" />

<!-- StringFormat -->
<TextBlock Text="{Binding Price, StringFormat='${0:F2}'}" />

<!-- Relative -->
<TextBlock Text="{Binding $parent[Window].Title}" />

<!-- Element -->
<TextBox Name="Input" />
<TextBlock Text="{Binding #Input.Text}" />
```

## Styles

**Selector Syntax:**

```xml
<!-- Type -->
<Style Selector="Button">
    <Setter Property="Background" Value="Blue" />
</Style>

<!-- Class -->
<Style Selector="Button.Primary">
    <Setter Property="Background" Value="Red" />
</Style>

<!-- Pseudo-class -->
<Style Selector="Button:pointerover">
    <Setter Property="Opacity" Value="0.8" />
</Style>

<!-- Child combinator -->
<Style Selector="StackPanel > Button">
    <Setter Property="Margin" Value="5" />
</Style>

<!-- Descendant combinator -->
<Style Selector="Grid Button">
    <Setter Property="Padding" Value="10" />
</Style>
```

**Usage:**

```xml
<Button Classes="Primary" Content="Red Button" />
```

## Markup Extensions

- `{StaticResource Key}`: Fixed resource
- `{DynamicResource Key}`: Dynamic resource (themes)
- `{x:Static Class.Member}`: Static member
- `{x:Null}`: Null value
- `{Binding Path}`: Data binding
- `{CompiledBinding Path}`: Explicit compiled binding
- `{ReflectionBinding Path}`: Opt-out of compiled binding

**Examples:**

```xml
<!-- Static Resource -->
<TextBlock Text="{StaticResource WelcomeMessage}" />

<!-- Dynamic Resource (for theming) -->
<Button Background="{DynamicResource ThemeBrush}" />

<!-- Static Property -->
<TextBlock TexStatic local:Constants.AppName}" />
<PathIcon Data="{x:Static icons:Icons.Home}" />

<!-- Null Value -->
<Button Command="{x:Null}" />
```

## Best Practices

1. **x:DataType:** Always use for compiled bindings

2. **SukiWindow:** Use `SukiWindow` as root for main windows

   ```xml
   <suki:SukiWindow xmlns:suki="https://github.com/kikipoulet/SukiUI"
                    x:Class="YourApp.Views.MainWindow"
                    Title="My App"
                    Width="1200"
                    Height="800">
       <!-- Content -->
   </suki:SukiWindow>
   ```

3. **Explicit Sizes:** Set explicit `Width` and `Height` for windows

4. **BulletDecorator:** Deprecated. Use `StackPanel` with `Orientation="Horizontal"`

5. **TwoWay Binding:** Always explicit for input controls

   ```xml
   <TextBox Text="{Binding Name, Mode=TwoWay}" />
   ```

6. **Namespace Prefixes:** Use consistent, short prefixes
   - `vm` for ViewModels
   - `views` for Views
   - `local` for current namespace
   - `suki` for SukiUI

## Common Mistakes

| Mistake                  | Correction                                |
| ------------------------ | ----------------------------------------- |
| Missing `x:DataType`     | Add `x:DataType="vm:MyVM"`                |
| `xmlns:suki="using:..."` | Use `xmlns:suki="https://github.com/..."` |
| `<Window>` with Suki     | Use `<suki:SukiWindow>`                   |
| Default Binding on Input | Use `Mode=TwoWay`                         |
| Missing `x:Class`        | Add `x:Class="Namespace.Views.ViewName"`  |

## SukiUI Specific Patterns

### SukiWindow Setup

```xml
<suki:SukiWindow xmlns="https://github.com/avaloniaui"
                 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                 xmlns:suki="https://github.com/kikipoulet/SukiUI"
                 xmlns:vm="using:YourApp.ViewModels"
                 xmlns:views="using:YourApp.Views"
                 x:Class="YourApp.Views.MainWindow"
                 x:DataType="vm:MainWindowViewModel"
                 Title="My Application"
                 Width="1200"
                 Height="800">
    
    <!-- DataTemplates for navigation -->
    <suki:SukiWindow.DataTemplates>
        <DataTemplate DataType="vm:HomePageViewModel">
            <views:HomePage />
        </DataTemplate>
        <DataTemplate DataType="vm:SettingsPageViewModel">
            <views:Setting />
        </DataTemplate>
    </suki:SukiWindow.DataTemplates>
    
    <!-- Content -->
    <Grid>
        <!-- Your UI here -->
    </Grid>
</suki:SukiWindow>
```

### SukiSideMenu

```xml
<suki:SukiSideMenu ItemsSource="{Binding Pages}"
                   SelectedItem="{Binding SelectedPage, Mode=TwoWay}"
                   HeaderContent="{Binding AppTitle}">
    <suki:SukiSideMenu.ItemTemplate>
        <DataTemplate>
            <suki:SukiSideMenuItem Header="{Binding DisplayName}">
                <suki:SukiSideMenuItem.Icon>
                    <PathIcon Data="{Binding Icon}" />
                </suki:SukiSideMenuItem.Icon>
            </suki:SukiSideMenuItem>
        </DataTemplate>
    </suki:SukiSideMenu.ItemTemplate>
</suki:SukiSideMenu>
```

## Quick Troubleshooting

**Binding not working?**

- Check `x:DataType` is set
- Verify property exists in ViewModel
- Use `Mode=TwoWay` for input controls
- Check for typos in property names

**SukiUI controls not found?**

- Verify namespace: `xmlns:suki="https://github.com/kikipoulet/SukiUI"`
- Check NuGet package is installed
- Ensure using `<suki:SukiWindow>` not `<Window>`

**Navigation not working?**

- Define DataTemplates in SukiWindow
- Map each ViewModel to its View
- Ensure SelectedItem binding is TwoWay

**Styles not applying?**

- Check selector syntax
- Verify class names match
- Ensure styles are in correct scope (Window.Styles, UserControl.Styles, etc.)
