using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;
using Habbo.Silo.Abstractions.Wired;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents;

/// <summary>2511 UpdateVariable — a variable box's dialog saved.</summary>
internal sealed class UpdateVariableHandler : IMessageHandler<UpdateVariableMessageEvent>
{
    public Task<bool> HandleAsync(GatewaySessionContext context, UpdateVariableMessageEvent m, CancellationToken ct) =>
        WiredSave.SendAsync(context, new WiredSaveData(WiredKind.Variable, m.ItemId, [.. m.IntParams], m.StringParam,
            [.. m.StuffIds], [.. m.StuffIds2], [.. m.FurniSources], [.. m.UserSources], [.. m.VariableIds]));
}
