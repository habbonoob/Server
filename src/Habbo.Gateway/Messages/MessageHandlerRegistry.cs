using System.Reflection;
using Habbo.Protocol.Messages;

namespace Habbo.Gateway.Messages;

/// <summary>Builds the header→handler lookups. A handler that implements IMessageHandler&lt;T&gt;
/// gets its header from the message type and is wrapped so the dispatcher can call it;
/// a handler that reads the payload itself declares its header with an attribute. Either kind
/// lands in the pre-crypto table when it carries [PreCrypto].
/// Throws at startup on duplicate bindings, so registration drift can't go unnoticed.</summary>
public sealed class MessageHandlerRegistry
{
    private readonly Dictionary<ushort, IMessageHandler> _postCrypto = new();
    private readonly Dictionary<ushort, IMessageHandler> _preCrypto = new();

    public MessageHandlerRegistry(IEnumerable<IMessageHandler> handlers, IEnumerable<object> parsedHandlers)
    {
        foreach (var handler in handlers)
        {
            // One class may bind several headers, so every attribute is taken.
            foreach (var attribute in handler.GetType().GetCustomAttributes<MessageHandlerAttribute>())
                Add(TableFor(handler.GetType()), attribute.Header, handler, handler.GetType().Name);
        }

        foreach (var handler in parsedHandlers)
        {
            var table = TableFor(handler.GetType());

            foreach (var (header, adapter) in Adapt(handler))
                Add(table, header, adapter, handler.GetType().Name);
        }
    }

    public IMessageHandler? FindPostCrypto(ushort header) => _postCrypto.GetValueOrDefault(header);

    public IMessageHandler? FindPreCrypto(ushort header) => _preCrypto.GetValueOrDefault(header);

    public static bool AllowsUnauthenticated(IMessageHandler handler) =>
        Target(handler).IsDefined(typeof(AllowUnauthenticatedAttribute));

    private Dictionary<ushort, IMessageHandler> TableFor(Type handlerType) =>
        handlerType.IsDefined(typeof(PreCryptoAttribute)) ? _preCrypto : _postCrypto;

    /// <summary>The handler's own type, looking through the parsing wrapper.</summary>
    private static Type Target(IMessageHandler handler) =>
        handler is IParsingMessageHandler parsing ? parsing.Inner.GetType() : handler.GetType();

    /// <summary>Wraps every IMessageHandler&lt;T&gt; the class implements, taking the header from T.</summary>
    private static IEnumerable<(ushort Header, IMessageHandler Adapter)> Adapt(object handler)
    {
        foreach (var contract in handler.GetType().GetInterfaces())
        {
            if (!contract.IsGenericType || contract.GetGenericTypeDefinition() != typeof(IMessageHandler<>))
                continue;

            var messageType = contract.GetGenericArguments()[0];
            var header = (ushort)messageType
                .GetProperty(nameof(IIncomingMessage<DummyMessage>.Header),
                    BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null)!;

            var adapter = (IMessageHandler)Activator.CreateInstance(
                typeof(ParsingMessageHandler<>).MakeGenericType(messageType), handler)!;

            yield return (header, adapter);
        }
    }

    private static void Add(
        Dictionary<ushort, IMessageHandler> target, ushort header, IMessageHandler handler, string name)
    {
        if (!target.TryAdd(header, handler))
        {
            throw new InvalidOperationException(
                $"Duplicate message handler for header {header}: " +
                $"{Target(target[header]).Name} and {name}.");
        }
    }

    /// <summary>Only exists so nameof can reach the interface's static member.</summary>
    private sealed record DummyMessage : IIncomingMessage<DummyMessage>
    {
        public static ushort Header => 0;
        public static DummyMessage Read(Habbo.Protocol.IO.MessageReader reader) => new();
    }
}

/// <summary>Lets the registry see through the parsing wrapper to the handler's own attributes.</summary>
internal interface IParsingMessageHandler
{
    object Inner { get; }
}
