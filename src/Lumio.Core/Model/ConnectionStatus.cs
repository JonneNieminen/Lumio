namespace Lumio.Core.Model;

/// <summary>The status shown for a saved connection, derived from its latest check.</summary>
public enum ConnectionStatus
{
    Untested,
    Connected,
    NeedsAttention,
    Failed,
}
