using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Chat;

/// <summary>1557 UserTypingMessageComposer.</summary>
[Message(ServerMessages.UserTypingMessageComposer)]
public sealed partial record UserTypingMessageComposer(int UserId, int IsTyping)
{
    public const int Typing = 1;
    public const int NotTyping = 0;
}
