using System.Reflection;

namespace Habbo.Gateway;

internal static class AssemblyExtensions
{
    /// <summary><see cref="Assembly.GetTypes()"/>, but tolerant of unreferenced dependencies:
    /// returns the types that did load instead of throwing.</summary>
    public static IEnumerable<Type> LoadableTypes(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
