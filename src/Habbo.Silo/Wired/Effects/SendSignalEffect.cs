using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Sends a signal through its antennas to every stack that receives on them, carrying the
/// furni and users it forwards — as one signal, or one for each furni and/or each user, as the
/// dialog chooses. Its first furni slot is the antennas, its second the furni to forward.</summary>
[WiredBox("wf_act_send_signal")]
public class SendSignalEffect : WiredEffect
{
    private const int SplitFurni = 0;
    private const int SplitUsers = 1;

    /// <summary>The most signals one run of the box sends: split by every furni and every user through every
    /// antenna, one box could otherwise send millions in a single turn.</summary>
    private const int MaxSignalsPerRun = 100;

    public override int Code => EffectCodes.SendSignal;
    public override IReadOnlyList<int> DefaultIntParams => [0, 0];

    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.EitherPicks, WiredSlot.EitherPicks.DefaultingTo(WiredSources.SelectedFurni)];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users.DefaultingTo(WiredSources.SelectedUsers)];

    public override WiredPickRule? PickRule => new(WiredFurni.IsAntenna, "wiredfurni.error.require_antenna_furni");

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        var antennas = run.Furni(box, 0).Where(WiredFurni.IsAntenna).ToList();
        if (antennas.Count == 0)
            return;

        var furni = run.Furni(box, 1).ToList();
        var users = run.Users(box).ToList();
        IEnumerable<List<RoomItemStore.PlacedItem>> furniParts = box.Flag(SplitFurni) ? furni.Select(item => new List<RoomItemStore.PlacedItem> { item }) : [furni];
        var userParts = box.Flag(SplitUsers) ? users.Select(user => new List<RoomEntityStore.RoomEntity> { user }).ToList() : [users];

        var sent = 0;
        foreach (var furniPart in furniParts)
        {
            foreach (var userPart in userParts)
            {
                foreach (var antenna in antennas)
                {
                    if (++sent > MaxSignalsPerRun)
                        return;
                    await run.Room.SendSignalAsync(antenna, furniPart, userPart, run);
                }
            }
        }
    }
}
