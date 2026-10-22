namespace Habbo.Domain.Chat;

/// <summary>An emoticon that makes the avatar pull a face when it appears in chat.
/// Adding one is a row, not a code change.</summary>
public sealed class ChatGesture
{
    /// <summary>The literal text to look for, e.g. ":)" or "&gt;:(".</summary>
    public required string Emoticon { get; set; }

    /// <summary>The gesture the client plays, matching the wire's gesture numbers.</summary>
    public int GestureId { get; set; }
}
