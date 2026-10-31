using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>FurniRecolor (October 2026) — the recolour window's apply: a recolourable furni, what to recolour with it
/// (<see cref="Scope"/>, the window's "Apply to:"), and the colour, a channel each.</summary>
[Incoming(ClientMessages.FurniRecolorMessageEvent)]
public sealed partial record FurniRecolorMessageEvent(int ItemId, int Scope, int Red, int Green, int Blue)
{
    /// <summary>"Only this furniture".</summary>
    public const int OnlyThis = 0;

    /// <summary>"Furniture with the same color".</summary>
    public const int SameColor = 1;

    /// <summary>"Furniture of the same type".</summary>
    public const int SameType = 2;

    /// <summary>"Furniture of the same type &amp; color".</summary>
    public const int SameTypeAndColor = 3;
}
