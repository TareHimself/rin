using System.Numerics;
using Rin.World.Transforms;

namespace Rin.World.Graphics;

public static class CameraExtensions
{
    /// <summary>
    ///     Builds the view from the entity's <see cref="Camera" /> and <see cref="WorldTransform" />, or the identity
    ///     matrix when it has no transform.
    /// </summary>
    public static CameraView GetCameraView(this World world, Entity camera)
    {
        ref readonly var settings = ref world.Get<Camera>(camera);
        var matrix = world.Has<WorldTransform>(camera) ? world.Get<WorldTransform>(camera).Matrix : Matrix4x4.Identity;
        return new CameraView(matrix, settings.FieldOfView, settings.NearClipPlane, settings.FarClipPlane);
    }
}
