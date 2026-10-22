using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using Habbo.Silo.Abstractions.Chat;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Wired;

namespace Habbo.Silo.Abstractions.Rooms;

/// <summary>One grain per room (key = wire room id). Owns room state, the tick, and walking.</summary>
public interface IGameRoomGrain : IGrainWithIntegerKey
{
    /// <summary>The silo the room is held on: a session in it is out of it when that silo goes.</summary>
    Task<SiloAddress> GetHostAsync();

    /// <summary>Lets the player in on their next entry, past the door, the password and any ban: staff summoned them,
    /// or are following them. It only hands out a pass, so it may run between the turns of another call: a staff
    /// command run in this room cannot wait on the room itself, nor two staff following each other from two rooms on
    /// each other.</summary>
    [AlwaysInterleave]
    Task ExpectSummonedAsync(Guid playerId);

    /// <summary>Whether the player is in the room and asleep there: idle for as long as the room lets them be.</summary>
    Task<bool> IsAsleepAsync(Guid playerId);

    /// <summary>Room info for navigator rows and the GetGuestRoomResult round-trip.
    /// <paramref name="requesterId"/> drives per-requester fields like CanMute.</summary>
    Task<RoomInfoData?> GetInfoAsync(Guid requesterId);

    /// <summary>Registers the player as an entity and returns the room-entry snapshot.</summary>
    Task<RoomLoadData?> EnterAsync(Guid playerId, Guid sessionGuid, PlayerData player);

    Task LeaveAsync(Guid playerId);

    /// <summary>A session leaving: the player leaves unless their avatar here belongs to a newer session (a
    /// second login that entered before the first's connection closed).</summary>
    Task LeaveSessionAsync(Guid playerId, Guid sessionGuid);

    /// <summary>Walks to a tile; re-paths mid-walk. Clicking the avatar's own tile is a no-op —
    /// steps are never interrupted mid-slide (an early stand update would snap the animation).</summary>
    Task MoveAsync(Guid playerId, int x, int y);

    Task ChatAsync(Guid playerId, string text, int styleId, int trackingId, RoomChatKind kind);

    Task PlaceFloorItemAsync(Guid playerId, int itemId, int x, int y, int rotation);
    Task PlaceWallItemAsync(Guid playerId, int itemId, string wallLocation);

    /// <summary>Borrows Builders Club furniture onto the floor, if the player may build here and has room in their
    /// club limit; on the trial it first asks whether to hide the room.</summary>
    Task PlaceBuildersClubFloorItemAsync(Guid playerId, BuildersClubPlacement placement, int x, int y, int rotation);

    /// <summary>Borrows Builders Club furniture onto a wall, as for the floor.</summary>
    Task PlaceBuildersClubWallItemAsync(Guid playerId, BuildersClubPlacement placement, string wallLocation);
    Task MoveFloorItemAsync(Guid playerId, int itemId, int x, int y, int rotation);
    Task MoveWallItemAsync(Guid playerId, int itemId, string wallLocation);
    /// <summary>Picks an item up; one that takes more with it (a wired variable, its values) is only picked up once
    /// the player has <paramref name="confirmed"/> it.</summary>
    Task PickupItemAsync(Guid playerId, int itemId, bool confirmed);

    /// <summary>A player asked an item to do something. What it does, if anything, is up to the
    /// item's behaviour; the room checks the player may use it first.</summary>
    Task UseItemAsync(Guid playerId, int itemId, FurniAction action, int param);

    /// <summary>Sends a wired box's dialog, as the client asks for it after an OpenComposer, to a
    /// player who may change the room's wired.</summary>
    Task OpenWiredAsync(Guid playerId, int itemId);

    /// <summary>A wired box's dialog saved. The player is told it was saved, or why not.</summary>
    Task SaveWiredAsync(Guid playerId, WiredSaveData save);

    /// <summary>The "apply" button of a match-to-snapshot effect's dialog: puts its furni back.</summary>
    Task ApplyWiredSnapshotAsync(Guid playerId, int itemId);

    /// <summary>The owner gives rights to a user (by account id); null when nothing changed.</summary>
    Task<FlatControllerAdded?> AssignRightsAsync(Guid ownerId, int accountId);

    /// <summary>The owner takes rights back from these users; says from whom they were taken.</summary>
    Task<RightsRemoved> RemoveRightsAsync(Guid ownerId, IReadOnlyList<int> accountIds);

