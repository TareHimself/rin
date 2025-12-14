using System.Numerics;
using Rin.Framework;
using Rin.Framework.Extensions;
using Rin.Framework.Graphics;
using Rin.Framework.Views;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Layouts;
using Rin.Engine.World;
using Rin.Engine.World.Actors;
using Rin.Engine.World.Components;
using Rin.Engine.World.Components.Lights;
using Rin.Engine.World.Graphics.Default;
using Rin.Engine.World.Physics;
using rin.Examples.Common.Views;
using rin.Examples.SceneTest.entities;
using Rin.Framework.Graphics.Images;
using Rin.Framework.Shared.Math;

namespace rin.Examples.SceneTest;

[Module(typeof(SWorldModule), typeof(SViewsModule))]
[AlwaysLoad]
public class SSceneTestModule : IModule
{
    public void Start(IApplication app)
    {
        //var compShader = SGraphicsModule.Get().MakeCompute("World/Shaders/Mesh/compute_skinning.slang");
        SViewsModule.Get().OnSurfaceCreated += surf =>
        {
            var scene = new World();
            scene.Start();

            // Extensions.LoadStaticMesh(Path.Join(SRuntime.ResourcesDirectory,"models","real_plane.glb")).After(mesh =>
            // {
            //     var planeActor = new Actor()
            //     {
            //         RootComponent = new StaticMeshComponent()
            //         {
            //             Mesh = mesh,
            //             Location = new Vector3(0.0f, 0.0f, 0.0f),
            //             Scale = new Vector3(1.0f, 1.0f, 1.0f),
            //             Materials = [
            //             new DefaultMaterial()
            //             {
            //
            //             }]
            //         }
            //     };
            //
            //     scene.AddActor(planeActor);
            //     
            //     SGraphicsModule.Get().OnFreeRemainingMemory += () =>
            //     {
            //         mesh?.Dispose();
            //     };
            // });

            var camera = scene.AddActor<CameraActor>();
            var comp = camera.GetCameraComponent();
            var location = new Vector3(0.0f, 0, 0);
            comp.SetLocation(location);

            Extensions.LoadStaticMesh(Path.Join(SFramework.Directory,"assets", "models", "cube.glb")
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
                //
                var dist = 8.0f;
                var height = 15.0f;

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
                //e1.SetRotation(Quaternion.Identity.AddPitch(45).AddYaw(45));
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
                app.OnUpdate += delta =>
                {
                    scene.Update(delta);
                    // var weight = (float.Sin(SEngine.Get().GetTimeSeconds()) + 1) / 2.0f;
                    // var loc1 = new Transform()
                    // {
                    //     Position = new Vector3(-10, 0, 10)
                    // }.ToMatrix() * weight;
                    // var loc2 = new Transform()
                    // {
                    //     Position = new Vector3(10, 0, 10)
                    // }.ToMatrix() * (1f - weight);
                    // e1.SetTransform(Transform.From(loc1 + loc2));
                    // e1.SetRotation(e1.GetRotation().AddLocalYaw(-50.0f * delta * 2F).AddLocalPitch(-20.0f * delta * 2F));
                };

                Extensions.LoadSkinnedMesh(Path.Join(SFramework.Directory,"assets", "models", "fox.glb")).After(skinned =>
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


            var window = surf.GetRenderer().GetWindow();

            window.OnClose += _ =>
            {
                if (window.Parent != null)
                    window.Dispose();
                else
                    SFramework.Get().RequestExit();
            };


            surf.Add(new PanelView
            {
                Slots =
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

    public void Stop(IApplication app)
    {
    }

    public static async Task<ImageHandle> LoadTexture(string path)
    {
        using var imgData = await Task.Run(() => HostImage.Create(File.OpenRead(path)));
        var (id, task) = imgData.CreateTexture();
        await task;
        return id;
    }

    public static async Task<DefaultMeshMaterial> LoadGoldMaterial()
    {
        var albedo = LoadTexture(Path.Join(SFramework.Directory,"assets", "textures", "au_albedo.png"));
        var roughness = LoadTexture(Path.Join(SFramework.Directory,"assets", "textures", "au_roughness.png"));
        var metallic = LoadTexture(Path.Join(SFramework.Directory,"assets", "textures", "au_metallic.png"));
        var normal = LoadTexture(Path.Join(SFramework.Directory,"assets", "textures", "au_normal.png"));


        await Task.WhenAll(albedo, roughness, metallic, normal);
        return new DefaultMeshMaterial
        {
            ColorImageId = albedo.Result,
            RoughnessImageId = roughness.Result,
            MetallicImageId = metallic.Result
        };
    }
}