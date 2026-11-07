using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Styling;
using Lumio.Theme.Controls;

[assembly: AssemblyFixture(typeof(Lumio.Tests.HeadlessSession))]

namespace Lumio.Tests;

/// <summary>
/// One Avalonia headless session for the whole test run. It drives Avalonia directly
/// (Avalonia.Headless) instead of the xunit-specific adapter, so the tests aren't tied to one
/// xunit major version.
/// The session is intentionally never disposed: it lives exactly as long as the test process, its
/// dispatcher runs on a background thread that ends with the process, and stopping it explicitly
/// blocks indefinitely once Lumio's App has started (the idle dispatcher never wakes to stop).
/// </summary>
public sealed class HeadlessSession
{
    private readonly HeadlessUnitTestSession _session = HeadlessUnitTestSession.StartNew(typeof(HeadlessSession), AvaloniaTestIsolationLevel.PerAssembly);

    /// <summary>Used by the session to configure the app.</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());

    /// <summary>Runs <paramref name="test"/> on the Avalonia UI thread.</summary>
    public Task Run(Action test) => _session.Dispatch(test, TestContext.Current.CancellationToken);


}

public class ThemeHeadlessTests(HeadlessSession avalonia)
{
    [Theory]
    [InlineData("Dark", "#FF12152B", "#FFFFC857")]
    [InlineData("Light", "#FFFBF7EF", "#FFE08E00")]
    public Task Tokens_resolve_per_theme(string variant, string bg, string accent) => avalonia.Run(() =>
    {
        var app = Application.Current!;
        var theme = variant == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

        Assert.True(app.TryGetResource("LmBgColor", theme, out var bgValue));
        Assert.True(app.TryGetResource("LmAccentColor", theme, out var accentValue));
        Assert.Equal(Color.Parse(bg), bgValue);
        Assert.Equal(Color.Parse(accent), accentValue);
    });

    [Fact]
    public Task Stepper_lights_exactly_one_step() => avalonia.Run(() =>
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
    });
}
