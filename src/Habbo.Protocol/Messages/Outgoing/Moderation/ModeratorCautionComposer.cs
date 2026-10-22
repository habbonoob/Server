using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>ModeratorCautionComposer — a caution from the hotel's staff</summary>
[Message(ServerMessages.ModeratorCautionComposer)]
public sealed partial record ModeratorCautionComposer(string Message, string Url);
