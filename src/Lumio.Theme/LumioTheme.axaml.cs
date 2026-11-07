using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace Lumio.Theme;

/// <summary>
/// The Lumio theme: Fluent as the control base, recoloured from the Lumio tokens, plus Lumio
/// control styles and signature controls. Add it once in <c>Application.Styles</c>.
/// </summary>
public partial class LumioTheme : Styles
{
    public LumioTheme()
    {
        AvaloniaXamlLoader.Load(this);
        ApplyFluentPalettes();
    }

    /// <summary>Feeds the Lumio planes, text and accent colours into Fluent's palette system.</summary>
    private void ApplyFluentPalettes()
    {
        var fluent = this.OfType<FluentTheme>().FirstOrDefault();
        var tokens = Resources.MergedDictionaries.OfType<ResourceDictionary>().FirstOrDefault();
        if (fluent is null || tokens is null)
            return;

        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            if (!tokens.ThemeDictionaries.TryGetValue(variant, out var provider) || provider is not IResourceDictionary d)
                continue;

            Color C(string name) => d.TryGetValue($"Lm{name}Color", out var v) && v is Color c ? c : Colors.Magenta;

            fluent.Palettes[variant] = new ColorPaletteResources
            {
                Accent = C("Accent"),
                RegionColor = C("Bg"),
                ErrorText = C("Danger"),
                AltHigh = C("Bg"),
                AltLow = C("Surface1"),
                AltMedium = C("Surface1"),
                AltMediumHigh = C("Surface1"),
                AltMediumLow = C("Surface1"),
                BaseHigh = C("TextPrimary"),
                BaseMediumHigh = C("TextSecondary"),
                BaseMedium = C("TextMuted"),
                BaseMediumLow = C("BorderStrong"),
                BaseLow = C("Border"),
                ChromeAltLow = C("TextSecondary"),
                ChromeHigh = C("BorderStrong"),
                ChromeMedium = C("Surface2"),
                ChromeMediumLow = C("Surface1"),
                ChromeLow = C("Surface2"),
                ChromeDisabledHigh = C("Border"),
                ChromeDisabledLow = C("TextMuted"),
                ChromeGray = C("TextMuted"),
                ListLow = C("Surface3"),
                ListMedium = C("AccentSoft"),
            };
        }
    }
}
