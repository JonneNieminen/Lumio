using Avalonia.Controls;

namespace Lumio.Services;

public sealed class UrlLauncher : IUrlLauncher
{
    private TopLevel? _topLevel;

    public void Attach(TopLevel? topLevel) => _topLevel = topLevel;

    public async Task OpenAsync(string url)
    {
        if (_topLevel?.Launcher is { } launcher && Uri.TryCreate(url, UriKind.Absolute, out var uri))
            await launcher.LaunchUriAsync(uri);
    }
}
