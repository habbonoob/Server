using System.Buffers;

namespace Habbo.Protocol.Frames;

/// <summary>A decoded wire frame: 2-byte header + payload. Dispose to return the rented payload buffer.</summary>
public readonly struct GameFrame : IDisposable
{
    private readonly byte[]? _rented;

    public ushort Header { get; }

    public ReadOnlyMemory<byte> Payload { get; }

    internal GameFrame(ushort header, byte[] rented, int frameLength)
    {
        Header = header;
        _rented = rented;
        Payload = rented.AsMemory(2, frameLength - 2);
    }

    public void Dispose()
    {
        if (_rented is not null)
            ArrayPool<byte>.Shared.Return(_rented);
    }
}

public sealed class InvalidFrameException(string message) : Exception(message);
