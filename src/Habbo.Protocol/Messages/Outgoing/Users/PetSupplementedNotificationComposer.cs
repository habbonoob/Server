using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>PetSupplementedNotificationComposer — someone watered a plant or gave it light</summary>
[Message(ServerMessages.PetSupplementedNotificationComposer)]
public sealed partial record PetSupplementedNotificationComposer(int PetId, int UserId, int SupplementType);
