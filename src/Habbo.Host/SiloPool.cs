using Habbo.Silo.Grains.Rooms;
using Orleans.Configuration;

namespace Habbo.Host;

/// <summary>Which grains a silo holds: every one (no pool: one silo runs the whole hotel), the rooms alone ("rooms"),
/// or everything but the rooms ("core"). Orleans places a grain only on a silo that holds its class, so the rooms live
/// on silos of their own, which can be deployed without the players, their friends or their purse going: losing them
/// sends their visitors to the hotel view. Only a core silo (or one holding everything) migrates the database and runs
/// the hotel's timed jobs.</summary>
public static class SiloPool
{
    public const string Core = "core";
    public const string Rooms = "rooms";

    private static readonly Type[] RoomGrains = [typeof(RoomGrain)];

    public static bool RunsHotelJobs(string? pool) => Validate(pool) != Rooms;

    /// <summary>Takes out of the silo's grain classes those its pool doesn't hold.</summary>
    public static void Hold(GrainTypeOptions grainTypes, string? pool)
    {
        switch (Validate(pool))
        {
            case Core:
                foreach (var room in RoomGrains)
                    grainTypes.Classes.Remove(room);
                break;
            case Rooms:
                grainTypes.Classes.RemoveWhere(type => !RoomGrains.Contains(type));
                break;
        }
    }

    private static string? Validate(string? pool) => pool switch
    {
        null or "" => null,
        Core or Rooms => pool,
        _ => throw new InvalidOperationException($"Orleans:Pool '{pool}' is none of '{Core}', '{Rooms}' (or empty, every grain)."),
    };
}
