using Habbo.Domain.Subscriptions;
using Habbo.Protocol.Messages.Outgoing.Availability;
using Habbo.Protocol.Messages.Outgoing.CallForHelp;
using Habbo.Protocol.Messages.Outgoing.Handshake;
using Habbo.Protocol.Messages.Outgoing.Inventory.Achievements;
using Habbo.Protocol.Messages.Outgoing.MysteryBox;
using Habbo.Protocol.Messages.Outgoing.Navigator;
using Habbo.Protocol.Messages.Outgoing.Notifications;
using Habbo.Protocol.Messages.Outgoing.Perk;
using Habbo.Silo.Abstractions.Achievements;
using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Abstractions.Moderation;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Subscriptions;
using Microsoft.Extensions.Options;
using Orleans;

namespace Habbo.Gateway.Messages;

/// <summary>Everything the server pushes on its own once a login is accepted, in the order a
/// capture of Habbo sends it. The client asks for none of this: it waits for it, so the order
/// is part of the contract rather than a detail. Each message is built from the player or from
/// hotel configuration, so what goes out is data and only the order lives here.</summary>
internal sealed class PostAuthenticationSequence(
    IGrainFactory grains,
    IPerkService perks,
    ICallForHelpService callForHelp,
    IAchievementService achievements,
    ISubscriptionService subscriptions,
    IModerationService moderation,
    IHotelAvailabilityService availability,
    ICampaignCalendarService campaignCalendar,
    IClubService club,
    IDeferredNotificationService deferredNotifications,
    IOptions<HotelOptions> hotel)
{
    /// <param name="previousLoginAt">When the player last logged in before now, or null when
    /// this is their first ever login. Both how new the client treats them and whether it plays
    /// the daily welcome come from it.</param>
    public async Task SendAsync(
        GatewaySessionContext context,
        PlayerData player,
        DateTimeOffset? previousLoginAt,
        CancellationToken ct)
    {
        var options = hotel.Value;
        var navigator = grains.GetGrain<INavigatorGrain>(0);
        var playerGrain = grains.GetGrain<IPlayerGrain>(player.Id);

        await playerGrain.SendEffectsAsync();
        await context.SendAsync(NavigatorSettingsFor(player, options), ct);
        await context.SendAsync(
            new FavouritesComposer(
                await navigator.GetFavouriteLimitAsync(),
                await navigator.GetFavouritesAsync(player.Id)),
            ct);
        await context.SendAsync(
            SubscriptionEvents.From(
                await subscriptions.GetAsync(player.Id, SubscriptionProducts.HabboClub, ct),
                SubscriptionResponseType.Info),
            ct);
        await context.SendAsync(
            SubscriptionEvents.From(await subscriptions.GetBuildersClubAsync(player.Id, ct)), ct);
        await playerGrain.SendFigureSetsAsync();
        await context.SendAsync(new NoobnessLevelMessageComposer(NoobnessOf(previousLoginAt)), ct);
        await context.SendAsync(UserRightsMessageComposer.From(player), ct);
        var hours = availability.GetAvailability();
        await context.SendAsync(new AvailabilityStatusMessageComposer(
            hours.IsOpen, hours.IsShuttingDown(availability.LongestWarningMinutes), options.IsAuthenticHabbo), ct);
        await context.SendAsync(new InfoFeedEnableMessageComposer(await InfoFeedEnabledAsync(context)), ct);
        await playerGrain.SendActivityPointsAsync();
        await context.SendAsync(
            new AchievementsScoreComposer(await achievements.GetScoreAsync(player.Id, ct)), ct);
        await context.SendAsync(new IsFirstLoginOfDayComposer(IsFirstLoginOfDay(previousLoginAt)), ct);
        await context.SendAsync(
            new MysteryBoxKeysMessageComposer(options.MysteryBoxColor, options.MysteryBoxKeyColor), ct);
        foreach (var achievement in await achievements.GetProgressAsync(player.Id, ct))
            await context.SendAsync(new AchievementComposer(AchievementWire.From(achievement)), ct);

        var buildersClub = await subscriptions.GetBuildersClubAsync(player.Id, ct);
        await context.SendAsync(SubscriptionEvents.From(buildersClub), ct);
        await UnlockBuildersClubRoomsAsync(context, player.Id, buildersClub, ct);
        await context.SendAsync(await CallForHelpTopicsAsync(ct), ct);
        await context.SendAsync(PerkAllowancesMessageComposer.From(await perks.GetAllowancesAsync(player, ct)), ct);
        await playerGrain.SendChatStylesAsync();
        await campaignCalendar.SendAsync(player);
        await club.NotifyGiftsAsync(player);
        await deferredNotifications.DeliverAsync(player.Id);

        // staff get the mod tool, with the issues waiting in it
        await moderation.SendInitAsync(player.Id);
    }

    /// <summary>Entering the home room needs one; otherwise the landing view initializes.</summary>
    private static NavigatorSettingsComposer NavigatorSettingsFor(PlayerData player, HotelOptions options)
    {
        var enterHome = options.EnterHomeRoomOnLogin && player.HomeRoomId != PlayerData.NoHomeRoom;
        return new NavigatorSettingsComposer(
            player.HomeRoomId,
            enterHome ? player.HomeRoomId : NavigatorSettingsComposer.StayOnHotelView);
    }

    /// <summary>How new the client should treat the player as. It only ever checks the value
    /// against zero, and switches its new-identity flow on for anything else. An account holds
    /// one avatar, so there is no separate identity to be new: a player who has never logged in
    /// is new, and everyone else is not.</summary>
    /// <summary>Rooms that building on the Builders Club trial hid are shown again once the player has membership
    /// time: "Your rooms that contain Builders Club items are now unlocked, and your club limit has been raised to
    /// %FURNI_LIMIT% items!" Habbo sells the membership on its website, so the hotel finds out when they come back.</summary>
    private async Task UnlockBuildersClubRoomsAsync(
        GatewaySessionContext context, Guid playerId, BuildersClubView buildersClub, CancellationToken ct)
    {
        if (buildersClub.SecondsLeft <= 0 || await subscriptions.UnhideBuildersClubRoomsAsync(playerId, ct) == 0)
            return;

        await context.SendAsync(new NotificationDialogMessageComposer(BuildersClubRenewed,
            [new NotificationParameterData(FurniLimitParameter, buildersClub.FurniLimit.ToString(System.Globalization.CultureInfo.InvariantCulture))]), ct);
    }

    private const string BuildersClubRenewed = "builders_club.membership_renewed";
    private const string FurniLimitParameter = "FURNI_LIMIT";

    private static int NoobnessOf(DateTimeOffset? previousLoginAt) =>
        previousLoginAt is null ? NoobnessLevelMessageComposer.RealNoob : NoobnessLevelMessageComposer.Established;

    /// <summary>True on the first login of the calendar day, which is when the client plays the
    /// daily welcome. A player who has never logged in before counts as first.</summary>
    private static bool IsFirstLoginOfDay(DateTimeOffset? previousLoginAt) =>
        previousLoginAt is null || previousLoginAt.Value.UtcDateTime.Date < DateTime.UtcNow.Date;

    private static async Task<bool> InfoFeedEnabledAsync(GatewaySessionContext context) =>
        context.Session.PlayerGrain is not { } playerGrain
        || (await playerGrain.GetPreferencesAsync()).InfoFeedEnabled;

    private async Task<CfhTopicsInitMessageComposer> CallForHelpTopicsAsync(CancellationToken ct) =>
        new((await callForHelp.GetCategoriesAsync(ct))
            .Select(category => new CallForHelpCategoryData(
                category.Name,
                category.Topics
                    .Select(topic => new CallForHelpTopicData(topic.Name, topic.Id, topic.Consequence))
                    .ToList()))
            .ToList());
}
