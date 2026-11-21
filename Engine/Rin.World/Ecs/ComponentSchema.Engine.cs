using Rin.World.Graphics;
using Rin.World.Graphics.ImageBasedLighting;
using Rin.World.Physics;
using Rin.World.Scripting;
using Rin.World.Transforms;

namespace Rin.World;

public sealed partial class ComponentSchema
{
    /// <summary>
    ///     A test checks that this lists every component and tag declared in Rin.World.
    /// </summary>
    private static ComponentSchema CreateEngine()
    {
        var schema = new ComponentSchema();
        schema.IdOf<LocalTransform>();
        schema.IdOf<WorldTransform>();
        schema.IdOf<AttachSocket>();
        schema.IdOf<StaticMesh>();
        schema.IdOf<SkinnedMesh>();
        schema.IdOf<RenderProxy>();
        schema.IdOf<PointLight>();
        schema.IdOf<DirectionalLight>();
        schema.IdOf<EnvironmentLight>();
        schema.IdOf<Camera>();
        schema.IdOf<RigidBody>();
        schema.IdOf<Velocity>();
        schema.IdOf<PhysicsBody>();
        schema.IdOf<ScriptRef>();
        schema.Freeze();
        return schema;
    }
}
