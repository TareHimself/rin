using System.Numerics;
using Rin.Engine;
using Rin.Engine.Extensions;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Textures;
using Rin.Engine.Graphics.Windows;
using Rin.Engine.Math;
using Rin.Engine.Views;
using Rin.Engine.Views.Composite;
using Rin.Engine.Views.Layouts;
using Rin.Engine.World;
using Rin.Engine.World.Actors;
using Rin.Engine.World.Components;
using Rin.Engine.World.Components.Lights;
using Rin.Engine.World.Graphics;
using Rin.Engine.World.Physics;
using rin.Examples.Common.Views;
using rin.Examples.SceneTest.entities;

namespace rin.Examples.SceneTest;

[Module(typeof(SWorldModule), typeof(SViewsModule))]
[AlwaysLoad]
public class SSceneTestModule : IModule
{
    public void Start(SEngine engine)
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

            Extensions.LoadStaticMesh(Path.Join(SEngine.AssetsDirectory, "models", "cube.glb")
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
                engine.OnUpdate += delta =>
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

                Extensions.LoadSkinnedMesh(Path.Join(SEngine.AssetsDirectory, "models", "fox.glb")).After(skinned =>
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
                    SEngine.Get().RequestExit();
            };


            surf.Add(new Panel
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
        SGraphicsModule.Get().CreateWindow(500, 500, "Rin Scene Test");
    }

    public void Stop(SEngine engine)
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
        var albedo = LoadTexture(Path.Join(SEngine.AssetsDirectory, "textures", "au_albedo.png"));
        var roughness = LoadTexture(Path.Join(SEngine.AssetsDirectory, "textures", "au_roughness.png"));
        var metallic = LoadTexture(Path.Join(SEngine.AssetsDirectory, "textures", "au_metallic.png"));
        var normal = LoadTexture(Path.Join(SEngine.AssetsDirectory, "textures", "au_normal.png"));


        await Task.WhenAll(albedo, roughness, metallic, normal);
        return new DefaultMeshMaterial
        {
            ColorImageId = albedo.Result,
            RoughnessImageId = roughness.Result,
            MetallicImageId = metallic.Result
        };
    }
}