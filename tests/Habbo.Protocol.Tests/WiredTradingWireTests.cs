using System.Buffers;
using Habbo.Protocol.IO;
using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Contracts;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests.Storage;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Contracts;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade.Requirements;

namespace Habbo.Protocol.Tests;

/// <summary>Contracts go both ways in one layout (WiredContractContentsMessageParser, WiredUpdateContractMessageComposer):
/// what the server writes the reader must take back field for field, nested rules and the kind's own fields included.</summary>
public class WiredTradingWireTests
{
    private static MessageReader Written(Composers.IComposer composer)
    {
        var buffer = new ArrayBufferWriter<byte>();
        composer.Compose(new MessageWriter(buffer));
        return new MessageReader(buffer.WrittenMemory);
    }

    private static TradeRequirementRuleData Rule(params TradeRequirementNodeData[] nodes) => new(nodes);

    private static TradeRequirementNodeData Credits(int amount) => new(TradeRequirementNodeData.Coins, amount, null);

    private static TradeRequirementNodeData Furni(int amount, int typeId, string poster = "") =>
        new(TradeRequirementNodeData.Furni, amount, new ChestItemTypeData(false, typeId, poster));

    [Fact]
    public void A_payment_contract_reads_back_as_written()
    {
        var definition = new TradeRequirementRulesDefinitionData([Rule(Credits(5)), Rule(Furni(2, 18), Credits(1))], null);
        var reader = Written(new WiredContractContentsMessageComposer(
            7, WiredUpdateContractMessageEvent.Payment, definition, 1, "a hat", "games", null, null, null));

        var read = WiredUpdateContractMessageEvent.Read(reader);

        Assert.Equal(7, read.ContractId);
        Assert.Equal(WiredUpdateContractMessageEvent.Payment, read.ContractType);
        Assert.Equal(2, read.Definition.YouGive!.Count);
        Assert.Null(read.Definition.YouGet);
        Assert.Equal(18, read.Definition.YouGive[1].Nodes[0].ItemType!.TypeId);
        Assert.Null(read.Definition.YouGive[1].Nodes[1].ItemType);
        Assert.Equal((short)1, read.PaymentMode);
        Assert.Equal("a hat", read.ReceiveText);
        Assert.Equal("games", read.LayoutType);
        Assert.Null(read.RewardCategory);
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void A_reward_contract_reads_back_as_written()
    {
        var definition = new TradeRequirementRulesDefinitionData(null, Rule(Furni(1, 4077, "5")));
        var reader = Written(new WiredContractContentsMessageComposer(
            9, WiredUpdateContractMessageEvent.Reward, definition, null, null, null, 13, true, "well done"));

        var read = WiredUpdateContractMessageEvent.Read(reader);

        Assert.Null(read.Definition.YouGive);
        Assert.Equal("5", read.Definition.YouGet!.Nodes[0].ItemType!.PosterId);
        Assert.Null(read.PaymentMode);
        Assert.Equal((short)13, read.RewardCategory);
        Assert.True(read.ShowDialog);
        Assert.Equal("well done", read.RewardText);
        Assert.Equal(0, reader.Remaining);
    }
}
