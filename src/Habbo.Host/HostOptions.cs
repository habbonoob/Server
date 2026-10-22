namespace Habbo.Host;

public sealed class WebOptions
{
    public int HttpPort { get; set; } = 8080;

    /// <summary>Requests come through a reverse proxy (a deployment's ingress): the client's address and scheme are
    /// taken from its X-Forwarded-For and X-Forwarded-Proto, so rate limits count people rather than the proxy and
    /// the session cookie knows it travels over HTTPS. Only for a server nothing reaches but through the proxy.</summary>
    public bool BehindProxy { get; set; }

    /// <summary>Every response asks search engines not to index, follow or keep it: a private hotel.</summary>
    public bool NoIndex { get; set; }
}

public sealed class OrleansOptions
{
    public const string LocalhostClustering = "localhost";
    public const string PostgresClustering = "postgres";

    public int SiloPort { get; set; } = 11111;
    public int GatewayPort { get; set; } = 31111;

    /// <summary>How silos and gateways find each other: "localhost" (one machine, the loopback addresses, as
    /// development runs), or "postgres" (the membership table in the hotel's own database, for processes in pods of
    /// their own). Not "Clustering": Orleans reads Orleans:Clustering itself, as its provider's settings.</summary>
    public string Membership { get; set; } = LocalhostClustering;

    /// <summary>The address a silo tells the others it is at, with postgres membership: the pod's own (POD_IP).</summary>
    public string? AdvertisedAddress { get; set; }

    /// <summary>Which grains a silo holds (<see cref="SiloPool"/>): "core", "rooms", or empty for every one.</summary>
    public string? Pool { get; set; }
}
