using System.Numerics;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Shared.Math;
using Rin.Core.Views;
using Rin.Core.Views.Content;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Blur;
using Rin.Core.Views.Graphics.Quads;

namespace UiGallery;

/// <summary>
/// Creates the gallery: one canvas that draws every quad mode, blur and clipping.
/// </summary>
public static class UiGalleryScene
{
    public static void Create(IWindowRenderer renderer)
    {
        if (IViewsModule.Get().GetWindowSurface(renderer) is not { } surface) return;

        surface.Add(new GalleryCanvas());
    }

    private delegate void DrawSection(CommandList commands, in Matrix4x4 origin, float time);

    private readonly record struct Section(string Title, Vector2 Size, DrawSection Draw);

    /// <summary>
    /// Draws fixed-size sections that wrap to the window width and scroll vertically with the mouse wheel.
    /// The layout is cached and only recomputed when the width changes.
    /// </summary>
    private sealed class GalleryCanvas : ContentView
    {
        private const float Margin = 16f;
        private const float Gap = 16f;
        private const float TitleHeight = 28f;
        private const float ScrollStep = 48f;

        private static readonly string[] TextLabels = ["Text 12", "Text 18", "Text 28", "Text 48"];
        private static readonly float[] TextSizes = [12f, 18f, 28f, 48f];
        private static readonly float[] Thickness = [1f, 2f, 4f, 8f];

        private readonly ResourceHandle _checker = CreateChecker();
        private readonly string _fontName = new TextBoxView().FontFamily;
        private readonly Section[] _sections;
        private readonly Vector2[] _positions;

        private float _layoutWidth = -1f;
        private float _contentHeight;
        private float _scroll;

        public GalleryCanvas()
        {
            _sections =
            [
                new Section("Rectangles", new Vector2(700, 150), DrawRectangles),
                new Section("Circles", new Vector2(460, 150), DrawCircles),
                new Section("Lines", new Vector2(270, 150), DrawLines),
                new Section("Quadratic curves", new Vector2(330, 150), DrawQuadratics),
                new Section("Cubic curves", new Vector2(400, 150), DrawCubics),
                new Section("Textures", new Vector2(790, 170), DrawTextures),
                new Section("Text (MTSDF)", new Vector2(360, 230), DrawText),
                new Section("Color wheel", new Vector2(170, 190), DrawColorWheel),
                new Section("Background blur", new Vector2(280, 170), DrawBlur),
                new Section("Clipping", new Vector2(230, 230), DrawClip)
            ];
            _positions = new Vector2[_sections.Length];
        }

        protected override Vector2 LayoutContent(in Vector2 availableSpace)
        {
            return availableSpace;
        }

        protected override bool OnScroll(ScrollSurfaceEvent e)
        {
            var maxScroll = MathF.Max(0f, _contentHeight - GetContentSize().Y);
            _scroll = Math.Clamp(_scroll - e.Delta.Y * ScrollStep, 0f, maxScroll);
            return true;
        }

        public override void CollectContent(in Matrix4x4 transform, CommandList commands)
        {
            var size = GetContentSize();
            var time = (float)IApplication.Get().TimeSeconds;
            Layout(size.X);
            _scroll = Math.Clamp(_scroll, 0f, MathF.Max(0f, _contentHeight - size.Y));

            commands.AddRect(transform, size, new Color(0.13f, 0.14f, 0.17f, 1f));

            for (var i = 0; i < _sections.Length; i++)
            {
                var section = _sections[i];
                var top = _positions[i].Y - _scroll;
                if (top + TitleHeight + section.Size.Y < 0f || top > size.Y) continue;

                var sectionOrigin = Matrix4x4.Identity.Translate(new Vector2(_positions[i].X, top)).ChildOf(transform);
                commands.AddText(sectionOrigin, _fontName, section.Title, 16f, Color.White);

                var contentOrigin = Matrix4x4.Identity.Translate(new Vector2(0f, TitleHeight)).ChildOf(sectionOrigin);
                section.Draw(commands, contentOrigin, time);
            }
        }

