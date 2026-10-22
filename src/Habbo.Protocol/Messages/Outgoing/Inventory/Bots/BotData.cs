using Habbo.Domain.Bots;
using Habbo.Domain.Users;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Bots;

/// <summary>BotData — a bot in the inventory</summary>
[WireStruct]
public sealed partial record BotData(int Id, string Name, string Motto, string Gender, string Figure)
{
    public static BotData From(Bot bot) => new(bot.Id, bot.Name, bot.Motto, bot.Gender.ToLetter(), bot.Figure);
}
