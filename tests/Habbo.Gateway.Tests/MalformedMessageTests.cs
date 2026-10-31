using Habbo.Gateway.Messages;
using Habbo.Protocol.IO;
using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Tests;

public class MalformedMessageTests
{
    private sealed class NeverCalled : IMessageHandler<MoveAvatarMessageEvent>
    {
        public Task<bool> HandleAsync(GatewaySessionContext context, MoveAvatarMessageEvent message, CancellationToken ct) =>
            throw new InvalidOperationException("a message that did not read reached its handler");
    }

    [Fact]
    public async Task AMessageShorterThanItsLayoutIsMalformedNotAFailure()
    {
        // MoveAvatar is two ints; one arrived
        var handler = new ParsingMessageHandler<MoveAvatarMessageEvent>(new NeverCalled());

        var thrown = await Assert.ThrowsAsync<MalformedMessageException>(() =>
            handler.HandleAsync(null!, new MessageReader(new byte[] { 0, 0, 0, 7 }), CancellationToken.None));

        Assert.Equal(nameof(MoveAvatarMessageEvent), thrown.Name);
    }
}
