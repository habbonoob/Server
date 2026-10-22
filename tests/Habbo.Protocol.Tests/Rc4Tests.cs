using Habbo.Protocol.Crypto;

namespace Habbo.Protocol.Tests;

public class Rc4Tests
{
    [Fact]
    public void KnownAnswerVector()
    {
        var rc4 = new Rc4("Key"u8);
        var data = "Plaintext"u8.ToArray();
        rc4.Process(data);
        Assert.Equal("BBF316E8D940AF0AD3", Convert.ToHexString(data));
    }

    [Fact]
    public void ProcessTwiceWithSameKeyRestoresOriginal()
    {
        var key = "secret"u8;
        var original = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();

        var data = original.ToArray();
        new Rc4(key).Process(data);
        new Rc4(key).Process(data);

        Assert.Equal(original, data);
    }

    [Fact]
    public void SaveAndRestoreRollsBackExactly()
    {
        var rc4 = new Rc4("key"u8);
        var snapshot = rc4.SaveState();

        var first = new byte[16];
        rc4.Process(first);

        rc4.RestoreState(snapshot);
        var second = new byte[16];
        rc4.Process(second);

        Assert.Equal(first, second);
    }
}
