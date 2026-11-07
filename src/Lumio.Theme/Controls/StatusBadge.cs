using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

namespace Lumio.Theme.Controls;

public enum BadgeKind
{
    Neutral,
    Success,
    Warning,
    Danger,
    Info,
}

/// <summary>
/// A status badge: soft background, matching text colour and an icon, so colour is never
/// the only signal.
/// </summary>
[PseudoClasses(":neutral", ":success", ":warning", ":danger", ":info")]
public class StatusBadge : TemplatedControl
{
    public static readonly StyledProperty<BadgeKind> KindProperty =
        AvaloniaProperty.Register<StatusBadge, BadgeKind>(nameof(Kind));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<StatusBadge, string?>(nameof(Text));

    public BadgeKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public StatusBadge() => UpdatePseudoClasses();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == KindProperty)
            UpdatePseudoClasses();
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":neutral", Kind == BadgeKind.Neutral);
        PseudoClasses.Set(":success", Kind == BadgeKind.Success);
        PseudoClasses.Set(":warning", Kind == BadgeKind.Warning);
        PseudoClasses.Set(":danger", Kind == BadgeKind.Danger);
        PseudoClasses.Set(":info", Kind == BadgeKind.Info);
    }
}
