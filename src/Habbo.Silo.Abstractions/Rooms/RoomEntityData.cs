using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Rooms;

/// <summary>An avatar currently in a room — a user or a bot — as the Users packet needs it.</summary>
[GenerateSerializer]
public partial record RoomEntityData(
    [property: Id(0)] int RoomIndex,
    [property: Id(1)] int AccountId,
    [property: Id(2)] string Name,
    [property: Id(3)] string Motto,
    [property: Id(4)] string Figure,
    [property: Id(5)] string Sex,
    [property: Id(6)] int X,
    [property: Id(7)] int Y,
    [property: Id(8)] string Z,
    [property: Id(9)] int HeadDirection,
    [property: Id(10)] RoomEntityType EntityType,
    [property: Id(11)] int GroupId,
    [property: Id(12)] int GroupStatus,
    [property: Id(13)] string GroupName,
    [property: Id(14)] string SwimFigure,
    [property: Id(15)] int AchievementScore,
    [property: Id(16)] bool IsModerator,
    /// <summary>For a bot: who owns it.</summary>
    [property: Id(17)] int OwnerAccountId = 0,
    [property: Id(18)] string OwnerName = "",
    /// <summary>For a bot: the skills its menu offers.</summary>
    [property: Id(19)] IReadOnlyList<short>? BotSkills = null,
    /// <summary>For a pet: its kind, level and posture.</summary>
    [property: Id(20)] int PetType = 0,
    [property: Id(21)] int PetLevel = 0,
    [property: Id(22)] string PetPosture = "",
    [property: Id(23)] bool PetHasSaddle = false,
    [property: Id(24)] bool PetIsRidden = false,
    [property: Id(25)] bool PetBreedingAllowed = false,
    [property: Id(26)] int PetRarity = 0,
    [property: Id(27)] bool PetCanBreed = false,
    [property: Id(28)] bool PetCanHarvest = false,
    [property: Id(29)] bool PetCanRevive = false,
    [property: Id(30)] int BadgesRank = -1);
