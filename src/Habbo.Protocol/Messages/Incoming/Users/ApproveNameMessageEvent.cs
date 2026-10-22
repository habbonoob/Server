using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>2742 ApproveName — would this name do for a thing of this kind (a pet, a bot)?</summary>
[Incoming(ClientMessages.ApproveNameMessageEvent)]
public sealed partial record ApproveNameMessageEvent(string Name, int Type);
