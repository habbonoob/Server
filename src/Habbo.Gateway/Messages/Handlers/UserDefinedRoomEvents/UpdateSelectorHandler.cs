using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;
using Habbo.Silo.Abstractions.Wired;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents;

/// <summary>2815 UpdateSelector — a selector's dialog saved.</summary>
internal sealed class UpdateSelectorHandler : IMessageHandler<UpdateSelectorMessageEvent>
{
    public Task<bool> HandleAsync(GatewaySessionContext context, UpdateSelectorMessageEvent m, CancellationToken ct) =>
        WiredSave.SendAsync(context, new WiredSaveData(WiredKind.Selector, m.ItemId, [.. m.IntParams], m.StringParam,
            [.. m.StuffIds], [.. m.StuffIds2], [.. m.FurniSources], [.. m.UserSources], [.. m.VariableIds],
            IsFilter: m.IsFilter, IsInvert: m.IsInvert));
}
