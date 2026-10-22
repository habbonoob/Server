using System.Net;
using Habbo.Gateway;
using Habbo.Host;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Achievements;
using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Abstractions.Chat;
using Habbo.Silo.Abstractions.Friends;
using Habbo.Silo.Abstractions.Games;
using Habbo.Silo.Abstractions.Groups;
using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Abstractions.Inventory;
using Habbo.Silo.Abstractions.Moderation;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Abstractions.Subscriptions;
using Habbo.Silo.Achievements;
using Habbo.Silo.Auth;
using Habbo.Silo.CallForHelp;
using Habbo.Silo.Catalog;
using Habbo.Silo.Friends;
using Habbo.Silo.Groups;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.LandingView;
using Habbo.Silo.Messenger;
using Habbo.Silo.Moderation;
using Habbo.Silo.Navigator;
using Habbo.Silo.Perks;
using Habbo.Silo.Rooms;
using Habbo.Silo.Subscriptions;
using Habbo.Silo.Users;
using Habbo.Silo.Wired;
using Habbo.Web;
using Microsoft.AspNetCore.Connections;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Habbo.Host;

/// <summary>The game's services, by the role a process plays in it:
///  - the silo (its grains, and on a core silo the background monitors: availability, payouts, catalog releases);
///  - the gateway (the game port's sessions, and the handlers that call the hotel's services);
///  - both, the hotel's services themselves, which the grains and the handlers alike call.
/// A silo without the gateway in its process reaches the players through the gateways that registered with it; a
/// gateway without the silo registers its sessions' way in with the silo.</summary>
public static class GameServices
{
    public static IServiceCollection AddSiloRole(
        this IServiceCollection services, IConfiguration configuration, bool withGateway, string? pool)
    {
        if (SiloPool.RunsHotelJobs(pool))
        {
            services.AddHostedService<Habbo.Silo.Hotel.HotelAvailabilityMonitor>();
            services.AddHostedService<Habbo.Silo.Users.CurrencyPayoutMonitor>();
            services.AddHostedService<Habbo.Silo.Catalog.CatalogReleaseMonitor>();
        }
        if (!withGateway)
        {
            // started with the silo (Program), so the gateways' list is kept from the start
            services.AddSingleton<Habbo.Silo.Hotel.GatewayPacketSink>();
            services.AddSingleton<IClientPacketSink>(sp => sp.GetRequiredService<Habbo.Silo.Hotel.GatewayPacketSink>());
            services.AddSingleton<Habbo.Silo.Commands.IReloadRelay>(sp => sp.GetRequiredService<Habbo.Silo.Hotel.GatewayPacketSink>());
        }
        return services;
    }

    public static IServiceCollection AddGatewayRole(this IServiceCollection services, IConfiguration configuration) =>
        services.AddGatewaySeam(configuration);

