using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Xabbo;

namespace Habbo.PacketLogger;

/// <summary>
/// A packet written in G-Earth's expression syntax, e.g. <c>{out:MoveAvatar}{i:5}{i:7}</c>.
/// The first token is the direction and the message name or header id; the rest are values:
/// <c>{i:int}</c>, <c>{s:"string"}</c>, <c>{b:true|false|byte}</c>, <c>{u:short}</c> (G-Earth's; <c>{h:}</c>
/// too), <c>{l:long}</c>, <c>{d:double}</c> and <c>{x:hex bytes}</c> for anything else.
/// </summary>
/// <remarks>Encodes for Flash and Unity (big-endian, UTF-8 strings with a 16-bit length).</remarks>
public sealed record PacketExpression(Direction Direction, string? Name, short? Id, byte[] Body)
{
    public static PacketExpression Parse(string expression)
    {
        var tokens = Tokenize(expression);
        if (tokens.Count == 0)
            throw new ArgumentException("empty expression");

        var (kind, target) = tokens[0];
        var direction = kind switch
        {
            "out" => Direction.Out,
            "in" => Direction.In,
            _ => throw new ArgumentException("the expression must start with {out:...} or {in:...}"),
        };
        short? id = short.TryParse(target, out var n) ? n : null;
        var name = id is null ? target : null;

        using var body = new MemoryStream();
        foreach (var (type, value) in tokens.Skip(1))
            Write(body, type, value);

        return new PacketExpression(direction, name, id, body.ToArray());
    }

    static void Write(Stream body, string type, string value)
    {
        Span<byte> buf = stackalloc byte[8];
        switch (type)
        {
            case "i":
                BinaryPrimitives.WriteInt32BigEndian(buf, int.Parse(value, CultureInfo.InvariantCulture));
                body.Write(buf[..4]);
                break;
            case "u" or "h":
                // a short either way round: G-Earth writes 0..65535, a signed one reads -32768..32767
                var number = int.Parse(value, CultureInfo.InvariantCulture);
                if (number is < short.MinValue or > ushort.MaxValue)
                    throw new ArgumentException($"{number} does not fit a short");
                BinaryPrimitives.WriteUInt16BigEndian(buf, unchecked((ushort)number));
                body.Write(buf[..2]);
                break;
            case "d":
                BinaryPrimitives.WriteDoubleBigEndian(buf, double.Parse(value, CultureInfo.InvariantCulture));
                body.Write(buf[..8]);
                break;
            case "l":
                BinaryPrimitives.WriteInt64BigEndian(buf, long.Parse(value, CultureInfo.InvariantCulture));
                body.Write(buf[..8]);
                break;
            case "b":
                body.WriteByte(value switch
                {
                    "true" => 1,
                    "false" => 0,
                    _ => byte.Parse(value, CultureInfo.InvariantCulture),
                });
                break;
            case "s":
                var bytes = Encoding.UTF8.GetBytes(value);
                BinaryPrimitives.WriteUInt16BigEndian(buf, checked((ushort)bytes.Length));
                body.Write(buf[..2]);
                body.Write(bytes);
                break;
            case "x":
                body.Write(Convert.FromHexString(value.Replace(" ", "")));
                break;
            default:
                throw new ArgumentException($"unknown value type '{type}'; use i, s, b, u, l, d or x");
        }
    }

    /// <summary>Splits <c>{type:value}</c> tokens; string values are quoted with \" and \\ escapes.</summary>
    static List<(string Type, string Value)> Tokenize(string text)
    {
        var tokens = new List<(string, string)>();
        var i = 0;
        while (true)
        {
            while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
            if (i == text.Length) return tokens;
            if (text[i] != '{') throw new ArgumentException($"expected '{{' at {i}");

            var colon = text.IndexOf(':', i);
            if (colon < 0) throw new ArgumentException($"expected ':' after {i}");
            var type = text[(i + 1)..colon].Trim().ToLowerInvariant();
            i = colon + 1;

            string value;
            if (type == "s")
            {
                if (i >= text.Length || text[i] != '"') throw new ArgumentException($"string at {i} must be quoted");
                var sb = new StringBuilder();
                for (i++; ; i++)
                {
                    if (i >= text.Length) throw new ArgumentException("unterminated string");
                    if (text[i] == '\\' && i + 1 < text.Length) sb.Append(text[++i]);
                    else if (text[i] == '"') break;
                    else sb.Append(text[i]);
                }
                value = sb.ToString();
                i++;
                if (i >= text.Length || text[i] != '}') throw new ArgumentException($"expected '}}' at {i}");
            }
            else
            {
                var close = text.IndexOf('}', i);
                if (close < 0) throw new ArgumentException($"expected '}}' after {i}");
                value = text[i..close].Trim();
                i = close;
            }
            i++;
            tokens.Add((type, value));
        }
    }
}
