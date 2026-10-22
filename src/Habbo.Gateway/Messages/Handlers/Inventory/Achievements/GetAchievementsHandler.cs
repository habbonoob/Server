using Habbo.Protocol.Messages.Incoming.Inventory.Achievements;
using Habbo.Protocol.Messages.Outgoing.Inventory.Achievements;
using Habbo.Silo.Abstractions.Achievements;
using Microsoft.Extensions.Options;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Achievements;

/// <summary>2844 GetAchievements — the player opened the achievement dialog. The client builds
/// the whole window from this one answer and will not show it at all until the answer arrives,
/// so every achievement the hotel has goes back, started or not.</summary>
internal sealed class GetAchievementsHandler(
    IAchievementService achievements,
    IOptions<HotelOptions> hotel) : IMessageHandler<GetAchievementsEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, GetAchievementsEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        var all = await achievements.GetAllAsync(player.Id, ct);
        var categories = all.Select(achievement => achievement.Category).ToHashSet();

        // The tab to open on. A category the hotel names but has no achievements in would leave
        // the client with nothing selected, so fall back to one that is actually there.
        var configured = hotel.Value.AchievementsDefaultCategory;
        var defaultCategory = categories.Contains(configured)
            ? configured
            : categories.FirstOrDefault() ?? string.Empty;

        await context.SendAsync(
            new AchievementsComposer(all.Select(AchievementWire.From).ToList(), defaultCategory), ct);
        return true;
    }
}
