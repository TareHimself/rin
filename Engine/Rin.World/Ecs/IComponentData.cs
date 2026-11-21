namespace Rin.World;

/// <summary>
///     Marks a struct as ECS component data.
/// </summary>
public interface IComponentData;

/// <summary>
///     Marks an empty struct as an ECS tag, a component without a column.
/// </summary>
/// <remarks>
///     An empty struct still reports a size of 1, so the marker, not the size, is what makes it a tag.
/// </remarks>
public interface ITagData : IComponentData;
