using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.LandingView;
using Habbo.Domain.Subscriptions;
using Habbo.Domain.Catalog;
using Habbo.Domain.Chat;
using Habbo.Domain.Competitions;
using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Data;
using Habbo.Domain.Groups;
using Habbo.Domain.Moderation;
using Habbo.Domain.Navigator;
using Habbo.Domain.Rooms;
using Habbo.Domain.Support;
using Habbo.Domain.Users;
using Habbo.Domain.Web;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>The query filter that hides borrowed Builders Club furni which is in no room.</summary>
    public const string BuildersClubOnlyPlaced = "BuildersClubOnlyPlaced";

    public DbSet<User> Users => Set<User>();
    public DbSet<SsoTicket> SsoTickets => Set<SsoTicket>();
    public DbSet<InviteCode> InviteCodes => Set<InviteCode>();
    public DbSet<WebGamedata> WebGamedata => Set<WebGamedata>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomModelData> RoomModels => Set<RoomModelData>();
    public DbSet<ItemDefinition> ItemDefinitions => Set<ItemDefinition>();
    public DbSet<UserItem> UserItems => Set<UserItem>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<Perk> Perks => Set<Perk>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<AchievementLevel> AchievementLevels => Set<AchievementLevel>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<FriendCategory> FriendCategories => Set<FriendCategory>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();
    public DbSet<IgnoredUser> IgnoredUsers => Set<IgnoredUser>();
    public DbSet<BlockedUser> BlockedUsers => Set<BlockedUser>();
    public DbSet<CurrencyType> CurrencyTypes => Set<CurrencyType>();
    public DbSet<UserCurrency> UserCurrencies => Set<UserCurrency>();
    public DbSet<CurrencySeasonReset> CurrencySeasonResets => Set<CurrencySeasonReset>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();
    public DbSet<BadgeRequest> BadgeRequests => Set<BadgeRequest>();
    public DbSet<UserOutfit> UserOutfits => Set<UserOutfit>();
    public DbSet<UserEffect> UserEffects => Set<UserEffect>();
    public DbSet<UserFigureSet> UserFigureSets => Set<UserFigureSet>();
    public DbSet<UserChatStyle> UserChatStyles => Set<UserChatStyle>();
    public DbSet<ChatStyle> ChatStyles => Set<ChatStyle>();
    public DbSet<CurrencyTransaction> CurrencyTransactions => Set<CurrencyTransaction>();
    public DbSet<ClubPayday> ClubPaydays => Set<ClubPayday>();
    public DbSet<StarGemGift> StarGemGifts => Set<StarGemGift>();
    public DbSet<ConsoleMessage> ConsoleMessages => Set<ConsoleMessage>();
    public DbSet<RoomBan> RoomBans => Set<RoomBan>();
    public DbSet<RoomMute> RoomMutes => Set<RoomMute>();
    public DbSet<RoomFilterWord> RoomFilterWords => Set<RoomFilterWord>();
    public DbSet<FilteredWord> FilteredWords => Set<FilteredWord>();
    public DbSet<ModerationIssue> ModerationIssues => Set<ModerationIssue>();
    public DbSet<ModerationIssueChatLine> ModerationIssueChatLines => Set<ModerationIssueChatLine>();
    public DbSet<RoomChatLine> RoomChatLines => Set<RoomChatLine>();
    public DbSet<RoomVisit> RoomVisits => Set<RoomVisit>();
    public DbSet<Sanction> Sanctions => Set<Sanction>();
    public DbSet<SanctionType> SanctionTypes => Set<SanctionType>();
    public DbSet<ModerationPreset> ModerationPresets => Set<ModerationPreset>();
    public DbSet<ModToolPreference> ModToolPreferences => Set<ModToolPreference>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupBadgeLayer> GroupBadgeLayers => Set<GroupBadgeLayer>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<GroupMembershipRequest> GroupMembershipRequests => Set<GroupMembershipRequest>();
    public DbSet<GroupBlock> GroupBlocks => Set<GroupBlock>();
    public DbSet<GroupBadgePart> GroupBadgeParts => Set<GroupBadgePart>();
    public DbSet<GroupColor> GroupColors => Set<GroupColor>();
    public DbSet<ForumThread> ForumThreads => Set<ForumThread>();
    public DbSet<ForumMessage> ForumMessages => Set<ForumMessage>();
    public DbSet<ForumReadMarker> ForumReadMarkers => Set<ForumReadMarker>();
    public DbSet<BuildersClubLimits> BuildersClubLimits => Set<BuildersClubLimits>();
    public DbSet<TimingSchedule> TimingSchedules => Set<TimingSchedule>();
    public DbSet<TimingScheduleEntry> TimingScheduleEntries => Set<TimingScheduleEntry>();
    public DbSet<CallForHelpCategory> CallForHelpCategories => Set<CallForHelpCategory>();
    public DbSet<CallForHelpTopic> CallForHelpTopics => Set<CallForHelpTopic>();
    public DbSet<ChatGesture> ChatGestures => Set<ChatGesture>();
    public DbSet<AvatarEffect> AvatarEffects => Set<AvatarEffect>();
    public DbSet<RoomFavourite> RoomFavourites => Set<RoomFavourite>();
    public DbSet<RoomVote> RoomVotes => Set<RoomVote>();
    public DbSet<RoomEvent> RoomEvents => Set<RoomEvent>();
    public DbSet<RoomAdInteraction> RoomAdInteractions => Set<RoomAdInteraction>();
    public DbSet<CompetitionRoomEntry> CompetitionRoomEntries => Set<CompetitionRoomEntry>();
    public DbSet<OfficialRoomEntry> OfficialRoomEntries => Set<OfficialRoomEntry>();
    public DbSet<RoomNetwork> RoomNetworks => Set<RoomNetwork>();
    public DbSet<ForwardRoom> ForwardRooms => Set<ForwardRoom>();
    public DbSet<RoomRight> RoomRights => Set<RoomRight>();
    public DbSet<WiredRoomLog> WiredRoomLogs => Set<WiredRoomLog>();
    public DbSet<WiredVariableValueRow> WiredVariableValues => Set<WiredVariableValueRow>();
    public DbSet<NavigatorTab> NavigatorTabs => Set<NavigatorTab>();
    public DbSet<NavigatorQuickLink> NavigatorQuickLinks => Set<NavigatorQuickLink>();
    public DbSet<NavigatorBlock> NavigatorBlocks => Set<NavigatorBlock>();
    public DbSet<NavigatorCategory> NavigatorCategories => Set<NavigatorCategory>();
    public DbSet<RoomEventCategory> RoomEventCategories => Set<RoomEventCategory>();
    public DbSet<SavedNavigatorSearch> NavigatorSavedSearches => Set<SavedNavigatorSearch>();
    public DbSet<NavigatorCollapsedCategory> NavigatorCollapsedCategories => Set<NavigatorCollapsedCategory>();
    public DbSet<NavigatorViewMode> NavigatorViewModes => Set<NavigatorViewMode>();
    public DbSet<NavigatorWindowPreferences> NavigatorWindowPreferences => Set<NavigatorWindowPreferences>();
    public DbSet<LiftedRoom> LiftedRooms => Set<LiftedRoom>();
    public DbSet<PromotedRoomCategory> PromotedRoomCategories => Set<PromotedRoomCategory>();
    public DbSet<CatalogPage> CatalogPages => Set<CatalogPage>();
    public DbSet<CatalogFrontPageItem> CatalogFrontPageItems => Set<CatalogFrontPageItem>();
    public DbSet<CatalogOffer> CatalogOffers => Set<CatalogOffer>();
    public DbSet<CatalogProduct> CatalogProducts => Set<CatalogProduct>();
    public DbSet<CatalogListing> CatalogListings => Set<CatalogListing>();
    public DbSet<FurniState> FurniStates => Set<FurniState>();
    public DbSet<FurniTrophy> FurniTrophies => Set<FurniTrophy>();
    public DbSet<FurniPostIt> FurniPostIts => Set<FurniPostIt>();
    public DbSet<FurniGift> FurniGifts => Set<FurniGift>();
    public DbSet<ClubGiftClaim> ClubGiftClaims => Set<ClubGiftClaim>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<Habbo.Domain.Bots.BotType> BotTypes => Set<Habbo.Domain.Bots.BotType>();
    public DbSet<Habbo.Domain.Bots.Bot> Bots => Set<Habbo.Domain.Bots.Bot>();
    public DbSet<Habbo.Domain.Pets.PetType> PetTypes => Set<Habbo.Domain.Pets.PetType>();
    public DbSet<Habbo.Domain.Pets.PetPalette> PetPalettes => Set<Habbo.Domain.Pets.PetPalette>();
    public DbSet<Habbo.Domain.Pets.PetLayer> PetLayers => Set<Habbo.Domain.Pets.PetLayer>();
    public DbSet<Habbo.Domain.Pets.PetPackage> PetPackages => Set<Habbo.Domain.Pets.PetPackage>();
    public DbSet<Habbo.Domain.Pets.PetRarityCategory> PetRarityCategories => Set<Habbo.Domain.Pets.PetRarityCategory>();
    public DbSet<Habbo.Domain.Pets.PlantTrait> PlantTraits => Set<Habbo.Domain.Pets.PlantTrait>();
    public DbSet<Habbo.Domain.Pets.PlantSeed> PlantSeeds => Set<Habbo.Domain.Pets.PlantSeed>();
    public DbSet<Habbo.Domain.Support.Quiz> Quizzes => Set<Habbo.Domain.Support.Quiz>();
    public DbSet<Habbo.Domain.Support.QuizQuestion> QuizQuestions => Set<Habbo.Domain.Support.QuizQuestion>();
    public DbSet<Habbo.Domain.Support.ChatReviewRecord> ChatReviewRecords => Set<Habbo.Domain.Support.ChatReviewRecord>();
    public DbSet<Habbo.Domain.Furniture.Data.RentableSpaceRental> RentableSpaceRentals => Set<Habbo.Domain.Furniture.Data.RentableSpaceRental>();
    public DbSet<Habbo.Domain.Furniture.Youtube.YoutubePlaylist> YoutubePlaylists => Set<Habbo.Domain.Furniture.Youtube.YoutubePlaylist>();
    public DbSet<Habbo.Domain.Polls.Poll> Polls => Set<Habbo.Domain.Polls.Poll>();
    public DbSet<UserFilterWord> UserFilterWords => Set<UserFilterWord>();
    public DbSet<Habbo.Domain.Campaigns.CampaignCalendar> CampaignCalendars => Set<Habbo.Domain.Campaigns.CampaignCalendar>();
    public DbSet<Habbo.Domain.Campaigns.CampaignCalendarOpening> CampaignCalendarOpenings => Set<Habbo.Domain.Campaigns.CampaignCalendarOpening>();
    public DbSet<Habbo.Domain.LandingView.PromoArticle> PromoArticles => Set<Habbo.Domain.LandingView.PromoArticle>();
    public DbSet<Habbo.Domain.Catalog.ProductClaim> ProductClaims => Set<Habbo.Domain.Catalog.ProductClaim>();
    public DbSet<Habbo.Domain.Furniture.Chests.ChestCoins> ChestCoins => Set<Habbo.Domain.Furniture.Chests.ChestCoins>();
    public DbSet<Habbo.Domain.Furniture.Chests.ChestTransaction> ChestTransactions => Set<Habbo.Domain.Furniture.Chests.ChestTransaction>();
    public DbSet<Habbo.Domain.Furniture.Chests.ChestTransactionItem> ChestTransactionItems => Set<Habbo.Domain.Furniture.Chests.ChestTransactionItem>();
    public DbSet<DeferredNotification> DeferredNotifications => Set<DeferredNotification>();
    public DbSet<IncomeReward> IncomeRewards => Set<IncomeReward>();
    public DbSet<Habbo.Domain.Games.SnowWarGameResult> SnowWarGameResults => Set<Habbo.Domain.Games.SnowWarGameResult>();
    public DbSet<Habbo.Domain.Furniture.Chests.WiredContract> WiredContracts => Set<Habbo.Domain.Furniture.Chests.WiredContract>();
    public DbSet<Habbo.Domain.Catalog.ProductClaimRecord> ProductClaimRecords => Set<Habbo.Domain.Catalog.ProductClaimRecord>();
    public DbSet<Habbo.Domain.Camera.Photo> Photos => Set<Habbo.Domain.Camera.Photo>();
    public DbSet<Habbo.Domain.Polls.PollQuestion> PollQuestions => Set<Habbo.Domain.Polls.PollQuestion>();
    public DbSet<Habbo.Domain.Polls.PollResponse> PollResponses => Set<Habbo.Domain.Polls.PollResponse>();
    public DbSet<Habbo.Domain.Furniture.Youtube.YoutubeVideo> YoutubeVideos => Set<Habbo.Domain.Furniture.Youtube.YoutubeVideo>();
    public DbSet<Habbo.Domain.Furniture.Youtube.YoutubeDisplayState> YoutubeDisplayStates => Set<Habbo.Domain.Furniture.Youtube.YoutubeDisplayState>();
    public DbSet<Habbo.Domain.Pets.PetCommand> PetCommands => Set<Habbo.Domain.Pets.PetCommand>();
    public DbSet<Habbo.Domain.Pets.Pet> Pets => Set<Habbo.Domain.Pets.Pet>();
    public DbSet<Habbo.Domain.Crafting.CraftingRecipe> CraftingRecipes => Set<Habbo.Domain.Crafting.CraftingRecipe>();
    public DbSet<Habbo.Domain.Crafting.CraftingIngredient> CraftingIngredients => Set<Habbo.Domain.Crafting.CraftingIngredient>();
    public DbSet<Habbo.Domain.Recycler.RecyclerPrizeLevel> RecyclerPrizeLevels => Set<Habbo.Domain.Recycler.RecyclerPrizeLevel>();
    public DbSet<Habbo.Domain.Recycler.RecyclerPrize> RecyclerPrizes => Set<Habbo.Domain.Recycler.RecyclerPrize>();
    public DbSet<Habbo.Domain.Recycler.RecyclerUse> RecyclerUses => Set<Habbo.Domain.Recycler.RecyclerUse>();
    public DbSet<Habbo.Domain.Marketplace.MarketplaceOffer> MarketplaceOffers => Set<Habbo.Domain.Marketplace.MarketplaceOffer>();
    public DbSet<Habbo.Domain.Marketplace.MarketplaceTokens> MarketplaceTokens => Set<Habbo.Domain.Marketplace.MarketplaceTokens>();
    public DbSet<BonusRareProgress> BonusRareProgress => Set<BonusRareProgress>();
    public DbSet<TargetedOffer> TargetedOffers => Set<TargetedOffer>();
    public DbSet<TargetedOfferState> TargetedOfferStates => Set<TargetedOfferState>();
    public DbSet<VoucherRedemption> VoucherRedemptions => Set<VoucherRedemption>();
    public DbSet<FurniStackHeight> FurniStackHeights => Set<FurniStackHeight>();
    public DbSet<FurniDimmerPreset> FurniDimmerPresets => Set<FurniDimmerPreset>();
    public DbSet<FurniTeleportLink> FurniTeleportLinks => Set<FurniTeleportLink>();
    public DbSet<ItemDefinitionState> ItemDefinitionStates => Set<ItemDefinitionState>();
    public DbSet<ItemDefinitionDirection> ItemDefinitionDirections => Set<ItemDefinitionDirection>();
    public DbSet<FurniMapEntry> FurniMapEntries => Set<FurniMapEntry>();
    public DbSet<FurniStringEntry> FurniStringEntries => Set<FurniStringEntry>();
    public DbSet<FurniIntEntry> FurniIntEntries => Set<FurniIntEntry>();
    public DbSet<FurniVoteResult> FurniVoteResults => Set<FurniVoteResult>();
    public DbSet<FurniCrackable> FurniCrackables => Set<FurniCrackable>();
    public DbSet<FurniHighScore> FurniHighScores => Set<FurniHighScore>();
    public DbSet<FurniHighScoreEntry> FurniHighScoreEntries => Set<FurniHighScoreEntry>();
    public DbSet<FurniHighScoreUser> FurniHighScoreUsers => Set<FurniHighScoreUser>();
    public DbSet<FurniWired> FurniWiredBoxes => Set<FurniWired>();
    public DbSet<FurniWiredSnapshot> FurniWiredSnapshots => Set<FurniWiredSnapshot>();
    public DbSet<WiredRewardGrant> WiredRewardGrants => Set<WiredRewardGrant>();
    public DbSet<FurniAchievementResolution> FurniAchievementResolutions => Set<FurniAchievementResolution>();
    public DbSet<RoomCompetitionEntry> RoomCompetitionEntries => Set<RoomCompetitionEntry>();
    public DbSet<RoomCompetitionVote> RoomCompetitionVotes => Set<RoomCompetitionVote>();
    public DbSet<RoomCompetitionRulesAcceptance> RoomCompetitionRulesAcceptances => Set<RoomCompetitionRulesAcceptance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.ToTable("users");
            user.HasKey(u => u.Id);
            user.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");
            user.Property(u => u.AccountId).UseIdentityAlwaysColumn();
            user.HasIndex(u => u.AccountId).IsUnique();
            user.Property(u => u.Username).HasMaxLength(32).IsRequired();
            // also unique as lower("Username"), which lookups use: an index the UsernamesUniqueIgnoringCase migration makes
        user.HasIndex(u => u.Username).IsUnique();
            user.Property(u => u.PasswordHash).IsRequired();
            user.Property(u => u.Figure).HasMaxLength(256);
            // Stored as the client's own letter rather than a number, so the column still
            // reads as itself in SQL.
            user.Property(u => u.Gender)
                .HasMaxLength(1)
                .HasConversion(gender => gender.ToLetter(), letter => GenderExtensions.ParseLetter(letter))
                .IsRequired();
            user.Property(u => u.Motto).HasMaxLength(128);
            user.Property(u => u.Email).HasMaxLength(User.EmailMaxLength);
        });

        modelBuilder.Entity<CurrencyType>(type =>
        {
            type.ToTable("currency_types");
            type.HasKey(t => t.Type);
            type.Property(t => t.Type).ValueGeneratedNever();
            type.Property(t => t.Name).HasMaxLength(32);
        });

        modelBuilder.Entity<UserCurrency>(currency =>
        {
            currency.ToTable("user_currencies");
            currency.HasKey(c => new { c.UserId, c.Type });
            currency.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserBadge>(badge =>
        {
            badge.ToTable("user_badges");
            badge.HasKey(b => b.Id);
            badge.Property(b => b.Id).UseIdentityAlwaysColumn();
            badge.Property(b => b.Code).HasMaxLength(UserBadge.CodeMaxLength);
            badge.HasOne(b => b.User).WithMany().HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Cascade);
            badge.HasIndex(b => new { b.UserId, b.Code }).IsUnique();
        });

        modelBuilder.Entity<BadgeRequest>(request =>
        {
            request.ToTable("badge_requests");
            request.HasKey(r => r.Code);
            request.Property(r => r.Code).HasMaxLength(BadgeRequest.CodeMaxLength);
            request.Property(r => r.BadgeCode).HasMaxLength(UserBadge.CodeMaxLength);
        });

        modelBuilder.Entity<UserOutfit>(outfit =>
        {
            outfit.ToTable("user_outfits");
            outfit.HasKey(o => new { o.UserId, o.Slot });
            outfit.Property(o => o.Figure).HasMaxLength(256);
            outfit.Property(o => o.Gender)
                .HasMaxLength(1)
                .HasConversion(gender => gender.ToLetter(), letter => GenderExtensions.ParseLetter(letter));
            outfit.HasOne(o => o.User).WithMany().HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserEffect>(effect =>
        {
            effect.ToTable("user_effects");
            effect.HasKey(e => e.Id);
            effect.Property(e => e.Id).UseIdentityAlwaysColumn();
            effect.Ignore(e => e.ExpiresAt);
            effect.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            effect.HasIndex(e => new { e.UserId, e.EffectId }).IsUnique();
        });

        modelBuilder.Entity<UserFigureSet>(set =>
        {
            set.ToTable("user_figure_sets");
            set.HasKey(s => new { s.UserId, s.SetId });
            set.Property(s => s.FurniClassName).HasMaxLength(128);
            set.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatStyle>(style =>
        {
            style.ToTable("chat_styles");
            style.HasKey(s => s.Id);
            style.Property(s => s.Id).ValueGeneratedNever();
            style.Property(s => s.AssetId).HasMaxLength(64);
            style.Ignore(s => s.IsNft);
        });

        modelBuilder.Entity<CurrencyTransaction>(transaction =>
        {
            transaction.ToTable("currency_transactions");
            transaction.HasKey(t => t.Id);
            transaction.Property(t => t.Id).UseIdentityAlwaysColumn();
            transaction.Property(t => t.Reason).HasMaxLength(CurrencyTransaction.ReasonMaxLength);
            transaction.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            transaction.HasIndex(t => new { t.UserId, t.Currency, t.CreatedAt });
        });

        modelBuilder.Entity<Group>(group =>
        {
            group.ToTable("groups");
            group.HasKey(g => g.Id);
            group.Property(g => g.Id).UseIdentityAlwaysColumn();
            group.Property(g => g.Name).HasMaxLength(Group.NameMaxLength);
            group.Property(g => g.Description).HasMaxLength(Group.DescriptionMaxLength);
            group.Property(g => g.BadgeCode).HasMaxLength(Group.BadgeCodeMaxLength);
            group.Property(g => g.Url).HasMaxLength(255);
            group.HasOne(g => g.Room).WithMany().HasForeignKey(g => g.RoomId).OnDelete(DeleteBehavior.Cascade);
            group.HasIndex(g => g.RoomId).IsUnique();
            group.HasOne(g => g.Owner).WithMany().HasForeignKey(g => g.OwnerId).OnDelete(DeleteBehavior.Cascade);
            group.HasMany(g => g.BadgeLayers).WithOne().HasForeignKey(l => l.GroupId).OnDelete(DeleteBehavior.Cascade);
            group.HasMany<User>().WithOne().HasForeignKey(u => u.FavouriteGroupId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<GroupBadgeLayer>(layer =>
        {
            layer.ToTable("group_badge_layers");
            layer.HasKey(l => new { l.GroupId, l.Layer });
        });

        modelBuilder.Entity<GroupMember>(member =>
        {
            member.ToTable("group_members");
            member.HasKey(m => new { m.GroupId, m.UserId });
            member.HasOne(m => m.Group).WithMany().HasForeignKey(m => m.GroupId).OnDelete(DeleteBehavior.Cascade);
            member.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
            member.HasIndex(m => m.UserId);
        });

        modelBuilder.Entity<GroupMembershipRequest>(request =>
        {
            request.ToTable("group_membership_requests");
            request.HasKey(r => new { r.GroupId, r.UserId });
            request.HasOne<Group>().WithMany().HasForeignKey(r => r.GroupId).OnDelete(DeleteBehavior.Cascade);
            request.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GroupBlock>(block =>
        {
            block.ToTable("group_blocks");
            block.HasKey(b => new { b.GroupId, b.UserId });
            block.HasOne<Group>().WithMany().HasForeignKey(b => b.GroupId).OnDelete(DeleteBehavior.Cascade);
            block.HasOne(b => b.User).WithMany().HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GroupBadgePart>(part =>
        {
            part.ToTable("group_badge_parts");
            part.HasKey(p => new { p.Kind, p.Id });
            part.Property(p => p.FileName).HasMaxLength(128);
            part.Property(p => p.MaskFileName).HasMaxLength(128);
        });

        modelBuilder.Entity<GroupColor>(color =>
        {
            color.ToTable("group_colors");
            color.HasKey(c => new { c.Kind, c.Id });
            color.Property(c => c.Hex).HasMaxLength(6);
        });

        modelBuilder.Entity<ForumThread>(thread =>
        {
            thread.ToTable("forum_threads");
            thread.HasKey(t => t.Id);
            thread.Property(t => t.Id).UseIdentityAlwaysColumn();
            thread.Property(t => t.Subject).HasMaxLength(ForumThread.SubjectMaxLength);
            thread.HasOne<Group>().WithMany().HasForeignKey(t => t.GroupId).OnDelete(DeleteBehavior.Cascade);
            thread.HasOne(t => t.Author).WithMany().HasForeignKey(t => t.AuthorId).OnDelete(DeleteBehavior.Cascade);
            thread.HasOne(t => t.Admin).WithMany().HasForeignKey(t => t.AdminId).OnDelete(DeleteBehavior.SetNull);
            thread.HasMany(t => t.Messages).WithOne(m => m.Thread).HasForeignKey(m => m.ThreadId).OnDelete(DeleteBehavior.Cascade);
            thread.HasIndex(t => t.GroupId);
        });

        modelBuilder.Entity<ForumMessage>(message =>
        {
            message.ToTable("forum_messages");
            message.HasKey(m => m.Id);
            message.Property(m => m.Id).UseIdentityAlwaysColumn();
            message.Property(m => m.Text).HasMaxLength(ForumMessage.TextMaxLength);
            message.HasOne(m => m.Author).WithMany().HasForeignKey(m => m.AuthorId).OnDelete(DeleteBehavior.Cascade);
            message.HasOne(m => m.Admin).WithMany().HasForeignKey(m => m.AdminId).OnDelete(DeleteBehavior.SetNull);
            message.HasIndex(m => new { m.GroupId, m.Id });
            message.HasIndex(m => new { m.ThreadId, m.Id });
        });

        modelBuilder.Entity<ForumReadMarker>(marker =>
        {
            marker.ToTable("forum_read_markers");
            marker.HasKey(m => new { m.UserId, m.GroupId });
            marker.HasOne<User>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
            marker.HasOne<Group>().WithMany().HasForeignKey(m => m.GroupId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ModerationIssue>(issue =>
        {
            issue.ToTable("moderation_issues");
            issue.HasKey(i => i.Id);
            issue.Property(i => i.Id).UseIdentityAlwaysColumn();
            issue.Property(i => i.Message).HasMaxLength(ModerationIssue.MessageMaxLength);
            issue.Property(i => i.PhotoId).HasMaxLength(128);
            issue.Property(i => i.ReporterName).HasMaxLength(128);
            issue.Property(i => i.ReporterEmail).HasMaxLength(128);
            issue.HasOne(i => i.Reporter).WithMany().HasForeignKey(i => i.ReporterId).OnDelete(DeleteBehavior.SetNull);
            issue.HasOne(i => i.ReportedUser).WithMany().HasForeignKey(i => i.ReportedUserId).OnDelete(DeleteBehavior.SetNull);
            issue.HasOne(i => i.Picker).WithMany().HasForeignKey(i => i.PickerId).OnDelete(DeleteBehavior.SetNull);
            issue.HasMany(i => i.ChatLines).WithOne().HasForeignKey(l => l.IssueId).OnDelete(DeleteBehavior.Cascade);
            issue.HasIndex(i => i.State);
            issue.HasIndex(i => new { i.ReporterId, i.State });
        });

        modelBuilder.Entity<ModerationIssueChatLine>(line =>
        {
            line.ToTable("moderation_issue_chat_lines");
            line.HasKey(l => l.Id);
            line.Property(l => l.Id).UseIdentityAlwaysColumn();
            line.Property(l => l.Text).HasMaxLength(RoomChatLine.TextMaxLength);
            line.Property(l => l.UserName).HasMaxLength(32);
        });

        modelBuilder.Entity<RoomChatLine>(line =>
        {
            line.ToTable("room_chat_lines");
            line.HasKey(l => l.Id);
            line.Property(l => l.Id).UseIdentityAlwaysColumn();
            line.Property(l => l.Text).HasMaxLength(RoomChatLine.TextMaxLength);
            // no key to the room: a deleted room keeps its chat, which reports and the chatlogs still need
            line.HasOne<User>().WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Cascade);
            line.HasIndex(l => new { l.RoomId, l.Id });
            line.HasIndex(l => new { l.UserId, l.Id });
        });

        modelBuilder.Entity<RoomVisit>(visit =>
        {
            visit.ToTable("room_visits");
            visit.HasKey(v => v.Id);
            visit.Property(v => v.Id).UseIdentityAlwaysColumn();
            // no key to the room: the visits of a deleted room stay in the mod tool's history
            visit.HasIndex(v => v.RoomId);
            visit.HasOne<User>().WithMany().HasForeignKey(v => v.UserId).OnDelete(DeleteBehavior.Cascade);
            visit.HasIndex(v => new { v.UserId, v.Id });
        });

        modelBuilder.Entity<Sanction>(sanction =>
        {
            sanction.ToTable("sanctions");
            sanction.HasKey(s => s.Id);
            sanction.Property(s => s.Id).UseIdentityAlwaysColumn();
            sanction.Property(s => s.Message).HasMaxLength(ModerationIssue.MessageMaxLength);
            sanction.Property(s => s.MachineId).HasMaxLength(128);
            sanction.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
            sanction.HasIndex(s => new { s.UserId, s.Kind, s.ExpiresAt });
            sanction.HasIndex(s => s.MachineId);
        });

        modelBuilder.Entity<SanctionType>(type =>
        {
            type.ToTable("sanction_types");
            type.HasKey(t => t.Id);
            type.Property(t => t.Id).ValueGeneratedNever();
            type.Property(t => t.Name).HasMaxLength(32);
        });

        modelBuilder.Entity<ModerationPreset>(preset =>
        {
            preset.ToTable("moderation_presets");
            preset.HasKey(p => p.Id);
            preset.Property(p => p.Id).UseIdentityAlwaysColumn();
            preset.Property(p => p.Text).HasMaxLength(ModerationIssue.MessageMaxLength);
        });

        modelBuilder.Entity<ModToolPreference>(preference =>
        {
            preference.ToTable("mod_tool_preferences");
            preference.HasKey(p => p.UserId);
            preference.HasOne<User>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoomBan>(ban =>
        {
            ban.ToTable("room_bans");
            ban.HasKey(b => new { b.RoomId, b.UserId });
            ban.HasOne<Room>().WithMany().HasForeignKey(b => b.RoomId).OnDelete(DeleteBehavior.Cascade);
            ban.HasOne(b => b.User).WithMany().HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoomMute>(mute =>
        {
            mute.ToTable("room_mutes");
            mute.HasKey(m => new { m.RoomId, m.UserId });
            mute.HasOne<Room>().WithMany().HasForeignKey(m => m.RoomId).OnDelete(DeleteBehavior.Cascade);
            mute.HasOne<User>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoomFilterWord>(word =>
        {
            word.ToTable("room_filter_words");
            word.HasKey(w => new { w.RoomId, w.Word });
            word.Property(w => w.Word).HasMaxLength(RoomFilterWord.WordMaxLength);
            word.HasOne<Room>().WithMany().HasForeignKey(w => w.RoomId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FilteredWord>(word =>
        {
            word.ToTable("filtered_words");
            word.HasKey(w => w.Word);
            word.Property(w => w.Word).HasMaxLength(FilteredWord.WordMaxLength);
            word.Property(w => w.Replacement).HasMaxLength(FilteredWord.WordMaxLength);
        });

        modelBuilder.Entity<ConsoleMessage>(message =>
        {
            message.ToTable("console_messages");
            message.HasKey(m => m.Id);
            message.Property(m => m.Id).UseIdentityAlwaysColumn();
            message.Property(m => m.Key).HasMaxLength(ConsoleMessage.KeyLength);
            message.Property(m => m.Text).HasMaxLength(ConsoleMessage.TextMaxLength);
            message.HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Cascade);
            message.HasOne<User>().WithMany().HasForeignKey(m => m.RecipientId).OnDelete(DeleteBehavior.Cascade);
            message.HasOne<Group>().WithMany().HasForeignKey(m => m.GroupId).OnDelete(DeleteBehavior.Cascade);
            message.HasIndex(m => new { m.RecipientId, m.Delivered });
            message.HasIndex(m => new { m.SenderId, m.RecipientId, m.Id });
            message.HasIndex(m => new { m.GroupId, m.Id });
            message.HasIndex(m => m.Key).IsUnique();
        });

        modelBuilder.Entity<StarGemGift>(gift =>
        {
            gift.ToTable("star_gem_gifts");
            gift.HasKey(g => g.Id);
            gift.Property(g => g.Id).UseIdentityAlwaysColumn();
            gift.HasOne<User>().WithMany().HasForeignKey(g => g.GiverId).OnDelete(DeleteBehavior.Cascade);
            gift.HasOne<User>().WithMany().HasForeignKey(g => g.ReceiverId).OnDelete(DeleteBehavior.Cascade);
            gift.HasIndex(g => new { g.GiverId, g.GivenAt });
        });

        modelBuilder.Entity<ClubPayday>(payday =>
        {
            payday.ToTable("club_paydays");
            payday.HasKey(p => p.Id);
            payday.Property(p => p.Id).UseIdentityAlwaysColumn();
            payday.Ignore(p => p.Total);
            payday.HasOne<User>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
            payday.HasIndex(p => new { p.UserId, p.PaidAt }).IsUnique();
        });

        modelBuilder.Entity<UserChatStyle>(style =>
        {
            style.ToTable("user_chat_styles");
            style.HasKey(s => new { s.UserId, s.StyleId });
            style.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SsoTicket>(ticket =>
        {
            ticket.ToTable("sso_tickets");
            ticket.HasKey(t => t.Ticket);
            ticket.Property(t => t.Ticket).HasMaxLength(64).IsRequired();
            ticket.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId);
            ticket.HasIndex(t => new { t.ExpiresAt, t.ConsumedAt });
        });

        modelBuilder.Entity<InviteCode>(invite =>
        {
            invite.ToTable("invite_codes");
            invite.HasKey(i => i.Code);
            invite.Property(i => i.Code).HasMaxLength(InviteCode.CodeLength).IsRequired();
            invite.Property(i => i.Note).HasMaxLength(InviteCode.NoteMaxLength).IsRequired();
            invite.Ignore(i => i.IsUsed);
            invite.HasOne(i => i.CreatedBy).WithMany().HasForeignKey(i => i.CreatedById).OnDelete(DeleteBehavior.SetNull);
            invite.HasOne(i => i.UsedBy).WithMany().HasForeignKey(i => i.UsedById).OnDelete(DeleteBehavior.SetNull);
            invite.HasIndex(i => i.CreatedAt);
        });

        modelBuilder.Entity<Perk>(perk =>
        {
            perk.ToTable("perks");
            perk.HasKey(p => p.Code);
            perk.Property(p => p.Code).HasMaxLength(64);
            perk.Property(p => p.ErrorMessage).HasMaxLength(256).IsRequired();
        });

        modelBuilder.Entity<Achievement>(achievement =>
        {
            achievement.ToTable("achievements");
            achievement.HasKey(a => a.Id);
            // Habbo's own ids, which the client keys its dialog on, so they are supplied rather
            // than handed out.
            achievement.Property(a => a.Id).ValueGeneratedNever();
            achievement.Property(a => a.Name).HasMaxLength(64).IsRequired();
            achievement.Property(a => a.Category).HasMaxLength(64).IsRequired();
            achievement.Property(a => a.SubCategory).HasMaxLength(64).IsRequired();
            achievement.HasMany(a => a.Levels)
                .WithOne(l => l.Achievement!)
                .HasForeignKey(l => l.AchievementId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AchievementLevel>(level =>
        {
            level.ToTable("achievement_levels");
            level.HasKey(l => l.Id);
            level.Property(l => l.BadgeCode).HasMaxLength(64).IsRequired();
            level.HasIndex(l => new { l.AchievementId, l.Level }).IsUnique();
        });

        modelBuilder.Entity<UserAchievement>(progress =>
        {
            progress.ToTable("user_achievements");
            progress.HasKey(p => new { p.UserId, p.AchievementId });
            progress.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            progress.HasOne(p => p.Achievement).WithMany().HasForeignKey(p => p.AchievementId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FriendCategory>(category =>
        {
            category.ToTable("friend_categories");
            category.HasKey(c => c.Id);
            category.Property(c => c.Name).HasMaxLength(64).IsRequired();
            category.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            category.HasIndex(c => c.UserId);
        });

        modelBuilder.Entity<Friendship>(friendship =>
        {
            friendship.ToTable("friendships");
            // One row per direction, so each side keeps its own category and relationship.
            friendship.HasKey(f => new { f.UserId, f.FriendId });
            friendship.HasOne(f => f.User).WithMany().HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            friendship.HasOne(f => f.Friend).WithMany().HasForeignKey(f => f.FriendId)
                .OnDelete(DeleteBehavior.Cascade);
            // Emptying a category leaves the friends, so they fall back to no category.
            friendship.HasOne(f => f.Category).WithMany().HasForeignKey(f => f.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<FriendRequest>(request =>
        {
            request.ToTable("friend_requests");
            request.HasKey(r => r.Id);
            // Asking twice is the same request, not a second one.
            request.HasIndex(r => new { r.RequesterId, r.TargetId }).IsUnique();
            request.HasIndex(r => r.TargetId);
            request.HasOne(r => r.Requester).WithMany().HasForeignKey(r => r.RequesterId)
                .OnDelete(DeleteBehavior.Cascade);
            request.HasOne(r => r.Target).WithMany().HasForeignKey(r => r.TargetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IgnoredUser>(ignored =>
        {
            ignored.ToTable("ignored_users");
            ignored.HasKey(i => new { i.UserId, i.IgnoredUserId });
            ignored.HasOne(i => i.User).WithMany().HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            ignored.HasOne(i => i.Ignored).WithMany().HasForeignKey(i => i.IgnoredUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BlockedUser>(blocked =>
        {
            blocked.ToTable("blocked_users");
            blocked.HasKey(b => new { b.UserId, b.BlockedUserId });
            blocked.HasOne(b => b.User).WithMany().HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            blocked.HasOne(b => b.Blocked).WithMany().HasForeignKey(b => b.BlockedUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Subscription>(subscription =>
        {
            subscription.ToTable("subscriptions");
            subscription.HasKey(s => s.Id);
            subscription.Property(s => s.ProductName).HasMaxLength(64).IsRequired();
            // One row per product per player, so a purchase extends rather than duplicates.
            subscription.HasIndex(s => new { s.UserId, s.ProductName }).IsUnique();
            subscription.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BuildersClubLimits>(limits =>
        {
            limits.ToTable("builders_club_limits");
            limits.HasKey(l => l.Id);
            limits.Property(l => l.ProductName).HasMaxLength(64).IsRequired();
            limits.HasIndex(l => l.ProductName).IsUnique();
        });

        modelBuilder.Entity<TimingSchedule>(schedule =>
        {
            schedule.ToTable("timing_schedules");
            schedule.HasKey(s => s.Id);
            schedule.Property(s => s.SchedulingKey).HasMaxLength(128).IsRequired();
            schedule.HasIndex(s => s.SchedulingKey).IsUnique();
            schedule.HasMany(s => s.Entries)
                .WithOne(e => e.Schedule!)
                .HasForeignKey(e => e.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TimingScheduleEntry>(entry =>
        {
            entry.ToTable("timing_schedule_entries");
            entry.HasKey(e => e.Id);
            entry.Property(e => e.Code).HasMaxLength(128).IsRequired();
            entry.HasIndex(e => new { e.ScheduleId, e.StartsAt });
        });

        modelBuilder.Entity<CallForHelpCategory>(category =>
        {
            category.ToTable("call_for_help_categories");
            category.HasKey(c => c.Id);
            category.Property(c => c.Name).HasMaxLength(128).IsRequired();
            category.HasMany(c => c.Topics)
                .WithOne(t => t.Category!)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CallForHelpTopic>(topic =>
        {
            topic.ToTable("call_for_help_topics");
            topic.HasKey(t => t.Id);
            topic.Property(t => t.Name).HasMaxLength(128).IsRequired();
            topic.Property(t => t.Consequence).HasMaxLength(256).IsRequired();
        });

        modelBuilder.Entity<WiredVariableValueRow>(value =>
        {
            value.ToTable("wired_variable_values");
            value.HasKey(v => v.Id);
            value.Property(v => v.Id).UseIdentityAlwaysColumn();
            value.Property(v => v.VariableId).HasMaxLength(WiredVariableValueRow.VariableIdMaxLength);
            value.HasOne<Room>().WithMany().HasForeignKey(v => v.RoomId).OnDelete(DeleteBehavior.Cascade);
            value.HasIndex(v => new { v.RoomId, v.VariableId, v.ItemId, v.UserId }).IsUnique().AreNullsDistinct(false);
        });

        modelBuilder.Entity<WiredRoomLog>(log =>
        {
            log.ToTable("wired_room_logs");
            log.HasKey(l => l.Id);
            log.Property(l => l.Id).UseIdentityAlwaysColumn();
            log.Property(l => l.Message).HasMaxLength(WiredRoomLog.MessageMaxLength);
            log.HasOne(l => l.Room).WithMany().HasForeignKey(l => l.RoomId).OnDelete(DeleteBehavior.Cascade);
            log.HasIndex(l => new { l.RoomId, l.Id });
        });

        modelBuilder.Entity<RoomRight>(right =>
        {
            right.ToTable("room_rights");
            right.HasKey(r => new { r.RoomId, r.UserId });
            right.HasOne(r => r.Room).WithMany().HasForeignKey(r => r.RoomId).OnDelete(DeleteBehavior.Cascade);
            right.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoomFavourite>(favourite =>
        {
            favourite.ToTable("room_favourites");
            favourite.HasKey(f => f.Id);
            favourite.Property(f => f.Id).UseIdentityAlwaysColumn();
            favourite.HasOne(f => f.User).WithMany().HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Cascade);
            favourite.HasOne(f => f.Room).WithMany().HasForeignKey(f => f.RoomId).OnDelete(DeleteBehavior.Cascade);
            favourite.HasIndex(f => new { f.UserId, f.RoomId }).IsUnique();
        });

        modelBuilder.Entity<RoomVote>(vote =>
        {
            vote.ToTable("room_votes");
            vote.HasKey(v => new { v.RoomId, v.UserId });
            vote.HasOne<Room>().WithMany().HasForeignKey(v => v.RoomId).OnDelete(DeleteBehavior.Cascade);
            vote.HasOne<User>().WithMany().HasForeignKey(v => v.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoomEvent>(roomEvent =>
        {
            roomEvent.ToTable("room_events");
            roomEvent.HasKey(e => e.Id);
            roomEvent.Property(e => e.Id).UseIdentityAlwaysColumn();
            roomEvent.Property(e => e.Name).HasMaxLength(RoomEvent.NameMaxLength);
            roomEvent.Property(e => e.Description).HasMaxLength(RoomEvent.DescriptionMaxLength);
            roomEvent.HasOne(e => e.Room).WithMany().HasForeignKey(e => e.RoomId).OnDelete(DeleteBehavior.Cascade);
            roomEvent.HasOne(e => e.Owner).WithMany().HasForeignKey(e => e.OwnerId).OnDelete(DeleteBehavior.Cascade);
            roomEvent.HasOne<RoomEventCategory>().WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);
            roomEvent.HasIndex(e => new { e.RoomId, e.ExpiresAt });
            roomEvent.HasIndex(e => e.ExpiresAt);
        });

        modelBuilder.Entity<RoomAdInteraction>(interaction =>
        {
            interaction.ToTable("room_ad_interactions");
            interaction.HasKey(i => i.Id);
            interaction.Property(i => i.Id).UseIdentityAlwaysColumn();
            interaction.Property(i => i.Source).HasMaxLength(64);
            interaction.Property(i => i.Position).HasMaxLength(16);
            interaction.HasOne<User>().WithMany().HasForeignKey(i => i.UserId).OnDelete(DeleteBehavior.Cascade);
            interaction.HasOne<RoomEvent>().WithMany().HasForeignKey(i => i.EventId).OnDelete(DeleteBehavior.Cascade);
            interaction.HasIndex(i => new { i.EventId, i.Kind });
        });

        modelBuilder.Entity<CompetitionRoomEntry>(entry =>
        {
            entry.ToTable("competition_room_entries");
            entry.HasKey(e => e.Id);
            entry.Property(e => e.Id).UseIdentityAlwaysColumn();
            entry.HasOne(e => e.Room).WithMany().HasForeignKey(e => e.RoomId).OnDelete(DeleteBehavior.Cascade);
            entry.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entry.HasIndex(e => new { e.GoalId, e.RoomId }).IsUnique();
        });

        modelBuilder.Entity<OfficialRoomEntry>(entry =>
        {
            entry.ToTable("official_room_entries");
            entry.HasKey(e => e.Id);
            entry.Property(e => e.Id).UseIdentityAlwaysColumn();
            entry.Property(e => e.PopupCaption).HasMaxLength(128);
            entry.Property(e => e.PopupDescription).HasMaxLength(512);
            entry.Property(e => e.PicText).HasMaxLength(128);
            entry.Property(e => e.PicRef).HasMaxLength(256);
            entry.Property(e => e.Tag).HasMaxLength(64);
            entry.HasOne<OfficialRoomEntry>().WithMany().HasForeignKey(e => e.FolderId).OnDelete(DeleteBehavior.Cascade);
            entry.HasOne(e => e.Room).WithMany().HasForeignKey(e => e.RoomId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoomNetwork>(network =>
        {
            network.ToTable("room_networks");
            network.HasKey(n => n.Id);
            network.Property(n => n.Id).ValueGeneratedNever();
            network.Property(n => n.Name).HasMaxLength(64);
            network.PrimitiveCollection(n => n.RoomIds);
        });

        modelBuilder.Entity<ForwardRoom>(forward =>
        {
            forward.ToTable("forward_rooms");
            forward.HasKey(f => f.Type);
            forward.Property(f => f.Type).HasMaxLength(64);
            forward.HasOne<Room>().WithMany().HasForeignKey(f => f.RoomId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AvatarEffect>(effect =>
        {
            effect.ToTable("avatar_effects");
            effect.HasKey(e => e.Id);
            effect.Property(e => e.Id).ValueGeneratedNever();
            effect.Property(e => e.Library).HasMaxLength(64);
            effect.Property(e => e.Type).HasMaxLength(16);
            effect.HasIndex(e => e.Library);
        });

        modelBuilder.Entity<ChatGesture>(gesture =>
        {
            gesture.ToTable("chat_gestures");
            gesture.HasKey(g => g.Emoticon);
            gesture.Property(g => g.Emoticon).HasMaxLength(16);
        });

        modelBuilder.Entity<NavigatorTab>(tab =>
        {
            tab.ToTable("navigator_tabs");
            tab.HasKey(t => t.SearchCode);
            tab.Property(t => t.SearchCode).HasMaxLength(64);
            tab.HasMany(t => t.QuickLinks).WithOne(q => q.Tab!).HasForeignKey(q => q.TabSearchCode);
        });

        modelBuilder.Entity<NavigatorQuickLink>(link =>
        {
            link.ToTable("navigator_quick_links");
            link.HasKey(q => q.Id);
            link.Property(q => q.Id).UseIdentityAlwaysColumn();
            link.Property(q => q.TabSearchCode).HasMaxLength(64).IsRequired();
            link.Property(q => q.SearchCode).HasMaxLength(64).IsRequired();
            link.Property(q => q.Filter).HasMaxLength(128).IsRequired();
            link.Property(q => q.Localization).HasMaxLength(128).IsRequired();
        });

        modelBuilder.Entity<NavigatorBlock>(block =>
        {
            block.ToTable("navigator_blocks");
            block.HasKey(b => b.Id);
            block.Property(b => b.Id).UseIdentityAlwaysColumn();
            block.Property(b => b.RequestCode).HasMaxLength(64).IsRequired();
            block.Property(b => b.BlockCode).HasMaxLength(64).IsRequired();
            block.Property(b => b.Text).HasMaxLength(128).IsRequired();
            block.Property(b => b.Query).HasMaxLength(32).IsRequired();
            block.HasIndex(b => b.RequestCode);
        });

        modelBuilder.Entity<NavigatorCategory>(category =>
        {
            category.ToTable("navigator_categories");
            category.HasKey(c => c.Id);
            category.Property(c => c.Id).UseIdentityAlwaysColumn();
            category.Property(c => c.NodeName).HasMaxLength(128).IsRequired();
            category.Property(c => c.AutomaticCategoryKey).HasMaxLength(64).IsRequired();
            category.Property(c => c.GlobalCategoryKey).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<SavedNavigatorSearch>(search =>
        {
            search.ToTable("navigator_saved_searches");
            search.HasKey(s => s.Id);
            search.Property(s => s.Id).UseIdentityAlwaysColumn();
            search.Property(s => s.SearchCode).HasMaxLength(SavedNavigatorSearch.SearchCodeMaxLength);
            search.Property(s => s.Filter).HasMaxLength(SavedNavigatorSearch.FilterMaxLength);
            search.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
            search.HasIndex(s => s.UserId);
        });

        modelBuilder.Entity<NavigatorCollapsedCategory>(collapsed =>
        {
            collapsed.ToTable("navigator_collapsed_categories");
            collapsed.HasKey(c => new { c.UserId, c.Category });
            collapsed.Property(c => c.Category).HasMaxLength(NavigatorCollapsedCategory.CategoryMaxLength);
            collapsed.HasOne<User>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NavigatorViewMode>(mode =>
        {
            mode.ToTable("navigator_view_modes");
            mode.HasKey(m => new { m.UserId, m.SearchCode });
            mode.Property(m => m.SearchCode).HasMaxLength(SavedNavigatorSearch.SearchCodeMaxLength);
            mode.HasOne<User>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NavigatorWindowPreferences>(window =>
        {
            window.ToTable("navigator_window_preferences");
            window.HasKey(w => w.UserId);
            window.HasOne<User>().WithOne().HasForeignKey<NavigatorWindowPreferences>(w => w.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LiftedRoom>(lifted =>
        {
            lifted.ToTable("lifted_rooms");
            lifted.HasKey(l => l.Id);
            lifted.Property(l => l.Id).UseIdentityAlwaysColumn();
            lifted.Property(l => l.Image).HasMaxLength(256);
            lifted.Property(l => l.Caption).HasMaxLength(128);
            lifted.HasOne<Room>().WithMany().HasForeignKey(l => l.RoomId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoomEventCategory>(category =>
        {
            category.ToTable("room_event_categories");
            category.HasKey(c => c.Id);
            category.Property(c => c.Id).ValueGeneratedNever();
            category.Property(c => c.Name).HasMaxLength(128);
        });

        modelBuilder.Entity<PromotedRoomCategory>(category =>
        {
            category.ToTable("promoted_room_categories");
            category.HasKey(c => c.Code);
            category.Property(c => c.Code).HasMaxLength(64);
            category.PrimitiveCollection(c => c.RoomIds);
        });

        modelBuilder.Entity<CatalogPage>(page =>
        {
            page.ToTable("catalog_pages");
            page.HasKey(p => p.Id);

            // By default rather than always: a dumped page keeps Habbo's own id, and a page
            // the hotel creates itself gets the next one from the sequence.
            page.Property(p => p.Id).UseIdentityByDefaultColumn();
            page.Property(p => p.PageName).HasMaxLength(64).IsRequired();
            page.Property(p => p.Localization).HasMaxLength(128).IsRequired();
            page.Property(p => p.LayoutCode).HasMaxLength(64).IsRequired();

            // A page whose parent goes away takes its children with it.
            page.HasOne(p => p.Parent).WithMany(p => p.Children)
                .HasForeignKey(p => p.ParentId).OnDelete(DeleteBehavior.Cascade);
            page.HasIndex(p => p.ParentId);
        });

        modelBuilder.Entity<CatalogFrontPageItem>(item =>
        {
            item.ToTable("catalog_front_page_items");
            item.HasKey(i => i.Id);
            item.Property(i => i.Name).HasMaxLength(128).IsRequired();
            item.Property(i => i.PromoImage).HasMaxLength(256).IsRequired();
            item.Property(i => i.PageName).HasMaxLength(64);
            item.Property(i => i.ProductCode).HasMaxLength(64);
        });

        modelBuilder.Entity<CatalogOffer>(offer =>
        {
            offer.ToTable("catalog_offers");
            offer.HasKey(o => o.Id);
            // As with pages: a dumped offer keeps the id the client buys it by, and an offer
            // the hotel writes itself is numbered by the sequence.
            offer.Property(o => o.Id).UseIdentityByDefaultColumn();
            offer.Property(o => o.LocalizationId).HasMaxLength(128).IsRequired();

            offer.Property(o => o.PreviewImage).HasMaxLength(256).IsRequired();
        });

        modelBuilder.Entity<CatalogListing>(listing =>
        {
            // Habbo lists one offer under several pages, and at a different position on each,
            // so the link is a table in its own right rather than a page id on the offer.
            listing.ToTable("catalog_page_offers");
            listing.HasKey(l => new { l.PageId, l.OfferId });

            listing.HasOne(l => l.Page).WithMany(p => p.Listings)
                .HasForeignKey(l => l.PageId).OnDelete(DeleteBehavior.Cascade);
            listing.HasOne(l => l.Offer).WithMany(o => o.Listings)
                .HasForeignKey(l => l.OfferId).OnDelete(DeleteBehavior.Cascade);

            listing.HasIndex(l => l.OfferId);
        });

        modelBuilder.Entity<CatalogProduct>(product =>
        {
            product.ToTable("catalog_products");
            product.HasKey(p => p.Id);
            product.Property(p => p.Id).UseIdentityAlwaysColumn();
            product.Property(p => p.ExtraParam).HasMaxLength(128).IsRequired();
            product.HasOne(p => p.Offer).WithMany(o => o.Products)
                .HasForeignKey(p => p.OfferId).OnDelete(DeleteBehavior.Cascade);

            // A badge product has no definition, so the key stays nullable; removing a
            // definition that is still sold should fail loudly rather than empty the offer.
            product.HasOne(p => p.Definition).WithMany()
                .HasForeignKey(p => p.DefinitionId).OnDelete(DeleteBehavior.Restrict);
            product.HasIndex(p => p.OfferId);
        });

        // Every furni_* table hangs off the item and dies with it, so each one cascades.
        // They are separate tables rather than one payload column because the layouts have
        // genuinely different shapes — two scalars, three collections, one nested collection.
        modelBuilder.Entity<FurniState>(state =>
        {
            state.ToTable("furni_states");
            state.HasKey(s => s.ItemId);
            state.Property(s => s.State).HasMaxLength(512).IsRequired();
            state.HasOne(s => s.Item).WithOne().HasForeignKey<FurniState>(s => s.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FurniMapEntry>(entry =>
        {
            entry.ToTable("furni_map_entries");

            // Keyed by the pair: the client reads these into a map, where a repeated key would
            // only overwrite itself.
            entry.HasKey(e => new { e.ItemId, e.Key });
            entry.Property(e => e.Key).HasMaxLength(128);
            entry.Property(e => e.Value).HasMaxLength(512).IsRequired();
            entry.HasOne(e => e.Item).WithMany().HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FurniStringEntry>(entry =>
        {
            entry.ToTable("furni_string_entries");
            entry.HasKey(e => new { e.ItemId, e.Position });
            entry.Property(e => e.Value).HasMaxLength(512).IsRequired();
            entry.HasOne(e => e.Item).WithMany().HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FurniIntEntry>(entry =>
        {
            entry.ToTable("furni_int_entries");
            entry.HasKey(e => new { e.ItemId, e.Position });
            entry.HasOne(e => e.Item).WithMany().HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FurniVoteResult>(vote =>
        {
            vote.ToTable("furni_vote_results");
            vote.HasKey(v => v.ItemId);
            vote.HasOne(v => v.Item).WithOne().HasForeignKey<FurniVoteResult>(v => v.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FurniCrackable>(crackable =>
        {
            crackable.ToTable("furni_crackables");
            crackable.HasKey(c => c.ItemId);
            crackable.HasOne(c => c.Item).WithOne().HasForeignKey<FurniCrackable>(c => c.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FurniHighScore>(board =>
        {
            board.ToTable("furni_high_scores");
            board.HasKey(h => h.ItemId);
            board.HasOne(h => h.Item).WithOne().HasForeignKey<FurniHighScore>(h => h.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FurniHighScoreEntry>(entry =>
        {
            entry.ToTable("furni_high_score_entries");
            entry.HasKey(e => e.Id);
            entry.Property(e => e.Id).UseIdentityAlwaysColumn();
            entry.HasOne(e => e.HighScore).WithMany(h => h.Entries).HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entry.HasIndex(e => new { e.ItemId, e.Position }).IsUnique();
        });

        // A trophy's engraving belongs to the trophy behaviour rather than to a data layout:
        // the layout on the wire is a plain legacy string, built from these columns.
        modelBuilder.Entity<FurniDimmerPreset>(preset =>
        {
            preset.ToTable("furni_dimmer_presets");
            preset.HasKey(p => new { p.ItemId, p.PresetId });
            preset.Property(p => p.ColorHex).HasMaxLength(FurniDimmerPreset.ColorMaxLength);
            preset.HasOne(p => p.Item).WithMany().HasForeignKey(p => p.ItemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FurniStackHeight>(stack =>
        {
            stack.ToTable("furni_stack_heights");
            stack.HasKey(s => s.ItemId);
            stack.HasOne(s => s.Item).WithOne().HasForeignKey<FurniStackHeight>(s => s.ItemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BonusRareProgress>(progress =>
        {
            progress.ToTable("bonus_rare_progress");
            progress.HasKey(p => p.UserId);
            progress.HasOne<User>().WithOne().HasForeignKey<BonusRareProgress>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TargetedOffer>(offer =>
        {
            offer.ToTable("targeted_offers");
            offer.HasKey(o => o.Id);
            offer.Property(o => o.Id).UseIdentityAlwaysColumn();
            offer.Property(o => o.Identifier).HasMaxLength(64);
            offer.Property(o => o.Title).HasMaxLength(128);
            offer.Property(o => o.Description).HasMaxLength(512);
            offer.Property(o => o.ImageUrl).HasMaxLength(256);
            offer.Property(o => o.IconImageUrl).HasMaxLength(256);
            offer.PrimitiveCollection(o => o.SubProductCodes);
            offer.HasOne<CatalogOffer>().WithMany().HasForeignKey(o => o.OfferId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TargetedOfferState>(state =>
        {
            state.ToTable("targeted_offer_states");
            state.HasKey(s => new { s.UserId, s.TargetedOfferId });
            state.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
            state.HasOne<TargetedOffer>().WithMany().HasForeignKey(s => s.TargetedOfferId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habbo.Domain.Marketplace.MarketplaceOffer>(offer =>
        {
            offer.ToTable("marketplace_offers");
            offer.HasKey(o => o.Id);
            offer.Property(o => o.Id).UseIdentityAlwaysColumn();
            offer.HasOne(o => o.Item).WithMany().HasForeignKey(o => o.ItemId).OnDelete(DeleteBehavior.SetNull);
            offer.HasOne(o => o.Definition).WithMany().HasForeignKey(o => o.DefinitionId).OnDelete(DeleteBehavior.Restrict);
            offer.HasOne<User>().WithMany().HasForeignKey(o => o.SellerId).OnDelete(DeleteBehavior.Cascade);
            offer.HasIndex(o => new { o.SoldAt, o.ExpiresAt });
            offer.HasIndex(o => o.SellerId);
            offer.HasIndex(o => o.DefinitionId);
            // an item is on sale in one offer at a time
            offer.HasIndex(o => o.ItemId).IsUnique().HasFilter("\"SoldAt\" IS NULL");
        });

        modelBuilder.Entity<Habbo.Domain.Marketplace.MarketplaceTokens>(tokens =>
        {
            tokens.ToTable("marketplace_tokens");
            tokens.HasKey(t => t.UserId);
            tokens.HasOne<User>().WithOne().HasForeignKey<Habbo.Domain.Marketplace.MarketplaceTokens>(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habbo.Domain.Recycler.RecyclerPrizeLevel>(level =>
        {
            level.ToTable("recycler_prize_levels");
            level.HasKey(l => l.Id);
            level.Property(l => l.Id).ValueGeneratedNever();
            level.HasMany(l => l.Prizes).WithOne().HasForeignKey(p => p.LevelId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habbo.Domain.Recycler.RecyclerPrize>(prize =>
        {
            prize.ToTable("recycler_prizes");
            prize.HasKey(p => p.Id);
            prize.Property(p => p.Id).UseIdentityAlwaysColumn();
            prize.HasOne(p => p.Definition).WithMany().HasForeignKey(p => p.DefinitionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habbo.Domain.Recycler.RecyclerUse>(use =>
        {
            use.ToTable("recycler_uses");
            use.HasKey(u => u.UserId);
            use.HasOne<User>().WithOne().HasForeignKey<Habbo.Domain.Recycler.RecyclerUse>(u => u.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habbo.Domain.Crafting.CraftingRecipe>(recipe =>
        {
            recipe.ToTable("crafting_recipes");
            recipe.HasKey(r => r.Id);
            recipe.Property(r => r.Id).UseIdentityAlwaysColumn();
            recipe.Property(r => r.Code).HasMaxLength(Habbo.Domain.Crafting.CraftingRecipe.CodeMaxLength);
            recipe.HasIndex(r => r.Code).IsUnique();
            recipe.HasOne(r => r.Product).WithMany().HasForeignKey(r => r.ProductDefinitionId).OnDelete(DeleteBehavior.Cascade);
            recipe.HasOne<Habbo.Domain.Furniture.ItemDefinition>().WithMany().HasForeignKey(r => r.AltarDefinitionId).OnDelete(DeleteBehavior.Cascade);
            recipe.HasMany(r => r.Ingredients).WithOne().HasForeignKey(i => i.RecipeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habbo.Domain.Crafting.CraftingIngredient>(ingredient =>
        {
            ingredient.ToTable("crafting_ingredients");
            ingredient.HasKey(i => new { i.RecipeId, i.DefinitionId });
            ingredient.HasOne(i => i.Definition).WithMany().HasForeignKey(i => i.DefinitionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habbo.Domain.Bots.BotType>(type =>
        {
            type.ToTable("bot_types");
            type.HasKey(t => t.Id);
            type.Property(t => t.Id).UseIdentityAlwaysColumn();
            type.Property(t => t.Code).HasMaxLength(Habbo.Domain.Bots.BotType.CodeMaxLength);
            type.HasIndex(t => t.Code).IsUnique();
            type.Property(t => t.Gender).HasMaxLength(1)
                .HasConversion(gender => gender.ToLetter(), letter => GenderExtensions.ParseLetter(letter));
        });

        modelBuilder.Entity<Habbo.Domain.Bots.Bot>(bot =>
        {
            bot.ToTable("bots");
            bot.HasKey(b => b.Id);
            bot.Property(b => b.Id).UseIdentityAlwaysColumn();
            bot.Property(b => b.Name).HasMaxLength(Habbo.Domain.Bots.Bot.NameMaxLength);
            bot.Property(b => b.Motto).HasMaxLength(Habbo.Domain.Bots.Bot.MottoMaxLength);
            bot.Property(b => b.CreatedAt).HasDefaultValueSql("now()");
            bot.Property(b => b.Gender).HasMaxLength(1)
                .HasConversion(gender => gender.ToLetter(), letter => GenderExtensions.ParseLetter(letter));
            bot.HasOne(b => b.Owner).WithMany().HasForeignKey(b => b.OwnerId).OnDelete(DeleteBehavior.Cascade);
            bot.HasOne(b => b.Type).WithMany().HasForeignKey(b => b.TypeId).OnDelete(DeleteBehavior.Restrict);
            bot.HasOne(b => b.Room).WithMany().HasForeignKey(b => b.RoomId).OnDelete(DeleteBehavior.SetNull);
            bot.HasIndex(b => new { b.OwnerId, b.RoomId });
            bot.HasIndex(b => b.RoomId);
        });

        modelBuilder.Entity<Habbo.Domain.Pets.PetType>(type =>
        {
            type.ToTable("pet_types");
            type.HasKey(t => t.Id);
            type.Property(t => t.Id).ValueGeneratedNever();
            type.HasMany(t => t.Palettes).WithOne().HasForeignKey(p => p.TypeId).OnDelete(DeleteBehavior.Cascade);
            type.HasMany(t => t.Layers).WithOne().HasForeignKey(l => l.TypeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habbo.Domain.Pets.PetPackage>(package =>
        {
            package.ToTable("pet_packages");
            package.HasKey(p => p.DefinitionId);
            package.HasOne(p => p.Definition).WithOne().HasForeignKey<Habbo.Domain.Pets.PetPackage>(p => p.DefinitionId).OnDelete(DeleteBehavior.Cascade);
            package.HasOne<Habbo.Domain.Pets.PetType>().WithMany().HasForeignKey(p => p.TypeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habbo.Domain.Pets.PetRarityCategory>(category =>
        {
            category.ToTable("pet_rarity_categories");
            category.HasKey(c => new { c.BabyTypeId, c.Rank });
            category.HasOne<Habbo.Domain.Pets.PetType>().WithMany().HasForeignKey(c => c.BabyTypeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habbo.Domain.Pets.PlantTrait>(trait =>
        {
            trait.ToTable("plant_traits");
            trait.HasKey(t => new { t.Tag, t.Value });
            trait.Property(t => t.Tag).HasMaxLength(32);
        });

        modelBuilder.Entity<Habbo.Domain.Pets.PlantSeed>(seed =>
        {
            seed.ToTable("plant_seeds");
            seed.HasKey(s => s.ItemId);
            seed.HasOne(s => s.Item).WithOne().HasForeignKey<Habbo.Domain.Pets.PlantSeed>(s => s.ItemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habbo.Domain.Pets.PetLayer>(layer =>
        {
            layer.ToTable("pet_layers");
            layer.HasKey(l => new { l.TypeId, l.LayerId });
        });

        modelBuilder.Entity<Habbo.Domain.Pets.PetPalette>(palette =>
        {
            palette.ToTable("pet_palettes");
            palette.HasKey(p => new { p.TypeId, p.PaletteId });
        });

        modelBuilder.Entity<Habbo.Domain.Pets.PetCommand>(command =>
        {
            command.ToTable("pet_commands");
            command.HasKey(c => c.Id);
            command.Property(c => c.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Habbo.Domain.Pets.Pet>(pet =>
        {
            pet.ToTable("pets");
            pet.HasKey(p => p.Id);
            pet.Property(p => p.Id).UseIdentityAlwaysColumn();
            pet.Property(p => p.Name).HasMaxLength(Habbo.Domain.Pets.Pet.NameMaxLength);
            pet.Property(p => p.Color).HasMaxLength(Habbo.Domain.Pets.Pet.ColorLength);
            pet.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
            pet.HasOne(p => p.Owner).WithMany().HasForeignKey(p => p.OwnerId).OnDelete(DeleteBehavior.Cascade);
            pet.HasOne(p => p.Type).WithMany().HasForeignKey(p => p.TypeId).OnDelete(DeleteBehavior.Restrict);
            pet.HasOne(p => p.Room).WithMany().HasForeignKey(p => p.RoomId).OnDelete(DeleteBehavior.SetNull);
            pet.HasOne<Habbo.Domain.Furniture.ItemDefinition>().WithMany().HasForeignKey(p => p.SaddleDefinitionId).OnDelete(DeleteBehavior.SetNull);
            pet.HasIndex(p => new { p.OwnerId, p.RoomId });
            pet.HasIndex(p => p.RoomId);
        });

        modelBuilder.Entity<Habbo.Domain.Support.Quiz>(quiz =>
        {
            quiz.ToTable("quizzes");
            quiz.HasKey(q => q.Code);
            quiz.Property(q => q.Code).HasMaxLength(Habbo.Domain.Support.Quiz.CodeMaxLength);
            quiz.HasMany(q => q.Questions).WithOne().HasForeignKey(q => q.QuizCode).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habbo.Domain.Campaigns.CampaignCalendar>(calendar =>
        {
            calendar.ToTable("campaign_calendars");
            calendar.HasKey(c => c.Name);
            calendar.Property(c => c.Name).HasMaxLength(Habbo.Domain.Campaigns.CampaignCalendar.NameMaxLength);
            calendar.HasMany(c => c.Doors).WithOne().HasForeignKey(d => d.CampaignName).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habbo.Domain.Campaigns.CampaignCalendarDoor>(door =>
        {
            door.ToTable("campaign_calendar_doors");
            door.HasKey(d => new { d.CampaignName, d.Day });
        });

        modelBuilder.Entity<Habbo.Domain.Campaigns.CampaignCalendarOpening>(opening =>
        {
            opening.ToTable("campaign_calendar_openings");
            opening.HasKey(o => new { o.UserId, o.CampaignName, o.Day });
            opening.HasOne<User>().WithMany().HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Cascade);
            opening.HasOne<Habbo.Domain.Campaigns.CampaignCalendar>().WithMany().HasForeignKey(o => o.CampaignName).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habbo.Domain.Furniture.Chests.ChestCoins>(coins =>
        {
            coins.ToTable("chest_coins");
            coins.HasKey(c => c.ItemId);
            coins.Property(c => c.ItemId).ValueGeneratedNever();
            coins.HasOne<UserItem>().WithOne().HasForeignKey<Habbo.Domain.Furniture.Chests.ChestCoins>(c => c.ItemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habbo.Domain.Furniture.Chests.ChestTransaction>(transaction =>
        {
            transaction.ToTable("chest_transactions");
            transaction.HasKey(t => t.Id);
            transaction.Property(t => t.Id).UseIdentityAlwaysColumn();
            transaction.Property(t => t.Info).HasMaxLength(Habbo.Domain.Furniture.Chests.ChestTransaction.InfoMaxLength);
            transaction.HasMany(t => t.Items).WithOne().HasForeignKey(i => i.TransactionId).OnDelete(DeleteBehavior.Cascade);
            transaction.HasIndex(t => t.RoomId);
        });

        modelBuilder.Entity<Habbo.Domain.Furniture.Chests.ChestTransactionItem>(item =>
        {
            item.ToTable("chest_transaction_items");
            item.HasKey(i => new { i.TransactionId, i.Deposited, i.IsWallItem, i.TypeId, i.PosterId });
            item.Property(i => i.PosterId).HasMaxLength(Habbo.Domain.Furniture.Chests.ChestTransactionItem.PosterIdMaxLength);
        });

        modelBuilder.Entity<Habbo.Domain.Furniture.Chests.WiredContract>(contract =>
        {
            contract.ToTable("wired_contracts");
            contract.HasKey(c => c.ItemId);
            contract.Property(c => c.ItemId).ValueGeneratedNever();
            contract.HasOne<UserItem>().WithOne().HasForeignKey<Habbo.Domain.Furniture.Chests.WiredContract>(c => c.ItemId).OnDelete(DeleteBehavior.Cascade);

            // the rules nest, and are only ever read and written whole
            contract.Property(c => c.YouGive).StoredAsJson();
            contract.Property(c => c.YouGet).StoredAsJson();
            contract.Property(c => c.ReceiveText).HasMaxLength(Habbo.Domain.Furniture.Chests.WiredContract.ReceiveTextMaxLength);
            contract.Property(c => c.RewardText).HasMaxLength(Habbo.Domain.Furniture.Chests.WiredContract.RewardTextMaxLength);
        });

        modelBuilder.Entity<Habbo.Domain.Games.SnowWarGameResult>(result =>
        {
            result.ToTable("snowwar_game_results");
            result.HasKey(r => r.Id);
            result.Property(r => r.Id).UseIdentityAlwaysColumn();
            result.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
            result.HasIndex(r => new { r.UserId, r.PlayedAt });
            result.HasIndex(r => r.PlayedAt);
        });

        modelBuilder.Entity<IncomeReward>(reward =>
        {
            reward.ToTable("income_rewards");
            reward.HasKey(r => r.Id);
            reward.Property(r => r.Id).UseIdentityAlwaysColumn();
            reward.Property(r => r.ProductCode).HasMaxLength(IncomeReward.ProductCodeMaxLength);
            reward.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
            reward.HasIndex(r => r.UserId);
        });

        modelBuilder.Entity<DeferredNotification>(notification =>
        {
            notification.ToTable("deferred_notifications");
            notification.HasKey(n => n.Id);
            notification.Property(n => n.Id).UseIdentityAlwaysColumn();
            notification.HasOne<User>().WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
            notification.HasIndex(n => n.UserId);
        });

        modelBuilder.Entity<Habbo.Domain.Catalog.ProductClaim>(claim =>
        {
            claim.ToTable("product_claims");
            claim.HasKey(c => c.Id);
            claim.Property(c => c.Id).HasMaxLength(Habbo.Domain.Catalog.ProductClaim.IdMaxLength);
        });

        modelBuilder.Entity<Habbo.Domain.Catalog.ProductClaimRecord>(record =>
        {
            record.ToTable("product_claim_records");
            record.HasKey(r => new { r.UserId, r.ClaimId });
            record.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
            record.HasOne<Habbo.Domain.Catalog.ProductClaim>().WithMany().HasForeignKey(r => r.ClaimId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habbo.Domain.Camera.Photo>(photo =>
        {
            photo.ToTable("photos");
            photo.HasKey(p => p.Id);
            photo.Property(p => p.Id).ValueGeneratedNever();
            photo.HasOne<User>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
            // a player's latest photo is the one bought or published
            photo.HasIndex(p => new { p.UserId, p.CreatedAt });
        });

        modelBuilder.Entity<Habbo.Domain.LandingView.PromoArticle>(article =>
        {
            article.ToTable("promo_articles");
            article.HasKey(a => a.Id);
            article.Property(a => a.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<UserFilterWord>(word =>
        {
            word.ToTable("user_filter_words");
            word.HasKey(w => new { w.UserId, w.Word });
            word.Property(w => w.Word).HasMaxLength(UserFilterWord.WordMaxLength);
            word.HasOne<User>().WithMany().HasForeignKey(w => w.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habbo.Domain.Polls.Poll>(poll =>
        {
            poll.ToTable("polls");
            poll.HasKey(p => p.Id);
            poll.Property(p => p.Id).ValueGeneratedNever();
            poll.HasOne<Habbo.Domain.Rooms.Room>().WithMany().HasForeignKey(p => p.RoomId).OnDelete(DeleteBehavior.Cascade);
            poll.HasMany(p => p.Questions).WithOne().HasForeignKey(q => q.PollId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habbo.Domain.Polls.PollQuestion>(question =>
        {
            question.ToTable("poll_questions");
            question.HasKey(q => q.Id);
            question.Property(q => q.Id).ValueGeneratedNever();
            question.HasMany(q => q.Choices).WithOne().HasForeignKey(c => c.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habbo.Domain.Polls.PollChoice>(choice =>
        {
            choice.ToTable("poll_choices");
            choice.HasKey(c => new { c.QuestionId, c.Position });
        });

        modelBuilder.Entity<Habbo.Domain.Polls.PollResponse>(response =>
        {
            response.ToTable("poll_responses");
            response.HasKey(r => r.Id);
            response.Property(r => r.Id).UseIdentityAlwaysColumn();
            response.HasOne<Habbo.Domain.Polls.Poll>().WithMany().HasForeignKey(r => r.PollId).OnDelete(DeleteBehavior.Cascade);
            response.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
            response.HasIndex(r => new { r.PollId, r.UserId });
        });

        modelBuilder.Entity<Habbo.Domain.Furniture.Youtube.YoutubePlaylist>(playlist =>
        {
            playlist.ToTable("youtube_playlists");
            playlist.HasKey(p => p.Id);
            playlist.Property(p => p.Id).HasMaxLength(Habbo.Domain.Furniture.Youtube.YoutubePlaylist.IdMaxLength);
            playlist.HasMany(p => p.Videos).WithOne().HasForeignKey(v => v.PlaylistId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habbo.Domain.Furniture.Youtube.YoutubeVideo>(video =>
        {
            video.ToTable("youtube_videos");
            video.HasKey(v => new { v.PlaylistId, v.Position });
        });

        modelBuilder.Entity<Habbo.Domain.Furniture.Youtube.YoutubeDisplayState>(state =>
        {
            state.ToTable("youtube_display_states");
            state.HasKey(s => s.ItemId);
            state.HasOne(s => s.Item).WithOne().HasForeignKey<Habbo.Domain.Furniture.Youtube.YoutubeDisplayState>(s => s.ItemId).OnDelete(DeleteBehavior.Cascade);
            state.HasOne<Habbo.Domain.Furniture.Youtube.YoutubePlaylist>().WithMany().HasForeignKey(s => s.PlaylistId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habbo.Domain.Furniture.Data.RentableSpaceRental>(rental =>
        {
            rental.ToTable("rentable_space_rentals");
            rental.HasKey(r => r.ItemId);
            rental.HasOne(r => r.Item).WithOne().HasForeignKey<Habbo.Domain.Furniture.Data.RentableSpaceRental>(r => r.ItemId).OnDelete(DeleteBehavior.Cascade);
            rental.HasOne(r => r.Renter).WithMany().HasForeignKey(r => r.RenterId).OnDelete(DeleteBehavior.Cascade);
            rental.HasIndex(r => r.RenterId);
        });

        modelBuilder.Entity<Habbo.Domain.Support.ChatReviewRecord>(record =>
        {
            record.ToTable("chat_review_records");
            record.HasKey(r => r.Id);
            record.Property(r => r.Id).UseIdentityAlwaysColumn();
            record.HasOne<User>().WithMany().HasForeignKey(r => r.ReporterId).OnDelete(DeleteBehavior.Cascade);
            record.HasOne<User>().WithMany().HasForeignKey(r => r.ReportedId).OnDelete(DeleteBehavior.Cascade);
            record.HasIndex(r => new { r.ReporterId, r.CreatedAt });
        });

        modelBuilder.Entity<Habbo.Domain.Support.QuizQuestion>(question =>
        {
            question.ToTable("quiz_questions");
            question.HasKey(q => new { q.QuizCode, q.QuestionId });
        });

        modelBuilder.Entity<Voucher>(voucher =>
        {
            voucher.ToTable("vouchers");
            voucher.HasKey(v => v.Code);
            voucher.Property(v => v.Code).HasMaxLength(Voucher.CodeMaxLength);
        });

        modelBuilder.Entity<VoucherRedemption>(redemption =>
        {
            redemption.ToTable("voucher_redemptions");
            redemption.HasKey(r => new { r.Code, r.UserId });
            redemption.HasOne<Voucher>().WithMany().HasForeignKey(r => r.Code).OnDelete(DeleteBehavior.Cascade);
            redemption.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClubGiftClaim>(claim =>
        {
            claim.ToTable("club_gift_claims");
            claim.HasKey(c => c.Id);
            claim.Property(c => c.Id).UseIdentityAlwaysColumn();
            claim.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
            claim.HasIndex(c => c.UserId);
        });

        modelBuilder.Entity<FurniGift>(gift =>
        {
            gift.ToTable("furni_gifts");
            gift.HasKey(g => g.ItemId);
            gift.Property(g => g.ExtraData).HasMaxLength(FurniGift.MaxExtraDataLength);
            gift.HasOne(g => g.Item).WithOne().HasForeignKey<FurniGift>(g => g.ItemId).OnDelete(DeleteBehavior.Cascade);
            gift.HasOne(g => g.Sender).WithMany().HasForeignKey(g => g.SenderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FurniPostIt>(postIt =>
        {
            postIt.ToTable("furni_post_its");
            postIt.HasKey(p => p.ItemId);
            postIt.Property(p => p.Text).HasMaxLength(FurniPostIt.MaxTextLength);
            postIt.HasOne(p => p.Item).WithOne().HasForeignKey<FurniPostIt>(p => p.ItemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FurniTrophy>(trophy =>
        {
            trophy.ToTable("furni_trophies");
            trophy.HasKey(t => t.ItemId);
            trophy.Property(t => t.EngraverName).HasMaxLength(FurniTrophy.MaxEngraverNameLength).IsRequired();
            trophy.Property(t => t.Message).HasMaxLength(FurniTrophy.MaxMessageLength).IsRequired();
            trophy.HasOne(t => t.Item).WithOne().HasForeignKey<FurniTrophy>(t => t.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // A teleport's partner. Deleting either item removes both sides: a teleport whose partner
        // is gone leads nowhere, and its own row must not point at a missing item.
        modelBuilder.Entity<FurniWired>(wired =>
        {
            wired.ToTable("furni_wired");
            wired.HasKey(w => w.ItemId);
            wired.HasOne(w => w.Item).WithOne().HasForeignKey<FurniWired>(w => w.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            wired.Property(w => w.StringParam).HasMaxLength(FurniWired.StringParamMaxLength);
            wired.HasMany(w => w.Snapshots).WithOne().HasForeignKey(s => s.WiredItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // a give reward box's record of what it gave, which goes with the box or the user
        modelBuilder.Entity<WiredRewardGrant>(grant =>
        {
            grant.ToTable("wired_reward_grants");
            grant.HasKey(g => g.Id);
            grant.Property(g => g.Id).UseIdentityAlwaysColumn();
            grant.Property(g => g.Code).HasMaxLength(WiredRewardGrant.CodeMaxLength);
            grant.HasOne<UserItem>().WithMany().HasForeignKey(g => g.WiredItemId).OnDelete(DeleteBehavior.Cascade);
            grant.HasOne<User>().WithMany().HasForeignKey(g => g.UserId).OnDelete(DeleteBehavior.Cascade);
            grant.HasIndex(g => new { g.WiredItemId, g.UserId });
        });

        // a resolution statue's challenge while it is under way; it goes with the statue, the user or the achievement
        modelBuilder.Entity<FurniAchievementResolution>(resolution =>
        {
            resolution.ToTable("furni_achievement_resolutions");
            resolution.HasKey(r => r.ItemId);
            resolution.HasOne<UserItem>().WithOne().HasForeignKey<FurniAchievementResolution>(r => r.ItemId).OnDelete(DeleteBehavior.Cascade);
            resolution.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
            resolution.HasOne<Achievement>().WithMany().HasForeignKey(r => r.AchievementId).OnDelete(DeleteBehavior.Cascade);
            resolution.HasIndex(r => new { r.UserId, r.AchievementId });
        });

        // room competitions: the rooms entered, the votes for them and who accepted the rules; they go with the room or the user
        modelBuilder.Entity<RoomCompetitionEntry>(entry =>
        {
            entry.ToTable("room_competition_entries");
            entry.HasKey(e => new { e.GoalId, e.RoomId });
            entry.HasIndex(e => new { e.GoalId, e.UserId }).IsUnique();
            entry.HasOne<Room>().WithMany().HasForeignKey(e => e.RoomId).OnDelete(DeleteBehavior.Cascade);
            entry.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoomCompetitionVote>(vote =>
        {
            vote.ToTable("room_competition_votes");
            vote.HasKey(v => v.Id);
            vote.Property(v => v.Id).UseIdentityAlwaysColumn();
            vote.HasIndex(v => new { v.GoalId, v.VoterId, v.VotedAt });
            vote.HasIndex(v => new { v.GoalId, v.RoomId });
            vote.HasOne<Room>().WithMany().HasForeignKey(v => v.RoomId).OnDelete(DeleteBehavior.Cascade);
            vote.HasOne<User>().WithMany().HasForeignKey(v => v.VoterId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoomCompetitionRulesAcceptance>(acceptance =>
        {
            acceptance.ToTable("room_competition_rules_acceptances");
            acceptance.HasKey(a => new { a.GoalId, a.UserId });
            acceptance.HasOne<User>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FurniWiredSnapshot>(snapshot =>
        {
            snapshot.ToTable("furni_wired_snapshots");
            snapshot.HasKey(s => new { s.WiredItemId, s.ItemId });
            snapshot.HasOne(s => s.Item).WithMany().HasForeignKey(s => s.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            snapshot.Property(s => s.State).HasMaxLength(FurniWiredSnapshot.StateMaxLength);
        });

        modelBuilder.Entity<FurniTeleportLink>(link =>
        {
            link.ToTable("furni_teleport_links");
            link.HasKey(l => l.ItemId);
            link.HasOne(l => l.Item).WithOne().HasForeignKey<FurniTeleportLink>(l => l.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            link.HasOne(l => l.LinkedItem).WithMany().HasForeignKey(l => l.LinkedItemId)
                .OnDelete(DeleteBehavior.Cascade);
            link.HasIndex(l => l.LinkedItemId).IsUnique();
        });

        modelBuilder.Entity<FurniHighScoreUser>(user =>
        {
            user.ToTable("furni_high_score_users");
            user.HasKey(u => new { u.EntryId, u.Position });

            // The name as displayed, not a player reference: a score outlives the account.
            user.Property(u => u.Username).HasMaxLength(32).IsRequired();
            user.HasOne(u => u.Entry).WithMany(e => e.Users).HasForeignKey(u => u.EntryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WebGamedata>(gamedata =>
        {
            gamedata.ToTable("web_gamedata");
            gamedata.HasKey(g => g.Name);
            gamedata.Property(g => g.Name).HasMaxLength(64);
            gamedata.Property(g => g.Content).HasColumnType("text");
            gamedata.Property(g => g.Hash).HasMaxLength(64);
        });

        modelBuilder.Entity<RoomModelData>(model =>
        {
            model.ToTable("room_models");
            model.HasKey(m => m.Id);
            model.Property(m => m.Id).UseIdentityAlwaysColumn();
            model.Property(m => m.Name).HasMaxLength(64).IsRequired();
            model.HasIndex(m => m.Name).IsUnique();
            model.Property(m => m.Heightmap).HasColumnType("text").IsRequired();
        });

        modelBuilder.Entity<Room>(room =>
        {
            room.ToTable("rooms");
            room.HasKey(r => r.Id);
            room.Property(r => r.Id).UseIdentityAlwaysColumn();
            room.Property(r => r.Name).HasMaxLength(64).IsRequired();
            room.Property(r => r.Description).HasMaxLength(256);
            room.Property(r => r.WiredTimezone).HasMaxLength(Room.WiredTimezoneMaxLength);
            room.Property(r => r.FloorItemLimit).HasDefaultValue(Room.DefaultItemLimit);
            room.Property(r => r.WallItemLimit).HasDefaultValue(Room.DefaultItemLimit);
            room.Property(r => r.WiredModifyPermissionMask).HasDefaultValue(Room.DefaultWiredPermissionMask);
            room.Property(r => r.WiredReadPermissionMask).HasDefaultValue(Room.DefaultWiredPermissionMask);
            room.Property(r => r.FloorType).HasMaxLength(32);
            room.Property(r => r.Wallpaper).HasMaxLength(32);
            room.Property(r => r.Landscape).HasMaxLength(32);
            room.PrimitiveCollection(r => r.Tags);
            room.HasOne(r => r.Owner).WithMany().HasForeignKey(r => r.OwnerId);
            room.HasOne(r => r.Model).WithMany().HasForeignKey(r => r.ModelId);
        });

        modelBuilder.Entity<Permission>(permission =>
        {
            permission.ToTable("permissions");
            permission.HasKey(p => p.Name);
            permission.Property(p => p.Name).HasMaxLength(Permission.NameMaxLength);
        });

        modelBuilder.Entity<UserPermission>(exception =>
        {
            exception.ToTable("user_permissions");
            exception.HasKey(e => new { e.UserId, e.PermissionName });
            exception.Property(e => e.PermissionName).HasMaxLength(Permission.NameMaxLength);
            exception.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            exception.HasOne<Permission>().WithMany().HasForeignKey(e => e.PermissionName).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CurrencySeasonReset>(reset =>
        {
            reset.ToTable("currency_season_resets");
            reset.HasKey(r => new { r.Type, r.SeasonEndedAt });
        });

        modelBuilder.Entity<UserPreferences>(preferences =>
        {
            preferences.ToTable("user_preferences");
            preferences.HasKey(p => p.UserId);
            preferences.HasOne(p => p.User).WithOne().HasForeignKey<UserPreferences>(p => p.UserId);
            preferences.Property(p => p.WiredUiStyle).HasMaxLength(32);
        });

        modelBuilder.Entity<ItemDefinition>(definition =>
        {
            definition.ToTable("item_definitions");
            definition.HasKey(d => d.Id);

            // By-default rather than always: the rows that predate this key kept their ids,
            // and the migration has to be able to write them back.
            definition.Property(d => d.Id).UseIdentityByDefaultColumn();
            definition.Property(d => d.ClassName).HasMaxLength(64).IsRequired();
            definition.Property(d => d.CategoryName).HasMaxLength(64).IsRequired();
            definition.Property(d => d.FurniLine).HasMaxLength(64).IsRequired();
            definition.Property(d => d.Name).HasMaxLength(128).IsRequired();
            definition.Property(d => d.Description).HasMaxLength(512).IsRequired();
            definition.HasIndex(d => d.ClassName).IsUnique();

            // furnidata reuses a class id between the floor and wall sections, so only the
            // pair identifies an item.
            definition.HasIndex(d => new { d.ClassId, d.Type }).IsUnique();

            definition.Property(d => d.CustomParams).HasMaxLength(512).IsRequired();
            definition.Property(d => d.Logic).HasMaxLength(64).IsRequired();
            definition.Property(d => d.Visualization).HasMaxLength(64).IsRequired();
            definition.Property(d => d.Interaction).HasMaxLength(64);
            definition.Ignore(d => d.InteractionName);

            definition.HasMany(d => d.States).WithOne().HasForeignKey(s => s.DefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
            definition.HasMany(d => d.Directions).WithOne().HasForeignKey(d => d.DefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ItemDefinitionState>(state =>
        {
            state.ToTable("item_definition_states");
            state.HasKey(s => new { s.DefinitionId, s.State });
        });

        modelBuilder.Entity<ItemDefinitionDirection>(direction =>
        {
            direction.ToTable("item_definition_directions");
            direction.HasKey(d => new { d.DefinitionId, d.Direction });
        });

        modelBuilder.Entity<UserItem>(item =>
        {
            item.ToTable("user_items");
            item.HasKey(i => i.Id);
            item.Property(i => i.Id).UseIdentityAlwaysColumn();
            // The item's data lives in whichever furni_* table its layout calls for; the item
            // itself only records which layout that is.
            item.HasOne(i => i.Owner).WithMany().HasForeignKey(i => i.OwnerId);
            item.HasOne(i => i.Definition).WithMany().HasForeignKey(i => i.DefinitionId);
            item.HasOne(i => i.Room).WithMany().HasForeignKey(i => i.RoomId).OnDelete(DeleteBehavior.SetNull);
            // Borrowed Builders Club furni exists only placed: one whose room was deleted from under it (the
            // database empties its RoomId) would otherwise turn up in its owner's inventory, theirs to keep.
            item.HasQueryFilter(BuildersClubOnlyPlaced, i => !i.BuildersClub || i.RoomId != null);
            // group furniture outlives its group: it is of no group then
            item.HasOne<Habbo.Domain.Groups.Group>().WithMany().HasForeignKey(i => i.GroupId).OnDelete(DeleteBehavior.SetNull);
            item.Property(i => i.WallPosition).HasMaxLength(64);
            // a chest holding something cannot be deleted from under it
            item.HasOne<UserItem>().WithMany().HasForeignKey(i => i.ChestId).OnDelete(DeleteBehavior.Restrict);
            item.HasIndex(i => i.ChestId);
        });
    }
}
