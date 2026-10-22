using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>ChestPreferencesUpdateSuccessMessageComposer — a chest's settings were saved</summary>
[Message(ServerMessages.ChestPreferencesUpdateSuccessMessageComposer)]
public sealed partial record ChestPreferencesUpdateSuccessMessageComposer(int ChestId, bool IsNotificationPreferences);
