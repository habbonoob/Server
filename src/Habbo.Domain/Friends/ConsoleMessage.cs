using Habbo.Domain.Users;

namespace Habbo.Domain.Friends;

/// <summary>A console message, kept so a friend who was signed out gets it on their next login
/// and so either side can scroll back through the conversation. A group chat's message names the
/// group instead of a recipient.</summary>
public sealed class ConsoleMessage
{
    public const int TextMaxLength = 255;
    public const int KeyLength = 32;

    public long Id { get; set; }

    /// <summary>The id the client knows the message by.</summary>
    public required string Key { get; set; }

    public Guid SenderId { get; set; }
    public User? Sender { get; set; }

    /// <summary>Who it is for; null for a group chat's message.</summary>
    public Guid? RecipientId { get; set; }

    /// <summary>The group chat it was written in, if it was.</summary>
    public int? GroupId { get; set; }

    public required string Text { get; set; }

    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Whether the recipient has been shown it.</summary>
    public bool Delivered { get; set; }
}
