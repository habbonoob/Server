using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Customfilter;

/// <summary>ModifyCustomFilterResultMessageComposer — a word added to or taken off the filter</summary>
[Message(ServerMessages.ModifyCustomFilterResultMessageComposer)]
public sealed partial record ModifyCustomFilterResultMessageComposer(int Result, string Word)
{
    public const int Added = 1;
    public const int Removed = 3;
}
