using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Avatar;

/// <summary>2862 GetWardrobe.</summary>
[Incoming(ClientMessages.GetWardrobeMessageEvent)]
public sealed partial record GetWardrobeMessageEvent;
