namespace Habbo.Protocol.Crypto;

/// <summary>A continuous byte-stream cipher (RC4-style): state advances as bytes are processed.
/// Callers snapshot/restore state when a partially received frame must be re-parsed.</summary>
public abstract class StreamCipher
{
    public abstract void Process(Span<byte> data);
    public abstract StreamCipherState SaveState();
    public abstract void RestoreState(in StreamCipherState state);
}

/// <summary>Snapshot of a cipher's internal state, used for mark/reset while framing partial reads.</summary>
public readonly record struct StreamCipherState(int I, int J, byte[] S);
