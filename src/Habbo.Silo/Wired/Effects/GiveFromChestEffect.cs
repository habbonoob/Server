using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Transactions;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Gives its users something from its chests: a set amount — a number, or a variable's value — or all
/// they hold. All of it can only go to one user. The users are shown what they got, with the box's text.</summary>
public abstract class GiveFromChestEffect : WiredEffect
{
    protected const int Mode = 0;
    private const int Number = 1;
    private const int ReferenceOption = 2;
    private const int ReferenceTarget = 3;
    private const int OpenByDefault = 4;

    /// <summary>The box's own last setting: the earnings category, or the iteration.</summary>
    protected const int Extra = 5;

    private const int GiveAmount = 0;
    private const int GiveAll = 1;
    private const int FromVariable = 1;

    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.EitherPicks, WiredSlot.EitherPicks.DefaultingTo(WiredSources.SelectedFurni)];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users, WiredSlot.Users.DefaultingTo(WiredSources.SelectedUsers)];

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        var chests = run.Furni(box).ToList();
        // bots and pets have no account to be paid into
        var users = run.Users(box).Where(user => user.Bot is null && user.Pet is null).ToList();
        if (users.Count == 0)
            return;

        int? amount = null;
        if (box.IntParam(Mode) != GiveAll)
        {
            amount = WiredVariableUse.Reference(box, run, box.IntParam(ReferenceOption) == FromVariable, box.IntParam(Number, 1),
                box.IntParam(ReferenceTarget), variableIndex: 0, slot: 1);
            if (amount is not > 0)
                return;
        }
        else if (users.Count > 1)
        {
            foreach (var user in users)
                run.Room.Chests.Fail(user, WiredTransactionFailure.AllContentsToManyUsers);
            return;
        }

        var gift = new WiredChestGift(box.Id, box.Settings.StringParam, box.Flag(OpenByDefault));
        foreach (var user in users)
            await GiveAsync(box, run, chests, user, amount, gift);
    }

    protected abstract Task GiveAsync(WiredBox box, WiredRun run, List<Rooms.RoomItemStore.PlacedItem> chests,
        Rooms.RoomEntityStore.RoomEntity user, int? amount, WiredChestGift gift);
}
