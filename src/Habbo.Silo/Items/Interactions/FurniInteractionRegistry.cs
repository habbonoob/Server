using System.Reflection;
using Habbo.Domain.Furniture;

namespace Habbo.Silo.Items.Interactions;

/// <summary>Finds each item's behaviour by <see cref="ItemDefinition.InteractionName"/>.
/// Behaviours register themselves with <see cref="FurniInteractionAttribute"/>; an item whose
/// name has no behaviour yet gets the basic one and does nothing on use, which is the honest
/// answer for something the server does not implement.</summary>
public sealed class FurniInteractionRegistry
{
    private readonly Dictionary<string, FurniInteraction> _byName = new(StringComparer.Ordinal);
    private readonly FurniInteraction _fallback;

    public FurniInteractionRegistry() : this(typeof(FurniInteraction).Assembly)
    {
    }

    public FurniInteractionRegistry(Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(type => type is { IsAbstract: false } && type.IsSubclassOf(typeof(FurniInteraction)));

        foreach (var type in types)
        {
            var names = type.GetCustomAttribute<FurniInteractionAttribute>()?.Names ?? [];
            var interaction = (FurniInteraction)Activator.CreateInstance(type)!;
            foreach (var name in names)
            {
                if (!_byName.TryAdd(name, interaction))
                    throw new InvalidOperationException(
                        $"Furniture behaviour '{name}' is claimed by both {_byName[name].GetType().Name} and {type.Name}.");
            }
        }

        _fallback = _byName.GetValueOrDefault(ItemDefinition.BasicLogic)
                    ?? throw new InvalidOperationException($"No behaviour is registered for '{ItemDefinition.BasicLogic}'.");
    }

    public FurniInteraction For(ItemDefinition definition) =>
        _byName.GetValueOrDefault(definition.InteractionName, _fallback);
}
