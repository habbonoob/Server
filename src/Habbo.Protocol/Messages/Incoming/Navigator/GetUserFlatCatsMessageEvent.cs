using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>1258 GetUserFlatCats — the legacy room categories.</summary>
[Incoming(ClientMessages.GetUserFlatCatsMessageEvent)]
public sealed partial record GetUserFlatCatsMessageEvent;
