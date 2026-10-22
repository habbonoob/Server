using Habbo.Protocol.Wire;
using Habbo.Protocol.Messages.Outgoing.Inventory.Badges;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>CreateGuildMessageEvent — buy a group; the badge comes as part, colour, position triples</summary>
[Incoming(ClientMessages.CreateGuildMessageEvent)]
public sealed partial record CreateGuildMessageEvent(string Name, string Description, int RoomId, int PrimaryColorId, int SecondaryColorId, IReadOnlyList<int> BadgeData);