    /// <summary>The owner takes rights back from everyone.</summary>
    Task<RightsRemoved> RemoveAllRightsAsync(Guid ownerId);

    /// <summary>A user gives up their own rights.</summary>
    Task RemoveOwnRightsAsync(Guid playerId);

    /// <summary>Who has rights, for the owner or staff; null for anyone else.</summary>
    Task<IReadOnlyList<FlatController>?> GetFlatControllersAsync(Guid requesterId);

    // the wired menu: each answers the user, if they may read the room's wired

    Task GetWiredSettingsAsync(Guid playerId);

    /// <summary>The owner (or staff) sets who may change and read the room's wired, and its time zone.</summary>
    Task SetWiredSettingsAsync(Guid playerId, int modifyPermissionMask, int readPermissionMask, string timezone);

    Task GetWiredLogsAsync(Guid playerId, WiredLogQuery query);
    Task GetWiredStatsAsync(Guid playerId);
    Task GetWiredErrorsAsync(Guid playerId);
    Task ClearWiredErrorsAsync(Guid playerId);

    /// <summary>The room's variables' hash, and what a client holding these variables lacks.</summary>
    Task GetWiredVariablesHashAsync(Guid playerId);
    Task GetWiredVariablesDiffsAsync(Guid playerId, IReadOnlyList<KnownWiredVariable> known);

    /// <summary>A room referencing one of this room's shared variables changed its value. One-way: the
    /// change may set off wired here that writes back through a reference into the room telling us.</summary>
    [Orleans.Concurrency.OneWay]
    Task SharedVariableChangedAsync(SharedVariableChange change);

    // the wired menu's variable tools
    Task GetWiredVariablesForObjectAsync(Guid playerId, int target, int objectId);
    Task EditWiredVariableAsync(Guid playerId, WiredVariableEdit edit);
    Task GetWiredVariableHoldersAsync(Guid playerId, string variableId);
    Task GetWiredVariableOwnersAsync(Guid playerId, WiredOwnersQuery query);
    Task GetWiredUserPermanentVariablesAsync(Guid playerId, int entityType, int entityId);
    Task EditWiredUserPermanentVariableAsync(Guid playerId, int entityType, WiredVariableEdit edit);

    /// <summary>The user clicked an item: the room's wired hears it.</summary>
    Task ClickFurniAsync(Guid playerId, int itemId);

    /// <summary>The user clicked the avatar with this room index, in a room whose wired listens for
    /// that: the wired hears it, and says whether the avatar menu may open.</summary>
    Task ClickUserAsync(Guid playerId, int roomIndex);

    /// <summary>The user turns to face a tile.</summary>
    Task LookToAsync(Guid playerId, int x, int y);

    /// <summary>1 = sit (on a sittable tile), 0 = stand.</summary>
    Task ChangePostureAsync(Guid playerId, int posture);

    /// <summary>An <see cref="AvatarExpression"/>: shown to the room, or (idle) falls asleep.</summary>
    Task ExpressAsync(Guid playerId, int expression);

    /// <summary>Starts a dance, or stops (0). Dances past the first are the club's.</summary>
    Task DanceAsync(Guid playerId, int style);

    Task ShowSignAsync(Guid playerId, int sign);

    /// <summary>Gives one of the player's respects to the user with that account id, if they are
    /// in the room and the player has any left today.</summary>
    Task RespectAsync(Guid playerId, int targetAccountId);

    /// <summary>The player's look, motto or score changed: the room sees the new them. It only swaps in the player's
    /// data and shows it, so it may run between the turns of another call: a staff command run in a room (a mute, a
    /// rank) refreshes whoever it is about, who may be in that same room.</summary>
    [AlwaysInterleave]
    Task RefreshPlayerAsync(Guid playerId, PlayerData player);

    /// <summary>The player took a new name: the room sees it.</summary>
    Task RenamePlayerAsync(Guid playerId, PlayerData player);

    /// <summary>The player now wears these badges: the room sees them.</summary>
    Task ShowWornBadgesAsync(Guid playerId, IReadOnlyList<WornBadge> badges);

    /// <summary>Wears an effect, or none (0), for the room to see.</summary>
    Task WearEffectAsync(Guid playerId, int effectId);

