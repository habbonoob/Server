namespace Habbo.Tools.AssetMirror;

/// <summary>What the mirror can fetch.</summary>
public enum AssetKind
{
    /// <summary>The files the hashes manifest lists: furnidata, productdata, external_variables, external_texts, figuredata.</summary>
    Gamedata,

    /// <summary>Every furni's library (.hab) and icon.</summary>
    Furni,

    /// <summary>figuremap.xml, the avatar libraries it names (.hab), the avatar's own libraries and its actions
    /// (HabboAvatarActions.xml).</summary>
    Figure,

    /// <summary>effectmap.xml and the effect libraries.</summary>
    Effects,

    /// <summary>The pet libraries in pet.configuration.</summary>
    Pets,

    /// <summary>The badges named in external_texts.</summary>
    Badges,

    /// <summary>The sound machine samples, by number.</summary>
    Sounds,

    /// <summary>Every image the gamedata names: landing view promos, reception and hotel view art, notification icons.</summary>
    Images,

    /// <summary>The in-client help pages (habbopages), followed from page to page, and the images they show.</summary>
    Habbopages,

    /// <summary>The catalogue page icons, by number.</summary>
    Catalogue,

    /// <summary>The group badge parts, by the names the hotel's seed gives them.</summary>
    BadgeParts,

    /// <summary>The habbicons: their index, sprite sheets and animations.</summary>
    Habbicons,

    /// <summary>What the server was asked for and the mirror did not have (wanted.txt, which it writes): the
    /// assets the client builds the names of in its code, which no gamedata spells out.</summary>
    Wanted,
}
