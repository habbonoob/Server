using System.Reflection;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.Handshake;

namespace Habbo.Protocol.Tests;

/// <summary>Guards the message layer the same way the handler registry guards inbound messages:
/// every message is named after the client's event class and carries that class's header.</summary>
public class ComposerCoverageTests
{
    private static IReadOnlyList<Type> MessageTypes =>
        typeof(IComposer).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IComposer).IsAssignableFrom(type))
            .ToList();

    private static Dictionary<string, ushort> DeclaredHeaders =>
        typeof(ServerMessages)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(header => header.FieldType == typeof(ushort))
            .ToDictionary(header => header.Name, header => (ushort)header.GetRawConstantValue()!);

    [Fact]
    public void MessagesExistForTheOutboundPacketsWeSend()
    {
        Assert.NotEmpty(MessageTypes);
    }

    [Fact]
    public void EveryMessageIsSourceGenerated()
    {
        var handWritten = MessageTypes
            .Where(type => type.GetCustomAttribute<Habbo.Protocol.Wire.MessageAttribute>() is null)
            .Select(type => type.Name)
            .ToList();

        Assert.True(handWritten.Count == 0,
            "Messages written by hand rather than generated from a [Message] record: " + string.Join(", ", handWritten));
    }

    [Fact]
    public void EveryMessageIsNamedAfterItsClientEventClass()
    {
        // The client's event classes are the vocabulary: UserObjectComposer is named UserObjectComposer.
        var headers = DeclaredHeaders;

        var unmatched = MessageTypes
            .Select(type => type.Name)
            .Where(name => !headers.ContainsKey(name))
            .ToList();

        Assert.True(unmatched.Count == 0,
            $"Messages whose name does not match a ServerMessages entry: {string.Join(", ", unmatched)}");
    }

    [Fact]
    public void EveryMessageCarriesTheHeaderItIsNamedAfter()
    {
        // Catches a message that was copied from another and kept the wrong [Message(...)] header:
        // the packet would be well-formed but land in the wrong client handler.
        var headers = DeclaredHeaders;
        var mismatched = new List<string>();

        foreach (var type in MessageTypes)
        {
            if (!headers.TryGetValue(type.Name, out var expected))
                continue;

            var instance = (IComposer)FormatterServices_CreateUninitialized(type);
            if (instance.Header != expected)
                mismatched.Add($"{type.Name} declares {instance.Header}, expected {expected}");
        }

        Assert.True(mismatched.Count == 0, string.Join("; ", mismatched));
    }

    /// <summary>The header is a constant expression, so it can be read without running a
    /// constructor whose arguments we would otherwise have to invent.</summary>
    private static object FormatterServices_CreateUninitialized(Type type) =>
        System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
}
