using System.Text.RegularExpressions;
using Habbo.Protocol.Messages.Outgoing.Users;
using Habbo.Silo.Chat;
using Microsoft.Extensions.Options;

namespace Habbo.Silo.Pets;

/// <summary>Whether a name will do for a pet: its length, its characters and the word filter. Answers
/// as the client's approve-name result, with what its message needs.</summary>
public sealed class PetNames(IOptions<PetOptions> options, WordFilter wordFilter)
{
    public async Task<(int Result, string Info)> CheckAsync(string name)
    {
        var rules = options.Value;
        if (name.Length > rules.NameMaxLength)
            return (ApproveNameMessageComposer.TooLong, rules.NameMaxLength.ToString());
        if (name.Trim().Length < rules.NameMinLength)
            return (ApproveNameMessageComposer.TooShort, rules.NameMinLength.ToString());

        var forbidden = string.Concat(name.Where(c => !Regex.IsMatch(c.ToString(), $"[{rules.NameCharacters}]")).Distinct());
        if (forbidden.Length > 0)
            return (ApproveNameMessageComposer.ForbiddenCharacters, forbidden);

        return (await wordFilter.FilterAsync(name)).FirstHit is { } hit
            ? (ApproveNameMessageComposer.ForbiddenWords, hit)
            : (ApproveNameMessageComposer.Accepted, string.Empty);
    }
}
