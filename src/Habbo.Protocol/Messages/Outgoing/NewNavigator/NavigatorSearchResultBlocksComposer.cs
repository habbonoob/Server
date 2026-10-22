using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Protocol.Messages.Outgoing.Navigator;

namespace Habbo.Protocol.Messages.Outgoing.NewNavigator;

/// <summary>1893 NavigatorSearchResultBlocksComposer — the search echoed back with its results.</summary>
[Message(ServerMessages.NavigatorSearchResultBlocksComposer)]
public sealed partial record NavigatorSearchResultBlocksComposer(
    string SearchCode, string FilteringData, IReadOnlyList<NavigatorSearchBlockData> Blocks)
{
    public static NavigatorSearchResultBlocksComposer From(
        string searchCode, string filteringData, IReadOnlyList<NavigatorSearchBlock> blocks) =>
        new(searchCode, filteringData, blocks
            .Select(block => new NavigatorSearchBlockData(
                block.SearchCode,
                block.Text,
                block.ActionAllowed,
                block.ForceClosed,
                block.ViewMode,
                block.Rooms.Select(GuestRoomData.From).ToList()))
            .ToList());
}
