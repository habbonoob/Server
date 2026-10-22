namespace Habbo.Relay;

/// <summary>One packet as the client recorded it: "in" or "out", its header, the client's name for it, its body
/// (hex, without the length and header), and when (milliseconds since the epoch, the client's clock).</summary>
public sealed record CapturedPacket(string Dir, int Id, string? Name, string Hex, long Time);
