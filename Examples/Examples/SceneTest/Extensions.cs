using System.Numerics;
using Rin.GLTF;
using Rin.World;
using Rin.World.Actors;
using Rin.World.Components;
using Rin.World.Components.Lights;

namespace Examples.SceneTest;

public static class Extensions
{
    public static async Task<Actor?> LoadMeshAsEntity(this World world, string modelPath)
    {
        var mesh = await GltfMeshImporter.LoadStaticMesh(modelPath);
        if (mesh == null) return null;
        var entity = world.AddActor(new Actor
        {
            RootComponent = new StaticMeshComponent
            {
                Mesh = mesh
            }
        });
        return entity;
    }

    public static Actor AddPointLight(this World world, Vector3 location)
    {
        var entity = new Actor
        {
            RootComponent = new PointLightComponent
            {
                Location = location,
                Radiance = 800.0f
            }
        };
        world.AddActor(entity);
        return entity;
    }
}
