using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>1264 InClientLinkMessageComposer — open a link inside the client.</summary>
[Message(ServerMessages.InClientLinkMessageComposer)]
public sealed partial record InClientLinkMessageComposer(string Link);
