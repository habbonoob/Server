using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.VariableFx;
using Habbo.Silo.Rooms;
using Habbo.Silo.Wired.Addons.VariableFx;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired;

/// <summary>The room's variable displays: their configs when one comes, is saved or goes, and when someone comes in; and
/// their holders' values, as they change (Habbo: experiment.fx).</summary>
public sealed partial class RoomWired
{
    /// <summary>Every display in the room, with the variable it shows: the user or furni variable whose box stands in its
    /// stack, of the kind it shows over.</summary>
    private IEnumerable<(WiredBox Box, VariableFxAddon Fx, WiredVariable Variable)> Displays() =>
        _boxes.Values.Where(box => box.Type is VariableFxAddon).ToList()
            .Select(box => (box, (VariableFxAddon)box.Type, Variable: DisplayedVariable(box)))
            .Where(display => display.Variable is not null)
            .Select(display => (display.box, display.Item2, display.Variable!));

    private WiredVariable? DisplayedVariable(WiredBox display) =>
        StackAt(display.Item.X, display.Item.Y)?.FirstOrDefault(box => box.Type is WiredVariableBox) is { } definition
        && FindVariable(definition.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)) is { } variable
        && variable.Target == (VariableFxAddon.ForUsers(display) ? WiredVariableTargets.User : WiredVariableTargets.Furni)
            ? variable
            : null;

    /// <summary>A display came: everyone gets its config, and its audiences the values it starts with.</summary>
    private void DisplayPlaced(WiredBox box)
    {
        broadcast(new VariableFxConfigsMessageComposer([((VariableFxAddon)box.Type).Config(box)]));
        foreach (var (receiver, statuses) in Snapshot(Displays().Where(display => display.Box == box)))
            sendTo(receiver, new VariableFxStatusMessageComposer(AllInitialize: true, statuses));
    }

    /// <summary>A display was saved: its new config, as Habbo sends it, alone.</summary>
    private void DisplaySaved(WiredBox box) =>
        broadcast(new VariableFxConfigsMessageComposer([((VariableFxAddon)box.Type).Config(box)]));

    private void DisplayRemoved(WiredBox box) => broadcast(new VariableFxConfigsRemovedMessageComposer([box.Id]));

    /// <summary>Someone came in: the room's displays, and the values they may see.</summary>
    private void WelcomeToDisplays(RoomEntityStore.RoomEntity user)
    {
        var displays = Displays().ToList();
        if (displays.Count == 0 || user.Bot is not null || user.Pet is not null)
            return;

        sendTo(user.Player.Id, new VariableFxConfigsMessageComposer(displays.Select(display => display.Fx.Config(display.Box)).ToList()));
        foreach (var (receiver, statuses) in Snapshot(displays))
            sendTo(receiver, new VariableFxStatusMessageComposer(AllInitialize: true, statuses));
    }

    /// <summary>Every holder's value of the displays, gathered for each user in their audience.</summary>
    private Dictionary<Guid, List<VariableFxStatusData>> Snapshot(IEnumerable<(WiredBox Box, VariableFxAddon Fx, WiredVariable Variable)> displays)
    {
        var byReceiver = new Dictionary<Guid, List<VariableFxStatusData>>();
        foreach (var (box, fx, variable) in displays)
        {
            foreach (var (holder, value) in VariableHolders(variable).ToList())
            {
                var status = fx.Status(box, variable, holder, value, initialize: true, this);
                foreach (var receiver in fx.Viewers(box, holder, this))
                {
                    if (!byReceiver.TryGetValue(receiver.Player.Id, out var statuses))
                        byReceiver[receiver.Player.Id] = statuses = [];
                    statuses.Add(status);
                }
            }
        }

        return byReceiver;
    }

    /// <summary>A holder's value of a variable changed: the displays showing it tell their audiences, a value given or
    /// changed (the client works out which), one taken away by its key.</summary>
    private void DisplaysHear(WiredVariable variable, WiredHolder holder, WiredVariableChange change, int? value)
    {
        foreach (var (box, fx, _) in Displays().Where(display => display.Variable.Id == variable.Id))
        {
            if (change == WiredVariableChange.Deleted || value is not { } shown)
            {
                broadcast(new VariableFxStatusRemovedMessageComposer([VariableFxAddon.RemovedKey(box, variable, holder)]));
                continue;
            }

            var status = new VariableFxStatusMessageComposer(AllInitialize: false, [fx.Status(box, variable, holder, shown, initialize: false, this)]);
            foreach (var receiver in fx.Viewers(box, holder, this))
                sendTo(receiver.Player.Id, status);
        }
    }
}
