#!/usr/bin/env python3
"""Generate the Avalonia token dictionary for Lumio from the lumio-theme tokens.

Source of truth: lumio-theme/tokens.json (resolved into dist/tokens.resolved.json by
lumio-theme/generate_theme.py). This script turns the resolved values into
src/Lumio.Theme/Tokens/Tokens.g.axaml:

* every semantic colour as a Color (LmXxxColor) and a SolidColorBrush (LmXxxBrush),
  in Dark and Light theme dictionaries;
* shadows and the glow as BoxShadows (LmShadowSm, LmShadowMd, LmShadowLg, LmGlow);
* Fluent theme resource keys aliased to Lumio tokens, so stock controls
  (TextBox, ToggleSwitch, ComboBox, NavigationPage, ...) pick up the identity;
* typography, radius, spacing and motion constants.

Usage:
    python3 tools/generate_avalonia_tokens.py [path/to/lumio-theme]

The default theme path is ../lumio-theme next to this repository.
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "src" / "Lumio.Theme" / "Tokens" / "Tokens.g.axaml"

# Fluent resource key -> Lumio token. Each becomes a SolidColorBrush in both variants.
FLUENT_ALIASES: dict[str, str] = {
    # Buttons (Fluent default button = Lumio secondary button)
    "ButtonBackground": "surface-2",
    "ButtonBackgroundPointerOver": "surface-3",
    "ButtonBackgroundPressed": "surface-3",
    "ButtonBackgroundDisabled": "surface-1",
    "ButtonForeground": "text-primary",
    "ButtonForegroundPointerOver": "text-primary",
    "ButtonForegroundPressed": "text-primary",
    "ButtonForegroundDisabled": "text-muted",
    "ButtonBorderBrush": "border",
    "ButtonBorderBrushPointerOver": "border-strong",
    "ButtonBorderBrushPressed": "border-strong",
    "ButtonBorderBrushDisabled": "border",
    # Text inputs
    "TextControlForeground": "text-primary",
    "TextControlForegroundPointerOver": "text-primary",
    "TextControlForegroundFocused": "text-primary",
    "TextControlForegroundDisabled": "text-muted",
    "TextControlBackground": "surface-2",
    "TextControlBackgroundPointerOver": "surface-2",
    "TextControlBackgroundFocused": "surface-2",
    "TextControlBackgroundDisabled": "surface-1",
    "TextControlBorderBrush": "border",
    "TextControlBorderBrushPointerOver": "border-strong",
    "TextControlBorderBrushFocused": "focus-ring",
    "TextControlBorderBrushDisabled": "border",
    "TextControlPlaceholderForeground": "text-muted",
    "TextControlPlaceholderForegroundPointerOver": "text-muted",
    "TextControlPlaceholderForegroundFocused": "text-muted",
    "TextControlPlaceholderForegroundDisabled": "text-muted",
    "TextControlSelectionHighlightColor": "accent-soft",
    "TextControlButtonForeground": "text-secondary",
    "TextControlButtonForegroundPointerOver": "text-primary",
    # Toggle switch
    "ToggleSwitchFillOn": "accent",
    "ToggleSwitchFillOnPointerOver": "accent-hover",
    "ToggleSwitchFillOnPressed": "accent-active",
    "ToggleSwitchStrokeOn": "accent",
    "ToggleSwitchKnobFillOn": "on-accent",
    "ToggleSwitchKnobFillOnPointerOver": "on-accent",
    "ToggleSwitchKnobFillOnPressed": "on-accent",
    "ToggleSwitchFillOff": "surface-2",
    "ToggleSwitchFillOffPointerOver": "surface-3",
    "ToggleSwitchStrokeOff": "border-strong",
    "ToggleSwitchStrokeOffPointerOver": "border-strong",
    "ToggleSwitchKnobFillOff": "text-secondary",
    "ToggleSwitchKnobFillOffPointerOver": "text-primary",
    # Check box and radio button
    "CheckBoxCheckBackgroundFillChecked": "accent",
    "CheckBoxCheckBackgroundFillCheckedPointerOver": "accent-hover",
    "CheckBoxCheckBackgroundFillCheckedPressed": "accent-active",
    "CheckBoxCheckBackgroundStrokeChecked": "accent",
    "CheckBoxCheckBackgroundStrokeCheckedPointerOver": "accent-hover",
    "CheckBoxCheckGlyphForegroundChecked": "on-accent",
    "CheckBoxCheckGlyphForegroundCheckedPointerOver": "on-accent",
    "CheckBoxCheckGlyphForegroundCheckedPressed": "on-accent",
    "RadioButtonOuterEllipseCheckedFill": "accent",
    "RadioButtonOuterEllipseCheckedFillPointerOver": "accent-hover",
    "RadioButtonOuterEllipseCheckedStroke": "accent",
    "RadioButtonOuterEllipseCheckedStrokePointerOver": "accent-hover",
    "RadioButtonCheckGlyphFill": "on-accent",
    "RadioButtonCheckGlyphFillPointerOver": "on-accent",
    "RadioButtonOuterEllipseStroke": "border-strong",
    # Combo box and flyouts
    "ComboBoxBackground": "surface-2",
    "ComboBoxBackgroundPointerOver": "surface-2",
    "ComboBoxBorderBrush": "border",
    "ComboBoxBorderBrushPointerOver": "border-strong",
    "ComboBoxDropDownBackground": "surface-3",
    "ComboBoxDropDownBorderBrush": "border",
    "FlyoutPresenterBackground": "surface-3",
    "FlyoutBorderThemeBrush": "border",
    "MenuFlyoutPresenterBackground": "surface-3",
    "MenuFlyoutPresenterBorderBrush": "border",
    # Page chrome
    "NavigationBarBackground": "bg",
    "NavigationBarForeground": "text-primary",
}


def pascal(token: str) -> str:
    return "".join(part.capitalize() for part in re.split(r"[-_]", token))


def hex_to_argb(value: str) -> str:
    value = value.strip()
    if value.startswith("#") and len(value) == 7:
        return "#FF" + value[1:].upper()
    if value.startswith("#") and len(value) == 9:  # #RRGGBBAA (CSS) -> #AARRGGBB
        return "#" + value[7:9].upper() + value[1:7].upper()
    m = re.fullmatch(r"rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*(?:,\s*([\d.]+))?\s*\)", value)
    if m:
        r, g, b = (int(m.group(i)) for i in (1, 2, 3))
        a = float(m.group(4)) if m.group(4) is not None else 1.0
        return f"#{round(a * 255):02X}{r:02X}{g:02X}{b:02X}"
    raise ValueError(f"Unsupported colour: {value}")


def css_shadow_to_avalonia(value: str) -> str:
    """'0 4px 12px rgba(0,0,0,.4), ...' -> '0 4 12 0 #66000000, ...'."""
    parts = re.split(r",\s*(?![^()]*\))", value)
    out = []
    for part in parts:
        m = re.fullmatch(r"\s*(inset\s+)?(.*?)\s*((?:rgba?\(.*\))|#[0-9a-fA-F]+)\s*", part)
        if not m:
            raise ValueError(f"Unsupported shadow: {part}")
        inset, lengths, colour = m.groups()
        nums = [n.replace("px", "") for n in lengths.split()]
        while len(nums) < 4:
            nums.append("0")
        out.append(f"{'inset ' if inset else ''}{' '.join(nums)} {hex_to_argb(colour)}")
    return ", ".join(out)


SHADOW_TOKENS = {"shadow-sm", "shadow-md", "shadow-lg", "glow"}


def theme_dictionary(key: str, values: dict[str, str]) -> str:
    lines = [f'    <ResourceDictionary x:Key="{key}">']
    lines.append("      <!-- Lumio semantic colours -->")
    for token, value in values.items():
        if token in SHADOW_TOKENS:
            continue
        name = pascal(token)
        lines.append(f'      <Color x:Key="Lm{name}Color">{hex_to_argb(value)}</Color>')
        lines.append(f'      <SolidColorBrush x:Key="Lm{name}Brush" Color="{hex_to_argb(value)}" />')
    lines.append("      <!-- Elevation and illumination -->")
    for token in ("shadow-sm", "shadow-md", "shadow-lg", "glow"):
        if token in values:
            lines.append(f'      <BoxShadows x:Key="Lm{pascal(token)}">{css_shadow_to_avalonia(values[token])}</BoxShadows>')
    lines.append("      <!-- Fluent theme keys mapped to Lumio tokens -->")
    for fluent_key, token in FLUENT_ALIASES.items():
        lines.append(f'      <SolidColorBrush x:Key="{fluent_key}" Color="{hex_to_argb(values[token])}" />')
    lines.append("    </ResourceDictionary>")
    return "\n".join(lines)


def main() -> int:
    theme_dir = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT.parent / "lumio-theme"
    resolved = json.loads((theme_dir / "dist" / "tokens.resolved.json").read_text())
    source = json.loads((theme_dir / "tokens.json").read_text())
    version = source.get("meta", {}).get("version", "?")

    typo = source["typography"]
    sizes = typo.get("size_px") or typo.get("sizes_px") or typo.get("scale_px") or {}
    weights = typo.get("weight") or typo.get("weights") or {}
    radius = source["radius_px"]
    spacing = source["spacing_px"]
    motion = source["motion"]
    durations = motion.get("duration_ms") or motion.get("durations_ms") or {
        k: v for k, v in motion.items() if isinstance(v, (int, float))
    }

    shared = ["  <!-- Brand constants (theme independent: icon tile, splash) -->"]
    for name, value in source["brand"].items():
        shared.append(f'  <Color x:Key="LmBrand{pascal(name)}Color">{hex_to_argb(value)}</Color>')
        shared.append(f'  <SolidColorBrush x:Key="LmBrand{pascal(name)}Brush" Color="{hex_to_argb(value)}" />')
    shared.append("  <!-- Typography -->")
    shared.append('  <FontFamily x:Key="LmFontSans">fonts:Inter#Inter, $Default</FontFamily>')
    shared.append(
        '  <FontFamily x:Key="LmFontMono">JetBrains Mono, SF Mono, Menlo, Cascadia Mono, Consolas, DejaVu Sans Mono, monospace</FontFamily>'
    )
    for name, px in sizes.items():
        shared.append(f'  <x:Double x:Key="LmFontSize{pascal(str(name))}">{px}</x:Double>')
    for name, w in weights.items():
        shared.append(f'  <FontWeight x:Key="LmFontWeight{pascal(str(name))}">{w}</FontWeight>')
    shared.append("  <!-- Radius -->")
    for name, px in radius.items():
        value = 9999 if str(px).lower() in ("9999", "full") else px
        shared.append(f'  <CornerRadius x:Key="LmRadius{pascal(str(name))}">{value}</CornerRadius>')
    shared.append("  <!-- Spacing (4 px scale) -->")
    if isinstance(spacing, dict):
        spacing_items = spacing.items()
    else:
        spacing_items = ((str(v), v) for v in spacing)
    for name, px in spacing_items:
        shared.append(f'  <x:Double x:Key="LmSpace{pascal(str(name))}">{px}</x:Double>')
    shared.append("  <!-- Motion -->")
    for name, ms in durations.items():
        shared.append(f'  <x:TimeSpan x:Key="LmDuration{pascal(str(name))}">0:0:{int(ms) / 1000:.3f}</x:TimeSpan>')

    xaml = f"""<!--
  GENERATED by tools/generate_avalonia_tokens.py from lumio-theme v{version}. Do not edit by hand.
  Regenerate after changing lumio-theme/tokens.json:
      python3 ../lumio-theme/generate_theme.py && python3 tools/generate_avalonia_tokens.py
-->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <ResourceDictionary.ThemeDictionaries>
{theme_dictionary("Dark", resolved["dark"])}
{theme_dictionary("Light", resolved["light"])}
  </ResourceDictionary.ThemeDictionaries>

{chr(10).join(shared)}
</ResourceDictionary>
"""
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(xaml)
    print(f"Wrote {OUT.relative_to(ROOT)} (lumio-theme v{version})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
