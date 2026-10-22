namespace Habbo.Silo.Commands;

/// <summary>A chat command: its name (its permission is "command." and the name), the other words that call it, and
/// how it is used, as :commands shows it.</summary>
public sealed record ChatCommandSpec(string Name, IReadOnlyList<string> Aliases, string Usage);
