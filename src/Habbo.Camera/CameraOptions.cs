namespace Habbo.Camera;

/// <summary>The camera's settings (the "Camera" section).</summary>
public sealed class CameraOptions
{
    public const string Section = "Camera";

    /// <summary>Where rendered photos are kept: absolute, or a folder searched for from the binary's directory
    /// upwards (as the asset mirror is), made beside the asset mirror when there is none yet.</summary>
    public string PhotoDirectory { get; set; } = "habbo-photos";

    /// <summary>Folders of .hab libraries the mirror does not have: the client's own (the room's planes, the
    /// placeholders), which it carries rather than downloads.</summary>
    public string LocalLibraryDirectory { get; set; } = "seed/camera";

    /// <summary>The client's avatar structure (its geometry, part sets and actions), which it carries rather than
    /// downloads.</summary>
    public string AvatarDirectory { get; set; } = "seed/avatar";

    /// <summary>The figure sets and their colours the avatars are drawn by.</summary>
    public string FigureDataFile { get; set; } = "seed/figuredata.xml";

    /// <summary>Where the photo lab's effect images are, under the hotel's url (the client loads them from
    /// image.library.url + "Habbo-Stories/", which is the mirror's).</summary>
    public string EffectImagePath { get; set; } = "/mirror/images.habbo.com/c_images/Habbo-Stories/";

    /// <summary>How many photos (and thumbnails) a player may have rendered a minute; past it the client is told
    /// it rendered too many (camera.render.count.info).</summary>
    public int RendersPerMinute { get; set; } = 10;

    /// <summary>Whether each render request is kept beside its photo as the client sent it (requests/{id}.json),
    /// for when a photo comes out other than the room looked.</summary>
    public bool KeepRenderRequests { get; set; }

    /// <summary>What a photo costs as a furni.</summary>
    public int CreditPrice { get; set; } = 2;

    public int DucketPrice { get; set; }

    /// <summary>What publishing a photo to the player's stories costs.</summary>
    public int PublishDucketPrice { get; set; } = 10;

    /// <summary>How long a player waits between publishing photos.</summary>
    public int PublishCooldownSeconds { get; set; } = 3 * 60;
}
