namespace Habbo.Camera;

/// <summary>One file in a .hab bundle: its name, media type and (inflated) bytes.</summary>
public sealed record HabEntry(string Name, string MimeType, byte[] Bytes);
