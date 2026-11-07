using Avalonia;
using Avalonia.Controls;

namespace Lumio.Theme.Controls;

/// <summary>
/// Lumio's signature component: a path of circular step nodes. Done steps are quiet,
/// the current step is lit (accent fill plus glow), upcoming steps are hollow rings.
/// Items are the step labels.
/// </summary>
public class Stepper : ItemsControl
{
    public static readonly StyledProperty<int> CurrentIndexProperty =
        AvaloniaProperty.Register<Stepper, int>(nameof(CurrentIndex));

    /// <summary>The zero-based index of the lit step.</summary>
    public int CurrentIndex
    {
        get => GetValue(CurrentIndexProperty);
        set => SetValue(CurrentIndexProperty, value);
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
        new StepperItem();

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<StepperItem>(item, out recycleKey);

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is StepperItem step)
            UpdateStep(step, index);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CurrentIndexProperty || change.Property == ItemCountProperty)
            UpdateAll();
    }

    private void UpdateAll()
    {
        foreach (var container in GetRealizedContainers())
        {
            if (container is StepperItem step)
                UpdateStep(step, IndexFromContainer(container));
        }
    }

    private void UpdateStep(StepperItem step, int index)
    {
        if (index < 0)
            return;
        var state = index < CurrentIndex ? StepState.Done
            : index == CurrentIndex ? StepState.Current
            : StepState.Upcoming;
        step.Apply(index, ItemCount, state);
    }
}