        private void Layout(float width)
        {
            if (width == _layoutWidth) return;

            _layoutWidth = width;
            var x = Margin;
            var y = Margin;
            var rowHeight = 0f;

            for (var i = 0; i < _sections.Length; i++)
            {
                var section = _sections[i];
                if (x > Margin && x + section.Size.X > width - Margin)
                {
                    x = Margin;
                    y += rowHeight + Gap;
                    rowHeight = 0f;
                }

                _positions[i] = new Vector2(x, y);
                x += section.Size.X + Gap;
                rowHeight = MathF.Max(rowHeight, TitleHeight + section.Size.Y);
            }

            _contentHeight = y + rowHeight + Margin;
        }

        private static Matrix4x4 At(in Matrix4x4 origin, float x, float y)
        {
            return Matrix4x4.Identity.Translate(new Vector2(x, y)).ChildOf(origin);
        }

        private static Matrix4x4 Rotating(in Matrix4x4 origin, Vector2 center, Vector2 size, float degrees)
        {
            return Matrix4x4.Identity.Translate(-size / 2f).Rotate2DDegrees(degrees).Translate(center).ChildOf(origin);
        }

        private static void DrawRectangles(CommandList commands, in Matrix4x4 origin, float time)
        {
            var size = new Vector2(120, 80);
            commands.AddRect(At(origin, 0, 0), size, new Color(0.95f, 0.25f, 0.25f, 1f));
            commands.AddRect(At(origin, 140, 0), size, new Color(0.3f, 0.85f, 0.4f, 1f), new Vector4(16f));
            commands.AddRect(At(origin, 280, 0), size, new Color(0.35f, 0.5f, 0.95f, 1f),
                new Vector4(40f, 0f, 0f, 40f));
            commands.AddRect(At(origin, 420, 0), size, new Color(1f, 1f, 1f, 0.5f), new Vector4(8f));
            commands.AddRect(At(origin, 450, 30), size, new Color(0.95f, 0.8f, 0.2f, 0.5f), new Vector4(8f));
            commands.AddRect(Rotating(origin, new Vector2(630, 60), size, time * 40f), size,
                new Color(0.8f, 0.4f, 0.95f, 1f), new Vector4(20f));
            commands.AddRect(Rotating(origin, new Vector2(630, 125), new Vector2(120, 10), time * -25f),
                new Vector2(120, 10), Color.White, new Vector4(5f));
        }

        private static void DrawCircles(CommandList commands, in Matrix4x4 origin, float time)
        {
            var x = 0f;
            for (var i = 0; i < 4; i++)
            {
                var radius = 10f * (i + 1);
                commands.AddCircle(At(origin, x, 60 - radius), radius, new Color(0.3f, 0.8f, 0.95f, 1f));
                x += radius * 2f + 20f;
            }

            commands.AddCircle(At(origin, 260, 0), 45f, new Color(1f, 0.3f, 0.3f, 0.6f));
            commands.AddCircle(At(origin, 310, 0), 45f, new Color(0.3f, 1f, 0.3f, 0.6f));
            commands.AddCircle(At(origin, 285, 40), 45f, new Color(0.3f, 0.3f, 1f, 0.6f));
        }

        private static void DrawLines(CommandList commands, in Matrix4x4 origin, float time)
        {
            for (var i = 0; i < Thickness.Length; i++)
                commands.AddLine(origin, new Vector2(0, 10 + i * 22), new Vector2(120, 10 + i * 22), Thickness[i],
                    Color.White);

            commands.AddLine(origin, new Vector2(150, 5), new Vector2(260, 95), 2f, Color.White);
            commands.AddLine(origin, new Vector2(150, 95), new Vector2(260, 5), 4f, Color.White);
        }

        private static void DrawQuadratics(CommandList commands, in Matrix4x4 origin, float time)
        {
            var yellow = new Color(1f, 0.85f, 0.2f, 1f);
            for (var i = 0; i < Thickness.Length; i++)
                commands.AddQuadraticCurve(origin, new Vector2(i * 80f, 100), new Vector2(i * 80f + 60f, 100),
                    new Vector2(i * 80f + 30f, 0), Thickness[i], yellow);
        }

