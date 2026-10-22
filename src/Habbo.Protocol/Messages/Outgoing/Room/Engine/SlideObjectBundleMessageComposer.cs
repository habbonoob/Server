using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>3976 SlideObjectBundleMessageComposer — a roller moving what is on it one tile: the furni,
/// and at most one avatar, which walks along ("mv") or slides in its posture ("sld"). The client reads
/// the avatar part only when bytes remain, so it is left off entirely when no avatar rides.</summary>
[Message(ServerMessages.SlideObjectBundleMessageComposer)]
public sealed partial record SlideObjectBundleMessageComposer(
    int FromX,
    int FromY,
    int ToX,
    int ToY,
    IReadOnlyList<SlideObjectData> Objects,
    int RollerId,
    [property: WireWhen("HasAvatar")] int? AvatarMoveType,
    [property: WireWhen("HasAvatar")] SlideObjectData? Avatar)
{
    // how the avatar moves along
    public const int AvatarWalks = 1;
    public const int AvatarSlides = 2;

    public bool HasAvatar => Avatar is not null;
}
