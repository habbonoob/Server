using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Hotel;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Rooms;

/// <summary>The game texts the hotel serves the client (external_texts): for the few things the
/// client names only there — which effect a wired freeze option is, say.</summary>
public sealed class GameTexts(IDbContextFactory<AppDbContext> dbFactory)
    : GamedataFile(dbFactory, "external_texts"), IGameTexts
{
    public override string ReloadName => "texts";
}

/// <summary>The client variables the hotel serves (external_variables).</summary>
public sealed class ClientVariables(IDbContextFactory<AppDbContext> dbFactory)
    : GamedataFile(dbFactory, "external_variables"), IClientVariables
{
    public override string ReloadName => "variables";
}
