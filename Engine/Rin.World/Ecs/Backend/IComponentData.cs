using Friflo.Engine.ECS;

namespace Rin.World.Ecs;

/// <summary>
///     Marks a struct as ECS component data. It inherits the backend's marker so component authors never reference the
///     backend.
/// </summary>
public interface IComponentData : IComponent;

/// <summary>
///     Marks an empty struct as an ECS tag.
/// </summary>
public interface ITagData : ITag;
