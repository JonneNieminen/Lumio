using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Lumio.Theme.Controls;

[assembly: AvaloniaTestApplication(typeof(Lumio.Tests.TestAppBuilder))]

namespace Lumio.Tests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

public class ThemeHeadlessTests
{
    [AvaloniaTheory]
    [InlineData("Dark", "#FF12152B", "#FFFFC857")]
    [InlineData("Light", "#FFFBF7EF", "#FFE08E00")]
    public void Tokens_resolve_per_theme(string variant, string bg, string accent)
    {
        var app = Application.Current!;
        var theme = variant == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

        Assert.True(app.TryGetResource("LmBgColor", theme, out var bgValue));
        Assert.True(app.TryGetResource("LmAccentColor", theme, out var accentValue));
        Assert.Equal(Color.Parse(bg), bgValue);
        Assert.Equal(Color.Parse(accent), accentValue);
    }

    [AvaloniaFact]
    public void Stepper_lights_exactly_one_step()
    {
        var stepper = new Stepper { ItemsSource = new[] { "One", "Two", "Three", "Four" }, CurrentIndex = 1 };
        var window = new Window { Content = stepper, Width = 800, Height = 200 };
        window.Show();

        var items = stepper.GetRealizedContainers().OfType<StepperItem>().ToList();
        Assert.Equal(4, items.Count);
        Assert.Equal([StepState.Done, StepState.Current, StepState.Upcoming, StepState.Upcoming],
            items.Select(i => i.State));

        stepper.CurrentIndex = 3;
        Assert.Single(items, i => i.State == StepState.Current);
        Assert.Equal(StepState.Current, items[3].State);
        window.Close();
    }
}