    /// <summary>An effect the player wore ran out: the avatar takes it off, if still wearing it. One way: the player
    /// grain tells it from a timer and does not wait, as the room may be waiting on that player grain.</summary>
    [OneWay]
    Task EffectExpiredAsync(Guid playerId, int effectId);

    /// <summary>Puts down whatever the avatar carries.</summary>
    Task DropHandItemAsync(Guid playerId);

    /// <summary>Hands what the avatar carries to the user with this account id.</summary>
    Task PassHandItemAsync(Guid playerId, int targetAccountId);

    /// <summary>The avatar started or stopped typing.</summary>
    Task SetTypingAsync(Guid playerId, bool typing);

    /// <summary>The room's group changed (made, edited, deleted, members in or out): the room
    /// rereads it, and who has rights here because of it.</summary>
    Task GroupChangedAsync();

    /// <summary>The player picked another favourite group; the room shows it.</summary>
    Task FavouriteGroupChangedAsync(Guid playerId);

    /// <summary>The badges of the groups the player sees here (HabboGroupBadges).</summary>
    Task SendGroupBadgesAsync(Guid playerId);

    /// <summary>Sends everything this owner has in the room back to their inventory.</summary>
    Task ReturnFurniOfAsync(Guid ownerId);

    /// <summary>Whether the player is in the room.</summary>
    Task<bool> HasUserAsync(Guid playerId);

    Task<int> UserCountAsync();

    /// <summary>Staff send the user out of the room.</summary>
    Task StaffKickAsync(Guid playerId);

    /// <summary>A message (or caution) from staff to everyone here but staff, who may be sent out too.</summary>
    Task StaffMessageAsync(string message, bool caution, bool kick);

    /// <summary>Staff take the room in hand: lock its door, rename it, empty it.</summary>
    Task StaffModerateAsync(bool lockDoor, string? newName, bool kickUsers);

    /// <summary>Someone is on their way in through the teleport or hopper with this id: they are let in, and arrive
    /// inside it. False when there is no such teleport here. It changes nothing another call could be halfway
    /// through, so it may run between the turns of one, and two rooms sending people to each other cannot wait on
    /// each other.</summary>
    [Orleans.Concurrency.AlwaysInterleave]
    Task<bool> ExpectArrivalAsync(Guid playerId, int itemId);

    /// <summary>The player uses up a clothing furni of theirs here: the clothing it holds is theirs for good.</summary>
    Task BindClothingAsync(Guid playerId, int itemId);

    /// <summary>Whether the player may come in; ringing the doorbell when they must wait.</summary>
    Task<RoomAccess> RequestAccessAsync(Guid playerId, Guid sessionGuid, PlayerData player, string password);

    /// <summary>Somebody with rights opens the door to whoever rang, or does not.</summary>
    Task LetUserInAsync(Guid playerId, string userName, bool allow);

    Task SendSettingsAsync(PlayerData player);

    Task SaveSettingsAsync(PlayerData player, RoomSettingsUpdate update);

    Task UpdateCategoryAndTradeAsync(PlayerData player, int categoryId, int tradeMode);

    /// <summary>Deletes the room: everyone out, the furniture back to its owners. True when it went.</summary>
    Task<bool> DeleteAsync(PlayerData player);

    Task SendFilterAsync(PlayerData player);

    Task UpdateFilterAsync(PlayerData player, bool add, string word);

    Task KickUserAsync(Guid playerId, int accountId);

    Task BanUserAsync(Guid playerId, int accountId, string duration);

    Task UnbanUserAsync(Guid playerId, int accountId);

    Task SendBannedUsersAsync(Guid playerId);

    Task MuteUserAsync(Guid playerId, int accountId, int minutes);

    Task UnmuteUserAsync(Guid playerId, int accountId);

    Task ToggleMuteAllAsync(Guid playerId);

    Task AmbassadorAlertAsync(Guid playerId, int accountId);

    /// <summary>A visitor likes the room.</summary>
    Task RateAsync(Guid playerId, int points);

    /// <summary>The room's promotion renamed or redescribed by its owner (or staff).</summary>
    Task EditEventAsync(PlayerData player, int eventId, string name, string description);

    /// <summary>The room's promotion ended early by its owner (or staff).</summary>
    Task CancelEventAsync(PlayerData player, int eventId);

    /// <summary>A promotion was bought for the room elsewhere: read it and show it to everyone here.</summary>
    Task EventChangedAsync();

