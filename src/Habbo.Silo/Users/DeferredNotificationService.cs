using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Notifications;
using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Abstractions.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans;

namespace Habbo.Silo.Users;

/// <summary>Sends a notification straight away to a player who is online; keeps it for one who is not, and shows
/// them the newest of what was kept when they log in.</summary>
public sealed class DeferredNotificationService(
    IDbContextFactory<AppDbContext> dbFactory, IGrainFactory grains, PlayerMessenger messenger, IOptions<DeferredNotificationOptions> options)
    : IDeferredNotificationService
{
    public async Task NotifyAsync(Guid playerId, string type, IReadOnlyDictionary<string, string> parameters)
    {
        var online = await grains.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).WhichAreOnlineAsync([playerId]);
        if (online.Count > 0)
        {
            await messenger.SendAsync(playerId, Dialog(type, parameters.Keys.ToList(), parameters.Values.ToList()));
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        db.DeferredNotifications.Add(new DeferredNotification
        {
            UserId = playerId, Type = type, ParameterKeys = parameters.Keys.ToList(), ParameterValues = parameters.Values.ToList(),
        });
        await db.SaveChangesAsync();
    }

    public async Task DeliverAsync(Guid playerId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var kept = await db.DeferredNotifications.Where(n => n.UserId == playerId).OrderByDescending(n => n.Id).ToListAsync();
        if (kept.Count == 0)
            return;

        foreach (var notification in kept.Take(options.Value.DeliveredOnLogin).Reverse())
            await messenger.SendAsync(playerId, Dialog(notification.Type, notification.ParameterKeys, notification.ParameterValues));

        db.DeferredNotifications.RemoveRange(kept);
        await db.SaveChangesAsync();
    }

    private static NotificationDialogMessageComposer Dialog(string type, IReadOnlyList<string> keys, IReadOnlyList<string> values) =>
        new(type, keys.Zip(values, (key, value) => new NotificationParameterData(key, value)).ToList());
}
