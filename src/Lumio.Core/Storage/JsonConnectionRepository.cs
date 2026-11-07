using System.Text.Json;
using Lumio.Core.Model;

namespace Lumio.Core.Storage;

/// <summary>Stores connections in a single JSON file, written atomically.</summary>
public sealed class JsonConnectionRepository(string filePath) : IConnectionRepository
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public string FilePath { get; } = filePath;

    public async Task<IReadOnlyList<Connection>> LoadAllAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!File.Exists(FilePath))
                return [];

            await using var stream = File.OpenRead(FilePath);
            var doc = await JsonSerializer.DeserializeAsync(stream, LumioJsonContext.Default.ConnectionsDocument, ct)
                .ConfigureAwait(false);
            return doc?.Connections ?? [];
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAllAsync(IReadOnlyList<Connection> connections, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var temp = FilePath + ".tmp";
            await using (var stream = File.Create(temp))
            {
                var doc = new ConnectionsDocument { Connections = [.. connections] };
                await JsonSerializer.SerializeAsync(stream, doc, LumioJsonContext.Default.ConnectionsDocument, ct)
                    .ConfigureAwait(false);
            }
            File.Move(temp, FilePath, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }
    }
}
