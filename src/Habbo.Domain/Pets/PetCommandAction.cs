namespace Habbo.Domain.Pets;

/// <summary>What a pet does when it obeys a command.</summary>
public enum PetCommandAction
{
    /// <summary>Takes up a posture: until told otherwise, or for a while.</summary>
    Posture,

    /// <summary>Wanders about again.</summary>
    Free,

    /// <summary>Stays where it is.</summary>
    Stay,

    /// <summary>Comes to whoever called.</summary>
    Come,

    /// <summary>Keeps close to whoever called.</summary>
    Follow,

    /// <summary>Stands up out of any posture.</summary>
    Stand,

    /// <summary>Takes a step the way it faces.</summary>
    Forward,

    TurnLeft,

    TurnRight,

    /// <summary>Goes to its nest to rest.</summary>
    Nest,

    /// <summary>Goes to drink from a bowl.</summary>
    Drink,

    /// <summary>Goes to eat from its food.</summary>
    Eat,

    /// <summary>Goes to a breeding nest for its kind.</summary>
    Breed,
}