    /// <summary>Staff picked the room, or unpicked it.</summary>
    Task StaffPickChangedAsync(bool staffPick);

    /// <summary>A plain click on another avatar: one's own avatar turns to them.</summary>
    Task ClickCharacterAsync(Guid playerId, int roomIndex);

    /// <summary>The visitor came in from a promoted category, at this place in it.</summary>
    Task RecordEntrySourceAsync(Guid playerId, string category, string position);

    /// <summary>The tiles furniture stands on, for the floor plan editor.</summary>
    Task SendOccupiedTilesAsync(Guid playerId);

    Task SendEntryTileAsync(Guid playerId);

    /// <summary>Saves a new floor plan, or says why not; everyone in the room comes back in to it.</summary>
    Task UpdateFloorPlanAsync(PlayerData player, string heightmap, int? doorX, int? doorY, int? doorDirection,
        int? wallThickness, int? floorThickness, int? fixedWallsHeight);

    /// <summary>Comes in to watch without an avatar, the stands having a place.</summary>
    Task<RoomLoadData?> SpectateAsync(Guid playerId, Guid sessionGuid, PlayerData player);

    /// <summary>Moves the player to the spectators' line (1) or the visitors' (2).</summary>
    Task ChangeQueueAsync(Guid playerId, PlayerData player, int target);

    /// <summary>Sticks a note up from a pad; without rights, the player is asked to write it first.</summary>
    Task PlacePostItAsync(Guid playerId, int padId, string location);

    /// <summary>Sticks up a note written beforehand, by someone without rights.</summary>
    Task AddSpamWallPostItAsync(Guid playerId, int padId, string location, string colorHex, string text);

    /// <summary>A note's colour and text, for the one reading it.</summary>
    Task SendItemDataAsync(Guid playerId, int itemId);

    Task SetItemDataAsync(Guid playerId, int itemId, string colorHex, string text);

    /// <summary>Tears a note off the wall.</summary>
    Task RemovePostItAsync(Guid playerId, int itemId);

    Task SendDimmerPresetsAsync(Guid playerId, int itemId);

    /// <summary>Saves a dimmer preset; applying it shows it at once.</summary>
    Task SaveDimmerPresetAsync(Guid playerId, int itemId, int presetId, int effectType, string colorHex, int brightness, bool apply);

    /// <summary>Switches the dimmer on or off.</summary>
    Task ToggleDimmerAsync(Guid playerId, int itemId);

    /// <summary>Puts a wallpaper, floor or landscape from the player's inventory down in the room, using it up.</summary>
    Task DecorateAsync(Guid playerId, int itemId);

    /// <summary>The background toner's colour.</summary>
    Task SetBackgroundColorAsync(Guid playerId, int itemId, int hue, int saturation, int lightness);

    /// <summary>A changing gate's outfit for one gender.</summary>
    Task SetClothingChangeDataAsync(Guid playerId, int itemId, string gender, string figure);

    Task SetMannequinNameAsync(Guid playerId, int itemId, string name);

    /// <summary>Dresses a mannequin in the player's own clothes.</summary>
    Task SetMannequinFigureAsync(Guid playerId, int itemId);

    /// <summary>Walks the player through a one-way door from the side it faces.</summary>
    Task EnterOneWayDoorAsync(Guid playerId, int itemId);

    Task SpinWheelAsync(Guid playerId, int itemId);

    /// <summary>The area an area hider hides.</summary>
    Task SetAreaHideAsync(Guid playerId, int itemId, int rootX, int rootY, int width, int length, bool invisibility, bool wallItems, bool invert);

    /// <summary>A stack helper's height, in hundredths; -100 puts it on top of what is under it.</summary>
    Task SetStackHeightAsync(Guid playerId, int itemId, int height, bool? multiWalk);

    /// <summary>An item's map data, from the info stand.</summary>
    Task SetObjectDataAsync(PlayerData player, int itemId, IReadOnlyList<string> keysAndValues);

    /// <summary>Opens one of the player's presents standing in the room.</summary>
    Task OpenPresentAsync(Guid playerId, int itemId);

    /// <summary>Tells the entrant what the avatars already here are doing that the Users packet does not carry: who
    /// sleeps, carries something, wears an effect or dances, one avatar after another.</summary>
    Task ShowOthersToAsync(Guid playerId);

