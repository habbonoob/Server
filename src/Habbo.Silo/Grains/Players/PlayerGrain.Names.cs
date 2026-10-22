using Habbo.Protocol.Messages.Outgoing.Avatar;
using Habbo.Silo.Abstractions.Players;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Players;

/// <summary>Changing the player's name, when the hotel allows them one change.</summary>
public sealed partial class PlayerGrain
{
    /// <summary>How far a taken name's suggestions number themselves.</summary>
    private const int SuggestionNumberLimit = 1000;

    /// <summary>How many candidates to try before giving up on finding free suggestions.</summary>
    private const int SuggestionAttemptsPerSuggestion = 10;

    public async Task CheckNameAsync(string name)
    {
        var (code, suggestions) = await JudgeNameAsync(name);
        Send(new CheckUserNameResultMessageComposer(code, name, suggestions));
    }

    public async Task<PlayerData?> ChangeNameAsync(string name)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await LoadUserAsync(db);
        if (!user.NameChangeAllowed)
        {
            Send(new ChangeUserNameResultMessageComposer(NameResultCodes.ChangeNotAllowed, name, []));
            return null;
        }

        var (code, suggestions) = await JudgeNameAsync(name, user.Username);
        if (code != NameResultCodes.Available)
        {
            Send(new ChangeUserNameResultMessageComposer(code, name, suggestions));
            return null;
        }

        user.Username = name;
        user.NameChangeAllowed = false;
        await db.SaveChangesAsync();

        Send(new ChangeUserNameResultMessageComposer(NameResultCodes.Available, name, []));
        return Remember(user);
    }

    /// <summary>Whether a name will do, and when it is taken, a few free ones like it.</summary>
    private async Task<(int Code, IReadOnlyList<string> Suggestions)> JudgeNameAsync(string name, string? currentName = null) =>
        await nameRules.JudgeAsync(name, currentName) switch
        {
            NameVerdict.TooShort => (NameResultCodes.TooShort, []),
            NameVerdict.TooLong => (NameResultCodes.TooLong, []),
            NameVerdict.Invalid => (NameResultCodes.Invalid, []),
            NameVerdict.Taken => (NameResultCodes.Taken, await SuggestNamesAsync(name)),
            _ => (NameResultCodes.Available, []),
        };

    private async Task<bool> IsNameTakenAsync(string name)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var lower = name.ToLowerInvariant();
        return await db.Users.AnyAsync(u => u.Username.ToLower() == lower);
    }

    private async Task<IReadOnlyList<string>> SuggestNamesAsync(string name)
    {
        var options = accountOptions.Value;
        var suggestions = new List<string>();
        for (var attempt = 0;
             suggestions.Count < options.NameSuggestionCount
             && attempt < options.NameSuggestionCount * SuggestionAttemptsPerSuggestion;
             attempt++)
        {
            var suffix = Random.Shared.Next(SuggestionNumberLimit).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var stem = name[..Math.Min(name.Length, options.NameMaxLength - suffix.Length)];
            var candidate = stem + suffix;
            if (!suggestions.Contains(candidate) && !await IsNameTakenAsync(candidate))
                suggestions.Add(candidate);
        }

        return suggestions;
    }
}
