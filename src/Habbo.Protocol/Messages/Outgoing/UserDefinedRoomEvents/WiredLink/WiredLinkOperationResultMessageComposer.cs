using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredLink;

/// <summary>WiredLinkOperationResult (October 2026) — what became of an operation in a Link's dialog (save, create,
/// rename, delete, connections): 0, or the failure the client shows as wiredlink.error.N; the Link's connection when
/// it changed; the network deleted ("0": none); and the networks the operation changed.</summary>
[Message(ServerMessages.WiredLinkOperationResultMessageComposer)]
public sealed partial record WiredLinkOperationResultMessageComposer(
    int RoomId,
    int ItemId,
    int Operation,
    int ResultCode,
    bool HasConfiguration,
    [property: WireWhen("HasConfiguration")] string? SelectedNetworkId,
    [property: WireWhen("HasConfiguration")] bool? ReceiveLocal,
    string RemovedNetworkId,
    IReadOnlyList<WiredLinkNetworkData> Networks)
{
    // the operations (WiredLinkController)
    public const int Save = 1;
    public const int Create = 2;
    public const int Rename = 3;
    public const int Delete = 4;
    public const int Connections = 5;

    // the results: done, or the failures wiredlink.error.N names
    public const int Done = 0;
    public const int NotOwner = 1;
    public const int NetworkNotFound = 2;
    public const int InvalidName = 3;
    public const int DuplicateName = 4;
    public const int NetworkLinkedHere = 5;
    public const int InvalidNetworkId = 6;
    public const int LinkGone = 7;
    public const int NetworkLimit = 8;

    /// <summary>The id the client knows "no network" by.</summary>
    public const string NoNetwork = "0";

    public static WiredLinkOperationResultMessageComposer Failed(int roomId, int itemId, int operation, int resultCode) =>
        new(roomId, itemId, operation, resultCode, false, null, null, NoNetwork, []);
}
