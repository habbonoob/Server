using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>3786 GetUserEventCats — the event categories.</summary>
[Incoming(ClientMessages.GetUserEventCatsMessageEvent)]
public sealed partial record GetUserEventCatsMessageEvent;