    /// <summary>The player's client has the room: the entrant steps in, and the room greets them.</summary>
    Task EnteredAsync(Guid playerId);

    /// <summary>Puts one of the player's bots down on a tile, or says why not.</summary>
    Task PlaceBotAsync(Guid playerId, int botId, int x, int y);

    /// <summary>Picks a bot up; it goes back to its owner.</summary>
    Task RemoveBotAsync(Guid playerId, int botId);

    /// <summary>Uses one of a bot's skills.</summary>
    Task CommandBotAsync(Guid playerId, int botId, int skillId, string data);

    /// <summary>How one of the player's bots has a skill set up.</summary>
    Task SendBotConfigurationAsync(Guid playerId, int botId, int skillId);

    /// <summary>Puts one of the player's pets down on a tile, or says why not.</summary>
    Task PlacePetAsync(Guid playerId, int petId, int x, int y);

    /// <summary>Picks a pet up; it goes back to its owner.</summary>
    Task RemovePetAsync(Guid playerId, int petId);

    /// <summary>Puts a pet on another tile, facing a direction.</summary>
    Task MovePetAsync(Guid playerId, int petId, int x, int y, int direction);

    Task SendPetInfoAsync(Guid playerId, int petId);

    /// <summary>The player picked a pet out.</summary>
    Task PetSelectedAsync(Guid playerId, int petId);

    /// <summary>The commands a pet knows, and those it obeys at its level.</summary>
    Task SendPetCommandsAsync(Guid playerId, int petId);

    Task RespectPetAsync(Guid playerId, int petId);

    /// <summary>Hands what the player carries to their pet to eat.</summary>
    Task PassHandItemToPetAsync(Guid playerId, int petId);

    /// <summary>Uses a dye, a hairstyle or a saddle on the player's pet.</summary>
    Task CustomizePetAsync(Guid playerId, int itemId, int petId);

    Task RemoveSaddleAsync(Guid playerId, int petId);

    /// <summary>Gets on a pet, or off it.</summary>
    Task MountPetAsync(Guid playerId, int petId, bool mount);

    /// <summary>Lets anyone ride the pet, or only its owner.</summary>
    Task TogglePetRidingAsync(Guid playerId, int petId);

    /// <summary>Lets the pet be bred with others' pets, or not.</summary>
    Task TogglePetBreedingAsync(Guid playerId, int petId);

    /// <summary>Breeds the two pets in the player's nest, naming the baby.</summary>
    Task ConfirmPetBreedingAsync(Guid playerId, int nestId, string name, int petOneId, int petTwoId);

    /// <summary>Sends the pets in the player's nest away without breeding them.</summary>
    Task CancelPetBreedingAsync(Guid playerId, int nestId);

    /// <summary>Asks to breed two plants (0), or turns down (1) or agrees to (2) such a request.</summary>
    Task BreedPlantsAsync(Guid playerId, int state, int petOneId, int petTwoId);

    /// <summary>Waters a plant (0) or gives it light (1).</summary>
    Task GiveSupplementAsync(Guid playerId, int petId, int supplementType);

    /// <summary>Harvests the player's grown plant.</summary>
    Task HarvestPlantAsync(Guid playerId, int petId);

    /// <summary>Turns the player's dead plant into compost.</summary>
    Task CompostPlantAsync(Guid playerId, int petId);

    /// <summary>The playlists a video TV offers, and what it shows.</summary>
    Task SendYoutubeDisplayAsync(Guid playerId, int itemId);

    Task SetYoutubePlaylistAsync(Guid playerId, int itemId, string playlistId);

    /// <summary>Previous (0), next (1), pause (2) or carry on (3).</summary>
    Task ControlYoutubeAsync(Guid playerId, int itemId, int command);

    /// <summary>The players in the room.</summary>
    Task<IReadOnlyList<Guid>> GetUserIdsAsync();

    /// <summary>Chests: looking inside, taking out, their settings and locks, and buying them more room.</summary>
    Task OpenChestAsync(Guid playerId, int chestId);

    Task CloseChestAsync(Guid playerId, int chestId);

    Task WithdrawChestItemsAsync(Guid playerId, int chestId, bool isWallItem, int typeId, string posterId, int amount);

    Task WithdrawChestCoinsAsync(Guid playerId, int chestId, int amount);

