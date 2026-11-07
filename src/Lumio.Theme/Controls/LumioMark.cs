using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Lumio.Theme.Controls;

/// <summary>
/// The Lumio "Elbow ring" mark, drawn from the brand geometry (64 x 64 grid).
/// With <see cref="ShowTile"/> it renders the app icon (navy tile); without it, the bare symbol
/// in <see cref="TemplatedControl.Foreground"/> with an accent node.
/// </summary>
[PseudoClasses(":tile")]
public class LumioMark : TemplatedControl
{
    public static readonly StyledProperty<bool> ShowTileProperty =
        AvaloniaProperty.Register<LumioMark, bool>(nameof(ShowTile));

    public static readonly StyledProperty<IBrush?> NodeBrushProperty =
        AvaloniaProperty.Register<LumioMark, IBrush?>(nameof(NodeBrush));

    public bool ShowTile
    {
        get => GetValue(ShowTileProperty);
        set => SetValue(ShowTileProperty, value);
    }

    /// <summary>The lit node. Always the accent colour, per the brand rules.</summary>
    public IBrush? NodeBrush
    {
        get => GetValue(NodeBrushProperty);
        set => SetValue(NodeBrushProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ShowTileProperty)
            PseudoClasses.Set(":tile", ShowTile);
    }
}
