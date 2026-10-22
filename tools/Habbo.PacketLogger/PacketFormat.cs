using System.Buffers.Binary;
using System.Text;
using Xabbo;

namespace Habbo.PacketLogger;

/// <summary>One intercepted packet, as it crossed the wire.</summary>
public sealed record LoggedPacket(
    long Seq,
    DateTimeOffset Time,
    Direction Direction,
    short Id,
    string? Name,
    byte[] Body,
    bool Blocked);

/// <summary>Renders packets the way G-Earth's packet logger exports them.</summary>
public static class PacketFormat
{
    /// <summary>
    /// The whole packet as it is framed on the wire: the length prefix and header for Flash and
    /// Unity, the base64 header for Shockwave. G-Earth prints the framed packet, so the logs match.
    /// </summary>
    public static byte[] Frame(LoggedPacket packet, ClientType client)
    {
        if (client == ClientType.Shockwave)
        {
            var framed = new byte[2 + packet.Body.Length];
            framed[0] = (byte)(0x40 | ((packet.Id >> 6) & 0x3f));
            framed[1] = (byte)(0x40 | (packet.Id & 0x3f));
            packet.Body.CopyTo(framed, 2);
            return framed;
        }

        var bytes = new byte[6 + packet.Body.Length];
        BinaryPrimitives.WriteInt32BigEndian(bytes, 2 + packet.Body.Length);
        BinaryPrimitives.WriteInt16BigEndian(bytes.AsSpan(4), packet.Id);
        packet.Body.CopyTo(bytes, 6);
        return bytes;
    }

    /// <summary>
    /// G-Earth's string form: control bytes (C0, DEL, C1) and brackets as <c>[n]</c>, everything else as its Latin-1
    /// character. <c>tools/achievements_from_capture.py</c> decodes exactly this.
    /// </summary>
    public static string Escape(ReadOnlySpan<byte> bytes)
    {
        var sb = new StringBuilder(bytes.Length + 16);
        foreach (var b in bytes)
        {
            if (b is < 32 or (byte)'[' or (byte)']' or (byte)'{' or (byte)'}' or >= 127 and < 160)
                sb.Append('[').Append(b).Append(']');
            else
                sb.Append((char)b);   // Latin-1 maps each byte to the char of the same value
        }
        return sb.ToString();
    }

    /// <summary>The block G-Earth's logger exports for one packet, minus its expression line.</summary>
    public static string ToGEarthBlock(LoggedPacket packet, ClientType client)
    {
        var direction = packet.Direction == Direction.In ? "Incoming" : "Outgoing";
        var blocked = packet.Blocked ? " (blocked)" : "";
        return $"[{packet.Name ?? "?"}]{blocked}\n" +
               $"{direction}[{packet.Id}] -> {Escape(Frame(packet, client))}\n" +
               "--------------------\n";
    }

    /// <summary>Reads the leading string of a Flash/Unity packet body, if it has one.</summary>
    public static string? ReadLeadingString(byte[] body)
    {
        if (body.Length < 2) return null;
        var length = BinaryPrimitives.ReadUInt16BigEndian(body);
        return body.Length < 2 + length ? null : Encoding.UTF8.GetString(body, 2, length);
    }
}
