using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;
using Habbo.Silo.Abstractions.Wired;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents;

/// <summary>842 UpdateCondition — a condition's dialog saved.</summary>
internal sealed class UpdateConditionHandler : IMessageHandler<UpdateConditionMessageEvent>
{
    public Task<bool> HandleAsync(GatewaySessionContext context, UpdateConditionMessageEvent m, CancellationToken ct) =>
        WiredSave.SendAsync(context, new WiredSaveData(WiredKind.Condition, m.ItemId, [.. m.IntParams], m.StringParam,
            [.. m.StuffIds], [.. m.StuffIds2], [.. m.FurniSources], [.. m.UserSources], [.. m.VariableIds],
            Quantifier: m.Quantifier));
}
