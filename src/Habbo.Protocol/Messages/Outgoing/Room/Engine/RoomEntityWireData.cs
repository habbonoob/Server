using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>One avatar in UsersMessageComposer. For a user the account id doubles as the client's webID,
/// which must match the login UserObject for the client to recognise its own avatar; for a bot it is
/// the bot's id. What follows the common fields depends on the kind of avatar.</summary>
[WireStruct]
public sealed partial record RoomEntityWireData(
    int AccountId,
    string Name,
    string Motto,
    string Figure,
    int RoomIndex,
    int X,
    int Y,
    string Z,
    int HeadDirection,
    int EntityType,
    RoomUserDetails? User,
    RoomBotDetails? Bot,
    RoomPetDetails? Pet)
{
    public static RoomEntityWireData From(RoomEntityData entity) => new(
        entity.AccountId,
        entity.Name,
        entity.Motto,
        entity.Figure,
        entity.RoomIndex,
        entity.X,
        entity.Y,
        entity.Z,
        entity.HeadDirection,
        (int)entity.EntityType,
        entity.EntityType == RoomEntityType.User
            ? new RoomUserDetails(
                entity.Sex,
                entity.GroupId,
                entity.GroupStatus,
                entity.GroupName,
                entity.SwimFigure,
                entity.AchievementScore,
                entity.IsModerator,
                entity.BadgesRank)
            : null,
        entity.EntityType == RoomEntityType.BotWithSkills
            ? new RoomBotDetails(entity.Sex, entity.OwnerAccountId, entity.OwnerName, entity.BotSkills ?? [])
            : null,
        entity.EntityType == RoomEntityType.Pet
            ? new RoomPetDetails(entity.PetType, entity.OwnerAccountId, entity.OwnerName, entity.PetRarity, entity.PetHasSaddle, entity.PetIsRidden,
                entity.PetCanBreed, entity.PetCanHarvest, entity.PetCanRevive, entity.PetBreedingAllowed, entity.PetLevel, entity.PetPosture)
            : null);
}
