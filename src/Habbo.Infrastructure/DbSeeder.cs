using System.Globalization;
using Habbo.Domain.Achievements;
using Habbo.Domain.Subscriptions;
using Habbo.Domain.Catalog;
using Habbo.Domain.Chat;
using Habbo.Domain.Furniture;
using Habbo.Domain.Navigator;
using Habbo.Domain.Rooms;
using Habbo.Domain.Support;
using Habbo.Domain.Users;
using Habbo.Domain.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

public static partial class DbSeeder
{
    /// <summary>Applies pending migrations and, when <c>Seed:Enabled</c>, fills in anything missing:
    /// the dev account, gamedata stubs, and the default room layout + welcome room. Each piece
    /// skips itself when data already exists, so this is safe to run on every startup.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        await db.Database.MigrateAsync(ct);
        logger.LogInformation("Database is up to date.");

        if (!configuration.GetValue("Seed:Enabled", false))
            return;

        var seedFiles = new SeedFiles(configuration);
        if (!seedFiles.Exists)
        {
            logger.LogWarning("Seed directory '{Root}' not found; skipping content seeding.", seedFiles.Root);
            return;
        }

        var user = await SeedUserAsync(scope.ServiceProvider, db, configuration, logger, ct);
        await SeedPerksAsync(db, seedFiles, logger, ct);
        await SeedPermissionsAsync(db, seedFiles, logger, ct);
        await SeedCallForHelpTopicsAsync(db, seedFiles, logger, ct);
        await SeedAchievementsAsync(db, seedFiles, logger, ct);
        await SeedBuildersClubLimitsAsync(db, seedFiles, logger, ct);
        await SeedNavigatorAsync(db, seedFiles, logger, ct);
        await SeedGesturesAsync(db, seedFiles, logger, ct);
        await SeedAvatarEffectsAsync(db, seedFiles, logger, ct);
        await SeedCurrenciesAsync(db, seedFiles, logger, ct);
        await SeedChatStylesAsync(db, seedFiles, logger, ct);
        await SeedGroupBadgePartsAsync(db, seedFiles, logger, ct);
        await SeedModerationAsync(db, seedFiles, logger, ct);
        await SeedWordFilterAsync(db, seedFiles, logger, ct);
        await SeedGamedataAsync(db, seedFiles, configuration, logger, ct);
        // what every hotel needs comes first, account or not: a room cannot be made without models, nor the
        // catalog filled without furniture definitions, and both mark themselves done once seeded
        var content = await seedFiles.ReadJsonAsync<SeedRoomFile>("rooms.json", ct);
        await SeedRoomModelsAsync(db, content, logger, ct);
        await SeedFurnitureDefinitionsAsync(db, seedFiles, logger, ct);
        // the starter rooms and furniture belong to the first account, and the official rooms name starter
        // rooms; a hotel with none yet gets them once it has one
        if (user is not null)
        {
            var firstSeed = await SeedStarterRoomsAsync(db, content, user, logger, ct);
            await SeedOfficialRoomsAsync(db, seedFiles, logger, ct);
            // the starter set comes with the starter rooms, once: an account that later gave everything away does not get
            // it again on the next restart
            if (firstSeed)
                await SeedStarterItemsAsync(db, seedFiles, user, logger, ct);
        }
        else
            logger.LogWarning("No account yet: the starter rooms, official rooms and starter furniture wait for the first one.");
        await SeedCatalogAsync(db, seedFiles, logger, CatalogFile, buildersClub: false, ct);
        await SeedCatalogAsync(db, seedFiles, logger, BuildersClubCatalogFile, buildersClub: true, ct);
        await SeedCatalogFrontPageAsync(db, seedFiles, logger, ct);
        await SeedClubAsync(db, seedFiles, logger, ct);
        await SeedRecyclerAsync(db, seedFiles, logger, ct);
        await SeedCraftingAsync(db, seedFiles, logger, ct);
        await SeedBotsAsync(db, seedFiles, logger, ct);
        await SeedPetsAsync(db, seedFiles, logger, ct);
        await SeedQuizzesAsync(db, seedFiles, logger, ct);
        await SeedYoutubeAsync(db, seedFiles, logger, ct);
        await SeedPollsAsync(db, seedFiles, logger, ct);
        await SeedCampaignCalendarsAsync(db, seedFiles, logger, ct);
        await SeedPromoArticlesAsync(db, seedFiles, logger, ct);
        await SeedProductClaimsAsync(db, seedFiles, logger, ct);
    }

    /// <summary>One row of seed/perks.json.</summary>
    private sealed record SeedPerk(string Code, string ErrorMessage, SecurityLevel MinimumRank);

    /// <summary>Loads the perk catalog from seed/perks.json. Existing rows keep whatever the hotel
    /// changed them to; only codes that are missing get added.</summary>
    private static async Task SeedPerksAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        var perks = await seedFiles.ReadJsonAsync<List<SeedPerk>>("perks.json", ct);
        if (perks is null || perks.Count == 0)
            return;

        var known = await db.Perks.Select(p => p.Code).ToListAsync(ct);
        var added = 0;

        foreach (var perk in perks.Where(p => !known.Contains(p.Code)))
        {
            db.Perks.Add(new Perk
            {
                Code = perk.Code,
                ErrorMessage = perk.ErrorMessage,
                MinimumRank = perk.MinimumRank,
            });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} perk(s) from {File}.", added, "perks.json");
        }
    }

    /// <summary>One category of seed/call_for_help_topics.json, with the reasons under it.</summary>
    private sealed record SeedCallForHelpCategory(string Name, List<SeedCallForHelpTopic> Topics);

    private sealed record SeedCallForHelpTopic(string Name, string Consequence);

    /// <summary>Loads the report reasons the client's help tool offers. The file is the whole
    /// list, so a category the hotel already has keeps whatever it was changed to and only the
    /// missing ones are added; order comes from the file.</summary>
    private static async Task SeedCallForHelpTopicsAsync(
        AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        const string file = "call_for_help_topics.json";
        var categories = await seedFiles.ReadJsonAsync<List<SeedCallForHelpCategory>>(file, ct);
        if (categories is null || categories.Count == 0)
            return;

        var known = await db.CallForHelpCategories.Select(c => c.Name).ToListAsync(ct);
        var added = 0;

        for (var index = 0; index < categories.Count; index++)
        {
            var category = categories[index];
            if (known.Contains(category.Name))
                continue;

            db.CallForHelpCategories.Add(new CallForHelpCategory
            {
                Name = category.Name,
                SortOrder = index,
                Topics = category.Topics
                    .Select((topic, order) => new CallForHelpTopic
                    {
                        Name = topic.Name,
                        Consequence = topic.Consequence,
                        SortOrder = order,
                    })
                    .ToList(),
            });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} call-for-help category(ies) from {File}.", added, file);
        }
    }

    /// <summary>One achievement of seed/achievements.json. The file is written from a capture
    /// of Habbo's own answer, which carries every achievement's identity but only the levels the
    /// captured account happened to be on — hence a level count apart from the levels.</summary>
    private sealed record SeedAchievement(
        int AchievementId, string Name, string Category, string? SubCategory,
        int DisplayMethod, int RewardPointType, int State, int LevelCount,
        List<SeedAchievementLevel> KnownLevels);

    private sealed record SeedAchievementLevel(
        int Level, string BadgeCode, int RequiredProgress, int RewardPoints);

    /// <summary>Loads the achievement definitions. An achievement the hotel already has keeps
    /// whatever it was changed to, levels included, so only new badge codes are added.</summary>
    private static async Task SeedAchievementsAsync(
        AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        const string file = "achievements.json";
        var achievements = await seedFiles.ReadJsonAsync<List<SeedAchievement>>(file, ct);
        if (achievements is null || achievements.Count == 0)
            return;

        var known = await db.Achievements.Select(a => a.Id).ToListAsync(ct);
        var added = 0;

        foreach (var achievement in achievements.Where(a => !known.Contains(a.AchievementId)))
        {
            db.Achievements.Add(new Achievement
            {
                // The ids are Habbo's own, and the client keys its dialog on them, so they are
                // taken from the file rather than handed out by the database.
                Id = achievement.AchievementId,
                Name = achievement.Name,
                Category = achievement.Category,
                SubCategory = achievement.SubCategory ?? string.Empty,
                DisplayMethod = achievement.DisplayMethod,
                RewardPointType = achievement.RewardPointType,
                State = achievement.State == 0 ? AchievementState.Active : (AchievementState)achievement.State,
                LevelCount = Math.Max(achievement.LevelCount, achievement.KnownLevels.Count),
                Levels = achievement.KnownLevels
                    .Select(level => new AchievementLevel
                    {
                        Level = level.Level,
                        BadgeCode = level.BadgeCode,
                        RequiredProgress = level.RequiredProgress,
                        RewardPoints = level.RewardPoints,
                    })
                    .ToList(),
            });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} achievement(s) from {File}.", added, file);
        }
    }

    /// <summary>One tier of seed/builders_club_limits.json.</summary>
    private sealed record SeedBuildersClubTier(
        string ProductName, int FurniLimit, int MaxFurniLimit, int GraceDays);

    /// <summary>Loads what each Builders Club tier allows.</summary>
    private static async Task SeedBuildersClubLimitsAsync(
        AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        const string file = "builders_club_limits.json";
        var tiers = await seedFiles.ReadJsonAsync<List<SeedBuildersClubTier>>(file, ct);
        if (tiers is null || tiers.Count == 0)
            return;

        var known = await db.BuildersClubLimits.Select(l => l.ProductName).ToListAsync(ct);
        var added = 0;

        foreach (var tier in tiers.Where(t => !known.Contains(t.ProductName)))
        {
            db.BuildersClubLimits.Add(new BuildersClubLimits
            {
                ProductName = tier.ProductName,
                FurniLimit = tier.FurniLimit,
                MaxFurniLimit = tier.MaxFurniLimit,
                Grace = TimeSpan.FromDays(tier.GraceDays),
            });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} Builders Club tier(s) from {File}.", added, file);
        }
    }

    /// <summary>One row of seed/gestures.json.</summary>
    private sealed record SeedGesture(string Emoticon, int Gesture);

    /// <summary>Loads the emoticon-to-gesture map; existing emoticons keep whatever the hotel set.</summary>
    private static async Task SeedGesturesAsync(
        AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        var gestures = await seedFiles.ReadJsonAsync<List<SeedGesture>>("gestures.json", ct);
        if (gestures is null || gestures.Count == 0)
            return;

        var known = await db.ChatGestures.Select(g => g.Emoticon).ToListAsync(ct);
        var added = 0;

        foreach (var gesture in gestures.Where(g => !known.Contains(g.Emoticon)))
        {
            db.ChatGestures.Add(new ChatGesture { Emoticon = gesture.Emoticon, GestureId = gesture.Gesture });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} chat gesture(s) from {File}.", added, "gestures.json");
        }
    }

    private const string EffectMapFile = "effectmap.xml";

    /// <summary>seed/effectmap.xml — Habbo's avatar effects, by id and library. Refreshed on every
    /// start, so a newer effect map simply replaces the old rows. The dances the map also lists
    /// (ids like "dance.1") are not effects an avatar wears and are left out.</summary>
    private static async Task SeedAvatarEffectsAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        var text = await seedFiles.ReadTextAsync(EffectMapFile, ct);
        if (text is null)
        {
            logger.LogWarning("{File} not found; avatars can wear no effects.", EffectMapFile);
            return;
        }

        var effects = System.Xml.Linq.XDocument.Parse(text).Root!.Elements("effect")
            .Select(e => (Id: (string?)e.Attribute("id"), Library: (string?)e.Attribute("lib"), Type: (string?)e.Attribute("type"),
                Revision: (string?)e.Attribute("revision")))
            .Where(e => int.TryParse(e.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) && e.Library is not null)
            .Select(e => new AvatarEffect
            {
                Id = int.Parse(e.Id!, CultureInfo.InvariantCulture),
                Library = e.Library!,
                Type = e.Type ?? string.Empty,
                Revision = int.TryParse(e.Revision, NumberStyles.Integer, CultureInfo.InvariantCulture, out var revision) ? revision : 0,
            })
            .DistinctBy(e => e.Id)
            .ToList();

        var stored = await db.AvatarEffects.ToDictionaryAsync(e => e.Id, ct);
        foreach (var effect in effects)
        {
            if (stored.TryGetValue(effect.Id, out var row))
            {
                row.Library = effect.Library;
                row.Type = effect.Type;
                row.Revision = effect.Revision;
            }
            else
            {
                db.AvatarEffects.Add(effect);
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Loaded {Count} avatar effect(s) from {File}.", effects.Count, EffectMapFile);
    }

    /// <summary>seed/navigator.json — the navigator's tabs, result blocks and categories.</summary>
    private sealed record SeedNavigator(
        List<SeedNavigatorTab> Contexts, List<SeedNavigatorBlock> Blocks, List<SeedNavigatorCategory> Categories,
        List<SeedEventCategory>? EventCategories);

    private sealed record SeedEventCategory(int Id, string Name, bool Visible);

    private sealed record SeedNavigatorTab(string SearchCode, List<SeedQuickLink> QuickLinks);

    private sealed record SeedQuickLink(string SearchCode, string Filter, string Localization);

    private sealed record SeedNavigatorBlock(
        string RequestCode, string BlockCode, string Text, string Query,
        int ActionAllowed, int ViewMode, int MaxResults, bool ForceClosed);

    private sealed record SeedNavigatorCategory(
        string NodeName, bool Visible, bool Automatic, string AutomaticCategoryKey,
        string GlobalCategoryKey, bool StaffOnly);

    /// <summary>Loads the navigator layout. Rows are matched by their codes, so adding a tab or
    /// block to the seed file appears on the next restart without disturbing what a hotel changed.</summary>
    private static async Task SeedNavigatorAsync(
        AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        var layout = await seedFiles.ReadJsonAsync<SeedNavigator>("navigator.json", ct);
        if (layout is null)
            return;

        var added = 0;

        var knownTabs = await db.NavigatorTabs.Select(tab => tab.SearchCode).ToListAsync(ct);
        var tabOrder = knownTabs.Count;
        foreach (var tab in (layout.Contexts ?? []).Where(t => !knownTabs.Contains(t.SearchCode)))
        {
            var linkOrder = 0;
            db.NavigatorTabs.Add(new NavigatorTab
            {
                SearchCode = tab.SearchCode,
                SortOrder = tabOrder++,
                QuickLinks = (tab.QuickLinks ?? []).Select(link => new NavigatorQuickLink
                {
                    TabSearchCode = tab.SearchCode,
                    SearchCode = link.SearchCode,
                    Filter = link.Filter,
                    Localization = link.Localization,
                    SortOrder = linkOrder++,
                }).ToList(),
            });
            added++;
        }

        // A quick link can be added to a tab that already exists.
        var existingLinks = await db.NavigatorQuickLinks
            .Select(link => new { link.TabSearchCode, link.SearchCode, link.SortOrder })
            .ToListAsync(ct);
        foreach (var tab in (layout.Contexts ?? []).Where(t => knownTabs.Contains(t.SearchCode)))
        {
            // after the tab's own links, one after another
            var nextOrder = existingLinks.Where(e => e.TabSearchCode == tab.SearchCode).Select(e => e.SortOrder + 1).DefaultIfEmpty(0).Max();
            foreach (var link in tab.QuickLinks ?? [])
            {
                if (existingLinks.Any(e => e.TabSearchCode == tab.SearchCode && e.SearchCode == link.SearchCode))
                    continue;

                db.NavigatorQuickLinks.Add(new NavigatorQuickLink
                {
                    TabSearchCode = tab.SearchCode,
                    SearchCode = link.SearchCode,
                    Filter = link.Filter,
                    Localization = link.Localization,
                    SortOrder = nextOrder++,
                });
                added++;
            }
        }

        var knownBlocks = await db.NavigatorBlocks
            .Select(block => new { block.RequestCode, block.BlockCode })
            .ToListAsync(ct);
        var blockOrder = knownBlocks.Count;
        foreach (var block in layout.Blocks ?? [])
        {
            if (knownBlocks.Any(k => k.RequestCode == block.RequestCode && k.BlockCode == block.BlockCode))
                continue;

            db.NavigatorBlocks.Add(new NavigatorBlock
            {
                RequestCode = block.RequestCode,
                BlockCode = block.BlockCode,
                Text = block.Text,
                Query = block.Query,
                ActionAllowed = block.ActionAllowed,
                ViewMode = block.ViewMode,
                MaxResults = block.MaxResults,
                ForceClosed = block.ForceClosed,
                SortOrder = blockOrder++,
            });
            added++;
        }

        var knownCategories = await db.NavigatorCategories.Select(category => category.NodeName).ToListAsync(ct);
        var categoryOrder = knownCategories.Count;
        foreach (var category in (layout.Categories ?? []).Where(c => !knownCategories.Contains(c.NodeName)))
        {
            db.NavigatorCategories.Add(new NavigatorCategory
            {
                NodeName = category.NodeName,
                Visible = category.Visible,
                Automatic = category.Automatic,
                AutomaticCategoryKey = category.AutomaticCategoryKey,
                GlobalCategoryKey = category.GlobalCategoryKey,
                StaffOnly = category.StaffOnly,
                SortOrder = categoryOrder++,
            });
            added++;
        }

        var knownEventCategories = await db.RoomEventCategories.Select(category => category.Id).ToListAsync(ct);
        var eventCategoryOrder = knownEventCategories.Count;
        foreach (var category in (layout.EventCategories ?? []).Where(c => !knownEventCategories.Contains(c.Id)))
        {
            db.RoomEventCategories.Add(new RoomEventCategory
            {
                Id = category.Id, Name = category.Name, Visible = category.Visible, SortOrder = eventCategoryOrder++,
            });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} navigator row(s) from {File}.", added, "navigator.json");
        }
    }

    /// <summary>A development account from the Seed:Users configuration list.</summary>
    /// <summary>Credits are part of the account's starting state, so the shop is usable from
    /// a fresh database without anyone editing a row by hand.</summary>
    /// <summary>A dev account from the seed settings. <see cref="SsoTicket"/>, when set, is a permanent SSO ticket for
    /// it: it never expires and is never used up, so the client can be pointed at it once and relaunched for good. A rank
    /// makes the account staff when it is created; a deployment seeds its first administrator this way, from its secret.</summary>
    private sealed record SeedAccount(string Username, string Password, int Credits, string? SsoTicket = null,
        SecurityLevel Rank = SecurityLevel.AboveNone);

    /// <summary>Creates every configured dev account that doesn't exist yet (Development settings list them). Returns
    /// the first account, which owns the default room, or null in a hotel with none yet.</summary>
    private static async Task<User?> SeedUserAsync(
        IServiceProvider services, AppDbContext db, IConfiguration configuration, ILogger logger, CancellationToken ct)
    {
        var accounts = configuration.GetSection("Seed:Users").Get<List<SeedAccount>>() ?? [];
        var hasher = services.GetRequiredService<IPasswordHasher<User>>();

        foreach (var account in accounts)
        {
            // usernames are unique whatever their case, so "Admin" already registered is this account too
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == account.Username.ToLower(), ct);
            if (user is null)
            {
                user = new User
                {
                    Username = account.Username,
                    PasswordHash = string.Empty,
                    Credits = account.Credits,
                    Rank = account.Rank,
                };
                user.PasswordHash = hasher.HashPassword(user, account.Password);
                db.Users.Add(user);
                await db.SaveChangesAsync(ct);
                logger.LogWarning("Seeded user '{Username}' ({Rank}).", account.Username, account.Rank);
            }

            await SeedPermanentTicketAsync(db, account, user, logger, ct);
        }

        return await db.Users.OrderBy(u => u.AccountId).FirstOrDefaultAsync(ct);
    }

    /// <summary>Keeps the account's configured permanent ticket in place on every start: made if missing, and made
    /// permanent and the account's again if the row was changed. A ticket the settings no longer name is not touched.</summary>
    private static async Task SeedPermanentTicketAsync(AppDbContext db, SeedAccount account, User user, ILogger logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(account.SsoTicket))
            return;

        var value = account.SsoTicket.Trim();
        var ticket = await db.SsoTickets.FirstOrDefaultAsync(t => t.Ticket == value, ct);
        if (ticket is null)
        {
            db.SsoTickets.Add(new SsoTicket
            {
                Ticket = value, UserId = user.Id, Permanent = true, ExpiresAt = DateTimeOffset.MaxValue,
            });
            logger.LogWarning("Seeded a permanent SSO ticket for development user '{Username}'.", user.Username);
        }
        else if (!ticket.Permanent || ticket.UserId != user.Id)
        {
            ticket.UserId = user.Id;
            ticket.Permanent = true;
            ticket.ExpiresAt = DateTimeOffset.MaxValue;
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>seed/rooms.json — the room models and the rooms that ship with a fresh hotel.</summary>
    private sealed record SeedRoomFile(List<SeedRoomModel> Models, List<SeedRoom> Rooms);

    private sealed record SeedRoomModel(string Name, int DoorX, int DoorY, int DoorDirection, List<string> Heightmap);

    private sealed record SeedRoom(
        string Name, string Description, string ModelName,
        string? FloorType, string? Wallpaper, string? Landscape, bool IsStaffPick);

    /// <summary>Seeds the room models, once: what the floor plans of every room are made from.</summary>
    private static async Task SeedRoomModelsAsync(AppDbContext db, SeedRoomFile? content, ILogger logger, CancellationToken ct)
    {
        if (content is null || await db.RoomModels.AnyAsync(ct))
            return;

        foreach (var model in content.Models ?? [])
        {
            db.RoomModels.Add(new RoomModelData
            {
                Name = model.Name,
                // The client splits the heightmap on carriage returns.
                Heightmap = string.Join('\r', model.Heightmap),
                DoorX = model.DoorX,
                DoorY = model.DoorY,
                DoorDirection = model.DoorDirection,
            });
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Models} room model(s) from {File}.", (content.Models ?? []).Count, "rooms.json");
    }

    /// <summary>Seeds the starter rooms for the first account, in a hotel with no rooms yet. The client needs a
    /// floor and wall material to draw anything, so those come from the seed file rather than being left blank.</summary>
    private static async Task<bool> SeedStarterRoomsAsync(
        AppDbContext db, SeedRoomFile? content, User owner, ILogger logger, CancellationToken ct)
    {
        if (content is null || await db.Rooms.AnyAsync(ct))
            return false;

        var modelsByName = await db.RoomModels.ToDictionaryAsync(m => m.Name, ct);
        foreach (var room in content.Rooms ?? [])
        {
            if (!modelsByName.TryGetValue(room.ModelName, out var model))
            {
                logger.LogWarning("Seed room '{Room}' references unknown model '{Model}'.", room.Name, room.ModelName);
                continue;
            }

            db.Rooms.Add(new Room
            {
                OwnerId = owner.Id,
                ModelId = model.Id,
                Name = room.Name,
                Description = room.Description,
                FloorType = room.FloorType ?? string.Empty,
                Wallpaper = room.Wallpaper ?? string.Empty,
                Landscape = room.Landscape ?? string.Empty,
                IsStaffPick = room.IsStaffPick,
            });
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Rooms} room(s) from {File}.", (content.Rooms ?? []).Count, "rooms.json");
        return true;
    }

    /// <summary>seed/items.json — what a new account starts with. The definitions themselves
    /// come from furnidata; this file only names ids and counts.</summary>
    private sealed record SeedItemFile(List<SeedStarterItem> StarterInventory);

    /// <summary>Furniture is named rather than numbered: a furnidata class id means nothing
    /// without its section, but a class name is unique across the whole file.</summary>
    private sealed record SeedStarterItem(string ClassName, int Count);

    /// <summary>The client's own furniture list, and therefore ours.</summary>
    private const string FurnidataFile = "furnidata.xml";

    /// <summary>Imports every furniture definition from furnidata, with its logic and interactions.
    ///
    /// Definitions are added by id and never overwritten: furnidata is the authority for what
    /// exists, but a hotel that has tuned a definition should not lose that on the next restart.
    /// Re-running therefore picks up newly released furniture without touching anything else.</summary>
    private static async Task SeedFurnitureDefinitionsAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        await SeedItemDefinitionsAsync(db, seedFiles, logger, ct);
        await FurniLogicImporter.ImportAsync(db, seedFiles, logger, ct);
        await FurniInteractionImporter.ImportAsync(db, seedFiles, logger, ct);
    }

    /// <summary>Hands the first account a starter set so furniture is testable straight away.</summary>
    private static async Task SeedStarterItemsAsync(
        AppDbContext db, SeedFiles seedFiles, User owner, ILogger logger, CancellationToken ct)
    {
        var content = await seedFiles.ReadJsonAsync<SeedItemFile>("items.json", ct);
        if (content is null)
            return;

        var byClassName = await db.ItemDefinitions.ToDictionaryAsync(d => d.ClassName, d => d.Id, ct);
        var granted = 0;

        foreach (var starter in content.StarterInventory ?? [])
        {
            if (!byClassName.TryGetValue(starter.ClassName, out var definitionId))
            {
                logger.LogWarning(
                    "Starter item '{Class}' is not in furnidata; skipping.", starter.ClassName);
                continue;
            }

            for (var i = 0; i < starter.Count; i++)
            {
                db.UserItems.Add(new UserItem { OwnerId = owner.Id, DefinitionId = definitionId });
                granted++;
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Granted {Count} starter item(s) to '{User}'.", granted, owner.Username);
    }

    /// <summary>Parses furnidata into item definitions, adding what is new and refreshing what
    /// furnidata owns. furnidata is the authority for geometry, heights and what an avatar may do
    /// with an item, so a stale local row is a bug rather than a customisation — the client draws
    /// from its own copy and would disagree with us. Groupable is the exception: furnidata has no
    /// such field, so whatever is already stored is kept.</summary>
    private static async Task SeedItemDefinitionsAsync(
        AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        await using var furnidata = seedFiles.OpenRead(FurnidataFile);
        if (furnidata is null)
        {
            logger.LogWarning(
                "{File} not found under '{Root}'; no furniture definitions were loaded.",
                FurnidataFile, seedFiles.Root);
            return;
        }

        // Keyed on the pair, because furnidata reuses a class id between its two sections.
        var existing = await db.ItemDefinitions.ToDictionaryAsync(d => (d.ClassId, d.Type), ct);
        var added = 0;
        var updated = 0;

        foreach (var parsed in FurnidataParser.Parse(furnidata))
        {
            if (!existing.TryGetValue((parsed.ClassId, parsed.Type), out var current))
            {
                db.ItemDefinitions.Add(parsed);
                added++;
                continue;
            }

            if (Apply(parsed, current))
                updated++;
        }

        if (added == 0 && updated == 0)
            return;

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Imported {Added} new and refreshed {Updated} furniture definition(s) from {File}.",
            added, updated, FurnidataFile);
    }

    /// <summary>Copies the furnidata-owned fields onto a stored definition, reporting whether
    /// anything actually changed so an unchanged run writes nothing.</summary>
    private static bool Apply(ItemDefinition source, ItemDefinition target)
    {
        if (target.ClassName == source.ClassName
            && target.XDim == source.XDim
            && target.YDim == source.YDim
            && target.StackHeight.Equals(source.StackHeight)
            && target.CanWalk == source.CanWalk
            && target.CanSit == source.CanSit
            && target.CanLay == source.CanLay
            && target.CanStackOn == source.CanStackOn
            && target.CategoryName == source.CategoryName
            && target.Name == source.Name
            && target.Description == source.Description
            && target.Revision == source.Revision
            && target.DefaultDirection == source.DefaultDirection
            && target.SpecialType == source.SpecialType
            && target.FurniLine == source.FurniLine
            && target.Rare == source.Rare
            && target.Tradeable == source.Tradeable
            && target.Recyclable == source.Recyclable
            && target.CustomParams == source.CustomParams)
        {
            return false;
        }

        target.ClassName = source.ClassName;
        target.XDim = source.XDim;
        target.YDim = source.YDim;
        target.StackHeight = source.StackHeight;
        target.CanWalk = source.CanWalk;
        target.CanSit = source.CanSit;
        target.CanLay = source.CanLay;
        target.CanStackOn = source.CanStackOn;
        target.CategoryName = source.CategoryName;
        target.Name = source.Name;
        target.Description = source.Description;
        target.Revision = source.Revision;
        target.DefaultDirection = source.DefaultDirection;
        target.SpecialType = source.SpecialType;
        target.FurniLine = source.FurniLine;
        target.Rare = source.Rare;
        target.Tradeable = source.Tradeable;
        target.Recyclable = source.Recyclable;
        target.CustomParams = source.CustomParams;
        return true;
    }

    /// <summary>seed/catalog.json — the shop: a tree of pages, each holding priced offers.</summary>
    private sealed record SeedCatalogFile(List<SeedCatalogPage> Pages);

    private sealed record SeedCatalogPage(
        int Id, int? ParentId, string PageName, string Localization, int Icon, bool Visible,
        string LayoutCode, int OrderIndex, SecurityLevel MinRank,
        List<string> Images, List<string> Texts, List<SeedCatalogOffer> Offers);

    /// <summary>The currency fields are optional: almost everything we seed is priced in
    /// credits, and an omitted activity-point price simply stays zero.</summary>
    private sealed record SeedCatalogOffer(
        int Id, string LocalizationId, int PriceInCredits, ClubLevel ClubLevel, int MaxAmount,
        int PriceInActivityPoints, string? ActivityPointType, int PriceInSilver,
        bool IsPet, bool Giftable, bool IsRentable, string? PreviewImage,
        List<SeedCatalogProduct> Products, int LimitedStack);

    /// <summary>A product is furniture when it names a class and a badge when it names nothing, unless
    /// its type says otherwise: an effect or a chat style names its id.</summary>
    private sealed record SeedCatalogProduct(
        string? ClassName, string? ExtraParam, int Amount, CatalogProductType? Type, int? ClassId, int DurationSeconds);

    private const string CatalogFile = "catalog.json";

    /// <summary>seed/catalog_builders_club.json — the Builders Club warehouse, in the shop's format (Habbo.CatalogDumper
    /// with CATALOG_TYPE=BUILDERS_CLUB).</summary>
    private const string BuildersClubCatalogFile = "catalog_builders_club.json";

    /// <summary>Loads a catalog: the shop, or the Builders Club warehouse. Pages are inserted parent-first so the
    /// self-referencing key always resolves, and an offer naming a definition that was never seeded is dropped with a
    /// warning rather than failing the whole seed. The two catalogs share the page and offer tables, and each file
    /// starts from a root of its own: a page or offer id the other catalog already holds (both roots are 1) is
    /// given a free one, consistently throughout the file.</summary>
    private static async Task SeedCatalogAsync(
        AppDbContext db, SeedFiles seedFiles, ILogger logger, string file, bool buildersClub, CancellationToken ct)
    {
        if (await db.CatalogPages.AnyAsync(page => page.BuildersClub == buildersClub, ct))
            return;

        var content = await seedFiles.ReadJsonAsync<SeedCatalogFile>(file, ct);
        if (content is null)
            return;

        var takenPages = (await db.CatalogPages.Select(p => p.Id).ToListAsync(ct)).ToHashSet();
        var takenOffers = (await db.CatalogOffers.Select(o => o.Id).ToListAsync(ct)).ToHashSet();
        var pageIdOf = FreeIds(content.Pages?.Select(p => p.Id) ?? [], takenPages);
        var offerIdOf = FreeIds(content.Pages?.SelectMany(p => p.Offers ?? []).Select(o => o.Id) ?? [], takenOffers);
        var pages = (content.Pages ?? [])
            .Select(page => page with
            {
                Id = pageIdOf[page.Id],
                ParentId = page.ParentId is { } parent && pageIdOf.TryGetValue(parent, out var mapped) ? mapped : page.ParentId,
                Offers = (page.Offers ?? []).Select(offer => offer with { Id = offerIdOf[offer.Id] }).ToList(),
            })
            .ToList();
        var pageIds = pages.Select(p => p.Id).ToHashSet();
        var definitionsByClassName = await db.ItemDefinitions.ToDictionaryAsync(d => d.ClassName, d => d.Id, ct);
        var offerCount = 0;
        // An offer can be listed on several pages (Habbo's own catalog does it constantly): it is written in full
        // where it first appears and by its id alone after that, and stored once with a listing per page.
        var seeded = new Dictionary<int, CatalogOffer>();
        var skipped = new HashSet<int>();

        // all of it or none: the catalog counts as seeded once it has a page, so a seed that stopped halfway (a bad
        // offer, say) would otherwise leave half a catalog that is never finished
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        foreach (var page in Ordered(pages))
        {
            var pageEntity = new CatalogPage
            {
                Id = page.Id,
                // a parent missing from the file makes the page a root, as Ordered places it, not a broken key
                ParentId = page.ParentId is { } parentId && pageIds.Contains(parentId) ? parentId : null,
                PageName = page.PageName,
                Localization = page.Localization,
                Icon = page.Icon,
                Visible = page.Visible,
                LayoutCode = page.LayoutCode,
                OrderIndex = page.OrderIndex,
                MinRank = page.MinRank == SecurityLevel.None ? SecurityLevel.AboveNone : page.MinRank,
                Images = page.Images ?? [],
                Texts = page.Texts ?? [],
                BuildersClub = buildersClub,
            };

            db.CatalogPages.Add(pageEntity);

            // Each page is saved before its offers so the foreign key is already there.
            await db.SaveChangesAsync(ct);

            var position = 0;
            var listed = new HashSet<int>();

            foreach (var offer in page.Offers ?? [])
            {
                // a page lists an offer once, however often the file names it there
                if (!listed.Add(offer.Id))
                    continue;
                if (seeded.TryGetValue(offer.Id, out var earlier))
                {
                    earlier.Listings.Add(new CatalogListing { Page = pageEntity, OrderIndex = position++ });
                    continue;
                }
                if (skipped.Contains(offer.Id) || offer.Products is null)
                    continue;

                var products = offer.Products;
                var unknown = products
                    .Where(p => p.ClassName is { } name && !definitionsByClassName.ContainsKey(name))
                    .Select(p => p.ClassName!)
                    .ToList();

                if (unknown.Count > 0)
                {
                    logger.LogWarning(
                        "Catalog offer {Offer} names furniture that is not in furnidata ({Classes}); skipping the offer.",
                        offer.Id, string.Join(", ", unknown));
                    skipped.Add(offer.Id);
                    continue;
                }

                var entity = new CatalogOffer
                {
                    Id = offer.Id,
                    LocalizationId = offer.LocalizationId,
                    PriceInCredits = offer.PriceInCredits,
                    PriceInActivityPoints = offer.PriceInActivityPoints,
                    ActivityPointType = ParseCurrency(offer.ActivityPointType, offer.Id, logger),
                    PriceInSilver = offer.PriceInSilver,
                    IsPet = offer.IsPet,
                    Giftable = offer.Giftable,
                    IsRentable = offer.IsRentable,
                    PreviewImage = offer.PreviewImage ?? string.Empty,
                    ClubLevel = offer.ClubLevel,
                    MaxAmount = offer.MaxAmount == 0 ? 100 : offer.MaxAmount,
                    LimitedStack = offer.LimitedStack,
                };

                foreach (var product in products)
                {
                    entity.Products.Add(new CatalogProduct
                    {
                        Type = product.Type ?? (product.ClassName is null ? CatalogProductType.Badge : CatalogProductType.Furniture),
                        DefinitionId = product.ClassName is { } name ? definitionsByClassName[name] : null,
                        ClassId = product.ClassId,
                        DurationSeconds = product.DurationSeconds,
                        ExtraParam = product.ExtraParam ?? string.Empty,
                        Amount = product.Amount == 0 ? 1 : product.Amount,
                    });
                }

                // The seed nests an offer under the page that lists it; the link table is what
                // actually records that, because an offer may appear on several pages — and at
                // a different position on each, which is why the order lives on the link.
                entity.Listings.Add(new CatalogListing { Page = pageEntity, OrderIndex = position++ });
                db.CatalogOffers.Add(entity);
                seeded[offer.Id] = entity;
                offerCount++;
            }

            await db.SaveChangesAsync(ct);
        }

        await AdvanceCatalogSequencesAsync(db, ct);
        await transaction.CommitAsync(ct);

        logger.LogInformation("Seeded {Pages} catalog page(s) and {Offers} offer(s) from {File}.",
            pages.Count, offerCount, file);
    }

    /// <summary>Each id as it is, or, where it is already taken, the next one free past every id in sight.</summary>
    private static Dictionary<int, int> FreeIds(IEnumerable<int> ids, HashSet<int> taken)
    {
        var distinct = ids.Distinct().ToList();
        var next = Math.Max(taken.DefaultIfEmpty(0).Max(), distinct.DefaultIfEmpty(0).Max()) + 1;
        return distinct.ToDictionary(id => id, id => taken.Contains(id) ? next++ : id);
    }

    /// <summary>The currency name as written in the seed file. An unknown one falls back to
    /// duckets with a warning rather than failing the seed, since the price is still right.</summary>
    private static ActivityPointType ParseCurrency(string? name, int offerId, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ActivityPointType.Ducket;

        if (Enum.TryParse<ActivityPointType>(name, ignoreCase: true, out var currency))
            return currency;

        logger.LogWarning(
            "Catalog offer {Offer} names unknown currency '{Currency}'; treating it as duckets.",
            offerId, name);
        return ActivityPointType.Ducket;
    }

    /// <summary>Moves the page and offer sequences past the ids the seed file chose.
    ///
    /// Both columns are identity-by-default so a dump or a seed can keep the ids the client
    /// already knows, but that leaves the sequence untouched — a page created in the hotel
    /// afterwards would be handed id 1 and collide with the seeded root.</summary>
    private static async Task AdvanceCatalogSequencesAsync(AppDbContext db, CancellationToken ct)
    {
        // GREATEST guards the floor: a hidden page's id is derived and negative.
        await db.Database.ExecuteSqlRawAsync(
            """
            SELECT setval(pg_get_serial_sequence('catalog_pages', 'Id'),
                GREATEST((SELECT COALESCE(MAX("Id"), 0) FROM catalog_pages), 1));
            SELECT setval(pg_get_serial_sequence('catalog_offers', 'Id'),
                GREATEST((SELECT COALESCE(MAX("Id"), 0) FROM catalog_offers), 1));
            """, ct);
    }

    /// <summary>Parents before children, so a page's ParentId always points at a row that exists.
    /// A page whose parent is missing from the file is treated as a root rather than dropped.</summary>
    private static List<SeedCatalogPage> Ordered(List<SeedCatalogPage> pages)
    {
        var byId = pages.ToDictionary(p => p.Id);
        var ordered = new List<SeedCatalogPage>(pages.Count);
        var placed = new HashSet<int>();

        void Place(SeedCatalogPage page)
        {
            if (!placed.Add(page.Id))
                return;

            if (page.ParentId is { } parentId && byId.TryGetValue(parentId, out var parent))
                Place(parent);

            ordered.Add(page);
        }

        foreach (var page in pages)
            Place(page);

        return ordered;
    }

    /// <summary>The gamedata files the client downloads. Each is a file under seed/gamedata/,
    /// with {urlPrefix} and {gamePort} substituted so one set of files works on any deployment.</summary>
    private static readonly string[] GamedataFiles =
        ["external_variables", "external_texts", "furnidata", FurnidataJsonGamedataName, "productdata", "figuredata"];

    /// <summary>The client's own furniture list. It reads the very file the server imports, so
    /// the two cannot disagree about what an item is: a class the client does not know about is
    /// one it cannot draw, and it fails while rendering rather than by saying so.</summary>
    private const string FurnidataGamedataName = "furnidata";

    /// <summary>The same furniture list in Habbo's JSON form, made from the same file (FurnidataJson): the web
    /// client reads it far faster than the XML, which the AIR client still gets.</summary>
    private const string FurnidataJsonGamedataName = "furnidata_json";

    /// <summary>Habbo's figure data: the clothing sets and colours. The client draws avatars
    /// from it and the server checks every look against it.</summary>
    private const string FiguredataGamedataName = "figuredata";

    private const string FiguredataFile = "figuredata.xml";

    private static string GamedataSource(string name) => name switch
    {
        FurnidataGamedataName or FurnidataJsonGamedataName => FurnidataFile,
        FiguredataGamedataName => FiguredataFile,
        _ => $"gamedata/{name}.txt",
    };

    private const string UrlPrefixPlaceholder = "{urlPrefix}";
    private const string GamePortPlaceholder = "{gamePort}";

    private static async Task SeedGamedataAsync(
        AppDbContext db, SeedFiles seedFiles, IConfiguration configuration, ILogger logger, CancellationToken ct)
    {
        var urlPrefix = configuration["Web:UrlPrefix"] ?? string.Empty;
        var gamePort = configuration["Gateway:GamePort"] ?? string.Empty;
        var added = 0;

        foreach (var name in GamedataFiles)
        {
            var source = GamedataSource(name);
            var content = await seedFiles.ReadTextAsync(source, ct);
            if (content is null)
            {
                logger.LogWarning("Gamedata file '{Source}' is missing from the seed directory.", source);
                continue;
            }
            if (name == FurnidataJsonGamedataName)
                content = FurnidataJson.FromXml(content);

            content = content
                .Replace(UrlPrefixPlaceholder, urlPrefix, StringComparison.Ordinal)
                .Replace(GamePortPlaceholder, gamePort, StringComparison.Ordinal);

            // Refreshed rather than only seeded: the client caches by the hash we publish, so a
            // stored copy that no longer matches the seed file would be served forever.
            var hash = ComputeGamedataHash(content);
            var stored = await db.WebGamedata.FirstOrDefaultAsync(e => e.Name == name, ct);
            if (stored is null)
            {
                db.WebGamedata.Add(new WebGamedata { Name = name, Content = content, Hash = hash });
                added++;
            }
            else if (stored.Hash != hash)
            {
                stored.Content = content;
                stored.Hash = hash;
                added++;
            }
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded or refreshed {Count} gamedata file(s) from the seed directory.", added);
        }
    }

    private static string ComputeGamedataHash(string content)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
