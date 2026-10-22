namespace Habbo.Domain.Users;

/// <summary>A chat bubble a player has bought. The free ones need no row; these are the styles the
/// chat style selector marks as purchased.</summary>
public sealed class UserChatStyle
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public int StyleId { get; set; }
}
