using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Habbo.Protocol.IO;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu;

/// <summary>A variable the client holds, by the hash it cached it with.</summary>
[WireStruct]
public sealed partial record WiredKnownVariableData(string Id, int Hash);

/// <summary>671 WiredGetAllVariablesDiffs — the variables the client holds; it wants the rest.
///
/// The client keeps them in a Dictionary keyed by id, which hands a numeric id ("-120", a box's "1234") back as a
/// number, and its encoder writes a number as an int: so each id comes as an int or, when it is not a number (a
/// sub-variable's "1234.level"), as a string, then its hash. Read here by whichever of the two takes the payload to
/// its end.</summary>
public sealed record WiredGetAllVariablesDiffsMessageEvent(IReadOnlyList<WiredKnownVariableData> Known)
    : IIncomingMessage<WiredGetAllVariablesDiffsMessageEvent>
{
    public static ushort Header => ClientMessages.WiredGetAllVariablesDiffsMessageEvent;

    public static WiredGetAllVariablesDiffsMessageEvent Read(MessageReader r)
    {
        var count = r.ReadInt();
        var payload = r.ReadBytes(r.Remaining).ToArray();
        if (count < 0 || count > payload.Length / ShortestEntry)
            throw new InvalidDataException($"WiredGetAllVariablesDiffs: {count} variables in {payload.Length} bytes");

        var known = new List<WiredKnownVariableData>(count);
        if (!Entries(payload, 0, count, known, []))
            throw new InvalidDataException($"WiredGetAllVariablesDiffs: {count} variables do not read from {payload.Length} bytes");
        return new WiredGetAllVariablesDiffsMessageEvent(known);
    }

    /// <summary>The shortest entry: a one-character id written as a string, and its hash.</summary>
    private const int ShortestEntry = 2 + 1 + 4;

    /// <summary>Reads <paramref name="left"/> entries from <paramref name="at"/>, each id a string or an int, so that
    /// the last one ends the payload; false when no reading does. Where a reading failed is remembered, so trying
    /// both ways at each entry stays linear.</summary>
    private static bool Entries(byte[] payload, int at, int left, List<WiredKnownVariableData> known, HashSet<(int, int)> failed)
    {
        if (left == 0)
            return at == payload.Length;
        if (failed.Contains((at, left)))
            return false;

        if (at + 2 <= payload.Length)
        {
            var length = payload[at] << 8 | payload[at + 1];
            if (length > 0 && at + 2 + length + 4 <= payload.Length && IsId(payload.AsSpan(at + 2, length)))
            {
                known.Add(new WiredKnownVariableData(
                    Encoding.UTF8.GetString(payload, at + 2, length),
                    BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(at + 2 + length))));
                if (Entries(payload, at + 2 + length + 4, left - 1, known, failed))
                    return true;
                known.RemoveAt(known.Count - 1);
            }
        }

        if (at + 8 <= payload.Length)
        {
            known.Add(new WiredKnownVariableData(
                BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(at)).ToString(CultureInfo.InvariantCulture),
                BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(at + 4))));
            if (Entries(payload, at + 8, left - 1, known, failed))
                return true;
            known.RemoveAt(known.Count - 1);
        }

        failed.Add((at, left));
        return false;
    }

    /// <summary>What a variable's id is made of: printable ASCII, no spaces (a box's or an internal one's digits and
    /// sign, a smart one's "~146", a sub-variable's "1234.level").</summary>
    private static bool IsId(ReadOnlySpan<byte> text)
    {
        foreach (var c in text)
        {
            if (c is < 0x21 or > 0x7E)
                return false;
        }

        return true;
    }
}
