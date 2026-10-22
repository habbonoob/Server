using Habbo.Domain.Users;
using Orleans;
using Orleans.Concurrency;
using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Silo.Abstractions.Players;

/// <summary>One grain per player; owns online presence and the player's live state.</summary>
public interface IPlayerGrain : IGrainWithGuidKey
{
    Task<PlayerData> GetDataAsync();
    Task SetOnlineAsync(Guid sessionId);

    /// <summary>A session ended. True when it was the player's last, so they are now offline; false when
    /// another session (a second login) is still connected.</summary>
    Task<bool> SetOfflineAsync(Guid sessionId);

    /// <summary>Sends a message to wherever the player is connected, if anywhere. It only hands the
    /// message on, so it runs alongside whatever the player is doing: two players telling each other
    /// something at once must not wait on each other.</summary>
    [Orleans.Concurrency.AlwaysInterleave]
    Task SendAsync(ComposedMessage message);

    /// <summary>Sets the home room; returns the stored id (unchanged when the room doesn't exist).</summary>
    Task<int> SetHomeRoomAsync(int roomId);

    /// <summary>The player's saved client settings (created with defaults on first use).</summary>
    Task<AccountPreferences> GetPreferencesAsync();

    /// <summary>Saves the non-null fields of the update.</summary>
    Task UpdatePreferencesAsync(PreferencesUpdate update);

    /// <summary>The player's furniture inventory: what they are holding, not what they have
    /// placed. A placed item is part of the room it sits in.</summary>
    Task<List<InventoryItemData>> GetInventoryAsync();

    /// <summary>Buys an offer: checks the price against the balance, debits it and grants the
    /// items in one transaction. The grain is the serialization point, so two purchases from
    /// the same account cannot both pass the balance check. <paramref name="extraData"/> is what
    /// the buyer typed into the offer, such as a trophy's engraving; each item's behaviour
    /// decides whether it means anything.</summary>
    Task<PurchaseResult> PurchaseAsync(ShopOffer offer, int amount, string extraData);

    /// <summary>Buys one of the offer wrapped for somebody else, who finds it in their inventory.</summary>
    Task<PurchaseResult> PurchaseGiftAsync(ShopOffer offer, string extraData, GiftWrapping wrapping);

    /// <summary>Opens one of the player's presents: what it held is theirs, and the present is gone.</summary>
    Task<UnwrappedGift?> UnwrapGiftAsync(int presentId);

    /// <summary>Tells the player of items that arrived in their inventory. It only reads and sends, so it may run while the
    /// player is busy: rooms and other players call it from the middle of their own work, which waiting would deadlock.</summary>
    [AlwaysInterleave]
    Task AnnounceItemsAsync(List<int> itemIds);

    /// <summary>Puts these items back in the open inventory without marking them new: an item taken back off the
    /// marketplace is not one the player has not seen. Like <see cref="AnnounceItemsAsync"/> it only reads and sends.</summary>
    [AlwaysInterleave]
    Task RestoreItemsAsync(List<int> itemIds);

    /// <summary>Hands the player so many of an item, free — with the parameter its product gives it, such as a poster's
    /// number — and shows them arriving in their inventory.</summary>
    Task<IReadOnlyList<int>> ReceiveFurnitureAsync(int definitionId, string productParam, int amount);

    /// <summary>Pays the account, e.g. for furniture exchanged for what it is worth, and answers
    /// with the new balance.</summary>
    Task<int> AddCreditsAsync(int credits, string reason);

    /// <summary>Takes credits when the player has them and tells them the new balance; null when short.</summary>
    Task<int?> SpendCreditsAsync(int credits, string reason);

    /// <summary>Uses one of today's respects; false when none are left.</summary>
    Task<bool> SpendRespectAsync();

    /// <summary>Moves the player on towards an achievement, by its name; each level reached pays out and
    /// is announced.</summary>
    Task ProgressAchievementAsync(string achievementName, int amount);

