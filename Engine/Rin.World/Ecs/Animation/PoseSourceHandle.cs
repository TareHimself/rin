namespace Rin.World.Ecs.Animation;

/// <summary>
///     A handle to a pose source in <see cref="PoseSources" />. <c>default</c> is no pose source.
/// </summary>
public readonly record struct PoseSourceHandle(int Id)
{
    public bool IsValid => Id != 0;
}
