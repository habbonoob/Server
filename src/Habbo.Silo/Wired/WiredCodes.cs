namespace Habbo.Silo.Wired;

// The client's numbers for the wired dialog layouts, named after its own constants
// (com.sulake.habbo.roomevents.wired_setup: TriggerConfCodes, ActionTypeCodes, ConditionCodes,
// AddonCodes) so every value cross-references the decompiled client one-to-one. Only the codes
// of boxes the server implements are listed.

public static class TriggerCodes
{
    public const int AvatarSaysSomething = 0;
    public const int WalksOnFurni = 1;
    public const int WalksOffFurni = 2;
    public const int TriggerOnce = 3;
    public const int UseStuff = 4;
    public const int TriggerPeriodically = 6;
    public const int AvatarEntersRoom = 7;
    public const int GameStarts = 8;
    public const int GameEnds = 9;
    public const int ScoreAchieved = 10;
    public const int AvatarCaught = 11;
    public const int PeriodicLong = 12;
    public const int BotDestinationReached = 13;
    public const int BotAvatarReached = 14;
    public const int ClockReachTime = 15;
    public const int PerformAction = 16;
    public const int ReceiveSignal = 17;
    public const int AvatarClicksFurni = 18;
    public const int PeriodicShort = 19;
    public const int StateChange = 20;
    public const int AvatarClicksTile = 21;
    public const int AvatarLeavesRoom = 23;
    public const int VariableUpdate = 22;
    public const int UserClicksUser = 24;
    public const int TransactionCompleted = 25;
    public const int TransactionFailed = 26;
}

public static class EffectCodes
{
    public const int ToggleFurniState = 0;
    public const int Reset = 1;
    public const int SetFurniStateTo = 3;
    public const int MoveFurni = 4;
    public const int GiveScore = 6;
    public const int Chat = 7;
    public const int Teleport = 8;
    public const int JoinTeam = 9;
    public const int LeaveTeam = 10;
    public const int Chase = 11;
    public const int Flee = 12;
    public const int MoveToDirection = 13;
    public const int GiveScoreToPredefinedTeam = 14;
    public const int ToggleToRandomState = 15;
    public const int MoveFurniTo = 16;
    public const int GiveReward = 17;
    public const int CallAnotherStack = 18;
    public const int KickFromRoom = 19;
    public const int MuteUser = 20;
    public const int BotTeleport = 21;
    public const int BotMove = 22;
    public const int BotTalk = 23;
    public const int BotGiveHandItem = 24;
    public const int BotFollowAvatar = 25;
    public const int BotChangeFigure = 26;
    public const int BotTalkDirectToAvatar = 27;
    public const int ControlClock = 28;
    public const int SetFurniAltitude = 29;
    public const int TeleportToRoom = 44;
    public const int ClickConfiguration = 54;
    public const int PlaceFurni = 55;
    public const int RemoveFurni = 56;
    public const int MoveAsGroup = 57;
    public const int SendSignal = 30;
    public const int FreezeUser = 31;
    public const int UnfreezeUser = 32;
    public const int RelativeFurniMove = 33;
    public const int MoveFurniToFurni = 34;
    public const int MoveFurniToUser = 35;
    public const int NegativeCallAnotherStack = 36;
    public const int NegativeSendSignal = 37;
    public const int AdjustClock = 38;
    public const int GiveVariable = 39;
    public const int RemoveVariable = 40;
    public const int ChangeVariable = 41;
    public const int MoveUser = 42;
    public const int MoveUserToFurni = 43;
    public const int GiveCurrencyFromChest = 45;
    public const int GiveFurniFromChest = 46;
    public const int InitiateTransaction = 47;
    public const int CancelTransaction = 48;
    public const int WriteToLogs = 49;
    public const int NegativeWriteToLogs = 50;
}

public static class ConditionCodes
{
    public const int StatesMatch = 0;
    public const int FurnisHaveAvatars = 1;
    public const int TriggererIsOnFurni = 2;
    public const int TimeElapsedMore = 3;
    public const int TimeElapsedLess = 4;
    public const int UserCountIn = 5;
    public const int ActorIsInTeam = 6;
    public const int HasStackedFurnis = 7;
    public const int StuffTypeMatches = 8;
    public const int ActorIsGroupMember = 10;
    public const int ActorIsWearingBadge = 11;
    public const int ActorIsWearingEffect = 12;

    // the negatives with dialogs of their own
    public const int NotFurnisHaveAvatars = 14;
    public const int NotHasStackedFurnis = 18;

