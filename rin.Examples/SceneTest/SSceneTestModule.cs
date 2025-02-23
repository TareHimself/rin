using System.Numerics;
using rin.Examples.SceneTest.entities;
using rin.Framework.Core;
using rin.Framework.Core.Extensions;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Windows;
using rin.Editor.Scene;
using rin.Editor.Scene.Actors;
using rin.Editor.Scene.Components;
using rin.Editor.Scene.Components.Lights;
using rin.Editor.Scene.Graphics;
using rin.Framework.Views;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Content;
using rin.Framework.Views.Layouts;
using SixLabors.ImageSharp.PixelFormats;

namespace rin.Examples.SceneTest;

[Module(typeof(SSceneModule), typeof(SViewsModule)),AlwaysLoad]
public class SSceneTestModule : IModule
{

    public static async Task<int> LoadTexture(string path)
    {
        using var imgData = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(path);
        using var buffer = imgData.ToBuffer();
        return await SGraphicsModule.Get().GetTextureManager().CreateTexture(buffer,
            new Extent3D
            {
                Width = (uint)imgData.Width,
                Height = (uint)imgData.Height,
            },
            ImageFormat.RGBA8);
    }
    public static async Task<DefaultMeshMaterial> LoadGoldMaterial()
    {
        var albedo = LoadTexture(Path.Join(SRuntime.AssetsDirectory, "textures", "au_albedo.png"));
        var roughness = LoadTexture(Path.Join(SRuntime.AssetsDirectory, "textures", "au_roughness.png"));
        var metallic = LoadTexture(Path.Join(SRuntime.AssetsDirectory, "textures", "au_metallic.png"));
        var normal = LoadTexture(Path.Join(SRuntime.AssetsDirectory, "textures", "au_normal.png"));


        await Task.WhenAll(albedo,roughness,metallic,normal);
        return new DefaultMeshMaterial()
        {
            ColorTextureId = albedo.Result,
            RoughnessTextureId = roughness.Result,
            MetallicTextureId = metallic.Result
        };
    }
    Task<StaticMesh> CreatePlane(float size = 0.0f)
    {
        var halfSize = size * 0.5f;
        var normal = Constants.UpVector;
        var right = new Vector3(0.0f) + (Constants.RightVector * halfSize);
        var forward = new Vector3(0.0f) + (Constants.ForwardVector * halfSize);
        StaticMesh.Vertex[] vertices =
        [
            new StaticMesh.Vertex()
            {
                Location = new Vector4(forward + -right, 0.0f),
                Normal = new Vector4(normal, 0.0f),
            },
            new StaticMesh.Vertex()
            {
                Location = new Vector4(forward + right, 1.0f),
                Normal = new Vector4(normal, 0.0f),
            },new StaticMesh.Vertex()
            {
                Location = new Vector4(-forward + right, 1.0f),
                Normal = new Vector4(normal, 1.0f),
            },new StaticMesh.Vertex()
            {
                Location = new Vector4(-forward + right, 1.0f),
                Normal = new Vector4(normal, 1.0f),
            },new StaticMesh.Vertex()
            {
                Location = new Vector4(-forward + -right, 0.0f),
                Normal = new Vector4(normal, 1.0f),
            },new StaticMesh.Vertex()
            {
                Location = new Vector4(forward + -right, 0.0f),
                Normal = new Vector4(normal, 0.0f),
            }
        ];
        return StaticMesh.Create([
            new MeshSurface()
            {
                StartIndex = 0,
                Count = (uint)vertices.Length,
            }
        ], vertices, [0, 1, 2, 2, 3, 0]);
    }
    public void Start(SRuntime runtime)
    {
        
        runtime.OnUpdate += (delta) =>
        {
            SGraphicsModule.Get().PollWindows();
          // camera.GetCameraComponent().SetRelativeRotation(camera.GetCameraComponent().GetRelativeRotation().ApplyYaw(20.0f * (float)delta));
        };

        SViewsModule.Get().OnSurfaceCreated += (surf) =>
        {
            var scene = SSceneModule.Get().CreateScene();
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
            var location = new Vector3(0.0f, 60.0f, -50.0f);
            comp.SetRelativeLocation(location);
            var lookAtRotation = Rotator.LookAt(location,new Vector3(0.0f,50.0f,0.0f),Constants.UpVector);
            comp.SetRelativeRotation(lookAtRotation);
            Extensions.LoadStaticMesh(Path.Join(SRuntime.AssetsDirectory,"models","real_cube.glb")).After(mesh =>
            {
                
                scene.AddPointLight(new Vector3(0.0f, 20.0f, 0.0f));
                    
                scene.AddPointLight(new Vector3(0.0f, -20.0f, 0.0f));
                var directionalLight = scene.AddActor(new Actor()
                {
                    RootComponent = new DirectionalLightComponent()
                    {
                        Radiance = 10.0f,
                        Location = new Vector3(0.0f, 200.0f, 0.0f),
                    }
                });
                
                directionalLight.SetRelativeRotation(Rotator.LookAt(directionalLight.GetRelativeLocation(),new Vector3(0.0f),Constants.UpVector));
                //
                var dist = 50.0f;
                var height = 50.0f;
                var e1 = new Actor()
                {
                    RootComponent = new StaticMeshComponent()
                    {
                        Mesh = mesh,
                        Location = new Vector3(dist,height,0.0f)
                    }
                };
                var e2 = new Actor()
                {
                    RootComponent = new StaticMeshComponent()
                    {
                        Mesh = mesh,
                        Location = new Vector3(-dist,height,0.0f)
                    }
                };
                var box = new BoxCollisionComponent()
                {
                    Location = new Vector3(0.0f, 100.0f, 0.0f),
                };

                box.OnHit += (_) =>
                {
                    Console.WriteLine("Box hit something");
                };
                
                var e3 = new Actor()
                {
                    RootComponent = box 
                };

                var sm = e3.AddComponent(new StaticMeshComponent()
                {
                    Mesh = mesh,
                });

                sm.AttachTo(box);
                box.IsSimulating = true;
                scene.AddActor(e1);
                scene.AddActor(e2);
                scene.AddActor(e3);

                LoadGoldMaterial().After((material) =>
                {
                    sm.Materials = ((StaticMeshComponent)e2.RootComponent).Materials = ((StaticMeshComponent)e1.RootComponent).Materials = [material];
                });
                runtime.OnUpdate += (delta) =>
                {
                    
                    scene.Update(delta);
                    var lookAtRotation = Rotator.LookAt(camera.GetRelativeLocation(),e3.GetRelativeLocation(), Constants.UpVector);
                    comp.SetRelativeRotation(lookAtRotation);
                    // var root = e1.RootComponent!;
                    // root.SetRelativeRotation(root.GetRelativeRotation().Delta(pitch: -50.0f * (float)delta));
                    e1.AddRelativeRotation(yaw: -50.0f * (float)delta,pitch: -20.0f * (float)delta);
                    e2.AddRelativeRotation(yaw: -50.0f * (float)delta,pitch: 20.0f * (float)delta);
                    //e3.AddRelativeRotation(yaw: 50.0f * (float)delta,pitch: -20.0f * (float)delta);
                };
                
                SGraphicsModule.Get().OnFreeRemainingMemory += () =>
                {
                    mesh?.Dispose();
                };
            });
            
           
            
            var window = surf.GetRenderer().GetWindow();
            
            window.OnCloseRequested += (_) =>
            {
                if (window.Parent != null)
                {
                    window.Dispose();
                }
                else
                {
                    SRuntime.Get().RequestExit();
                }
            };
            
            var text = new TextBox("Selected Channel Text", inFontSize: 50);
            
            surf.Add(new Panel()
            {
                Slots =
                [
                    new PanelSlot()
                    {
                        Child = new TestViewport(camera, text),
                        MinAnchor = new Vector2(0.0f),
                        MaxAnchor = new Vector2(1.0f),
                    },
                    new PanelSlot()
                    {
                        Child = text,
                        SizeToContent = true
                    }
                ]
            });
        };
        
        Task.Factory.StartNew(() =>
        {
            while (SRuntime.Get().IsRunning)
            {
                SGraphicsModule.Get().DrawWindows();
            }
        }, TaskCreationOptions.LongRunning);

        SGraphicsModule.Get().CreateWindow(500, 500, "Rin Scene Test", new CreateOptions()
        {
            Visible = true,
            Decorated = true,
            Transparent = true
        });
    }

    public void Stop(SRuntime runtime)
    {
    }
}