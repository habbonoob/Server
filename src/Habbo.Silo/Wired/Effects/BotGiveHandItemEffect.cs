namespace Habbo.Silo.Wired.Effects;

/// <summary>Gives its users a hand item: brought over by the bot the box names, which walks up to each in turn, or
/// straight into their hand when the box names no bot.</summary>
[WiredBox("wf_act_bot_give_handitem")]
public sealed class BotGiveHandItemEffect : WiredEffect
{
    private const int HandItem = 0;
    private const int Receivers = 1;

    public override int Code => EffectCodes.BotGiveHandItem;
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Bots, WiredSlot.Users];
    public override IReadOnlyList<int> DefaultIntParams => [2];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        var handItem = Math.Max(0, box.IntParam(HandItem));
        var receivers = run.Users(box, Receivers).Where(user => user.Bot is null && user.Pet is null).ToList();
        if (receivers.Count == 0)
            return;

        if (WiredBots.Name(box).Length == 0)
        {
            foreach (var receiver in receivers)
                await run.Room.HandOverAsync(null, receiver, handItem);
            return;
        }

        // each bot visits its receivers in turn
        foreach (var bot in WiredBots.Of(box, run))
            await Bring(bot, 0);

        Task Bring(Rooms.RoomEntityStore.RoomEntity bot, int index) => index >= receivers.Count
            ? Task.CompletedTask
            : run.Room.BringAsync(bot, receivers[index], async () =>
            {
                await run.Room.HandOverAsync(bot, receivers[index], handItem);
                await Bring(bot, index + 1);
            });
    }
}
