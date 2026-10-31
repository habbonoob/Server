using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>A wired label on a furni, as the room's labels and regions carry it: type 0 (a region, type 1, carries a
/// layout of its own), the furni, its text, bubble colour, who sees it and whether it stays open.</summary>
[WireStruct]
public sealed partial record FurnitureLabelData(
    int Type,
    int ItemId,
    string Text,
    int BubbleColor,
    int Visibility,
    bool AlwaysOpen)
{
    public const int LabelType = 0;
}
