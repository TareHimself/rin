using System.Numerics;
using Rin.Engine;
using Rin.Engine.Extensions;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Windows;
using Rin.Engine.Math;
using Rin.Engine.Views;
using Rin.Engine.Views.Composite;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Layouts;
using Rin.Engine.World;
using Rin.Engine.World.Actors;
using Rin.Engine.World.Components;
using Rin.Engine.World.Components.Lights;
using Rin.Engine.World.Graphics;
using rin.Examples.Common.Views;
using rin.Examples.SceneTest.entities;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace rin.Examples.SceneTest;

[Module(typeof(SWorldModule), typeof(SViewsModule))]
[AlwaysLoad]
public class SSceneTestModule : IModule
{
    public void Start(SEngine engine)
    {
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
                    RootComponent = new StaticMeshComponent
                    {
                        MeshId = mesh,
                        Location = new Vector3(0, 0, 10) //new Vector3(dist, height, 0.0f)
                        //Scale = new Vector3(3.0f)
                    }
                };
                // var e2 = new Actor()
                // {
                //     RootComponent = new StaticMeshComponent()
                //     {
                //         MeshId = mesh,
                //         Location = new Vector3(-dist,height,0.0f)
                //     }
                // };
                // var boxCollisionLocation = new Vector3(0.0f, 30, 0.0f);
                // var lookAtRotation = RMath.LookAt(location,boxCollisionLocation,RMath.Up).ToQuaternion();
                // comp.SetRotation(lookAtRotation);
                // var box = new BoxCollisionComponent
                // {
                //     Location = boxCollisionLocation
                // };

                //box.OnHit += _ => { Console.WriteLine("Box hit something"); };

                // var e3 = new Actor
                // {
                //     RootComponent = box
                // };

                // var sm = e3.AddComponent(new StaticMeshComponent
                // {
                //     MeshId = mesh
                // });

                // sm.AttachTo(box);
                // box.IsSimulating = true;
                scene.AddActor(e1);
                //scene.AddActor(e2);
                // scene.AddActor(e3);
                e1.SetRotation(Quaternion.Identity.AddPitch(45).AddYaw(45));

                LoadGoldMaterial().After(material =>
                {
                    //((StaticMeshComponent)e2.RootComponent).Materials
                    ((StaticMeshComponent)e1.RootComponent).Materials = [material];
                });
                engine.OnUpdate += delta =>
                {
                    scene.Update(delta);
                    var sin = float.Abs(float.Sin(SEngine.Get().GetTimeSeconds()));
                    //e1.SetLocation(new Vector3(0.0f, 0.0f,float.Max(sin * 20.0f,3)));
                    // e1.SetScale(new Vector3(sin));
                    // Console.WriteLine("Scale {0}",sin);
                    // var lookAtRotation = RMath.LookAt(camera.GetLocation(), e3.GetLocation(), RMath.Up).ToQuaternion();
                    // comp.SetRotation(lookAtRotation);
                    // var root = e1.RootComponent!;
                    // root.SetRelativeRotation(root.GetRelativeRotation().Delta(pitch: -50.0f * (float)delta));
                    e1.SetRotation(e1.GetRotation().AddLocalYaw(-50.0f * delta * 2F).AddLocalPitch(-20.0f * delta * 2F));
                    //e2.AddRelativeRotation(yaw: -50.0f * (float)delta,pitch: 20.0f * (float)delta);
                    //e3.AddRelativeRotation(yaw: 50.0f * (float)delta,pitch: -20.0f * (float)delta);
                };

                // SGraphicsModule.Get().OnFreeRemainingMemory += () =>
                // {
                //     mesh?.Dispose();
                // };
            });


            var window = surf.GetRenderer().GetWindow();

            window.OnCloseRequested += _ =>
            {
                if (window.Parent != null)
                    window.Dispose();
                else
                    SEngine.Get().RequestExit();
            };

            var text = new TextBox
            {
                Content = "Selected Channel Text",
                FontSize = 50
            };

            surf.Add(new Panel
            {
                Slots =
                [
                    new PanelSlot
                    {
                        Child = new TestViewport(camera, text),
                        MinAnchor = new Vector2(0.0f),
                        MaxAnchor = new Vector2(1.0f)
                    },
                    new PanelSlot
                    {
                        Child = text,
                        SizeToContent = true
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
        SGraphicsModule.Get().CreateWindow(500, 500, "Rin Scene Test", new CreateOptions
        {
            Visible = true,
            Decorated = true,
            Transparent = true
        });
    }

    public void Stop(SEngine engine)
    {
    }

    public static async Task<int> LoadTexture(string path)
    {
        using var imgData = await Image.LoadAsync<Rgba32>(path);
        using var buffer = imgData.ToBuffer();
        var (id, task) = SGraphicsModule.Get().GetTextureFactory().CreateTexture(buffer,
            new Extent3D
            {
                Width = (uint)imgData.Width,
                Height = (uint)imgData.Height
            },
            ImageFormat.RGBA8);
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
            ColorTextureId = albedo.Result,
            RoughnessTextureId = roughness.Result,
            MetallicTextureId = metallic.Result
        };
    }
}