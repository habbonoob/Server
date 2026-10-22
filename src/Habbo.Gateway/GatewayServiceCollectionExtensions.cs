using System.Reflection;
using Habbo.Gateway.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Habbo.Silo.Abstractions.Hotel;

namespace Habbo.Gateway;

public static class GatewayServiceCollectionExtensions
{
    public static IServiceCollection AddGatewaySeam(this IServiceCollection services, IConfiguration configuration)
    {
        // a pong timeout no longer than the ping interval would close every client before it could answer
        services.AddOptions<GameNetworkOptions>()
            .Bind(configuration.GetSection("Gateway"))
            .Validate(options => options.PingIntervalSeconds <= 0 || options.PongTimeoutSeconds > options.PingIntervalSeconds,
                "Gateway:PongTimeoutSeconds must be longer than Gateway:PingIntervalSeconds")
            .ValidateOnStart();
        services.AddHostedService<SessionKeepAlive>();
        services.Configure<CryptoOptions>(configuration.GetSection("Crypto"));
        services.Configure<HotelOptions>(configuration.GetSection("Hotel"));
        services.AddSingleton<SessionManager>();
        services.AddSingleton<IClientPacketSink, ClientPacketSink>();
        services.AddSingleton<TcpGameConnectionHandler>();
        services.AddSingleton<Messages.PostAuthenticationSequence>();
        services.AddSingleton<Messages.FriendListEvents>();
        services.AddSingleton<Messages.PlayerNotifier>();
        services.AddSingleton<Messages.FriendPresenceNotifier>();

        // Message handlers: discovered from this assembly, constructed by DI. A handler either
        // parses nothing itself (IMessageHandler) or takes a parsed message (IMessageHandler<T>).
        var handlerTypes = typeof(GatewayServiceCollectionExtensions).Assembly.LoadableTypes()
            // The parsing wrapper is an open generic the registry closes itself.
            .Where(type => type is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false })
            .ToList();

        foreach (var handlerType in handlerTypes.Where(type => typeof(IMessageHandler).IsAssignableFrom(type)))
            services.AddSingleton(typeof(IMessageHandler), handlerType);

        foreach (var handlerType in handlerTypes.Where(HandlesAParsedMessage))
            services.AddKeyedSingleton(typeof(object), ParsedHandlerKey, handlerType);

        services.AddSingleton(provider => new MessageHandlerRegistry(
            provider.GetServices<IMessageHandler>(),
            provider.GetKeyedServices<object>(ParsedHandlerKey)));
        services.AddSingleton<MessageDispatcher>();
        return services;
    }

    /// <summary>The DI key the parsed handlers are registered under, so they can be resolved
    /// as a group without a marker interface on every one of them.</summary>
    private const string ParsedHandlerKey = "habbo.parsed-message-handler";

    private static bool HandlesAParsedMessage(Type type) =>
        type.GetInterfaces().Any(contract =>
            contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IMessageHandler<>));
}
