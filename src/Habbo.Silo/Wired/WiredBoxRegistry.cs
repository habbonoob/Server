using System.Reflection;

namespace Habbo.Silo.Wired;

/// <summary>Finds each wired box's type by the item's class name. Types register themselves with
/// <see cref="WiredBoxAttribute"/>; a wf_ box whose name has no type yet does nothing and opens no
/// dialog — the honest answer for a box the server does not implement.</summary>
public sealed class WiredBoxRegistry
{
    private readonly Dictionary<string, WiredBoxType> _byClassName = new(StringComparer.Ordinal);

    public WiredBoxRegistry() : this(typeof(WiredBoxType).Assembly)
    {
    }

    public WiredBoxRegistry(Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(type => type is { IsAbstract: false } && type.IsSubclassOf(typeof(WiredBoxType)));

        foreach (var type in types)
        {
            var names = type.GetCustomAttribute<WiredBoxAttribute>()?.ClassNames ?? [];
            var boxType = (WiredBoxType)Activator.CreateInstance(type)!;
            foreach (var name in names)
            {
                if (!_byClassName.TryAdd(name, boxType))
                    throw new InvalidOperationException(
                        $"Wired box '{name}' is claimed by both {_byClassName[name].GetType().Name} and {type.Name}.");
            }
        }
    }

    public IReadOnlyCollection<string> ClassNames => _byClassName.Keys;

    /// <summary>The type of the box with this class name, if the server implements it. Colour
    /// variants ("wf_act_toggle_state*2") share their base class's type.</summary>
    public WiredBoxType? For(string className)
    {
        var variant = className.IndexOf('*');
        return _byClassName.GetValueOrDefault(variant < 0 ? className : className[..variant]);
    }
}