    /// <summary>Raises an achievement's progress to a total the player has reached (friends, days, furni in a room);
    /// progress never falls.</summary>
    Task ReachAchievementAsync(string achievementName, int total);

    /// <summary>Counts the pets the player owns, and their babies' colours, for the pet-owning achievements.</summary>
    Task CountPetsAsync();

    /// <summary>Uses one of today's respects for pets; false when there are none left.</summary>
    Task<bool> SpendPetRespectAsync();

    /// <summary>Counts a respect received; returns the new total.</summary>
    Task<int> ReceiveRespectAsync();

    // The account: what the player holds, wears and has set up. A call that answers the client
    // sends the answer itself, wherever the player is connected.

    /// <summary>Every currency's balance (ActivityPoints).</summary>
    Task SendActivityPointsAsync();

    /// <summary>The balance of one activity-point currency.</summary>
    Task<int> GetActivityPointsAsync(int type);

    /// <summary>Takes <paramref name="amount"/> of a currency when the player has it, and tells them;
    /// false, and nothing taken, when they do not.</summary>
    Task<bool> SpendActivityPointsAsync(int type, int amount, string reason);

    /// <summary>Gives (or, negative, takes) a currency and tells the player; the new balance.</summary>
    Task<int> AddActivityPointsAsync(int type, int amount, string reason);

    /// <summary>The badge inventory (Badges).</summary>
    Task SendBadgesAsync();

    /// <summary>Wears these badges, slot by slot; what the player now wears.</summary>
    Task<IReadOnlyList<WornBadge>> SetWornBadgesAsync(IReadOnlyList<WornBadge> slots);

    /// <summary>What a user wears, looked up by account id.</summary>
    Task<IReadOnlyList<WornBadge>> GetWornBadgesAsync(int accountId);

    /// <summary>Gives the player a badge they do not have yet, and tells them; false when they had it.</summary>
    Task<bool> GiveBadgeAsync(string code);

    /// <summary>Takes a badge away; false when they did not have it.</summary>
    Task<bool> RemoveBadgeAsync(string code);

    Task<bool> HasBadgeAsync(string code);

    /// <summary>The score each achievement badge level needs (BadgePointLimits).</summary>
    Task SendBadgePointLimitsAsync();

    /// <summary>Hands over the badge a request code stands for, if the hotel has one.</summary>
    Task RequestBadgeAsync(string requestCode);

    /// <summary>Whether the player has what a request code gives (IsBadgeRequestFulfilled).</summary>
    Task SendBadgeRequestStatusAsync(string requestCode);

    /// <summary>The effects inventory (AvatarEffects).</summary>
    Task SendEffectsAsync();

    /// <summary>Adds effects to the inventory, and tells the player.</summary>
    Task AddEffectAsync(int effectId, int duration, bool isPermanent, int amount = 1);

    /// <summary>Starts one of the waiting effects of this type; false when there is none.</summary>
    Task<bool> ActivateEffectAsync(int effectId);

    /// <summary>Whether the player may wear this effect now: one of theirs that is running.</summary>
    Task<bool> CanWearEffectAsync(int effectId);

    /// <summary>Tells the player which effect they picked (AvatarEffectSelected).</summary>
    Task EffectSelectedAsync(int effectId);

    /// <summary>The clothing sets the player unlocked (FigureSetIds).</summary>
    Task SendFigureSetsAsync();

    /// <summary>Unlocks clothing sets from a clothing furni; the sets that were new.</summary>
    Task<IReadOnlyList<int>> UnlockFigureSetsAsync(IReadOnlyList<int> setIds, string furniClassName);

    /// <summary>Saves a new look when figuredata allows it; the updated player, or null when refused.</summary>
    Task<PlayerData?> UpdateFigureAsync(string figure, Gender gender);

    /// <summary>Saves a new motto; the updated player.</summary>
    Task<PlayerData> UpdateMottoAsync(string motto);

