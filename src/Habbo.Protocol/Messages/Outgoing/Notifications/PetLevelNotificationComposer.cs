using Habbo.Protocol.Messages.Outgoing.Inventory.Pets;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Notifications;

/// <summary>PetLevelNotificationComposer — the player's pet went up a level</summary>
[Message(ServerMessages.PetLevelNotificationComposer)]
public sealed partial record PetLevelNotificationComposer(int PetId, string PetName, int Level, PetFigureWireData Figure);
