using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Shared.Math;
using Rin.Core.Views;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;
using RenderGraphOverlay.Layout;
using RenderGraphOverlay.Snapshot;

namespace RenderGraphOverlay.Views;

public class GraphCanvasView : ContentView
{
    private const float MinZoom = 0.1f;
    private const float MaxZoom = 3f;
    private const float FitMargin = 40f;
    private const float ZoomStep = 0.03f;
    private const float MaxScrollDelta = 3f;
    private const float TitleSize = 15f;
    private const float SubtitleSize = 11f;
    private const float StageTitleSize = 13f;
    private const float BarrierTitleSize = 12f;
    private const float ArrowLength = 9f;
    private const float ArrowHalfWidth = 5f;

    private static readonly Color EdgeColor = new(0.62f, 0.68f, 0.82f, 1f);
    private static readonly Color FocusColor = new(0.55f, 0.85f, 1f, 1f);

    private bool _dragging;
    private Vector2 _dragStart;
    private GraphLayout? _layout;
    private Vector2 _pan = new(FitMargin);
    private Vector2 _panAtDragStart;
    private bool _showAllEdges;
    private float _zoom = 1f;

    public PassSnapshot? SelectedPass { get; private set; }

    public bool ShowAllEdges
    {
        get => _showAllEdges;
        set => _showAllEdges = value;
    }

    public event Action<PassSnapshot?>? OnSelectionChanged;

    public void SetSnapshot(GraphSnapshot snapshot)
    {
        _layout = GraphLayout.Compute(snapshot, title => TextMeasure.Width(title, TitleSize));
        SelectPass(null);
        FitToView();
    }

    public void FitToView()
    {
        if (_layout is null) return;

        var available = GetContentSize() - new Vector2(FitMargin * 2f);
        var scale = float.Min(available.X / float.Max(_layout.Size.X, 1f), available.Y / float.Max(_layout.Size.Y, 1f));
        _zoom = float.Clamp(scale, MinZoom, 1f);
        _pan = (GetContentSize() - _layout.Size * _zoom) / 2f;
        _pan.Y = float.Min(_pan.Y, FitMargin);
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return availableSpace;
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        if (_layout is null) return;

        var world = Matrix4x4.Identity.Scale(new Vector2(_zoom)).Translate(_pan).ChildOf(transform);
        foreach (var stage in _layout.Stages) CollectStageTitle(stage, world, commands);
        foreach (var edge in _layout.Edges.Where(e => !IsFocused(e))) CollectEdge(edge, false, world, commands);
        foreach (var edge in _layout.Edges.Where(IsFocused)) CollectEdge(edge, true, world, commands);
        foreach (var node in _layout.Nodes.Values) CollectNode(node, world, commands);
    }

    private bool IsFocused(EdgeLayout edge)
    {
        return SelectedPass is { } selected &&
               (edge.Edge.FromPassId == selected.Id || edge.Edge.ToPassId == selected.Id);
    }

    private static void CollectStageTitle(StageLayout stage, in Matrix4x4 world, CommandList commands)
    {
        var y = stage.Y + (stage.Height - StageTitleSize) / 2f - 2f;
        commands.AddText(At(new Vector2(0f, y), world), TextMeasure.FontName, stage.Stage.Title, StageTitleSize,
            GraphPalette.Muted);
    }

    private void CollectEdge(EdgeLayout edge, bool focused, in Matrix4x4 world, CommandList commands)
    {
        if (edge.Edge.IsRedundant && !focused && !_showAllEdges) return;

        var dimmed = SelectedPass is not null && !focused;
        var color = (focused ? FocusColor : EdgeColor) with { A = dimmed ? 0.12f : focused ? 1f : 0.6f };
        var thickness = focused ? 3f : 2f;
        var path = edge.Path;

        for (var i = 1; i < path.Count; i++) commands.AddLine(world, path[i - 1], path[i], thickness, color);
        CollectArrowHead(path[^1], world, thickness, color, commands);
    }

    private static void CollectArrowHead(Vector2 tip, in Matrix4x4 world, float thickness, Color color,
        CommandList commands)
    {
        commands.AddLine(world, tip, tip + new Vector2(-ArrowHalfWidth, -ArrowLength), thickness, color);
        commands.AddLine(world, tip, tip + new Vector2(ArrowHalfWidth, -ArrowLength), thickness, color);
    }

    private void CollectNode(NodeLayout node, in Matrix4x4 world, CommandList commands)
    {
        var pass = node.Pass;
        var fill = pass.IsBarrier ? GraphPalette.Barrier : GraphPalette.ForCategory(pass.Category);

        if (pass.Id == SelectedPass?.Id)
            commands.AddRect(At(node.Position - new Vector2(3f), world), node.Size + new Vector2(6f), Color.White,
                new Vector4(11f));

        commands.AddRect(At(node.Position, world), node.Size, fill, new Vector4(8f));

        var titleY = pass.IsBarrier ? 3f : 5f;
        commands.AddText(At(node.Position + new Vector2(10f, titleY), world), TextMeasure.FontName, pass.Name,
            pass.IsBarrier ? BarrierTitleSize : TitleSize, Color.White);

        if (pass.IsBarrier) return;

        commands.AddText(At(node.Position + new Vector2(10f, 26f), world), TextMeasure.FontName, Subtitle(pass),
            SubtitleSize, Color.White with { A = 0.75f });
    }

    private static string Subtitle(PassSnapshot pass)
    {
        var reads = pass.Uses.Count(u => u.Operation == ResourceOperation.Read);
        return $"{reads} reads, {pass.Uses.Count - reads} writes";
    }

    private static Matrix4x4 At(Vector2 position, in Matrix4x4 world)
    {
        return Matrix4x4.Identity.Translate(position).ChildOf(world);
    }

    private Vector2 ToLocal(Vector2 surfacePosition)
    {
        return surfacePosition.Transform(ComputeAbsoluteContentTransform().Inverse());
    }

    private Vector2 ToWorld(Vector2 surfacePosition)
    {
        return (ToLocal(surfacePosition) - _pan) / _zoom;
    }

    private void SelectPass(PassSnapshot? pass)
    {
        if (SelectedPass?.Id == pass?.Id) return;

        SelectedPass = pass;
        OnSelectionChanged?.Invoke(pass);
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        if (e.Button is not (CursorButton.One or CursorButton.Two)) return;

        if (e.Button == CursorButton.One && _layout?.HitTest(ToWorld(e.Position))?.Pass is { } hit)
            SelectPass(hit.Id == SelectedPass?.Id ? null : hit);
        _dragging = true;
        _dragStart = e.Position;
        _panAtDragStart = _pan;
        e.Target = this;
    }

    public override bool IsFocusable => true;

    public override void OnKeyboard(KeyboardSurfaceEvent e)
    {
        if (e is { Key: InputKey.Escape, State: InputState.Pressed }) SelectPass(null);
    }

    public override void OnCursorMove(CursorMoveSurfaceEvent e, in Matrix4x4 transform)
    {
        if (!_dragging) return;

        _pan = _panAtDragStart + (e.Position - _dragStart);
        e.Target = this;
    }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        base.OnCursorUp(e);
        _dragging = false;
    }

    protected override bool OnScroll(ScrollSurfaceEvent e)
    {
        var anchor = ToWorld(e.Position);
        _zoom = float.Clamp(_zoom * float.Exp(float.Clamp(e.Delta.Y, -MaxScrollDelta, MaxScrollDelta) * ZoomStep), MinZoom, MaxZoom);
        _pan = ToLocal(e.Position) - anchor * _zoom;
        return true;
    }
}
