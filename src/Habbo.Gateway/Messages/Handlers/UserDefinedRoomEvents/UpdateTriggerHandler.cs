using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;
using Habbo.Silo.Abstractions.Wired;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents;

/// <summary>478 UpdateTrigger — a trigger's dialog saved.</summary>
internal sealed class UpdateTriggerHandler : IMessageHandler<UpdateTriggerMessageEvent>
{
    public Task<bool> HandleAsync(GatewaySessionContext context, UpdateTriggerMessageEvent m, CancellationToken ct) =>
        WiredSave.SendAsync(context, new WiredSaveData(WiredKind.Trigger, m.ItemId, [.. m.IntParams], m.StringParam,
            [.. m.StuffIds], [.. m.StuffIds2], [.. m.FurniSources], [.. m.UserSources], [.. m.VariableIds]));
}
