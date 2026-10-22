namespace Habbo.Domain.Furniture.Chests;

/// <summary>One way of meeting a contract: every one of its elements together.</summary>
public sealed class ContractRule
{
    public List<ContractNode> Nodes { get; set; } = [];
}
