namespace Habbo.Domain.Furniture.Data;

/// <summary>What a WIRED Link (wf_link) is connected to, as its owner saved it in the Link's dialog: the network it
/// exchanges messages and dataframes on (none when disconnected), and whether it also receives what its own room sends
/// through it. A Link never set up has no row; picking a Link up disconnects it.</summary>
public sealed class WiredLinkEndpoint : IFurniRow
{
    public int ItemId { get; set; }
    public UserItem? Item { get; set; }

    public int? NetworkId { get; set; }
    public WiredLinkNetwork? Network { get; set; }

    /// <summary>"Receive messages sent by this room": the Link hears what is sent through it, network or not.</summary>
    public bool ReceiveLocal { get; set; }
}
