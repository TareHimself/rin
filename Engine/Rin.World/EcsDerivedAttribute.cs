namespace Rin.World;

/// <summary>
///     Marks a component or field computed from other data, like a world transform, so editing it does nothing lasting.
///     Editors hide it or show it read-only.
/// </summary>
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field)]
public sealed class EcsDerivedAttribute : Attribute;
