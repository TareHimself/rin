﻿using System.Numerics;
using Rin.Engine.Views.Composite;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Layouts;
using Rin.Engine.World;
using rin.Examples.SceneTest.entities;

namespace rin.Examples.SceneTest.Views;

public class MainPanel : Panel
{
    private readonly CameraActor _cameraActor;
    private readonly World _world = new();

    public MainPanel()
    {
        _cameraActor = _world.AddActor<CameraActor>();
        var text = new TextBox();
        Slots =
        [
            new PanelSlot
            {
                Child = new TestViewport(_cameraActor, text),
                MinAnchor = new Vector2(0.0f),
                MaxAnchor = new Vector2(1.0f)
            },
            new PanelSlot
            {
                Child = text,
                SizeToContent = true
            }
        ];
    }
}