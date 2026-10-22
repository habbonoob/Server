using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

[WireStruct]
public sealed partial record HabboGroupEntryData(
    int GroupId, string GroupName, string BadgeCode, string PrimaryColor, string SecondaryColor,
    bool Favourite, int OwnerId, bool HasForum);
