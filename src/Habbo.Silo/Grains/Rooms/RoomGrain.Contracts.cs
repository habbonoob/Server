using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Chests;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Contracts;
using Habbo.Silo.Abstractions.Wired;
using Habbo.Silo.Chests;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Contracts: those who may read the room's wired see what a contract furni asks for and gives, and those
/// who may change it write it. What may be asked for is what the dialog allows: a few rules of a few elements, only
/// furniture that can be traded.</summary>
public sealed partial class RoomGrain
{
    /// <summary>Why a contract was refused, as the client has a text for ("wiredcontracts.error.N").</summary>
    private const string InvalidContractRules = "invalid_rules";

    /// <summary>Using a contract opens it for those who may read the room's wired; false for anything else.</summary>
    private bool OpenContractOnUse(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem item)
    {
        if (item.Interaction is not ContractInteraction)
            return false;

        if (WiredRights(user).Read)
            SendTo(user.Player.Id, new WiredOpenContractMessageComposer(item.Item.Id));
        return true;
    }

    public async Task SendContractAsync(Guid playerId, int contractId)
    {
        if (_entities?.Find(playerId) is not { } user || !WiredRights(user).Read || ContractFurni(contractId) is not { } furni)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var contract = await ContractOfAsync(db, furni);
        SendTo(playerId, new WiredContractContentsMessageComposer(contract.ItemId, contract.Type, ContractWire.Definition(contract),
            contract.Type == WiredContract.Payment ? contract.PaymentMode : null,
            contract.Type == WiredContract.Payment ? contract.ReceiveText : null,
            contract.Type == WiredContract.Payment ? contract.LayoutType : null,
            contract.Type == WiredContract.Reward ? contract.RewardCategory : null,
            contract.Type == WiredContract.Reward ? contract.ShowDialog : null,
            contract.Type == WiredContract.Reward ? contract.RewardText : null));
    }

    public async Task UpdateContractAsync(Guid playerId, ContractUpdate update)
    {
        if (_entities?.Find(playerId) is not { } user || !WiredRights(user).Modify || ContractFurni(update.ContractId) is not { } furni)
            return;

        var type = ((ContractInteraction)furni.Interaction).ContractType;
        await using var db = await dbFactory.CreateDbContextAsync();
        var youGive = update.YouGive?.Select(ContractWire.Rule).ToList();
        var youGet = update.YouGet is { } get ? ContractWire.Rule(get) : null;
        if (update.ContractType != type || !await ContractRulesValidAsync(db, type, update.PaymentMode ?? WiredContract.SpecificMode, youGive, youGet))
        {
            SendTo(playerId, new WiredContractUpdateResultMessageComposer(update.ContractId, false, InvalidContractRules));
            return;
        }

        var contract = await db.WiredContracts.FirstOrDefaultAsync(c => c.ItemId == furni.Item.Id);
        if (contract is null)
            db.WiredContracts.Add(contract = WiredContract.Blank(furni.Item.Id, type));

        contract.YouGive = type == WiredContract.Reward ? null : youGive;
        contract.YouGet = type == WiredContract.Payment ? null : youGet;
        if (type == WiredContract.Payment)
        {
            contract.PaymentMode = update.PaymentMode == WiredContract.DonationMode ? WiredContract.DonationMode : WiredContract.SpecificMode;
            contract.ReceiveText = (await wordFilter.FilterAsync(Truncate(update.ReceiveText ?? string.Empty, WiredContract.ReceiveTextMaxLength), _filterWords)).Text;
            contract.LayoutType = WiredContract.LayoutTypes.Contains(update.LayoutType) ? update.LayoutType! : WiredContract.LayoutTypes[0];
        }
        else if (type == WiredContract.Reward)
        {
            contract.RewardCategory = update.RewardCategory is { } category && WiredContract.RewardCategories.Contains(category)
                ? category
                : WiredContract.RewardCategories[0];
            contract.ShowDialog = update.ShowDialog ?? false;
            contract.RewardText = (await wordFilter.FilterAsync(Truncate(update.RewardText ?? string.Empty, WiredContract.RewardTextMaxLength), _filterWords)).Text;
        }

        await db.SaveChangesAsync();
        SendTo(playerId, new WiredContractUpdateResultMessageComposer(update.ContractId, true, string.Empty));
    }

    private RoomItemStore.PlacedItem? ContractFurni(int itemId) => _items?.Find(itemId) is { Interaction: ContractInteraction } furni ? furni : null;

    /// <summary>The contract the furni holds, or a blank one of its kind when nobody has written it yet.</summary>
    private static async Task<WiredContract> ContractOfAsync(Habbo.Infrastructure.AppDbContext db, RoomItemStore.PlacedItem furni) =>
        await db.WiredContracts.AsNoTracking().FirstOrDefaultAsync(c => c.ItemId == furni.Item.Id)
        ?? WiredContract.Blank(furni.Item.Id, ((ContractInteraction)furni.Interaction).ContractType);

    /// <summary>Whether the rules are what the kind needs and the dialog allows: a payment asks for something unless it
    /// takes donations, a trade asks and gives, a reward gives; each rule has a few elements of sane amounts, and every
    /// furni element names furniture that can be traded.</summary>
    private static async Task<bool> ContractRulesValidAsync(Habbo.Infrastructure.AppDbContext db, short type, short paymentMode,
        List<ContractRule>? youGive, ContractRule? youGet)
    {
        var needsGive = type == WiredContract.Trade || (type == WiredContract.Payment && paymentMode == WiredContract.SpecificMode);
        var needsGet = type is WiredContract.Trade or WiredContract.Reward;
        if ((needsGive && (youGive is not { Count: > 0 } || youGive.Any(r => r.Nodes.Count == 0)))
            || (needsGet && youGet is not { Nodes.Count: > 0 })
            || (youGive?.Count ?? 0) > WiredContract.MaxRules)
            return false;

        var rules = (youGive ?? []).Concat(youGet is null ? [] : [youGet]).ToList();
        if (rules.Any(r => r.Nodes.Count > WiredContract.MaxNodesPerRule))
            return false;

        foreach (var node in rules.SelectMany(r => r.Nodes))
        {
            if (node.Amount <= 0 || node.Amount > (node.Type == ContractNode.Furni ? WiredContract.MaxFurni : WiredContract.MaxCredits))
                return false;
            if (node.Type != ContractNode.Furni)
                continue;

            var kind = node.IsWallItem ? ItemType.Wall : ItemType.Floor;
            if (await db.ItemDefinitions.AsNoTracking().FirstOrDefaultAsync(d => d.ClassId == node.TypeId && d.Type == kind) is not { Tradeable: true } definition
                || (node.PosterId.Length > 0 && definition.ClassName != PosterClassName))
                return false;
        }

        return true;
    }
}
