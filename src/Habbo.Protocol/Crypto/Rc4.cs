namespace Habbo.Protocol.Crypto;

/// <summary>RC4 (ArcFour) — the Habbo post-handshake stream cipher in both directions.</summary>
public sealed class Rc4 : StreamCipher
{
    private readonly byte[] _s = new byte[256];
    private int _i;
    private int _j;

    public Rc4(ReadOnlySpan<byte> key)
    {
        for (var i = 0; i < 256; i++)
            _s[i] = (byte)i;

        var j = 0;
        for (var i = 0; i < 256; i++)
        {
            j = (j + _s[i] + key[i % key.Length]) & 0xFF;
            (_s[i], _s[j]) = (_s[j], _s[i]);
        }
    }

    public override void Process(Span<byte> data)
    {
        for (var n = 0; n < data.Length; n++)
        {
            _i = (_i + 1) & 0xFF;
            _j = (_j + _s[_i]) & 0xFF;
            (_s[_i], _s[_j]) = (_s[_j], _s[_i]);
            data[n] ^= _s[(_s[_i] + _s[_j]) & 0xFF];
        }
    }

    public override StreamCipherState SaveState() => new(_i, _j, (byte[])_s.Clone());

    public override void RestoreState(in StreamCipherState state)
    {
        _i = state.I;
        _j = state.J;
        state.S.CopyTo(_s, 0);
    }
}
