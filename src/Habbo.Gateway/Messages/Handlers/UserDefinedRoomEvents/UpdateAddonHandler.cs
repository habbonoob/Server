using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;
using Habbo.Silo.Abstractions.Wired;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents;

/// <summary>1144 UpdateAddon — an add-on's dialog saved.</summary>
internal sealed class UpdateAddonHandler : IMessageHandler<UpdateAddonMessageEvent>
{
    public Task<bool> HandleAsync(GatewaySessionContext context, UpdateAddonMessageEvent m, CancellationToken ct) =>
        WiredSave.SendAsync(context, new WiredSaveData(WiredKind.Addon, m.ItemId, [.. m.IntParams], m.StringParam,
            [.. m.StuffIds], [.. m.StuffIds2], [.. m.FurniSources], [.. m.UserSources], [.. m.VariableIds]));
}
