using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Lumio.Core.Storage;

namespace Lumio.Services;

public sealed class ThemeService : IThemeService
{
    private Control? _root;
    private bool _reduceMotion;

    /// <summary>The element that receives the <c>reduce-motion</c> class (the shell).</summary>
    public void Attach(Control root)
    {
        _root = root;
        _root.Classes.Set("reduce-motion", _reduceMotion);
    }

    public void ApplyTheme(ThemeChoice theme)
    {
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = theme switch
            {
                ThemeChoice.Light => ThemeVariant.Light,
                ThemeChoice.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default,
            };
        }
    }

    public void ApplyReduceMotion(bool reduce)
    {
        _reduceMotion = reduce;
        _root?.Classes.Set("reduce-motion", reduce);
    }
}
