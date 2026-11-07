using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace Lumio.Theme.Controls;

public enum StepState
{
    Upcoming,
    Current,
    Done,
}

/// <summary>One node in a <see cref="Stepper"/>.</summary>
[PseudoClasses(":done", ":current", ":upcoming", ":first", ":last")]
public class StepperItem : ContentControl
{
    private string _numberText = "1";

    public static readonly Avalonia.DirectProperty<StepperItem, string> NumberTextProperty =
        Avalonia.AvaloniaProperty.RegisterDirect<StepperItem, string>(nameof(NumberText), o => o.NumberText);

    /// <summary>The one-based step number shown inside the node.</summary>
    public string NumberText
    {
        get => _numberText;
        private set => SetAndRaise(NumberTextProperty, ref _numberText, value);
    }

    public StepState State { get; private set; }

    internal void Apply(int index, int count, StepState state)
    {
        State = state;
        NumberText = (index + 1).ToString(System.Globalization.CultureInfo.CurrentCulture);
        PseudoClasses.Set(":done", state == StepState.Done);
        PseudoClasses.Set(":current", state == StepState.Current);
        PseudoClasses.Set(":upcoming", state == StepState.Upcoming);
        PseudoClasses.Set(":first", index == 0);
        PseudoClasses.Set(":last", index == count - 1);

        var stateText = state switch
        {
            StepState.Done => "done",
            StepState.Current => "current step",
            _ => "upcoming",
        };
        AutomationProperties.SetName(this, $"Step {index + 1} of {count}: {Content}, {stateText}");
    }
}
