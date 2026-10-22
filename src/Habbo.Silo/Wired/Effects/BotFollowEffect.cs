namespace Habbo.Silo.Wired.Effects;

/// <summary>Sets each of its bots following the first of its users, or stops it following anyone.</summary>
[WiredBox("wf_act_bot_follow_avatar")]
public sealed class BotFollowEffect : WiredEffect
{
    private const int Following = 0;
    private const int Start = 1;
    private const int Followed = 1;

    public override int Code => EffectCodes.BotFollowAvatar;
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Bots, WiredSlot.Users];
    public override IReadOnlyList<int> DefaultIntParams => [Start];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override Task RunAsync(WiredBox box, WiredRun run)
    {
        var followed = box.IntParam(Following) == Start
            ? run.Users(box, Followed).FirstOrDefault(user => user.Bot is null && user.Pet is null)
            : null;
        if (box.IntParam(Following) == Start && followed is null)
            return Task.CompletedTask;

        foreach (var bot in WiredBots.Of(box, run))
            run.Room.Follow(bot, followed);
        return Task.CompletedTask;
    }
}
