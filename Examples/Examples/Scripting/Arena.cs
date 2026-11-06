using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Views;
using Rin.World;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.Surfaces;
using Rin.World.Physics;
using Rin.World.Scripting;
using Rin.World.Transforms;

namespace Examples.Scripting;

/// <summary>
///     The building blocks the scripts spawn with: one cube mesh in a few materials, point lights, and the
///     <see cref="ScriptFeature" /> to attach scripts to new entities.
/// </summary>
public sealed class Arena
{
    private readonly int _cubeMeshId;

    public Arena(RenderFeature render, ScriptFeature scripts, int cubeMeshId, SurfaceMaterial<PbrMaterial> gold)
    {
        World = render.World;
        Scripts = scripts;
        _cubeMeshId = cubeMeshId;
        Gold = render.Materials.Add(gold);
        Stone = Solid(render, new Vector3(0.35f, 0.36f, 0.4f));
        Gate = Solid(render, new Vector3(0.2f, 0.45f, 0.25f));
        Bomb = Solid(render, new Vector3(0.6f, 0.08f, 0.05f));
        Debris = Solid(render, new Vector3(0.9f, 0.5f, 0.1f));
        Charger = Solid(render, new Vector3(0.2f, 0.4f, 0.9f));
        Crate = Solid(render, new Vector3(0.55f, 0.38f, 0.2f));
    }

    public World World { get; }

    public ScriptFeature Scripts { get; }

    public MaterialSet Gold { get; }

    public MaterialSet Stone { get; }

    public MaterialSet Gate { get; }

    public MaterialSet Bomb { get; }

    public MaterialSet Debris { get; }

    public MaterialSet Charger { get; }

    public MaterialSet Crate { get; }

    /// <summary>
    ///     A cube with no physics body, centered on <paramref name="position" />.
    /// </summary>
    public Entity SpawnCube(Vector3 position, Vector3 size, MaterialSet material)
    {
        var cube = World.Spawn();
        // cube.glb spans -1 to 1, so a scale of half the size gives a cube of that size.
        World.SetLocalTransform(cube, new LocalTransform { Position = position, Scale = size / 2f });
        World.Add(cube, new StaticMesh { MeshId = _cubeMeshId, Materials = material });
        return cube;
    }

    /// <summary>
    ///     A cube with a box collider of the same size.
    /// </summary>
    public Entity SpawnBody(Vector3 position, Vector3 size, MaterialSet material, PhysicsState state,
        Vector3 velocity = default)
    {
        var cube = SpawnCube(position, size, material);
        // Added before the body, which reads it once when it is created.
        if (velocity != Vector3.Zero) World.Add(cube, new Velocity { Linear = velocity });
        // The physics system multiplies the box by the entity scale, which is half the size.
        World.Add(cube, new RigidBody { Shape = ColliderShape.Box(new Vector3(2f)), State = state });
        return cube;
    }

    /// <summary>
    ///     A point light child of <paramref name="parent" />, despawned with it. It starts dark.
    /// </summary>
    public Entity SpawnLight(Entity parent, Vector3 localOffset, Color color)
    {
        var light = World.Spawn();
        World.SetPosition(light, localOffset);
        World.SetParent(light, parent);
        World.Add(light, new PointLight { Color = color, Radiance = 0f });
        return light;
    }

    private static MaterialSet Solid(RenderFeature render, Vector3 color)
    {
        return render.Materials.Add(new SurfaceMaterial<PbrMaterial>(new PbrMaterial
        {
            BaseColor = color,
            Roughness = 0.6f
        }));
    }
}
