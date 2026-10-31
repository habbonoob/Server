using Habbo.Domain.Furniture.Data;

namespace Habbo.Silo.Wired;

/// <summary>What the boxes that work with WIRED Links share: their picks must be Links, at least one when they use their
/// picks ("WIRED Links", their first furni slot), and a run sends through only so many.</summary>
public static class WiredLinks
{
    public const string RequireLinkFurni = "wiredfurni.error.require_link_furni";

    /// <summary>The most Links one run of a send box sends through: a selector could otherwise hand it every Link in the
    /// room, as for a signal's antennas.</summary>
    public const int MaxLinksPerRun = 100;

    /// <summary>The chat style of a Link speaker's bubble (the client's chatstyles: 253, wired_link_speaker).</summary>
    public const int SpeakerChatStyle = 253;

    public static readonly WiredPickRule PickRule = new(WiredFurni.IsLink, RequireLinkFurni);

    /// <summary>"Select at least one WIRED Link": a box that takes its Links from its picks has picked some.</summary>
    public static WiredValidation? RequireLinks(FurniWired settings) =>
        settings.FurniSources.FirstOrDefault(WiredSources.PickedFurni) == WiredSources.PickedFurni && settings.StuffIds.Count == 0
            ? WiredValidation.Of(RequireLinkFurni)
            : null;
}
