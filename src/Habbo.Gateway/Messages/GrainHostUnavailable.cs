using Orleans.Runtime;

namespace Habbo.Gateway.Messages;

/// <summary>Whether a call failed because no silo holds that kind of grain just now (the rooms' silo being deployed),
/// or the one holding it went: the player is not at fault, and the connection stays.</summary>
internal static class GrainHostUnavailable
{
    public static bool Is(Exception ex) => ex switch
    {
        SiloUnavailableException => true,
        // the cluster knows of no silo holding the class (Orleans' GrainInterfaceTypeToGrainTypeResolver), or of none
        // up that does (its placement): told apart from other failures only by their messages
        ArgumentException when ex.Message.StartsWith("Could not find an implementation for interface", StringComparison.Ordinal) => true,
        OrleansException when ex.Message.StartsWith("No active nodes are compatible with grain", StringComparison.Ordinal) => true,
        AggregateException { InnerExceptions: [var inner] } => Is(inner),
        _ => false,
    };
}
