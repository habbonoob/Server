using Habbo.Domain.Users;

namespace Habbo.Web;

public sealed class AuthOptions
{
    public int TicketLifetimeMinutes { get; set; } = 5;

    /// <summary>How long the website keeps someone logged in without them coming back.</summary>
    public int SessionDays { get; set; } = 30;

    /// <summary>Registering needs an invite code: the hotel is open to those staff let in.</summary>
    public bool InviteOnly { get; set; } = true;

    public int PasswordMinLength { get; set; } = 8;

    /// <summary>The longest password taken; hashing a megabyte for every guess would make guessing a way to load the
    /// server.</summary>
    public int PasswordMaxLength { get; set; } = 128;

    /// <summary>The credits a new account starts with.</summary>
    public int StartingCredits { get; set; }

    /// <summary>The rank that may make and withdraw invite codes.</summary>
    public SecurityLevel InviteManagerRank { get; set; } = SecurityLevel.Administrator;

    /// <summary>Where the keys that sign session cookies are kept. Empty keeps them where ASP.NET does by default,
    /// which a container loses on restart, logging everyone out; a deployment points it at a volume.</summary>
    public string KeysDirectory { get; set; } = string.Empty;
}
