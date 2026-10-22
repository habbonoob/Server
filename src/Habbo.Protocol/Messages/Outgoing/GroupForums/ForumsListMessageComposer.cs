using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.GroupForums;

/// <summary>ForumsListMessageComposer</summary>
[Message(ServerMessages.ForumsListMessageComposer)]
public sealed partial record ForumsListMessageComposer(int ListCode, int TotalAmount, int StartIndex, IReadOnlyList<ForumData> Forums);
