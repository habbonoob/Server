namespace Habbo.Silo.Abstractions.Navigator;

/// <summary>The old navigator's searches, by the numbers the client echoes back in its results.</summary>
public enum LegacyRoomSearch
{
    Popular = 1,
    HighestScore = 2,
    MyFriendsRooms = 3,
    RoomsWhereMyFriendsAre = 4,
    MyRooms = 5,
    MyFavourites = 6,
    MyHistory = 7,
    Text = 8,
    Tag = 9,
    RoomName = 10,
    Group = 13,
    GuildBases = 14,
    Competition = 15,
    TopPromotions = 16,
    NewPromotions = 17,
    MyRoomRights = 18,
    MyGuildBases = 19,
    Owner = 20,
    MyRecommended = 22,
    MyFrequentHistory = 23,
}
