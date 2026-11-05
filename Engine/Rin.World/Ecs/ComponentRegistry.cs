using System.ComponentModel;
using System.Runtime.CompilerServices;
using Friflo.Engine.ECS;

namespace Rin.World;

/// <summary>
///     Every component and tag type, registered at startup by the module initializer that Rin.SourceGenerators emits in
///     each assembly that declares them. Under NativeAOT the first <see cref="World" /> builds the ECS schema from it,
///     because the backend cannot scan assemblies there. Under JIT the backend scans anyway and the list is only checked
///     by a test.
/// </summary>
/// <remarks>
///     A new type registered under NativeAOT after the first <see cref="World" /> throws, because the schema is fixed by
///     then. Types the generator cannot register (generic, private, protected) can be registered by hand before that.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ComponentRegistry
{
    private static readonly Lock Gate = new();
    private static readonly HashSet<Type> Types = [];
    private static readonly List<Action<NativeAOT>> Registrations = [];
    private static bool _schemaBuilt;

    public static void RegisterComponent<T>() where T : struct, IComponentData =>
        Add(typeof(T), static aot => aot.RegisterComponent<T>());

    public static void RegisterTag<T>() where T : struct, ITagData =>
        Add(typeof(T), static aot => aot.RegisterTag<T>());

    public static Type[] GetRegisteredTypes()
    {
        lock (Gate) return [.. Types];
    }

    internal static void EnsureSchema()
    {
        lock (Gate)
        {
            if (_schemaBuilt) return;
            _schemaBuilt = true;
            if (RuntimeFeature.IsDynamicCodeSupported) return;

            var aot = new NativeAOT();
            foreach (var register in Registrations) register(aot);
            aot.CreateSchema();
        }
    }

    private static void Add(Type type, Action<NativeAOT> register)
    {
        lock (Gate)
        {
            if (Types.Contains(type)) return;
            if (_schemaBuilt && !RuntimeFeature.IsDynamicCodeSupported)
                throw new InvalidOperationException(
                    $"{type.FullName} was registered after the first World was created, so the NativeAOT ECS schema cannot include it. Register it before creating a World.");

            Types.Add(type);
            Registrations.Add(register);
        }
    }
}
