namespace Habbo.Host;

/// <summary>What one process of the hotel runs (Hosting:Roles):
///  - "web": the website's API (accounts, news, gamedata, the asset mirror, photos, imaging);
///  - "silo": the Orleans silo with the grains, and the background monitors;
///  - "gateway": the game port, an Orleans client of the silo;
///  - "game": the silo and the gateway together.
/// One process runs everything when none are named, as development does; production runs each in a deployment of its
/// own, so one part can be deployed without restarting the others.</summary>
public sealed record HostRoles(bool Web, bool Silo, bool Gateway)
{
    public const string WebRole = "web";
    public const string SiloRole = "silo";
    public const string GatewayRole = "gateway";
    public const string GameRole = "game";

    /// <summary>Any part of the game: the hotel's services are needed.</summary>
    public bool Game => Silo || Gateway;

    public static HostRoles From(IConfiguration configuration)
    {
        var named = configuration.GetSection("Hosting:Roles").Get<string[]>() ?? [];
        if (named.Length == 0)
            return new HostRoles(Web: true, Silo: true, Gateway: true);
        var unknown = named.Except([WebRole, SiloRole, GatewayRole, GameRole], StringComparer.OrdinalIgnoreCase).ToList();
        if (unknown.Count > 0)
            throw new InvalidOperationException(
                $"Hosting:Roles names {string.Join(", ", unknown)}; the roles are {WebRole}, {SiloRole}, {GatewayRole} and {GameRole}.");
        bool Has(string role) => named.Contains(role, StringComparer.OrdinalIgnoreCase);
        return new HostRoles(
            Web: Has(WebRole),
            Silo: Has(SiloRole) || Has(GameRole),
            Gateway: Has(GatewayRole) || Has(GameRole));
    }
}
