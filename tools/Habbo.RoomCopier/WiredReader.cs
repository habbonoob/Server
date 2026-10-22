using System.Collections.Concurrent;
using Habbo.Domain.Rooms.Copies;
using Xabbo.Messages;

namespace Habbo.RoomCopier;

/// <summary>Reads wired boxes' settings from the dialogs the hotel answers with when a box is opened: the copier asks
/// for each box in turn and takes the answer it waits for, which the client then never sees (no dialog opens). The
/// layouts are those of the Flash client's WiredFurniTrigger, -Action, -Condition, -Selector, -Addon and -Variable
/// (the same our server writes, Habbo.Protocol's WiredFurni*Composer).</summary>
public sealed class WiredReader
{
    private static readonly HashSet<string> Dialogs =
        ["WiredFurniTrigger", "WiredFurniAction", "WiredFurniCondition", "WiredFurniSelector", "WiredFurniAddon", "WiredFurniVariable"];

    /// <summary>The answer to a request for the room's variables, in chunks, the last one flagged.</summary>
    private const string VariablesAnswer = "WiredAllVariablesDiffs";

    private readonly ConcurrentDictionary<long, TaskCompletionSource<RoomCopyWired>> _waiting = new();

    private readonly Lock _variablesLock = new();
    private TaskCompletionSource<IReadOnlyList<RoomCopyVariable>>? _variablesWaiting;
    private readonly List<RoomCopyVariable> _variables = [];

    /// <summary>The room's variables, when the list asked for comes within the time given; null otherwise.</summary>
    public async Task<IReadOnlyList<RoomCopyVariable>?> ExpectVariablesAsync(TimeSpan within)
    {
        var answer = new TaskCompletionSource<IReadOnlyList<RoomCopyVariable>>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_variablesLock)
        {
            _variables.Clear();
            _variablesWaiting = answer;
        }
        try
        {
            return await answer.Task.WaitAsync(within);
        }
        catch (TimeoutException)
        {
            return null;
        }
        finally
        {
            lock (_variablesLock)
                _variablesWaiting = null;
        }
    }

    /// <summary>Reads the room's variables from a chunk of the list while they are waited for. The client is let have
    /// the list too: it may have asked for it itself, and a full list only refreshes what it holds.</summary>
    public void ReadVariables(string name, IPacket packet)
    {
        if (name != VariablesAnswer)
            return;

        lock (_variablesLock)
        {
            if (_variablesWaiting is not { } answer)
                return;

            var position = packet.Position;
            try
            {
                packet.Read<int>(); // the list's hash
                var isLastChunk = packet.Read<bool>();
                Strings(packet); // the ids gone, none when asked for all
                for (var count = packet.Read<int>(); count > 0; count--)
                {
                    packet.Read<int>(); // the variable's hash
                    _variables.Add(Variable(packet));
                }
                if (isLastChunk)
                    answer.TrySetResult(_variables.ToList());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not read {name}: {ex.Message}");
            }
            finally
            {
                packet.Position = position;
            }
        }
    }

    /// <summary>A variable as the client is told it (WiredVariable); the import needs its id, kind, name and holder.</summary>
    private static RoomCopyVariable Variable(IPacket p)
    {
        var id = p.Read<string>();
        var kind = p.Read<int>();
        var name = p.Read<string>();
        p.Read<int>(); // how long its values last
        var target = p.Read<int>();
        // always there, may be created and deleted, has a value, may be written, may be intercepted, invisible, has a
        // creation time, has an update time
        for (var flags = 0; flags < 8; flags++)
            p.Read<bool>();
        if (p.Read<bool>()) // a text connector: the texts its values stand for
        {
            for (var count = p.Read<int>(); count > 0; count--)
            {
                p.Read<int>();
                p.Read<string>();
            }
        }
        return new RoomCopyVariable(id, kind, name, target);
    }

    /// <summary>The settings of the box with this id, when its dialog comes within the time given; null otherwise.</summary>
    public async Task<RoomCopyWired?> ExpectAsync(long itemId, TimeSpan within)
    {
        var answer = new TaskCompletionSource<RoomCopyWired>(TaskCreationOptions.RunContinuationsAsynchronously);
        _waiting[itemId] = answer;
        try
        {
            return await answer.Task.WaitAsync(within);
        }
        catch (TimeoutException)
        {
            return null;
        }
        finally
        {
            _waiting.TryRemove(itemId, out _);
        }
    }

    /// <summary>Whether the packet is the dialog of a box being waited for, which is then taken: the caller blocks it.</summary>
    public bool TryTake(string name, IPacket packet)
    {
        if (!Dialogs.Contains(name) || _waiting.IsEmpty)
            return false;

        var position = packet.Position;
        try
        {
            var (itemId, wired) = Read(name, packet);
            if (!_waiting.TryGetValue(itemId, out var answer))
                return false;
            answer.TrySetResult(wired);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not read {name}: {ex.Message}");
            return false;
        }
        finally
        {
            packet.Position = position;
        }
    }

    private static (long ItemId, RoomCopyWired Wired) Read(string name, IPacket p)
    {
        // the box, as every dialog starts
        p.Read<int>(); // how many furni may be picked
        var stuffIds = Ints(p);
        var stuffIds2 = Ints(p);
        p.Read<int>(); // the box's furnidata id
        var itemId = p.Read<int>();
        var stringParam = p.Read<string>();
        var intParams = Ints(p);
        var variableIds = Strings(p);
        var furniSources = Ints(p);
        var userSources = Ints(p);
        p.Read<int>(); // the box's code
        var wired = new RoomCopyWired(stuffIds.Select(id => (long)id).ToList(), stuffIds2.Select(id => (long)id).ToList(),
            stringParam, intParams, variableIds, furniSources, userSources, 0, 0, false, false);

        switch (name)
        {
            case "WiredFurniAction":
                wired = wired with { DelayInPulses = p.Read<int>() };
                break;
            case "WiredFurniCondition":
                var quantifier = p.Read<int>();
                SkipOptions(p);
                p.Read<byte>(); // the quantifier's kind
                wired = wired with { Quantifier = quantifier, IsInvert = p.Read<bool>() };
                break;
            case "WiredFurniSelector":
                wired = wired with { IsFilter = p.Read<bool>(), IsInvert = p.Read<bool>() };
                break;
        }
        return (itemId, wired);
    }

    /// <summary>The dialog's choices of sources: the advanced mode, the furni and user source slots, their defaults, and
    /// whether wall furni may be picked.</summary>
    private static void SkipOptions(IPacket p)
    {
        p.Read<bool>();
        for (var slots = p.Read<int>(); slots > 0; slots--)
            Ints(p);
        for (var slots = p.Read<int>(); slots > 0; slots--)
            Ints(p);
        Ints(p);
        Ints(p);
        p.Read<bool>();
    }

    private static List<int> Ints(IPacket p)
    {
        var count = p.Read<int>();
        var ints = new List<int>(count);
        for (var i = 0; i < count; i++)
            ints.Add(p.Read<int>());
        return ints;
    }

    private static List<string> Strings(IPacket p)
    {
        var count = p.Read<int>();
        var strings = new List<string>(count);
        for (var i = 0; i < count; i++)
            strings.Add(p.Read<string>());
        return strings;
    }
}
