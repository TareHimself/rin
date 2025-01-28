using System.Numerics;
using System.Runtime.InteropServices;
using rin.Framework.Core;
using rin.Framework.Core.Extensions;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Windows;
using rin.Framework.Scene;
using rin.Framework.Scene.Actors;
using rin.Framework.Scene.Components;
using rin.Framework.Scene.Components.Lights;
using rin.Framework.Scene.Graphics;
using rin.Framework.Views;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Content;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Layouts;
using SceneTest.entities;

namespace SceneTest;

[Module(typeof(SSceneModule), typeof(SViewsModule)),AlwaysLoad]
public class SSceneTestModule : IModule
{
    Task<StaticMesh> CreatePlane(float size = 0.0f)
    {
        var halfSize = size * 0.5f;
        var normal = Vec3<float>.Up;
        var right = new Vec3<float>(0.0f) + (Vec3<float>.Right * halfSize);
        var forward = new Vec3<float>(0.0f) + (Vec3<float>.Forward * halfSize);
        StaticMesh.Vertex[] vertices =
        [
            new StaticMesh.Vertex()
            {
                Location = new Vec4<float>(forward + -right, 0.0f),
                Normal = new Vec4<float>(normal, 0.0f),
            },
            new StaticMesh.Vertex()
            {
                Location = new Vec4<float>(forward + right, 1.0f),
                Normal = new Vec4<float>(normal, 0.0f),
            },new StaticMesh.Vertex()
            {
                Location = new Vec4<float>(-forward + right, 1.0f),
                Normal = new Vec4<float>(normal, 1.0f),
            },new StaticMesh.Vertex()
            {
                Location = new Vec4<float>(-forward + right, 1.0f),
                Normal = new Vec4<float>(normal, 1.0f),
            },new StaticMesh.Vertex()
            {
                Location = new Vec4<float>(-forward + -right, 0.0f),
                Normal = new Vec4<float>(normal, 1.0f),
            },new StaticMesh.Vertex()
            {
                Location = new Vec4<float>(forward + -right, 0.0f),
                Normal = new Vec4<float>(normal, 0.0f),
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
    public void Startup(SRuntime runtime)
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
            //             Location = new Vec3<float>(0.0f, 0.0f, 0.0f),
            //             Scale = new Vec3<float>(1.0f, 1.0f, 1.0f),
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
            var location = new Vec3<float>(0.0f, 60.0f, -50.0f);
            comp.SetRelativeLocation(location);
            var lookAtRotation = Rotator.LookAt(location,new Vec3<float>(0.0f,50.0f,0.0f), Vec3<float>.Up);
            comp.SetRelativeRotation(lookAtRotation);
            Extensions.LoadStaticMesh(Path.Join(SRuntime.AssetsDirectory,"models","real_cube.glb")).After(mesh =>
            {
                
                scene.AddPointLight(new Vec3<float>(0.0f, 20.0f, 0.0f));
                    
                scene.AddPointLight(new Vec3<float>(0.0f, -20.0f, 0.0f));
                // var directionalLight = scene.AddActor(new Actor()
                // {
                //     RootComponent = new DirectionalLightComponent()
                //     {
                //         Radiance = 10.0f,
                //         Location = new Vec3<float>(0.0f, 200.0f, 0.0f),
                //     }
                // });
                //
                // directionalLight.SetRelativeRotation(Rotator.LookAt(directionalLight.GetRelativeLocation(),new Vec3<float>(0.0f),Vec3<float>.Up));
                //
                var dist = 50.0f;
                var height = 50.0f;
                var e1 = new Actor()
                {
                    RootComponent = new StaticMeshComponent()
                    {
                        Mesh = mesh,
                        Location = new Vec3<float>(dist,height,0.0f)
                    }
                };
                var e2 = new Actor()
                {
                    RootComponent = new StaticMeshComponent()
                    {
                        Mesh = mesh,
                        Location = new Vec3<float>(-dist,height,0.0f)
                    }
                };
                var box = new BoxCollisionComponent()
                {
                    Location = new Vec3<float>(0.0f, 100.0f, 0.0f),
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
                
                runtime.OnUpdate += (delta) =>
                {
                    
                    scene.Update(delta);
                    var lookAtRotation = Rotator.LookAt(camera.GetRelativeLocation(),e3.GetRelativeLocation(), Vec3<float>.Up);
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
                        MinAnchor = 0.0f,
                        MaxAnchor = 1.0f,
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

    public void Shutdown(SRuntime runtime)
    {
    }
}