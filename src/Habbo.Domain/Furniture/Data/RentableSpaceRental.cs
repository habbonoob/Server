using Habbo.Domain.Users;

namespace Habbo.Domain.Furniture.Data;

/// <summary>A rentable space someone rents: who, and until when. There is a row only while it is rented.</summary>
public sealed class RentableSpaceRental
{
    public int ItemId { get; set; }
    public UserItem? Item { get; set; }

    public Guid RenterId { get; set; }
    public User? Renter { get; set; }

    public DateTimeOffset RentedUntil { get; set; }
}
