using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>DoorbellMessageComposer — the doorbell: empty to the one ringing, their name to those who may open</summary>
[Message(ServerMessages.DoorbellMessageComposer)]
public sealed partial record DoorbellMessageComposer(string UserName);
