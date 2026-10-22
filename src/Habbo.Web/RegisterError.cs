namespace Habbo.Web;

/// <summary>What a refused registration says went wrong, for the website to explain.</summary>
public static class RegisterError
{
    public const string Password = "password";
    public const string Email = "email";
    public const string NameTooShort = "name_too_short";
    public const string NameTooLong = "name_too_long";
    public const string NameInvalid = "name_invalid";
    public const string NameTaken = "name_taken";
    public const string InviteCode = "invite_code";
}
