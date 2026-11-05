using System.Numerics;
using Rin.World.Ecs.Transforms;
using Rin.World.Graphics;

namespace Rin.World.Ecs.Graphics;

public static class CameraExtensions
{
    /// <summary>
    ///     Builds the view from the entity's <see cref="Camera" /> and <see cref="WorldTransform" />, or the identity
    ///     matrix when it has no transform.
    /// </summary>
    public static CameraView GetCameraView(this EntityWorld world, Entity camera)
    {
        ref readonly var settings = ref world.Get<Camera>(camera);
        var matrix = world.Has<WorldTransform>(camera) ? world.Get<WorldTransform>(camera).Matrix : Matrix4x4.Identity;
        return new CameraView(matrix, settings.FieldOfView, settings.NearClipPlane, settings.FarClipPlane);
    }
}
