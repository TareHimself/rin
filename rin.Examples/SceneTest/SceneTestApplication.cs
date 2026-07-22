using System.Numerics;
using Rin.World;
using Rin.World.Actors;
using Rin.World.Components;
using Rin.World.Components.Lights;
using Rin.World.Graphics.Default;
using Rin.World.Physics;
using rin.Examples.Common;
using rin.Examples.Common.Views;
using rin.Examples.SceneTest.entities;
using Rin.Core;
using Rin.Core.Extensions;
using Rin.Core.Graphics;
using Rin.Core.Shared.Math;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Layouts;

namespace rin.Examples.SceneTest;

public class SceneTestApplication : ExampleApplication
{
    protected override void OnStartup()
    {
        IViewsModule.Get().OnSurfaceCreated += surf =>
        {
            var scene = new World();
            scene.Start();

            var camera = scene.AddActor<CameraActor>();
            var comp = camera.GetCameraComponent();
            var location = new Vector3(0.0f, 0, 0);
            comp.SetLocation(location);

            Extensions.LoadStaticMesh(Path.Join(Global.Directory, "assets", "models", "cube.glb")
            ).After(mesh =>
            {
                scene.AddPointLight(new Vector3(0.0f, 20.0f, 0.0f));

                scene.AddPointLight(new Vector3(0.0f, -20.0f, 0.0f));
                var directionalLight = scene.AddActor(new Actor
                {
                    RootComponent = new DirectionalLightComponent
                    {
                        Radiance = 10.0f,
                        Location = new Vector3(0.0f, 200.0f, 0.0f)
                    }
                });

                directionalLight.SetRotation(MathR.LookTowards(directionalLight.GetLocation(), new Vector3(0.0f),
                    MathR.Up));

                var e1 = new Actor
                {
                    RootComponent = new BoxPhysicsComponent
                    {
                        Size = new Vector3(2, 2, 2),
                        Location = new Vector3(0, 0, 15),
                        Scale = new Vector3(3),
                        PhysicsState = PhysicsState.Static
                    },
                    InitialComponents =
                    [
                        new StaticMeshComponent
                        {
                            Mesh = mesh
                        }
                    ]
                };
                scene.AddActor(e1);
                scene.AddActor(new Actor
                {
                    RootComponent = new BoxPhysicsComponent
                    {
                        Size = new Vector3(2, 2, 2),
                        Location = new Vector3(0, -20, 0),
                        Scale = new Vector3(500, 1f, 500),
                        PhysicsState = PhysicsState.Static
                    }
                });
                LoadGoldMaterial().After(material =>
                {
                    if (e1.FindComponentByType<StaticMeshComponent>() is { } sm) sm.Materials = [material];
                });
                OnUpdate += delta => { scene.Update(delta); };

                Extensions.LoadSkinnedMesh(Path.Join(Global.Directory, "assets", "models", "fox.glb"))
                    .After(skinned =>
                    {
                        if (skinned is not null)
                            LoadGoldMaterial().After(material =>
                            {
                                scene.AddActor(new Actor
                                {
                                    RootComponent = new SkinnedMeshComponent
                                    {
                                        Mesh = skinned,
                                        Materials = [material],
                                        PoseSource = new TestPoseSource
                                        {
                                            Skeleton = skinned.Skeleton
                                        },
                                        Location = new Vector3(0, 0, 50)
                                    }
                                });

                                scene.AddActor(new Actor
                                {
                                    RootComponent = new SkinnedMeshComponent
                                    {
                                        Mesh = skinned,
                                        Materials = [material],
                                        PoseSource = new TestPoseSource
                                        {
                                            Skeleton = skinned.Skeleton
                                        },
                                        Location = new Vector3(-30, 0, 50)
                                    }
                                });

                                scene.AddActor(new Actor
                                {
                                    RootComponent = new SkinnedMeshComponent
                                    {
                                        Mesh = skinned,
                                        Materials = [material],
                                        PoseSource = new TestPoseSource
                                        {
                                            Skeleton = skinned.Skeleton
                                        },
                                        Location = new Vector3(30, 0, 50)
                                    }
                                });
                            });
                    });
            });


            var window = surf.Renderer.GetWindow();

            window.OnClose += _ =>
            {
                if (window.Parent != null)
                    window.Dispose();
                else
                    RequestExit();
            };


            surf.Add(new PanelView
            {
                InitSlots =
                [
                    new PanelSlot
                    {
                        Child = new TestViewport(camera),
                        MinAnchor = new Vector2(0.0f),
                        MaxAnchor = new Vector2(1.0f)
                    },
                    new PanelSlot
                    {
                        Child = new FpsView(),
                        MinAnchor = new Vector2(1f, 0f),
                        MaxAnchor = new Vector2(1f, 0f),
                        Alignment = new Vector2(1, 0),
                        SizeToContent = true
                    }
                ]
            });
        };
        IGraphicsModule.Get().CreateWindow("Rin Scene Test", new Extent2D(500));
    }

    protected override void OnShutdown()
    {
    }

    public static async Task<ResourceHandle> LoadTexture(string path)
    {
        using var imgData = await Task.Run(() => HostImage.Create(File.OpenRead(path)));
        var task = imgData.CreateTexture(out var handle);
        await task;
        return handle;
    }

    public static async Task<DefaultMeshMaterial> LoadGoldMaterial()
    {
        var albedo = LoadTexture(Path.Join(Global.Directory, "assets", "textures", "au_albedo.png"));
        var roughness = LoadTexture(Path.Join(Global.Directory, "assets", "textures", "au_roughness.png"));
        var metallic = LoadTexture(Path.Join(Global.Directory, "assets", "textures", "au_metallic.png"));
        var normal = LoadTexture(Path.Join(Global.Directory, "assets", "textures", "au_normal.png"));


        await Task.WhenAll(albedo, roughness, metallic, normal);
        return new DefaultMeshMaterial
        {
            ColorImageId = albedo.Result,
            RoughnessImageId = roughness.Result,
            MetallicImageId = metallic.Result
        };
    }
}
