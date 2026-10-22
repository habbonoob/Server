namespace Habbo.Silo.Achievements;

/// <summary>The hotel's achievements the server moves on, by their names in the achievements table; what each counts is
/// the hotel's own description of it (badge_desc_*).</summary>
public static class AchievementNames
{
    // identity
    /// <summary>"For logging in %limit% days in a row." A day missed starts it over (Habbox wiki, Resident).</summary>
    public const string Login = "ACH_Login";

    /// <summary>"For being a member of the community for %limit% day."</summary>
    public const string RegistrationDuration = "ACH_RegistrationDuration";

    /// <summary>"For spending %limit% minutes in the Hotel."</summary>
    public const string AllTimeHotelPresence = "ACH_AllTimeHotelPresence";

    /// <summary>"For having %limit% days of Habbo Club membership": days added to the account, not days gone by (Habbox
    /// wiki, HC Member).</summary>
    public const string HabboClub = "ACH_HC";

    /// <summary>"For joining the Habbo Club": counted in membership periods.</summary>
    public const string BasicClub = "ACH_BasicClub";

    /// <summary>"For changing your look for the first time."</summary>
    public const string AvatarLooks = "ACH_AvatarLooks";

    /// <summary>"Thanks for selecting your Habbo name after registering."</summary>
    public const string Name = "ACH_Name";

    /// <summary>"For logging in during happy hour": 19:00 to 20:00 UTC on weekdays, 17:00 to 18:00 UTC at weekends
    /// (Habbox wiki).</summary>
    public const string HappyHour = "ACH_HappyHour";

    /// <summary>"For collecting %limit% custom chat styles."</summary>
    public const string ChatStyleOwner = "ACH_ChatStyleOwner";

    // social
    /// <summary>"For having %limit% friends in your friend list."</summary>
    public const string FriendListSize = "ACH_FriendListSize";

    /// <summary>"For buying %limit% item as gift from shop", for another Habbo (Habbox wiki, Nice One).</summary>
    public const string GiftGiver = "ACH_GiftGiver";

    /// <summary>"For receiving a gift from other people."</summary>
    public const string GiftReceiver = "ACH_GiftReceiver";

    /// <summary>"For leaving %limit% stickie notes in other people's rooms."</summary>
    public const string NotesLeft = "ACH_NotesLeft";

    /// <summary>"For receiving %limit% stickie notes in your rooms."</summary>
    public const string NotesReceived = "ACH_NotesReceived";

    /// <summary>"For giving respect %limit% times."</summary>
    public const string RespectGiven = "ACH_RespectGiven";

    /// <summary>"For earning respect %limit% time."</summary>
    public const string RespectEarned = "ACH_RespectEarned";

    // rooms and the hotel
    /// <summary>"For %limit% room visits. Couch potato."</summary>
    public const string RoomEntry = "ACH_RoomEntry";

    /// <summary>"For using a furni (hint: double click/tap)."</summary>
    public const string HabboExplorer = "ACH_HabboExplorer";

    /// <summary>"For walking %limit% tiles."</summary>
    public const string LegDay = "ACH_LegDay";

    /// <summary>"For having visitors spend %limit% minutes in your rooms."</summary>
    public const string RoomDecoHosting = "ACH_RoomDecoHosting";

    /// <summary>"For being inside a chat / discussion or public room for %limit% minutes."</summary>
    public const string TimeSpentRoomTypeChat = "ACH_TimeSpentRoomTypeChat";

    /// <summary>"For being inside a gaming room for %limit% minutes."</summary>
    public const string TimeSpentRoomTypeGames = "ACH_TimeSpentRoomTypeGames";

    /// <summary>"For being inside an agency or RPG room for %limit% minutes."</summary>
    public const string TimeSpentRoomTypeRpg = "ACH_TimeSpentRoomTypeRPG";

    /// <summary>"For being inside a trading room for %limit% minutes."</summary>
    public const string TimeSpentRoomTypeTrade = "ACH_TimeSpentRoomTypeTrade";

    /// <summary>"For switching accessibility in room settings."</summary>
    public const string SelfModDoorModeSeen = "ACH_SelfModDoorModeSeen";

    /// <summary>"For changing forum visibility settings."</summary>
    public const string SelfModForumCanReadSeen = "ACH_SelfModForumCanReadSeen";

    /// <summary>"For changing forum post settings."</summary>
    public const string SelfModForumCanPostSeen = "ACH_SelfModForumCanPostSeen";

    /// <summary>"For changing forum thread creation settings."</summary>
    public const string SelfModForumCanPostThrdSeen = "ACH_SelfModForumCanPostThrdSeen";

    /// <summary>"For changing forum moderation settings."</summary>
    public const string SelfModForumCanModerateSeen = "ACH_SelfModForumCanModerateSeen";

    /// <summary>"For voting %limit% times in room competitions."</summary>
    public const string RoomCompetitionVoter = "ACH_RoomCompetitionVoter";

    // trading and buying
    /// <summary>"For completing %limit% trades."</summary>
    public const string TradeCount = "ACH_TradeCount";

    /// <summary>"For trading Credit Furni worth %limit% coins."</summary>
    public const string CoinValueTraded = "ACH_CoinValueTraded";

    /// <summary>"For selling %limit% items at the Marketplace."</summary>
    public const string MarketplaceSales = "ACH_HabboBuilder_TSale_";

    /// <summary>"For purchasing %limit% LTD rares."</summary>
    public const string LtdPurchaser = "ACH_LTDPurchaser";

