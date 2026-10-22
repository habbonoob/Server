using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;
using Habbo.Silo.Abstractions.Wired;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents;

/// <summary>2693 UpdateAction — an effect's dialog saved.</summary>
internal sealed class UpdateActionHandler : IMessageHandler<UpdateActionMessageEvent>
{
    public Task<bool> HandleAsync(GatewaySessionContext context, UpdateActionMessageEvent m, CancellationToken ct) =>
        WiredSave.SendAsync(context, new WiredSaveData(WiredKind.Effect, m.ItemId, [.. m.IntParams], m.StringParam,
            [.. m.StuffIds], [.. m.StuffIds2], [.. m.FurniSources], [.. m.UserSources], [.. m.VariableIds],
            DelayInPulses: m.DelayInPulses));
}
