using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Habbo.Silo.Users;

/// <summary>Names as the hotel allows them: within the configured length, letters, digits and a few extra
/// characters, no filtered word, and nobody else's whatever the case.</summary>
public sealed class NameRules(IDbContextFactory<AppDbContext> dbFactory, IOptions<AccountOptions> accountOptions, WordFilter wordFilter)
    : INameRules
{
    public async Task<NameVerdict> JudgeAsync(string name, string? currentName = null)
    {
        var options = accountOptions.Value;
        if (name.Length < options.NameMinLength)
            return NameVerdict.TooShort;
        if (name.Length > options.NameMaxLength)
            return NameVerdict.TooLong;
        if (!name.All(c => char.IsAsciiLetterOrDigit(c) || options.NameExtraCharacters.Contains(c))
            || !await wordFilter.IsCleanAsync(name))
            return NameVerdict.Invalid;

        // a player may take their own name in another case
        if (string.Equals(name, currentName, StringComparison.OrdinalIgnoreCase))
            return NameVerdict.Available;

        await using var db = await dbFactory.CreateDbContextAsync();
        var lower = name.ToLowerInvariant();
        return await db.Users.AnyAsync(u => u.Username.ToLower() == lower) ? NameVerdict.Taken : NameVerdict.Available;
    }
}
