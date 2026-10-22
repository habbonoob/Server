using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

[WireStruct]
public sealed partial record RelationshipStatusInfoData(
    int RelationshipStatusType, int FriendCount, int RandomFriendId, string RandomFriendName, string RandomFriendFigure);
