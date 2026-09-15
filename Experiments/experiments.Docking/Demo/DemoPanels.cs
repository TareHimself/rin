using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Graphics.Quads;
using Rin.Core.Shared.Math;
using Examples.Common.Views;
using experiments.Docking.Model;

namespace experiments.Docking.Demo;

/// <summary>Placeholder panel content for the docking demo.</summary>
public static class DemoPanels
{
    public static DockPanel Text(string id, string title, string body)
    {
        return new DockPanel(id, title, new RectView
        {
            Color = new Color(0.13f, 0.13f, 0.15f, 1f),
            Padding = new Padding(14f),
            InitChild = new TextBoxView { Content = body, FontSize = 15f, WrapContent = true }
        });
    }

    public static DockPanel ColorWheel(string id, string title)
    {
        return new DockPanel(id, title, new CanvasView
        {
            Paint = (view, transform, cmds) =>
            {
                // RenderMode.ColorWheel (batch.slang) assumes a square quad — it maps hue/sat
                // across the UVs and masks with a corner radius of size.x/2. Anything non-square
                // comes out stretched and lens-shaped, so centre a square here.
                var content = view.GetContentSize();
                var side = float.Min(content.X, content.Y);
                var offset = (content - new Vector2(side)) * 0.5f;
                var rect = Quad.Rect(transform.Translate(offset), new Vector2(side));
                rect.Mode = Quad.RenderMode.ColorWheel;
                cmds.AddQuads(rect);
            }
        });
    }

    public static DockPanel Stats(string id, string title)
    {
        return new DockPanel(id, title, new RectView
        {
            Color = new Color(0.10f, 0.10f, 0.12f, 1f),
            Padding = new Padding(12f),
            InitChild = new FpsView()
        });
    }

    /// <summary>
    ///     Explorer | ( Editors / Console ) — the layout from the plan.
    /// </summary>
    public static DockTree BuildInitialTree()
    {
        var left = new DockTabGroupNode(
            Text("explorer", "Explorer", "src/\n  main.rs\n  lib.rs\nassets/\n  logo.png\nCargo.toml"),
            Text("outline", "Outline", "• module\n  • fn main\n  • struct App\n  • impl App"));

        var editors = new DockTabGroupNode(
            ColorWheel("editorA", "Editor A"),
            ColorWheel("editorB", "Editor B"));

        var console = new DockTabGroupNode(Stats("console", "Console"));

        var rightSplit = new DockSplitNode(DockOrientation.Vertical, editors, console);
        rightSplit.Weights[0] = 0.72f;
        rightSplit.Weights[1] = 0.28f;
        rightSplit.NormalizeWeights();

        var root = new DockSplitNode(DockOrientation.Horizontal, left, rightSplit);
        root.Weights[0] = 0.24f;
        root.Weights[1] = 0.76f;
        root.NormalizeWeights();

        return new DockTree(root);
    }
}
