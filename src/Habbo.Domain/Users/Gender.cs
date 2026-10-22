namespace Habbo.Domain.Users;

/// <summary>An avatar's gender. The client sends and expects a single letter, and figure data
/// is split by it: every clothing part is drawn from the male or the female set.</summary>
public enum Gender
{
    Male,
    Female,
}

/// <summary>Moves a <see cref="Gender"/> across the wire and the database, both of which hold
/// the client's single letter rather than a number so they stay readable.</summary>
public static class GenderExtensions
{
    private const string MaleLetter = "M";
    private const string FemaleLetter = "F";

    public static string ToLetter(this Gender gender) => gender == Gender.Female ? FemaleLetter : MaleLetter;

    /// <summary>Reads the client's letter. Anything that is not female is male, which is what the
    /// client itself falls back to — its figure data only ever branches on "F".</summary>
    public static Gender ParseLetter(string? letter) =>
        string.Equals(letter, FemaleLetter, StringComparison.OrdinalIgnoreCase) ? Gender.Female : Gender.Male;
}
