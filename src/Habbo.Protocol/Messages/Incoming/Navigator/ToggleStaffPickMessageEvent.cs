using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>ToggleStaffPickMessageEvent</summary>
[Incoming(ClientMessages.ToggleStaffPickMessageEvent)]
public sealed partial record ToggleStaffPickMessageEvent(int FlatId, bool StaffPick);
