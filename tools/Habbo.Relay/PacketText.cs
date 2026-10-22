using System.Buffers.Binary;
using System.Text;

namespace Habbo.Relay;

/// <summary>A packet in G-Earth's export format, as Habbo.PacketLogger writes its .txt files, so the existing capture
/// tools (tools/achievements_from_capture.py and friends) read the relay's captures too.</summary>
public static class PacketText
{
    /// <summary>The packet as framed on the wire: its length, its header, its body.</summary>
    public static byte[] Frame(int id, byte[] body)
    {
        var bytes = new byte[6 + body.Length];
        BinaryPrimitives.WriteInt32BigEndian(bytes, 2 + body.Length);
        BinaryPrimitives.WriteInt16BigEndian(bytes.AsSpan(4), (short)id);
        body.CopyTo(bytes, 6);
        return bytes;
    }

    /// <summary>G-Earth's string form: control bytes (C0, DEL, C1) and brackets as [n], everything else as its
    /// Latin-1 character.</summary>
    public static string Escape(ReadOnlySpan<byte> bytes)
    {
        var sb = new StringBuilder(bytes.Length + 16);
        foreach (var b in bytes)
        {
            if (b is < 32 or (byte)'[' or (byte)']' or (byte)'{' or (byte)'}' or >= 127 and < 160)
                sb.Append('[').Append(b).Append(']');
            else
                sb.Append((char)b);
        }
        return sb.ToString();
    }

    /// <summary>The block G-Earth's logger exports for one packet, minus its expression line.</summary>
    public static string Block(bool incoming, int id, string? name, byte[] body) =>
        $"[{name ?? "?"}]\n{(incoming ? "Incoming" : "Outgoing")}[{id}] -> {Escape(Frame(id, body))}\n--------------------\n";
}
