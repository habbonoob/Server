using System.Reflection;
using Habbo.Gateway.Messages;
using Habbo.Protocol;
using Habbo.Protocol.IO;

namespace Habbo.Gateway.Tests;

public class MessageHandlerRegistryTests
{
    /// <summary>The headers a handler binds by implementing IMessageHandler&lt;T&gt;.</summary>
    private static IEnumerable<ushort> ParsedMessageHeaders(Type handlerType) =>
        handlerType.GetInterfaces()
            .Where(contract => contract.IsGenericType
                               && contract.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
            .Select(contract => (ushort)contract.GetGenericArguments()[0]
                .GetProperty("Header", BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null)!);

    [Fact]
    public void EveryDeclaredClientMessageHasAHandler()
    {
        // Adding a message to ClientMessages without a handler must fail this test. A handler
        // either declares its header with an attribute or takes a parsed message that carries it.
        var handlerTypes = typeof(MessageHandlerRegistry).Assembly.LoadableTypes().ToList();

        var boundHeaders = handlerTypes
            .SelectMany(t => t.GetCustomAttributes<MessageHandlerAttribute>().Select(a => a.Header))
            .Concat(handlerTypes.SelectMany(ParsedMessageHeaders))
            .ToList();

        var declaredHeaders = typeof(ClientMessages).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => (ushort)f.GetValue(null)!)
            .ToList();

        var missing = declaredHeaders.Except(boundHeaders).ToList();
        Assert.True(missing.Count == 0,
            $"ClientMessages headers without a handler: {string.Join(", ", missing)}");

        var unknown = boundHeaders.Except(declaredHeaders).ToList();
        Assert.True(unknown.Count == 0,
            $"Handlers bound to headers missing from ClientMessages: {string.Join(", ", unknown)}");
    }

    [Fact]
    public void DuplicateBindingThrowsAtStartup()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new MessageHandlerRegistry(new IMessageHandler[] { new FakeHandlerA(), new FakeHandlerB() }, []));
    }

    [MessageHandler(9999)]
    private sealed class FakeHandlerA : IMessageHandler
    {
        public Task<bool> HandleAsync(GatewaySessionContext context, MessageReader reader, CancellationToken ct)
            => Task.FromResult(true);
    }

    [MessageHandler(9999)]
    private sealed class FakeHandlerB : IMessageHandler
    {
        public Task<bool> HandleAsync(GatewaySessionContext context, MessageReader reader, CancellationToken ct)
            => Task.FromResult(true);
    }
}
