using Habbo.Silo.Abstractions.Permissions;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.UserClassification;
using Habbo.Silo.Abstractions.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans;
using Habbo.Silo.Permissions;

namespace Habbo.Silo.Users;

/// <summary>Classifies users for staff and ambassadors: new accounts, and everyone else. Staff may list any
/// class, here or across the hotel; ambassadors the new users in their room.</summary>
public sealed class UserClassificationService(
    IDbContextFactory<AppDbContext> dbFactory, IGrainFactory grains, PlayerMessenger messenger, PermissionService permissions,
    IOptions<UserClassificationOptions> options)
    : IUserClassificationService
{
    public Task SendRoomAsync(PlayerData player, IReadOnlyList<Guid> roomUsers, string classType) =>
        MayList(player, classType, hotelWide: false) ? SendAsync(player, roomUsers, classType) : Task.CompletedTask;

    public async Task SendHotelAsync(PlayerData player, string classType)
    {
        if (!MayList(player, classType, hotelWide: true))
            return;

        var online = await grains.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).GetOnlineAsync();
        await SendAsync(player, online, classType);
    }

    private bool MayList(PlayerData player, string classType, bool hotelWide)
    {
        var rules = options.Value;
        return permissions.Has(player, PermissionNames.UsersClassify) || (!hotelWide && player.IsAmbassador && classType == rules.NewClass);
    }

    private async Task SendAsync(PlayerData player, IReadOnlyList<Guid> userIds, string classType)
    {
        var rules = options.Value;
        var newSince = DateTimeOffset.UtcNow.AddDays(-rules.NewAccountDays);
        await using var db = await dbFactory.CreateDbContextAsync();
        var users = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.AccountId, u.Username, u.CreatedAt }).ToListAsync();
        var classified = users
            .Select(u => new ClassifiedUserData(u.AccountId, u.Username, u.CreatedAt > newSince ? rules.NewClass : rules.RegularClass))
            .Where(u => classType.Length == 0 || u.UserType == classType)
            .OrderBy(u => u.UserName)
            .ToList();
        await messenger.SendAsync(player.Id, new UserClassificationMessageComposer(classified));
    }
}
