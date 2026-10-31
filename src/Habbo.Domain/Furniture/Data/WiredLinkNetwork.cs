using Habbo.Domain.Users;

namespace Habbo.Domain.Furniture.Data;

/// <summary>A wired link network: a name a user gives to connect their WIRED Links (wf_link) across rooms, so the
/// messages and dataframes sent through one Link reach the others. Only its owner manages it; deleting it disconnects
/// every Link on it, in every room. The client knows it by its id as a decimal string, "0" being no network.</summary>
public sealed class WiredLinkNetwork
{
    /// <summary>The longest name the network dialog takes (wiredlink.error.3).</summary>
    public const int MaxNameLength = 255;

    public int Id { get; set; }

    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