    /// <summary>"For successfully purchasing an LTD in the first moments, %limit% times."</summary>
    public const string LtdEarlyBird = "ACH_LTDEarlyBird";

    /// <summary>"For developing %limit% photos."</summary>
    public const string CameraPhotoCount = "ACH_CameraPhotoCount";

    /// <summary>"For recycling %limit% items."</summary>
    public const string RecycledItems = "ACH_RecycledItems";

    // building rooms
    /// <summary>"For building a room with %limit% furni."</summary>
    public const string RoomDecoFurniCount = "ACH_RoomDecoFurniCount";

    /// <summary>"For building a room with %limit% different furni."</summary>
    public const string RoomDecoFurniTypeCount = "ACH_RoomDecoFurniTypeCount";

    /// <summary>"Place Credit Furni worth %limit% coins inside your room."</summary>
    public const string PlaceCreditValue = "ACH_PlaceCreditValue";

    /// <summary>"For making Swiss cheese by adding %limit% floor hole furni."</summary>
    public const string RoomDecoHoleFurniCount = "ACH_RoomDecoHoleFurniCount";

    /// <summary>A wallpaper, floor or landscape put down in a room: each one counts.</summary>
    public const string RoomDecoWallpaper = "ACH_RoomDecoWallpaper";

    public const string RoomDecoFloor = "ACH_RoomDecoFloor";

    public const string RoomDecoLandscape = "ACH_RoomDecoLandscape";

    /// <summary>"For getting %limit% of your rooms added to the Navigator's 'Explore' section."</summary>
    public const string StaffPickedRooms = "ACH_Spr";

    /// <summary>"For creating a game where users have paid credits %limit% times."</summary>
    public const string MoneyPrinter = "ACH_MoneyPrinter";

    /// <summary>"For earning %limit% credits by playing user created games."</summary>
    public const string GamerPaycheck = "ACH_GamerPaycheck";

    // pets
    /// <summary>"For increasing your pet's level %limit% times."</summary>
    public const string PetLevelUp = "ACH_PetLevelUp";

    /// <summary>"For scratching anyone's pets at least %limit% times": another Habbo's (Habbox wiki, Pet Lover).</summary>
    public const string PetRespectGiver = "ACH_PetRespectGiver";

    /// <summary>"For having your pets scratched %limit% times", by other users (Habbox wiki).</summary>
    public const string PetRespectReceiver = "ACH_PetRespectReceiver";

    /// <summary>"For giving pets %limit% points of food."</summary>
    public const string PetFeeding = "ACH_PetFeeding";

    /// <summary>"For owning %limit% pets": pets bought, those given to others too, not those received (Habbox wiki,
    /// Pet Owner).</summary>
    public const string PetLover = "ACH_PetLover";

    /// <summary>"For letting others ride your horse %limit% times."</summary>
    public const string HorseRent = "ACH_HorseRent";

    /// <summary>"For breeding %limit% monster plants."</summary>
    public const string MonsterPlantBreeder = "ACH_MonsterPlantBreeder";

    /// <summary>"For failing to get the seed %limit% times when breeding monster plants."</summary>
    public const string MonsterPlantUnluckyGardener = "ACH_MonsterPlantUnluckyGardener";

    /// <summary>"For tending to %limit% monster plants."</summary>
    public const string MonsterPlantTreater = "ACH_MonsterPlantTreater";

    /// <summary>"For giving %limit% cold monster plants a warm heart."</summary>
    public const string MonsterPlantHealer = "ACH_MonsterPlantHealer";

    /// <summary>"%limit% monster plants lost."</summary>
    public const string MonsterPlantGardenOfDeath = "ACH_MonsterPlantGardenOfDeath";

    /// <summary>"For having %limit% colours of monster plants": differently coloured plants owned.</summary>
    public const string MonsterPlantColorCollector = "ACH_MonsterPlantColorCollector";

    /// <summary>"For having %limit% shapes of monster plants": differently shaped (bodied) plants owned.</summary>
    public const string MonsterPlantShapeCollector = "ACH_MonsterPlantShapeCollector";

    // room games
    /// <summary>Battle Banzai tiles locked for one's team.</summary>
    public const string BattleBallTilesLocked = "ACH_BattleBallTilesLocked";

    /// <summary>Battle Banzai games played.</summary>
    public const string BattleBallPlayer = "ACH_BattleBallPlayer";

    /// <summary>Battle Banzai games won.</summary>
    public const string BattleBallWinner = "ACH_BattleBallWinner";

    /// <summary>Freeze games played.</summary>
    public const string FreezePlayer = "ACH_FreezePlayer";

    /// <summary>Freeze games won.</summary>
    public const string FreezeWinner = "ACH_FreezeWinner";

    /// <summary>Freeze power-ups picked up.</summary>
    public const string FreezePowerUp = "ACH_FreezePowerUp";

    /// <summary>Football goals scored, by the kicker.</summary>
    public const string FootballGoalScored = "ACH_FootballGoalScored";

    /// <summary>Football goals scored in one's rooms, for the room's owner.</summary>
    public const string FootballGoalScoredInRoom = "ACH_FootballGoalScoredInRoom";

    // crafting
    /// <summary>"For getting %roman% non-secret recipe items using a crafting table."</summary>
    public const string Crafting = "ACH_Atcg";

    /// <summary>"For getting %roman% secret recipe items using a crafting table."</summary>
    public const string SecretCrafting = "ACH_AtcgSecret";
}