    Task WithdrawAllFromChestAsync(Guid playerId, int chestId);

    Task SetChestOptionsAsync(Guid playerId, int chestId, bool locked, bool autoLock, int capacity);

    Task SetChestPreferencesAsync(Guid playerId, int chestId, string name, string description, bool everyoneCanOpen,
        bool everyoneCanDonate, int stateControlMode, int previewMode, int previewAmount, bool wiredEnabled);

    Task SetChestNotificationsAsync(Guid playerId, int chestId, int notifyMode, bool chestFull, bool donation,
        bool someoneWithdraws, bool chestEmpty, bool wiredTransaction);

    Task LockAllChestsAsync(Guid playerId, bool locked, bool allChests);

    Task UpgradeChestAsync(Guid playerId, int chestId, int levels);

    /// <summary>The chest transaction logs: a chest's, the room's, and one transaction in full.</summary>
    Task SendChestLogsAsync(Guid playerId, int chestId, int pageSize, int page);

    Task SendRoomChestLogsAsync(Guid playerId, int pageSize, int page);

    Task SendChestLogDetailsAsync(Guid playerId, long transactionId);

    /// <summary>Reloads the room from the wired menu, everyone coming back in; rolling its furni back first if asked.</summary>
    Task WiredUpdateRoomAsync(Guid playerId, bool rollBack);

    /// <summary>A contract's requirements for its dialog, and the dialog's save.</summary>
    Task SendContractAsync(Guid playerId, int contractId);

    Task UpdateContractAsync(Guid playerId, ContractUpdate update);

    /// <summary>Trading with wired: depositing into a chest.</summary>
    Task StartChestDepositAsync(Guid playerId, int chestId);

    Task ChangeWiredTradeItemsAsync(Guid playerId, bool remove, IReadOnlyList<int> itemIds);

    Task ConfirmWiredTradeAsync(Guid playerId, bool confirmed);

    Task CancelWiredTradeAsync(Guid playerId);

    /// <summary>One of two friends confirms, or turns down, locking a friendship furni.</summary>
    Task ConfirmFriendLockAsync(Guid playerId, int itemId, bool confirmed);

    /// <summary>Opens the player's mystery trophy box, engraving the trophy inside.</summary>
    Task OpenMysteryTrophyAsync(Guid playerId, int itemId, string inscription);

    /// <summary>Who rents a space, and whether the player may.</summary>
    Task SendRentableSpaceStatusAsync(Guid playerId, int itemId);

    Task RentSpaceAsync(Guid playerId, int itemId);

    /// <summary>Ends a space's rent, for its owner, staff or the renter.</summary>
    Task CancelRentAsync(Guid playerId, int itemId);

    /// <summary>Opens a box with a pet in it, naming the pet.</summary>
    Task OpenPetPackageAsync(Guid playerId, int itemId, string name);

    /// <summary>Starts trading with the avatar at that room index, or says why not.</summary>
    Task OpenTradingAsync(Guid playerId, int roomIndex);

    Task AddItemsToTradeAsync(Guid playerId, IReadOnlyList<int> itemIds);

    Task RemoveItemFromTradeAsync(Guid playerId, int itemId);

    /// <summary>Accepts what is on the table, or takes the acceptance back.</summary>
    Task SetTradeAcceptedAsync(Guid playerId, bool accepted);

    /// <summary>Confirms the accepted trade; when both have, it is done.</summary>
    Task ConfirmTradeAsync(Guid playerId);

    /// <summary>Ends the trade, for both.</summary>
    Task CloseTradeAsync(Guid playerId);

    /// <summary>A resolution statue's owner opens it (achievement 0): its challenge's progress, or the achievements it
    /// offers; or picks the achievement to take on.</summary>
    Task OpenResolutionAsync(Guid playerId, int itemId, int achievementId);

    /// <summary>Gives up a resolution statue's challenge, so another can be picked.</summary>
    Task ResetResolutionAsync(Guid playerId, int itemId);

    /// <summary>Changes a placed item's data from outside the room; false when the item is not here. It interleaves, as
    /// the change may come of something the room itself set off (a level reached by what a player did here). The change
    /// is made on the room's next tick, so it never lands in the middle of the room's own work.</summary>
    [Orleans.Concurrency.AlwaysInterleave]
    Task<bool> ReplaceItemDataAsync(int itemId, FurniDataView data);
}
