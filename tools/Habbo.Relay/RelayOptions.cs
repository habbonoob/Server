namespace Habbo.Relay;

/// <summary>The relay's settings (the "Relay" section, or RELAY__* environment variables).</summary>
public sealed class RelayOptions
{
    public const string Section = "Relay";

    /// <summary>The port the relay listens on, on localhost only.</summary>
    public int Port { get; set; } = 8791;

    /// <summary>The game servers a client may be carried to: a host as is, or "*.domain" for any host under it.
    /// Anything else is refused, so the relay is not an open proxy.</summary>
    public string[] AllowedHosts { get; set; } =
    [
        "*.habbo.com", "*.habbo.com.br", "*.habbo.com.tr", "*.habbo.de", "*.habbo.es", "*.habbo.fi", "*.habbo.fr",
        "*.habbo.it", "*.habbo.nl",
    ];

    /// <summary>Where captures go: absolute, or relative to the repository root (found from the binary upwards).</summary>
    public string CaptureDirectory { get; set; } = "habbo-packetlogs/captures";

    /// <summary>Where the client's room copies go: absolute; unset, ROOMCOPY_DIR or Documents/Habbo room copies, as the
    /// G-Earth copier saves them.</summary>
    public string? RoomCopyDirectory { get; set; }

    /// <summary>The pages allowed to call the capture API (the client's dev and preview servers), under either name
    /// of this machine: the preview serves on every interface, so its page may be opened as 127.0.0.1 too.</summary>
    public string[] AllowedOrigins { get; set; } =
    [
        "http://localhost:4173", "http://localhost:4174", "http://localhost:4175", "http://localhost:5173",
        "http://127.0.0.1:4173", "http://127.0.0.1:4174", "http://127.0.0.1:4175", "http://127.0.0.1:5173",
    ];
}
