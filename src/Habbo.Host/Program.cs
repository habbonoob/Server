using System.Net;
using Habbo.Gateway;
using Habbo.Host;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Achievements;
using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Abstractions.Chat;
using Habbo.Silo.Abstractions.Friends;
using Habbo.Silo.Abstractions.Games;
using Habbo.Silo.Abstractions.Groups;
using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Abstractions.Inventory;
using Habbo.Silo.Abstractions.Moderation;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Abstractions.Subscriptions;
using Habbo.Silo.Achievements;
using Habbo.Silo.Auth;
using Habbo.Silo.CallForHelp;
using Habbo.Silo.Catalog;
using Habbo.Silo.Friends;
using Habbo.Silo.Groups;
using Habbo.Silo.Hotel;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.LandingView;
using Habbo.Silo.Messenger;
using Habbo.Silo.Moderation;
using Habbo.Silo.Navigator;
using Habbo.Silo.Perks;
using Habbo.Silo.Rooms;
using Habbo.Silo.Subscriptions;
using Habbo.Silo.Users;
using Habbo.Silo.Wired;
using Habbo.Web;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Connections;
using Orleans.Configuration;
using Orleans.Hosting;

try
{
    // room ticks on Habbo's beat, not Windows' 15.6 ms timer grid
    TimerResolution.UseMilliseconds();

    // Logging is the host's default console provider, with levels from the Logging section
    // of appsettings — nothing to configure here.
    var builder = WebApplication.CreateBuilder(args);

    // Settings kept out of the repository: the RSA key the client's handshake is signed with.
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

    var configuration = builder.Configuration;
    var roles = HostRoles.From(configuration);
    if (roles.Game && (string.IsNullOrEmpty(configuration["Crypto:Rsa:Modulus"]) ||
        string.IsNullOrEmpty(configuration["Crypto:Rsa:PrivateExponent"])))
        throw new InvalidOperationException(
            "No RSA key is configured: put Crypto:Rsa:Modulus and Crypto:Rsa:PrivateExponent (hex, matching the " +
            "client's embedded public key) in src/Habbo.Host/appsettings.Local.json, which git ignores.");
    var gateway = configuration.GetSection("Gateway").Get<GameNetworkOptions>() ?? new GameNetworkOptions();
    var web = configuration.GetSection("Web").Get<WebOptions>() ?? new WebOptions();
    var orleans = configuration.GetSection("Orleans").Get<OrleansOptions>() ?? new OrleansOptions();

    builder.Services.AddInfrastructure(configuration);
    // the camera reads the asset mirror and the photos for both roles: the game renders, the website serves
    builder.Services.Configure<Habbo.Camera.CameraOptions>(configuration.GetSection(Habbo.Camera.CameraOptions.Section));
    builder.Services.AddSingleton<Habbo.Camera.PhotoStore>();
    builder.Services.AddSingleton<Habbo.Camera.AssetCatalog>();
    builder.Services.AddSingleton<Habbo.Camera.Avatars.AvatarImager>();
    // both roles judge names (the website registers players, the game renames them), and fetch from the web
    builder.Services.Configure<AccountOptions>(configuration.GetSection("Account"));
    builder.Services.Configure<Habbo.Silo.Chat.ChatOptions>(configuration.GetSection("Chat"));
    builder.Services.AddSingleton<Habbo.Silo.Chat.WordFilter>();
    builder.Services.AddSingleton<INameRules, NameRules>();
    builder.Services.AddHttpClient();
    if (roles.Web)
    {
        builder.Services.AddWebSeam(configuration);
        // copied rooms are imported by the website's API, straight into the database
        builder.Services.Configure<Habbo.Silo.Rooms.RoomOptions>(configuration.GetSection("Rooms"));
        builder.Services.AddSingleton<Habbo.Silo.Rooms.Copies.RoomImporter>();
        builder.Services.TryAddSingleton<Habbo.Silo.Wired.WiredBoxRegistry>();
    }
    if (roles.Web && !roles.Game)
        builder.Services.AddHostedService<WordFilterRefresh>();
    if (roles.Game)
        builder.Services.AddHotelServices(configuration);
    if (roles.Silo)
        builder.Services.AddSiloRole(configuration, withGateway: roles.Gateway, orleans.Pool);
    if (roles.Gateway)
        builder.Services.AddGatewayRole(configuration);

    // the game: the silo with its grains, and the gateway, in one process or each in its own; a gateway on its own is
    // an Orleans client of the silo
    var postgresClustering = string.Equals(orleans.Membership, OrleansOptions.PostgresClustering, StringComparison.OrdinalIgnoreCase);
    var connectionString = configuration.GetConnectionString("Default");
    if (roles.Silo)
    {
        builder.Host.UseOrleans(silo =>
        {
            if (postgresClustering)
            {
                silo.UseAdoNetClustering(options =>
                {
                    options.Invariant = "Npgsql";
                    options.ConnectionString = connectionString;
                });
                // reachable by the other pods (the gateway, other silos) at the pod's address; the network policy
                // keeps everyone else off these ports
                var advertised = IPAddress.Parse(orleans.AdvertisedAddress
                    ?? throw new InvalidOperationException("Postgres membership needs Orleans:AdvertisedAddress, the pod's address."));
                silo.ConfigureEndpoints(advertised, orleans.SiloPort, orleans.GatewayPort, listenOnAnyHostAddress: true);
                // where each grain lives, kept so that a silo going (the rooms' being deployed) loses none of the others'
                // entries: the default directory forgets what the gone silo's share recorded, and the next call makes a
                // second player or directory grain beside the first
#pragma warning disable ORLEANSEXP003
                silo.AddDistributedGrainDirectory();
#pragma warning restore ORLEANSEXP003
            }
            else
            {
                silo.UseLocalhostClustering();
                // on loopback: the silo and its client gateway take any grain call without asking who calls
                silo.ConfigureEndpoints(IPAddress.Loopback, orleans.SiloPort, orleans.GatewayPort);
            }
            silo.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "habbo";
                options.ServiceId = "habbo";
            });
            silo.Services.PostConfigure<GrainTypeOptions>(grainTypes => SiloPool.Hold(grainTypes, orleans.Pool));
        });
        // after the silo, whose directory it asks from the start
        if (!roles.Gateway)
            builder.Services.AddHostedService(sp => sp.GetRequiredService<GatewayPacketSink>());
        builder.Services.AddHostedService<RoomHostWatcher>();
    }
    else if (roles.Gateway)
    {
        builder.Host.UseOrleansClient(client =>
        {
            if (postgresClustering)
                client.UseAdoNetClustering(options =>
                {
                    options.Invariant = "Npgsql";
                    options.ConnectionString = connectionString;
                });
            else
                client.UseLocalhostClustering(orleans.GatewayPort);
            client.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "habbo";
                options.ServiceId = "habbo";
            });
            // a silo that started again is found in seconds, not the default minute, and so are the grains it holds
            client.Configure<GatewayOptions>(options => options.GatewayListRefreshPeriod = TimeSpan.FromSeconds(5));
            client.Configure<TypeManagementOptions>(options => options.TypeMapRefreshInterval = TimeSpan.FromSeconds(5));
        });
        // after the client, which it needs connected; before the game port, which waits for it
        builder.Services.AddHostedService<GatewayRegistration>();
    }

    builder.WebHost.ConfigureKestrel(options =>
    {
        // the website's API, or for the game alone its health
        options.ListenAnyIP(web.HttpPort);
        if (!roles.Gateway)
            return;
        // one game port for TCP clients and browser clients (WebSocket), told apart by their first bytes
        options.ListenAnyIP(gateway.GamePort, listen => listen
            .Use(next => ConnectionLimit.Middleware(next, gateway.MaxConnectionsPerAddress,
                listen.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(ConnectionLimit))))
            .Use(next => WebSocketGameTransport.Middleware(next,
                listen.ApplicationServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(WebSocketGameTransport))))
            .UseConnectionHandler<TcpGameConnectionHandler>());
    });

    var app = builder.Build();

    // the silo migrates the database and imports the seed (its own services read it); the others use what is there
    if (roles.Silo && SiloPool.RunsHotelJobs(orleans.Pool))
        await app.Services.InitializeDatabaseAsync();

    if (!roles.Web)
    {
        app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
        app.Run();
        return;
    }

    if (web.BehindProxy)
    {
        var forwarded = new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
        {
            ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                               | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto,
        };
        // the proxy is whatever sits in front: the server is reachable through nothing else
        forwarded.KnownIPNetworks.Clear();
        forwarded.KnownProxies.Clear();
        app.UseForwardedHeaders(forwarded);
    }

    if (web.NoIndex)
        app.Use((context, next) =>
        {
            context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
            return next(context);
        });

    // the browser client is served from another origin; the policy is registered by AddWebSeam
    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapAuthEndpoints();
    app.MapHotelEndpoints();
    app.MapNewsEndpoints();
    app.MapInviteEndpoints();
    app.MapGamedataEndpoints(configuration);
    app.MapMirrorEndpoints();
    app.MapPhotoEndpoints();
    app.MapHabboImagingEndpoints();
    app.MapClientDiagnosticsEndpoints();
    app.MapRoomImportEndpoints();
    app.Run();
}
catch (Exception ex)
{
    // The host can fail before it has a logger of its own — a port already in use is the
    // usual one — so this stands apart from the application's logging.
    using var logging = LoggerFactory.Create(logger => logger.AddConsole());
    logging.CreateLogger("Habbo.Host").LogCritical(ex, "Host terminated unexpectedly");
    // a failed start is a failure to whatever runs the host (a service manager, Docker), so it may restart it
    Environment.ExitCode = 1;
}