    // the negatives Habbo sends under codes of their own that open their positive's dialog, inverted
    public const int NotStatesMatch = 13;
    public const int NotTriggererIsOnFurni = 15;
    public const int NotUserCountIn = 16;
    public const int NotActorIsInTeam = 17;
    public const int NotStuffTypeMatches = 19;
    public const int NotActorIsWearingEffect = 23;
    public const int NotTriggererMatches = 27;
    public const int NotPerformingAction = 33;
    public const int NotHasVariable = 41;
    public const int NotActorIsGroupMember = 21;
    public const int NotActorIsWearingBadge = 22;
    public const int NotHasHandItem = 30;

    public const int DateRangeActive = 24;
    public const int ActorHasHandItem = 25;
    public const int TriggererMatches = 26;
    public const int TimeMatches = 28;
    public const int DateMatches = 29;
    public const int TeamIsWinning = 31;
    public const int PerformingAction = 32;
    public const int TeamHasScore = 34;
    public const int ClockTimeMatches = 35;
    public const int FurniHasAltitude = 36;
    public const int UserDirection = 37;
    public const int InputSourceQuantity = 38;
    public const int CanPerformMove = 39;
    public const int HasVariable = 40;
    public const int VariableValue = 42;
    public const int VariableAge = 43;
    public const int ChestHasItems = 45;
    public const int ChestHasItemTypes = 46;
}

public static class AddonCodes
{
    public const int ConditionEvaluation = 0;
    public const int PickOneRandomly = 1;
    public const int Unseen = 2;
    public const int ExceedExecutions = 5;
    public const int NoMoveAnimation = 6;
    public const int MovePhysics = 7;
    public const int CarryUsers = 8;
    public const int AnimationTime = 9;
    public const int FurniSelectorFilter = 10;
    public const int UserSelectorFilter = 11;
    public const int FurniVariableFilter = 12;
    public const int UserVariableFilter = 13;
    public const int UsernamePlaceholder = 14;
    public const int VariablePlaceholder = 15;
    public const int ExecuteInOrder = 17;
    public const int ChestItemTypeScanner = 18;
    public const int CustomContract = 20;
    public const int Projectile = 21;
    public const int JumpStrength = 22;
    public const int VariableCapturer = 16;
    public const int FurniNamePlaceholder = 19;
    public const int VariableTextConnector = 1000;
    public const int VariableLevelUp = 1001;
    public const int VariableTimeUtil = 1002;
    public const int VariableFxHealth = 1200;
    public const int VariableFxProgress = 1201;
    public const int VariableFxLevelling = 1202;
    public const int VariableFxStatus = 1203;
    public const int VariableFxBoss = 1204;
    public const int VariableFxNumber = 1205;
}

/// <summary>Where a box's targets come from, one source per target slot of its dialog. The numbers
/// are Habbo's (wiredfurni.params.sources.furni.N and .users.N in the game texts); the client
/// itself only knows 100 and 101, which switch on its furni picking.</summary>
public static class WiredSources
{
    /// <summary>"Use the triggering item".</summary>
    public const int TriggerFurni = 0;

    /// <summary>"Use picked furnis".</summary>
    public const int PickedFurni = 100;

    /// <summary>"Secondary furni picks".</summary>
    public const int PickedFurni2 = 101;

    /// <summary>"Use the snapshot": the furni the box's snapshot recorded.</summary>
    public const int SnapshotFurni = 110;

    /// <summary>"Use furni from Selector".</summary>
    public const int SelectedFurni = 200;

    /// <summary>"Use furni from Signal".</summary>
    public const int SignalFurni = 201;

    /// <summary>"All furni in the room".</summary>
    public const int AllFurni = 900;

    /// <summary>"All furni moved by the Wired stack".</summary>
    public const int MovedFurni = 901;

    /// <summary>"Use the triggering user".</summary>
    public const int Triggerer = 0;

    /// <summary>"The reached user": the user a bot reached.</summary>
    public const int ReachedUser = 10;

    /// <summary>"The clicked user".</summary>
    public const int ClickedUser = 11;

    /// <summary>"Use the bot specified by name".</summary>
    public const int BotByName = 100;

    /// <summary>"Use the user specified by name".</summary>
    public const int UserByName = 101;

    /// <summary>"Use users from Selector".</summary>
    public const int SelectedUsers = 200;

    /// <summary>"Use users from Signal".</summary>
    public const int SignalUsers = 201;

    /// <summary>"All users in the room".</summary>
    public const int AllUsers = 900;
}

