using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>ModeratorMessageComposer — a message from the hotel's staff</summary>
[Message(ServerMessages.ModeratorMessageComposer)]
public sealed partial record ModeratorMessageComposer(string Message, string Url);
