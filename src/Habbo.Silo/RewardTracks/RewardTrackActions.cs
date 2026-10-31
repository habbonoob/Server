namespace Habbo.Silo.RewardTracks;

/// <summary>What reward track and daily tasks count, by the action types Habbo's tracks name.</summary>
public static class RewardTrackActions
{
    public const string EnterOtherUsersRoom = "ENTER_OTHER_USERS_ROOM";
    public const string ChatWithSomeone = "CHAT_WITH_SOMEONE";
    public const string Dance = "DANCE";
    public const string Wave = "WAVE";
    public const string UseHabbicon = "USE_HABBICON";
    public const string GiveRespect = "GIVE_RESPECT";
    public const string ReplenishRespect = "REPLENISH_RESPECT";
    public const string RequestFriend = "REQUEST_FRIEND";
    public const string PetRespect = "PET_RESPECT";
    public const string PetEat = "PET_EAT";
    public const string PetLevel = "PET_LEVEL";
    public const string PetOtherUsersRoom = "PET_OTHER_USERS_ROOM";
    public const string ChangeMotto = "CHANGE_MOTTO";
    public const string ChangeFigure = "CHANGE_FIGURE";
    public const string WearBadge = "WEAR_BADGE";
    public const string FriendFurniLocked = "FRIEND_FURNI_LOCKED";
    public const string Teleport = "TELEPORT";
    public const string FindHandItem = "FIND_HAND_ITEM";
    public const string SwitchItemState = "SWITCH_ITEM_STATE";
    public const string Swim = "SWIM";
    public const string CreateRoom = "CREATE_ROOM";
    public const string BuyFromCatalogue = "BUY_FROM_CATALOGUE";
    public const string PlaceItem = "PLACE_ITEM";
    public const string PlaceBuildersClubFurni = "PLACE_BUILDERS_CLUB_FURNI";
    public const string MoveItem = "MOVE_ITEM";
    public const string RotateItem = "ROTATE_ITEM";
    public const string SendMessengerMessage = "SEND_MESSENGER_MESSAGE";
    public const string SendMessengerInvite = "SEND_MESSENGER_INVITE";
    public const string FollowFriend = "FOLLOW_FRIEND";
    public const string SetRelationshipStatus = "SET_RELATIONSHIP_STATUS";
    public const string PublishPicture = "PUBLISH_PICTURE";
    public const string PlantMonsterPlant = "PLANT_MONSTER_PLANT";
    public const string TreatMonsterPlant = "TREAT_MONSTER_PLANT";
    public const string BreedMonsterPlant = "BREED_MONSTER_PLANT";

    // counted by daily tasks: a sign held up (its number the parameter), a posture taken, a jump, and what the
    // catalogue was paid in (the amount the count)
    public const string ShowSign = "SHOW_SIGN";
    public const string Sit = "SIT";
    public const string Lay = "LAY";
    public const string Jump = "JUMP";
    public const string SpendCredits = "SPEND_CREDITS";
    public const string SpendDiamonds = "SPEND_DIAMONDS";
    public const string SpendDuckets = "SPEND_DUCKETS";

    // counted by daily tasks: a furni used (its class the parameter), a pet looked at (its type), a furni walked onto
    // (its class), an effect put on (its id), and dancing with three others
    public const string FindStuff = "FIND_STUFF";
    public const string FindPet = "FIND_PET";
    public const string WalkOverStuff = "WALK_OVER_STUFF";
    public const string WearEffect = "WEAR_EFFECT";
    public const string DanceCrew = "DANCE_CREW";

    /// <summary>Counted only by wired's "progress reward track" effect, which names the task.</summary>
    public const string CustomWiredAction = "CUSTOM_WIRED_ACTION";

    /// <summary>Achievements that count the same thing as an action: their progress counts it too, so the places that
    /// already tell the achievement need no word of the track.</summary>
    public static readonly IReadOnlyDictionary<string, string> ByAchievement = new Dictionary<string, string>
    {
        [Achievements.AchievementNames.RespectGiven] = GiveRespect,
        [Achievements.AchievementNames.PetRespectGiver] = PetRespect,
        [Achievements.AchievementNames.PetFeeding] = PetEat,
        [Achievements.AchievementNames.PetLevelUp] = PetLevel,
        [Achievements.AchievementNames.MonsterPlantTreater] = TreatMonsterPlant,
        [Achievements.AchievementNames.MonsterPlantBreeder] = BreedMonsterPlant,
    };
}
