namespace Habbo.Silo.Abstractions.Camera;

/// <summary>How publishing a photo went: published (its id), or how long until the player may publish again.</summary>
public sealed record PhotoPublication(bool Published, int SecondsToWait, Guid? PhotoId);
