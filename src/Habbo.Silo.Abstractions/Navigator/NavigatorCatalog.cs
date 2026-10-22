using Orleans;
using Orleans.Runtime;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Silo.Abstractions.Navigator;

/// <summary>A quick link inside a navigator top-level context.</summary>
public sealed record NavigatorSavedSearch(int Id, string SearchCode, string Filter, string Localization);

/// <summary>A top-level navigator context ("official_view", "myworld_view", ...).</summary>
public sealed record NavigatorContext(string SearchCode, IReadOnlyList<NavigatorSavedSearch> QuickLinks);

/// <summary>A flat category row for UserFlatCats/UserEventCats.</summary>
public sealed record FlatCategory(
    int NodeId, string NodeName, bool Visible, bool Automatic, string AutomaticCategoryKey,
    string GlobalCategoryKey, bool StaffOnly);

/// <summary>Which rooms a navigator block lists.</summary>
public enum RoomQuery
{
    /// <summary>Rooms the requester owns.</summary>
    Owned,

    /// <summary>Every room, the busiest first.</summary>
    Popular,

    /// <summary>Rooms staff have picked.</summary>
    StaffPicks,

    /// <summary>Rooms the requester has starred.</summary>
    Favourites,

    /// <summary>Every room, the best liked first.</summary>
    HighestScore,

    /// <summary>Rooms the requester's friends own.</summary>
    FriendsRooms,

    /// <summary>Rooms the requester's friends are in now.</summary>
    WhereFriendsAre,

    /// <summary>Rooms the requester visited, the latest first.</summary>
    History,

    /// <summary>Rooms the requester visits most.</summary>
    FrequentHistory,

    /// <summary>Rooms the requester has rights in.</summary>
    Rights,

    /// <summary>Group home rooms, the busiest first.</summary>
    GroupBases,

    /// <summary>The home rooms of the requester's groups.</summary>
    MyGroupBases,

    /// <summary>Rooms like the ones the requester visits: their categories, the best liked first.</summary>
    Recommended,

    /// <summary>Rooms being promoted, the busiest first.</summary>
    Promoted,

    /// <summary>Rooms being promoted, the newest promotion first.</summary>
    NewlyPromoted,

    /// <summary>What the search box matched, the busiest first: Habbo's "query" block, which a typed search shows
    /// in place of the tab's own blocks.</summary>
    Search,

    /// <summary>Expands to a block for each room category that has rooms ("category__&lt;name&gt;"), the busiest
    /// first in each.</summary>
    Categories,

    /// <summary>Expands to a block for each event category with rooms promoted in it ("eventcategory__&lt;id&gt;").</summary>
    EventCategories,

    /// <summary>Expands to a block for each folder of the official rooms ("official_&lt;id&gt;"), its rooms in order,
    /// collapsed unless the folder is open.</summary>
    OfficialFolders,
}

/// <summary>A room tag and how many rooms carry it, for the navigator's tag cloud.</summary>
[GenerateSerializer]
public sealed partial record RoomTagCount(
    [property: Id(0)] string Tag,
    [property: Id(1)] int RoomCount);

/// <summary>How one result block of a search code is built.</summary>
public sealed record NavigatorBlockDefinition(
    string Code, string Text, RoomQuery Query, int ActionAllowed, int ViewMode, int MaxResults, bool ForceClosed);

/// <summary>One result block in NavigatorSearchResultBlocksComposer.</summary>
[GenerateSerializer]
public sealed partial record NavigatorSearchBlock(
    [property: Id(0)] string SearchCode,
    [property: Id(1)] string Text,
    [property: Id(2)] int ActionAllowed,
    [property: Id(3)] bool ForceClosed,
    [property: Id(4)] int ViewMode,
    [property: Id(5)] List<RoomInfoData> Rooms);

/// <summary>The navigator's tab structure and categories, loaded from the database.</summary>
public interface INavigatorCatalog
{
    Task<IReadOnlyList<NavigatorContext>> GetContextsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<FlatCategory>> GetFlatCategoriesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<EventCategory>> GetEventCategoriesAsync(CancellationToken ct = default);

    /// <summary>The blocks a search code expands to; an unknown code falls back to the
    /// configured default request code.</summary>
    Task<IReadOnlyList<NavigatorBlockDefinition>> GetBlocksForAsync(string searchCode, CancellationToken ct = default);

    /// <summary>The blocks configured for exactly this request code, none when it has none.</summary>
    Task<IReadOnlyList<NavigatorBlockDefinition>> GetExactBlocksForAsync(string requestCode, CancellationToken ct = default);

    /// <summary>A block as some tab shows it, by its own code: what "show more" on that block asks for.</summary>
    Task<NavigatorBlockDefinition?> GetBlockAsync(string blockCode, CancellationToken ct = default);
}

/// <summary>The navigator's per-user state: starred rooms and the room-creation entry point.</summary>
public interface INavigatorGrain : IGrainWithIntegerKey
{
    /// <summary>The rooms this user has starred.</summary>
    Task<List<int>> GetFavouritesAsync(Guid userId);

    /// <summary>Stars or unstars a room. Returns false when the room does not exist or the
    /// user is already at their limit.</summary>
    Task<bool> SetFavouriteAsync(Guid userId, int roomId, bool isFavourite);

    /// <summary>How many rooms a user may star.</summary>
    Task<int> GetFavouriteLimitAsync();

    /// <summary>The most-used room tags, most popular first.</summary>
    Task<List<RoomTagCount>> GetPopularTagsAsync();

    /// <summary>Creates a room owned by the user. Returns null when the model is unknown.</summary>
    Task<RoomCreationResult?> CreateRoomAsync(PlayerData owner, RoomCreationRequest request);
}

/// <summary>What the client sends in CreateFlat.</summary>
[GenerateSerializer]
public sealed partial record RoomCreationRequest(
    [property: Id(0)] string Name,
    [property: Id(1)] string Description,
    [property: Id(2)] string ModelName,
    [property: Id(3)] int CategoryId,
    [property: Id(4)] int MaxVisitors,
    [property: Id(5)] int TradeMode);

/// <summary>The room the client should now open.</summary>
[GenerateSerializer]
public sealed partial record RoomCreationResult(
    [property: Id(0)] int RoomId,
    [property: Id(1)] string Name);

/// <summary>Runs navigator searches against the room store. Stateless; any instance will do.</summary>
public interface INavigatorSearchGrain : IGrainWithIntegerKey
{
    Task<List<NavigatorSearchBlock>> SearchAsync(Guid requesterId, string searchCode, string filter);
}

/// <summary>Hotel-wide live room population, fed by the room grains as avatars come and go.</summary>
public interface IRoomPopulationGrain : IGrainWithIntegerKey
{
    /// <summary>The room's count, from the silo it is held on.</summary>
    Task SetAsync(int roomId, int userCount, SiloAddress host);

    /// <summary>The silo went, and every room held there emptied with it.</summary>
    Task ForgetHostAsync(SiloAddress host);

    Task<Dictionary<int, int>> GetAsync();
}