    /// <summary>The wardrobe's saved looks (Wardrobe).</summary>
    Task SendWardrobeAsync();

    /// <summary>Keeps a look in a wardrobe slot the player's membership unlocks.</summary>
    Task SaveOutfitAsync(int slot, string figure, Gender gender);

    /// <summary>Whether a name would do (CheckUserNameResult).</summary>
    Task CheckNameAsync(string name);

    /// <summary>Takes a new name if it will do, telling the player either way; the updated player
    /// when it changed.</summary>
    Task<PlayerData?> ChangeNameAsync(string name);

    /// <summary>Mutes or unmutes a user's chat (IgnoreResult).</summary>
    Task SetIgnoredAsync(int accountId, bool ignored);

    /// <summary>Blocks or unblocks a user (BlockUserUpdate).</summary>
    Task SetBlockedAsync(int accountId, bool blocked);

    /// <summary>A user's profile (ExtendedProfile).</summary>
    Task SendExtendedProfileAsync(int accountId, bool openProfileWindow);

    Task SendExtendedProfileByNameAsync(string name);

    /// <summary>How many own a badge and how rare that makes it, for a badge display (September 2026).</summary>
    Task SendBadgeInfoAsync(string code);

    /// <summary>A present arrives in the inventory holding one item, with a card from the sender: a prize staff gave.
    /// False when the item, the sender or a free wrap is not there.</summary>
    Task<bool> ReceivePrizeGiftAsync(int definitionId, string message, Guid senderId);

    /// <summary>A page of a badge leaderboard, with the player's own place (September 2026).</summary>
    Task SendBadgeLeaderboardAsync(int type, int rarity, int page, int pageSize);

    /// <summary>A user's friends by relationship (RelationshipStatusInfo).</summary>
    Task SendRelationshipsAsync(int accountId);

    /// <summary>Buys the day's respects back for duckets; the updated player when it went through.</summary>
    Task<PlayerData?> ReplenishRespectAsync();

    Task SendEmailStatusAsync();

    Task ChangeEmailAsync(string email);

    /// <summary>The club payday figures (ScrSendKickbackInfo).</summary>
    Task SendKickbackInfoAsync();

    /// <summary>The chat bubbles the player has bought.</summary>
    Task SendChatStylesAsync();

    /// <summary>Whether the player may chat in this bubble. It only reads, so the room asking may be
    /// one the player's grain is waiting on.</summary>
    [Orleans.Concurrency.AlwaysInterleave]
    Task<bool> CanUseChatStyleAsync(int styleId);

    /// <summary>The messages of the day.</summary>
    Task SendMessagesOfTheDayAsync();

    /// <summary>Some looks other players wear (HotLooks).</summary>
    Task SendHotLooksAsync(int amount);

    /// <summary>Gives a user star gems, within the player's daily allowance.</summary>
    Task GiveStarGemsAsync(int accountId, int amount);

    /// <summary>The room the player is in, or null; kept so what runs out on its own (an effect)
    /// can be taken off the avatar there.</summary>
    Task SetCurrentRoomAsync(int? roomId);

    /// <summary>Closes every session the player has open, after what was sent to them has gone out.</summary>
    /// <summary>Closes every connection the player has, telling the client why (a DisconnectReasonComposer reason).</summary>
    Task DisconnectAsync(int reason);

    /// <summary>The room the player is in, or null. Only read, so it is answered even while the player is busy.</summary>
    [AlwaysInterleave]
    Task<int?> GetCurrentRoomAsync();

    /// <summary>The cached player, reloaded from the database, for when something else changed it.</summary>
    Task<PlayerData> ReloadAsync();

    /// <summary>Answers whether an ad may be shown as the player goes into a room, by the hotel's interval between ads
    /// and the most it shows in a visit.</summary>
    Task SendInterstitialAsync();

    /// <summary>An ad was shown to the end, which counts towards both.</summary>
    Task InterstitialShownAsync();
}
