namespace Habbo.Silo.Abstractions.Hotel;

/// <summary>Something the server holds in memory once read, which :update reads again by its name.</summary>
public interface IReloadable
{
    /// <summary>The name :update knows it by.</summary>
    string ReloadName { get; }

    Task ReloadAsync();
}