        private static void DrawCubics(CommandList commands, in Matrix4x4 origin, float time)
        {
            var cyan = new Color(0.3f, 0.95f, 0.9f, 1f);
            for (var i = 0; i < Thickness.Length; i++)
                commands.AddCubicCurve(origin, new Vector2(i * 95f, 100), new Vector2(i * 95f + 70f, 10),
                    new Vector2(i * 95f + 25f, -10), new Vector2(i * 95f + 45f, 120), Thickness[i], cyan);
        }

        private void DrawTextures(CommandList commands, in Matrix4x4 origin, float time)
        {
            var size = new Vector2(110, 110);
            commands.AddTexture(_checker, At(origin, 0, 0), size);
            commands.AddTexture(_checker, At(origin, 130, 0), size, new Color(1f, 0.6f, 0.6f, 1f));
            commands.AddTexture(_checker, At(origin, 260, 0), size, null, null, new Vector4(30f));
            commands.AddTexture(_checker, At(origin, 390, 0), size, null, new Vector4(0f, 0f, 0.25f, 0.25f));
            commands.AddTexture(_checker, At(origin, 520, 0), size, null, new Vector4(0f, 0f, 3f, 3f));
            commands.AddTexture(_checker, Rotating(origin, new Vector2(715, 55), size, time * 30f), size, null, null,
                new Vector4(55f));
        }

        private void DrawText(CommandList commands, in Matrix4x4 origin, float time)
        {
            var y = 0f;
            for (var i = 0; i < TextSizes.Length; i++)
            {
                commands.AddText(At(origin, 0, y), _fontName, TextLabels[i], TextSizes[i], Color.White);
                y += TextSizes[i] + 8f;
            }
        }

        private static void DrawColorWheel(CommandList commands, in Matrix4x4 origin, float time)
        {
            commands.AddQuads(Quad.ColorWheel(At(origin, 0, 0), new Vector2(150, 150)));
        }

        private void DrawBlur(CommandList commands, in Matrix4x4 origin, float time)
        {
            for (var i = 0; i < 14; i++)
            {
                var color = i % 2 == 0 ? new Color(0.9f, 0.3f, 0.4f, 1f) : new Color(0.2f, 0.5f, 0.95f, 1f);
                commands.AddRect(At(origin, i * 20f, 0), new Vector2(20, 140), color);
            }

            commands.AddCircle(At(origin, 40, 20), 22f, new Color(1f, 0.9f, 0.2f, 1f));
            commands.AddCircle(At(origin, 190, 80), 28f, new Color(0.2f, 0.9f, 0.5f, 1f));
            commands.AddBlur(At(origin, 60, 30), new Vector2(160, 80), 6f, 12f, new Color(1f, 1f, 1f, 0.15f));
            commands.AddText(At(origin, 76, 56), _fontName, "Blurred backdrop", 16f, Color.White);
        }

        private static void DrawClip(CommandList commands, in Matrix4x4 origin, float time)
        {
            var size = new Vector2(190, 190);
            commands.AddRect(At(origin, 0, 0), size, new Color(0.2f, 0.2f, 0.26f, 1f));
            commands.PushClip(At(origin, 0, 0), size);
            commands.AddRect(Rotating(origin, new Vector2(150, 140), new Vector2(240, 240), 20f + time * 10f),
                new Vector2(240, 240), new Color(1f, 0.55f, 0.1f, 1f));
            commands.AddRect(At(origin, -30, -30), new Vector2(90, 90), new Color(0.2f, 0.8f, 0.9f, 1f),
                new Vector4(30f));
            commands.PopClip();
        }

        private static ResourceHandle CreateChecker()
        {
            const int size = 128;
            var data = new byte[size * size * 4];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dark = ((x / 16) + (y / 16)) % 2 == 0;
                var index = (y * size + x) * 4;
                data[index] = (byte)(dark ? 40 + x : 220 - y);
                data[index + 1] = (byte)(dark ? 60 + y : 200);
                data[index + 2] = (byte)(dark ? 200 : 60 + x);
                data[index + 3] = 255;
            }

            IGraphicsModule.Get().CreateTexture(out var handle, data, new Extent2D(size), ImageFormat.RGBA8);
            return handle;
        }
    }
}
