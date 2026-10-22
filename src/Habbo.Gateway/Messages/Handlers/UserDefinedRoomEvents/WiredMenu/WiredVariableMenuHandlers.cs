using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu;
using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu.VariablesManagement;
using Habbo.Silo.Abstractions.Wired;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredMenu;

/// <summary>3979 WiredGetVariablesForObject.</summary>
internal sealed class WiredGetVariablesForObjectHandler : IMessageHandler<WiredGetVariablesForObjectMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredGetVariablesForObjectMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.GetWiredVariablesForObjectAsync(player.Id, message.Target, message.ObjectId);
        return true;
    }
}

/// <summary>1698 WiredSetObjectVariableValue.</summary>
internal sealed class WiredSetObjectVariableValueHandler : IMessageHandler<WiredSetObjectVariableValueMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredSetObjectVariableValueMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.EditWiredVariableAsync(player.Id,
            new WiredVariableEdit(message.Target, message.ObjectId, message.VariableId, message.Value, message.Action));
        return true;
    }
}

/// <summary>3999 WiredGetAllVariableHolders.</summary>
internal sealed class WiredGetAllVariableHoldersHandler : IMessageHandler<WiredGetAllVariableHoldersMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredGetAllVariableHoldersMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.GetWiredVariableHoldersAsync(player.Id, message.VariableId);
        return true;
    }
}

/// <summary>688 WiredGetVariableOwnersPage.</summary>
internal sealed class WiredGetVariableOwnersPageHandler : IMessageHandler<WiredGetVariableOwnersPageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredGetVariableOwnersPageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.GetWiredVariableOwnersAsync(player.Id,
            new WiredOwnersQuery(message.VariableId, message.Page, message.PageSize, message.SortType, message.UserTypeFilter));
        return true;
    }
}

/// <summary>2292 WiredGetUserPermanentVariables.</summary>
internal sealed class WiredGetUserPermanentVariablesHandler : IMessageHandler<WiredGetUserPermanentVariablesEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredGetUserPermanentVariablesEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.GetWiredUserPermanentVariablesAsync(player.Id, message.EntityType, message.EntityId);
        return true;
    }
}

/// <summary>714 WiredSetUserPermanentVariable.</summary>
internal sealed class WiredSetUserPermanentVariableHandler : IMessageHandler<WiredSetUserPermanentVariableEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredSetUserPermanentVariableEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.EditWiredUserPermanentVariableAsync(player.Id, message.EntityType,
            new WiredVariableEdit(message.EntityType, message.EntityId, message.VariableId, message.Value, message.Action));
        return true;
    }
}
