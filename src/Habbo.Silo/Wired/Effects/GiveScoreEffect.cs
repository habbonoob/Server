namespace Habbo.Silo.Wired.Effects;

/// <summary>Gives (or, negative, takes) points to the teams of its users, at most so many times per
/// game for each user (the dialog's "unlimited" is 0).</summary>
[WiredBox("wf_act_give_score")]
public class GiveScoreEffect : WiredEffect
{
    protected const int Points = 0;
    protected const int TimesPerGame = 1;
    private const int Unlimited = 0;

    public override int Code => EffectCodes.GiveScore;
    public override bool TargetsUsers => true;
    public override IReadOnlyList<int> DefaultIntParams => [5, Unlimited];

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        foreach (var (counted, team) in Scorers(box, run).ToList())
        {
            if (TakeTurn(box, run, counted))
                await run.Room.Games.AddScoreAsync(team, box.IntParam(Points, 1));
        }
    }

    /// <summary>Who scores, each with the key their times are counted under and the team the points go to: here each
    /// user in a team, counted on their own, as Habbo counts it (one player's scoring does not use up another's).</summary>
    protected virtual IEnumerable<(int Counted, int Team)> Scorers(WiredBox box, WiredRun run) =>
        run.Users(box).Where(user => user.Team != Rooms.RoomGames.NoTeam).Select(user => (user.Player.AccountId, user.Team));

    /// <summary>Counts one more time in this game for the key; false once it has used up its times.</summary>
    private static bool TakeTurn(WiredBox box, WiredRun run, int counted)
    {
        var game = run.Room.Games.GameNumber;
        if (box.CountedGame != game)
        {
            box.CountedGame = game;
            box.TimesThisGame.Clear();
        }

        var limit = box.IntParam(TimesPerGame);
        var times = box.TimesThisGame.GetValueOrDefault(counted);
        if (limit != Unlimited && times >= limit)
            return false;

        box.TimesThisGame[counted] = times + 1;
        return true;
    }
}
