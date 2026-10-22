using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Habbo.Gateway;

/// <summary>Caps how many game connections one address may hold open at once, so one machine cannot take up the
/// port with idle sockets. The first middleware on the game port: a connection over the cap is closed before
/// anything reads from it.</summary>
public static class ConnectionLimit
{
    private static readonly ConcurrentDictionary<IPAddress, int> Open = new();

    /// <summary>Kestrel connection middleware: <c>listen.Use(next => ConnectionLimit.Middleware(next, max, logger))</c>.
    /// A cap of 0 or less leaves connections uncapped.</summary>
    public static ConnectionDelegate Middleware(ConnectionDelegate next, int maxPerAddress, ILogger logger) =>
        async connection =>
        {
            if (maxPerAddress <= 0 || connection.RemoteEndPoint is not IPEndPoint { Address: var address })
            {
                await next(connection);
                return;
            }

            if (Open.AddOrUpdate(address, 1, (_, count) => count + 1) > maxPerAddress)
            {
                Release(address);
                logger.LogWarning("Connection {ConnectionId} from {Address} refused: {Max} already open from there",
                    connection.ConnectionId, address, maxPerAddress);
                return;
            }

            try
            {
                await next(connection);
            }
            finally
            {
                Release(address);
            }
        };

    private static void Release(IPAddress address)
    {
        // the count is dropped when it reaches nought, so addresses seen once do not stay for ever
        if (Open.AddOrUpdate(address, 0, (_, count) => count - 1) <= 0)
            Open.TryRemove(new KeyValuePair<IPAddress, int>(address, 0));
    }
}
