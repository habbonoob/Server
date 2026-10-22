using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>SetChestPreferencesMessageEvent — a chest's settings</summary>
[Incoming(ClientMessages.SetChestPreferencesMessageEvent)]
public sealed partial record SetChestPreferencesMessageEvent(int ChestId, string Name, string Description, bool EveryoneCanOpen, bool EveryoneCanDonate, int StateControlMode, int PreviewMode, int PreviewAmount, bool WiredEnabled);
