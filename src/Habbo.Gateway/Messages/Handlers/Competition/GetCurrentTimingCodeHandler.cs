using Habbo.Protocol.Messages.Incoming.Competition;
using Habbo.Protocol.Messages.Outgoing.Competition;
using Habbo.Silo.Abstractions.Hotel;

namespace Habbo.Gateway.Messages.Handlers.Competition;

/// <summary>1137 GetCurrentTimingCode — answer a landing-view widget with whatever its schedule
/// is showing. Several widgets ask at once, so the name asked about goes back with the answer.
/// </summary>
internal sealed class GetCurrentTimingCodeHandler(ITimingCodeService timingCodes)
    : IMessageHandler<GetCurrentTimingCodeMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, GetCurrentTimingCodeMessageEvent message, CancellationToken ct)
    {
        var code = await timingCodes.GetCurrentCodeAsync(message.SchedulingKey, ct);
        await context.SendAsync(new CurrentTimingCodeMessageComposer(message.SchedulingKey, code), ct);
        return true;
    }
}