    /// <summary>The hotel's services, their settings and the photo lab: what grains and handlers call, added once for
    /// either role or both.</summary>
    public static IServiceCollection AddHotelServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SsoTicketOptions>(configuration.GetSection("Auth"));
        services.Configure<NavigatorOptions>(configuration.GetSection("Navigator"));
        services.AddSingleton<Habbo.Camera.ExternalImages>();
        services.AddSingleton<Habbo.Camera.PhotoRenderer>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<Habbo.Camera.RenderLimit>();
        services.AddSingleton<Habbo.Silo.Abstractions.Camera.ICameraService, Habbo.Silo.Camera.CameraService>();
        services.Configure<BundleDiscountOptions>(configuration.GetSection("Catalog:BundleDiscount"));
        services.Configure<GiftWrappingOptions>(configuration.GetSection("Catalog:GiftWrapping"));
        services.AddSingleton<IGiftWrappingService, Habbo.Silo.Catalog.GiftWrappingService>();
        services.AddSingleton<IClubService, Habbo.Silo.Subscriptions.ClubService>();
        services.AddSingleton<IRoomAdService, RoomAdService>();
        services.AddSingleton<IVoucherService, Habbo.Silo.Catalog.VoucherService>();
        services.Configure<CatalogExtrasOptions>(configuration.GetSection("Catalog"));
        services.Configure<Habbo.Silo.Games.SnowWar.SnowWarOptions>(configuration.GetSection("SnowWar"));
        services.Configure<Habbo.Silo.Achievements.AchievementResolutionOptions>(configuration.GetSection("AchievementResolutions"));
        services.AddSingleton<Habbo.Silo.Achievements.AchievementResolutions>();
        services.Configure<Habbo.Silo.Competitions.RoomCompetitionOptions>(configuration.GetSection("RoomCompetitions"));
        services.AddSingleton<IRoomCompetitionService, Habbo.Silo.Competitions.RoomCompetitionService>();
        services.AddSingleton<Habbo.Infrastructure.SeedFiles>();
        services.AddSingleton<Habbo.Silo.Games.SnowWar.Arenas.SnowWarArenaCatalog>();
        services.AddSingleton<Habbo.Silo.Games.SnowWar.SnowWarAccounts>();
        services.AddSingleton<ISnowWarLeaderboards, Habbo.Silo.Games.SnowWar.SnowWarLeaderboards>();
        services.AddSingleton<ICatalogExtrasService, Habbo.Silo.Catalog.CatalogExtrasService>();
        services.AddSingleton<ITargetedOfferService, Habbo.Silo.Catalog.TargetedOfferService>();
        services.Configure<Habbo.Silo.Users.BadgeRarityOptions>(configuration.GetSection("BadgeRarity"));
        services.Configure<Habbo.Silo.Users.BadgeLeaderboardOptions>(configuration.GetSection("BadgeLeaderboards"));
        services.AddSingleton<Habbo.Silo.Users.BadgeLeaderboards>();
        services.Configure<ClubOptions>(configuration.GetSection("Club"));
        services.AddSingleton<FigureValidator>();
        services.Configure<GroupOptions>(configuration.GetSection("Groups"));
        services.Configure<MessengerOptions>(configuration.GetSection("Messenger"));
        services.Configure<GroupForumOptions>(configuration.GetSection("GroupForums"));
        services.AddSingleton<PlayerMessenger>();
        services.AddSingleton<Habbo.Silo.Friends.FriendActivityNotifier>();
        services.Configure<Habbo.Silo.Hotel.HotelAvailabilityOptions>(configuration.GetSection("Availability"));
        services.AddSingleton<IHotelAvailabilityService, Habbo.Silo.Hotel.HotelAvailabilityService>();
        services.AddSingleton<Habbo.Silo.Permissions.PermissionService>();
        services.AddSingleton<Habbo.Silo.Rooms.InvisiblePlayers>();
        services.AddSingleton<Habbo.Silo.Commands.HotelCommandService>();
        services.AddSingleton<Habbo.Silo.Commands.InventoryCommandService>();
        // what :update reads again
        services.AddSingleton<Habbo.Silo.Abstractions.Hotel.IReloadable>(sp => (Habbo.Silo.Abstractions.Hotel.IReloadable)sp.GetRequiredService<IGameTexts>());
        services.AddSingleton<Habbo.Silo.Abstractions.Hotel.IReloadable>(sp => (Habbo.Silo.Abstractions.Hotel.IReloadable)sp.GetRequiredService<IClientVariables>());
        services.AddSingleton<Habbo.Silo.Abstractions.Hotel.IReloadable>(sp => (Habbo.Silo.Abstractions.Hotel.IReloadable)sp.GetRequiredService<IGestureCatalog>());
        services.AddSingleton<Habbo.Silo.Abstractions.Hotel.IReloadable>(sp => (Habbo.Silo.Abstractions.Hotel.IReloadable)sp.GetRequiredService<IAvatarEffectCatalog>());
        services.AddSingleton<Habbo.Silo.Abstractions.Hotel.IReloadable>(sp => (Habbo.Silo.Abstractions.Hotel.IReloadable)sp.GetRequiredService<IGiftWrappingService>());
        services.AddSingleton<Habbo.Silo.Abstractions.Hotel.IReloadable>(sp => sp.GetRequiredService<Habbo.Silo.Chat.WordFilter>());
        services.AddSingleton<Habbo.Silo.Abstractions.Hotel.IReloadable>(sp => sp.GetRequiredService<Habbo.Silo.Permissions.PermissionService>());
        services.AddHostedService(services => services.GetRequiredService<Habbo.Silo.Permissions.PermissionService>());
        services.Configure<Habbo.Silo.Users.CurrencyOptions>(configuration.GetSection("Currencies"));
        services.Configure<Habbo.Silo.Users.EarningsOptions>(configuration.GetSection("Earnings"));
        services.AddSingleton<Habbo.Silo.Users.CurrencyService>();
        services.Configure<Habbo.Silo.Campaigns.CampaignCalendarOptions>(configuration.GetSection("CampaignCalendar"));
        services.AddSingleton<ICampaignCalendarService, Habbo.Silo.Campaigns.CampaignCalendarService>();
        services.AddSingleton<IPromoArticleService, Habbo.Silo.LandingView.PromoArticleService>();
        services.AddSingleton<IProductClaimService, Habbo.Silo.Catalog.ProductClaimService>();
        services.Configure<Habbo.Silo.Chests.ChestOptions>(configuration.GetSection("Chests"));
        services.AddSingleton<Habbo.Silo.Chests.ChestRules>();
        services.Configure<Habbo.Silo.Users.DeferredNotificationOptions>(configuration.GetSection("DeferredNotifications"));
        services.AddSingleton<IDeferredNotificationService, Habbo.Silo.Users.DeferredNotificationService>();
        services.AddSingleton<IEarningsService, Habbo.Silo.Users.EarningsService>();
        services.AddSingleton<Habbo.Silo.Wired.Rewards.IWiredRewards, Habbo.Silo.Wired.Rewards.WiredRewardService>();
        services.Configure<Habbo.Silo.Users.SelfDonationOptions>(configuration.GetSection("SelfDonation"));
        services.AddSingleton<ISelfDonationService, Habbo.Silo.Users.SelfDonationService>();
        services.AddSingleton<IGroupService, GroupService>();
        services.AddSingleton<IGroupForumService, GroupForumService>();
        services.Configure<ModerationOptions>(configuration.GetSection("Moderation"));
        services.Configure<RoomOptions>(configuration.GetSection("Rooms"));
        services.Configure<TradingOptions>(configuration.GetSection("Trading"));
        services.Configure<Habbo.Silo.Marketplace.MarketplaceOptions>(configuration.GetSection("Marketplace"));
        services.AddSingleton<IMarketplaceService, Habbo.Silo.Marketplace.MarketplaceService>();
        services.Configure<Habbo.Silo.Recycler.RecyclerOptions>(configuration.GetSection("Recycler"));
        services.AddSingleton<IRecyclerService, Habbo.Silo.Recycler.RecyclerService>();
        services.AddSingleton<ICraftingService, Habbo.Silo.Crafting.CraftingService>();
        services.Configure<Habbo.Silo.Items.FurnitureOptions>(configuration.GetSection("Furniture"));
        services.Configure<Habbo.Silo.Bots.BotOptions>(configuration.GetSection("Bots"));
        services.AddSingleton<IBotService, Habbo.Silo.Bots.BotService>();
        services.Configure<Habbo.Silo.Pets.PetOptions>(configuration.GetSection("Pets"));
        services.AddSingleton<Habbo.Silo.Pets.PetRules>();
        services.AddSingleton<Habbo.Silo.Pets.PetNames>();
        services.AddSingleton<IPetService, Habbo.Silo.Pets.PetService>();
        services.Configure<Habbo.Silo.Pets.PlantOptions>(configuration.GetSection("Plants"));
        services.AddSingleton<Habbo.Silo.Pets.PlantLooks>();
        services.AddSingleton<IQuizService, Habbo.Silo.Help.QuizService>();
        services.AddSingleton<IPollService, Habbo.Silo.Polls.PollService>();
        services.Configure<Habbo.Silo.Users.PlayerSettingsOptions>(configuration.GetSection("PlayerSettings"));
        services.AddSingleton<IPlayerSettingsService, Habbo.Silo.Users.PlayerSettingsService>();
        services.Configure<Habbo.Silo.Users.UserClassificationOptions>(configuration.GetSection("UserClassification"));
        services.AddSingleton<IUserClassificationService, Habbo.Silo.Users.UserClassificationService>();
        services.AddSingleton<IClientTrackingService, Habbo.Silo.Users.ClientTrackingService>();
        services.Configure<Habbo.Silo.Help.GuideOptions>(configuration.GetSection("Guides"));
        services.Configure<Habbo.Silo.Items.RentableSpaceOptions>(configuration.GetSection("RentableSpaces"));
        services.Configure<Habbo.Silo.Items.MysteryTrophyOptions>(configuration.GetSection("MysteryTrophy"));
        services.AddSingleton<Habbo.Silo.Rooms.RoomSettingsRules>();
        services.AddSingleton<IModerationService, ModerationService>();
        services.AddSingleton<IPerkService, PerkService>();
        services.AddSingleton<ICallForHelpService, CallForHelpService>();
        services.AddSingleton<IAchievementService, AchievementService>();
        services.Configure<FriendListOptions>(configuration.GetSection("FriendList"));
        services.AddSingleton<IFriendListService, FriendListService>();
        services.AddSingleton<IMessengerService, MessengerService>();
        services.AddSingleton<IUserRelationsService, UserRelationsService>();
        services.AddSingleton<ISubscriptionService, SubscriptionService>();
        services.AddSingleton<ITimingCodeService, TimingCodeService>();
        services.AddSingleton<IGestureCatalog, GestureCatalog>();
        services.AddSingleton<IAvatarEffectCatalog, AvatarEffectCatalog>();
        services.AddSingleton<IGameTexts, GameTexts>();
        services.AddSingleton<IClientVariables, ClientVariables>();
        services.AddSingleton<INavigatorCatalog, NavigatorCatalog>();
        services.AddSingleton<RoomLister>();
        services.AddSingleton<IRoomDirectoryService, RoomDirectoryService>();
        services.AddSingleton<INewNavigatorService, NewNavigatorService>();
        services.AddSingleton<IShopCatalog, ShopCatalog>();
        services.AddSingleton<FurniInteractionRegistry>();
        services.AddSingleton<WiredBoxRegistry>();
        services.AddSingleton<FurniDataLoader>();
        return services;
    }
}
