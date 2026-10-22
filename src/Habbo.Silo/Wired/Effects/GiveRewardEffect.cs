using Habbo.Silo.Abstractions.Permissions;
using System.Globalization;
using Habbo.Domain.Furniture.Data;
using Habbo.Silo.Wired.Rewards;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Rewards its users with a badge or a product: drawn by chance, or each reward once to each user, as often as
/// the box allows and until it has given as many as it may.</summary>
[WiredBox("wf_act_give_reward")]
public sealed class GiveRewardEffect : WiredEffect
{
    private const int Interval = 0;
    private const int Unique = 1;
    private const int Limit = 2;
    private const int Every = 3;

    /// <summary>The dialog's bounds: rewards, the total limit, and the chances out of a hundred.</summary>
    private const int MaxRewards = 20;
    private const int MaxLimit = 1000;
    private const int MaxChance = 100;

    /// <summary>How the rewards are written: each "type,code,chance", separated by semicolons; type 0 a badge.</summary>
    private const char RewardSeparator = ';';
    private const char FieldSeparator = ',';
    private const string BadgeType = "0";

    /// <summary>Achievement badges are earned, never handed out by wired.</summary>
    private const string AchievementBadgePrefix = "ACH_";

    public override int Code => EffectCodes.GiveReward;
    public override bool TargetsUsers => true;
    public override IReadOnlyList<int> DefaultIntParams => [WiredRewardRules.Once, 0, 0, 1];

    /// <summary>Badges and furni out of nothing: staff set this up, not any room owner.</summary>
    public override string? Permission => PermissionNames.WiredGiveReward;

    public override WiredValidation? Validate(FurniWired settings)
    {
        // the dialog checks the rest itself; what it lets through that is still wrong is named as not existing
        var unique = settings.IntParams.ElementAtOrDefault(Unique) == 1;
        var texts = Texts(settings);
        var malformed = texts.Where(text => Parse(text) is not { } r || r.Code.Length == 0 || r.Code.Length > WiredRewardGrant.CodeMaxLength
                                            || (!unique && r.Probability is < 1 or > MaxChance)).ToList();
        if (malformed.Count > 0)
            return Refusal(malformed);

        var rewards = Rewards(settings);
        if (texts.Length > MaxRewards || (!unique && rewards.Sum(r => r.Probability) > MaxChance))
            return Refusal(texts);

        return rewards.Any(r => r.IsBadge && r.Code.StartsWith(AchievementBadgePrefix, StringComparison.OrdinalIgnoreCase))
            ? WiredValidation.Of("wiredfurni.error.achievement_not_allowed")
            : null;
    }

    public override async Task<WiredValidation?> ValidateAsync(FurniWired settings, IWiredRoom room)
    {
        var missing = await room.Rewards.MissingAsync(Rewards(settings));
        return missing.Count == 0 ? null : Refusal(missing);
    }

    private static WiredValidation Refusal(IEnumerable<string> products) =>
        new("wiredfurni.error.invalid_reward_products", new Dictionary<string, string> { ["products"] = string.Join(", ", products) });

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        var rules = new WiredRewardRules(
            Math.Clamp(box.IntParam(Interval), WiredRewardRules.Once, WiredRewardRules.Minutes),
            Math.Max(1, box.IntParam(Every, 1)),
            box.IntParam(Unique) == 1,
            Math.Clamp(box.IntParam(Limit), 0, MaxLimit),
            Rewards(box.Settings));
        // one user after another: each counts what the box has given before giving, so all at once would
        // each count none and a limit of one would reward everyone
        foreach (var user in run.Users(box).ToList())
            await run.Room.Rewards.RewardAsync(box.Id, user, rules, run.Room.Random);
    }

    private static string[] Texts(FurniWired settings) => settings.StringParam.Split(RewardSeparator, StringSplitOptions.RemoveEmptyEntries);

    private static List<WiredReward> Rewards(FurniWired settings) => Texts(settings).Select(Parse).OfType<WiredReward>().ToList();

    private static WiredReward? Parse(string text)
    {
        var fields = text.Split(FieldSeparator);
        return fields.Length == 3 && int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var chance)
            ? new WiredReward(fields[0] == BadgeType, fields[1].Trim(), chance)
            : null;
    }
}