/// <summary>A target slot of a box's dialog: the sources the player may choose between, and the
/// one a new box starts with. The sets are the ones Habbo's dialogs offer (bc_run4: every box placed new).</summary>
public sealed record WiredSlot(IReadOnlyList<int> Sources, int Default)
{
    /// <summary>The furni an effect or condition works on.</summary>
    public static readonly WiredSlot Furni = new(
        [WiredSources.TriggerFurni, WiredSources.PickedFurni, WiredSources.SelectedFurni, WiredSources.SignalFurni],
        WiredSources.PickedFurni);

    /// <summary>A slot of a box that picks two sets of furni: either set of picks, or anywhere else.</summary>
    public static readonly WiredSlot EitherPicks = new(
        [WiredSources.TriggerFurni, WiredSources.PickedFurni, WiredSources.PickedFurni2, WiredSources.SelectedFurni,
         WiredSources.SignalFurni],
        WiredSources.PickedFurni);

    /// <summary>The furni a selector or filter starts from: never another selector's.</summary>
    public static readonly WiredSlot SelectorFurni = new(
        [WiredSources.TriggerFurni, WiredSources.PickedFurni, WiredSources.SignalFurni], WiredSources.PickedFurni);

    /// <summary>The users a selector or filter starts from.</summary>
    public static readonly WiredSlot SelectorUsers = new([WiredSources.Triggerer, WiredSources.SignalUsers], WiredSources.Triggerer);

    /// <summary>What a stack's selectors chose, or the trigger's, or the signal's: no picks.</summary>
    public static readonly WiredSlot Unpicked = new(
        [WiredSources.TriggerFurni, WiredSources.SelectedFurni, WiredSources.SignalFurni], WiredSources.SelectedFurni);

    /// <summary>Furni the box's own picks name, and nothing else.</summary>
    public static readonly WiredSlot PickedOnly = new([WiredSources.PickedFurni], WiredSources.PickedFurni);

    /// <summary>The furni a trigger listens to: its picks, or what its stack's selectors choose.</summary>
    public static readonly WiredSlot TriggerFurni = new([WiredSources.PickedFurni, WiredSources.SelectedFurni], WiredSources.PickedFurni);

    /// <summary>The users an effect or condition works on.</summary>
    public static readonly WiredSlot Users = new(
        [WiredSources.Triggerer, WiredSources.SelectedUsers, WiredSources.SignalUsers], WiredSources.Triggerer);

    /// <summary>The furni Place Furni copies: those its snapshot saw, by default, where they stood when it saw them.</summary>
    public static readonly WiredSlot Placed = new(
        [WiredSources.TriggerFurni, WiredSources.PickedFurni, WiredSources.PickedFurni2, WiredSources.SnapshotFurni,
         WiredSources.SelectedFurni, WiredSources.SignalFurni],
        WiredSources.SnapshotFurni);

    /// <summary>The bots a bot box works on: the one it names, by default.</summary>
    public static readonly WiredSlot Bots = new(
        [WiredSources.Triggerer, WiredSources.BotByName, WiredSources.SelectedUsers, WiredSources.SignalUsers],
        WiredSources.BotByName);

    /// <summary>The bot a bot trigger listens for: the one it names, or one its stack's selectors choose.</summary>
    public static readonly WiredSlot NamedBot = new([WiredSources.BotByName, WiredSources.SelectedUsers], WiredSources.BotByName);

    /// <summary>Users to compare with: the one the box names, by default.</summary>
    public static readonly WiredSlot NamedUsers = new(
        [WiredSources.Triggerer, WiredSources.UserByName, WiredSources.SelectedUsers, WiredSources.SignalUsers],
        WiredSources.UserByName);

    /// <summary>The slot with another source to start from.</summary>
    public WiredSlot DefaultingTo(int source) => this with { Default = source };
}

public static class SelectorCodes
{
    public const int FurniByType = 0;
    public const int FurniPicks = 1;
    public const int UsersByType = 2;
    public const int UsersInTeam = 3;
    public const int FurniOnFurni = 4;
    public const int FurniFromSignal = 5;
    public const int FurniInNeighborhood = 6;
    public const int FurniInArea = 7;
    public const int UsersOnFurni = 8;
    public const int UsersPerformingAction = 9;
    public const int UsersFromSignal = 10;
    public const int UsersByName = 11;
    public const int UsersInNeighborhood = 12;
    public const int UsersInArea = 13;
    public const int FurniWithAltitude = 16;
    public const int UsersWithHandItem = 14;
    public const int UsersInGroup = 15;
    public const int FurniWithVariable = 17;
    public const int UsersWithVariable = 18;
    public const int RemoteSelector = 19;
}
