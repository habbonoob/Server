namespace Habbo.Silo.Rooms;

/// <summary>A space as rented: by whom, and until when.</summary>
public sealed record RentedSpace(Guid RenterId, int RenterAccountId, string RenterName, DateTimeOffset Until);
