using System.Numerics;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Shared.Math;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Blur;
using Rin.Core.Views.Graphics.Quads;

namespace UiGallery;

public static class UiGalleryScene
{
    public static void Create(IWindowRenderer renderer)
    {
        if (IViewsModule.Get().GetWindowSurface(renderer) is not { } surface) return;

        surface.Add(new GalleryCanvas());
    }

    private delegate void DrawSection(CommandList commands, Func<float, float, Matrix4x4> at, float time);

    private sealed record Section(string Title, Vector2 Size, DrawSection Draw);

    private sealed class GalleryCanvas : ContentView
    {
        private const float Padding = 16f;
        private const float Gap = 16f;
        private const float TitleHeight = 28f;

        private readonly ResourceHandle _checker = CreateChecker();
        private readonly string _fontName = new TextBoxView().FontFamily;
        private readonly Section[] _sections;

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
        }

        protected override Vector2 LayoutContent(in Vector2 availableSpace)
        {
            return availableSpace;
        }

        public override void CollectContent(in Matrix4x4 transform, CommandList commands)
        {
            var root = transform;
            var size = GetContentSize();
            var time = (float)IApplication.Get().TimeSeconds;

            commands.AddRect(root, size, new Color(0.13f, 0.14f, 0.17f, 1f));

            var scale = ChooseScale(size);
            foreach (var (section, position) in Flow(size.X / scale))
            {
                var sectionRoot = Matrix4x4.Identity.Scale(new Vector2(scale)).Translate(position * scale)
                    .ChildOf(root);

                Matrix4x4 At(float x, float y)
                {
                    return Matrix4x4.Identity.Translate(new Vector2(x, y + TitleHeight)).ChildOf(sectionRoot);
                }

                commands.AddText(sectionRoot, _fontName, section.Title, 16f, Color.White);
                section.Draw(commands, At, time);
            }
        }

        private float ChooseScale(Vector2 available)
        {
            for (var scale = 1f; scale > 0.3f; scale -= 0.05f)
                if (FlowHeight(available.X / scale) * scale <= available.Y)
                    return scale;

            return 0.3f;
        }

        private float FlowHeight(float width)
        {
            var bottom = 0f;
            foreach (var (section, position) in Flow(width))
                bottom = MathF.Max(bottom, position.Y + TitleHeight + section.Size.Y);

            return bottom + Padding;
        }

        private IEnumerable<(Section Section, Vector2 Position)> Flow(float width)
        {
            var x = Padding;
            var y = Padding;
            var rowHeight = 0f;

            foreach (var section in _sections)
            {
                if (x > Padding && x + section.Size.X > width - Padding)
                {
                    x = Padding;
                    y += rowHeight + Gap;
                    rowHeight = 0f;
                }

                yield return (section, new Vector2(x, y));
                x += section.Size.X + Gap;
                rowHeight = MathF.Max(rowHeight, TitleHeight + section.Size.Y);
            }
        }

        private static Matrix4x4 Rotating(Func<float, float, Matrix4x4> at, Vector2 center, Vector2 size, float degrees)
        {
            return Matrix4x4.Identity.Translate(-size / 2f).Rotate2DDegrees(degrees)
                .Translate(center)
                .ChildOf(at(0, 0));
        }

        private static void DrawRectangles(CommandList commands, Func<float, float, Matrix4x4> at, float time)
        {
            var size = new Vector2(120, 80);
            commands.AddRect(at(0, 0), size, new Color(0.95f, 0.25f, 0.25f, 1f));
            commands.AddRect(at(140, 0), size, new Color(0.3f, 0.85f, 0.4f, 1f), new Vector4(16f));
            commands.AddRect(at(280, 0), size, new Color(0.35f, 0.5f, 0.95f, 1f), new Vector4(40f, 0f, 0f, 40f));
            commands.AddRect(at(420, 0), size, new Color(1f, 1f, 1f, 0.5f), new Vector4(8f));
            commands.AddRect(at(450, 30), size, new Color(0.95f, 0.8f, 0.2f, 0.5f), new Vector4(8f));
            commands.AddRect(Rotating(at, new Vector2(630, 60), size, time * 40f), size,
                new Color(0.8f, 0.4f, 0.95f, 1f), new Vector4(20f));
            commands.AddRect(Rotating(at, new Vector2(630, 125), new Vector2(120, 10), time * -25f),
                new Vector2(120, 10), Color.White, new Vector4(5f));
        }

        private static void DrawCircles(CommandList commands, Func<float, float, Matrix4x4> at, float time)
        {
            var x = 0f;
            foreach (var radius in new[] { 10f, 20f, 30f, 40f })
            {
                commands.AddCircle(at(x, 40 - radius + 20), radius, new Color(0.3f, 0.8f, 0.95f, 1f));
                x += radius * 2f + 20f;
            }

            commands.AddCircle(at(260, 0), 45f, new Color(1f, 0.3f, 0.3f, 0.6f));
            commands.AddCircle(at(310, 0), 45f, new Color(0.3f, 1f, 0.3f, 0.6f));
            commands.AddCircle(at(285, 40), 45f, new Color(0.3f, 0.3f, 1f, 0.6f));
        }

        private static void DrawLines(CommandList commands, Func<float, float, Matrix4x4> at, float time)
        {
            var origin = at(0, 0);
            float[] thickness = [1f, 2f, 4f, 8f];
            for (var i = 0; i < thickness.Length; i++)
                commands.AddLine(origin, new Vector2(0, 10 + i * 22), new Vector2(120, 10 + i * 22), thickness[i],
                    Color.White);

            commands.AddLine(origin, new Vector2(150, 5), new Vector2(260, 95), 2f, Color.White);
            commands.AddLine(origin, new Vector2(150, 95), new Vector2(260, 5), 4f, Color.White);
        }

        private static void DrawQuadratics(CommandList commands, Func<float, float, Matrix4x4> at, float time)
        {
            var origin = at(0, 0);
            var yellow = new Color(1f, 0.85f, 0.2f, 1f);
            float[] thickness = [1f, 2f, 4f, 8f];
            for (var i = 0; i < thickness.Length; i++)
                commands.AddQuadraticCurve(origin, new Vector2(i * 80f, 100), new Vector2(i * 80f + 60f, 100),
                    new Vector2(i * 80f + 30f, 0), thickness[i], yellow);
        }

        private static void DrawCubics(CommandList commands, Func<float, float, Matrix4x4> at, float time)
        {
            var origin = at(0, 0);
            var cyan = new Color(0.3f, 0.95f, 0.9f, 1f);
            float[] thickness = [1f, 2f, 4f, 8f];
            for (var i = 0; i < thickness.Length; i++)
                commands.AddCubicCurve(origin, new Vector2(i * 95f, 100), new Vector2(i * 95f + 70f, 10),
                    new Vector2(i * 95f + 25f, -10), new Vector2(i * 95f + 45f, 120), thickness[i], cyan);
        }

        private void DrawTextures(CommandList commands, Func<float, float, Matrix4x4> at, float time)
        {
            var size = new Vector2(110, 110);
            commands.AddTexture(_checker, at(0, 0), size);
            commands.AddTexture(_checker, at(130, 0), size, new Color(1f, 0.6f, 0.6f, 1f));
            commands.AddTexture(_checker, at(260, 0), size, null, null, new Vector4(30f));
            commands.AddTexture(_checker, at(390, 0), size, null, new Vector4(0f, 0f, 0.25f, 0.25f));
            commands.AddTexture(_checker, at(520, 0), size, null, new Vector4(0f, 0f, 3f, 3f));
            commands.AddTexture(_checker, Rotating(at, new Vector2(715, 55), size, time * 30f), size, null, null,
                new Vector4(55f));
        }

        private void DrawText(CommandList commands, Func<float, float, Matrix4x4> at, float time)
        {
            var y = 0f;
            foreach (var fontSize in new[] { 12f, 18f, 28f, 48f })
            {
                commands.AddText(at(0, y), _fontName, $"Text {fontSize}", fontSize, Color.White);
                y += fontSize + 8f;
            }
        }

        private static void DrawColorWheel(CommandList commands, Func<float, float, Matrix4x4> at, float time)
        {
            commands.AddQuads(Quad.ColorWheel(at(0, 0), new Vector2(150, 150)));
        }

        private void DrawBlur(CommandList commands, Func<float, float, Matrix4x4> at, float time)
        {
            for (var i = 0; i < 14; i++)
            {
                var color = i % 2 == 0 ? new Color(0.9f, 0.3f, 0.4f, 1f) : new Color(0.2f, 0.5f, 0.95f, 1f);
                commands.AddRect(at(i * 20f, 0), new Vector2(20, 140), color);
            }

            commands.AddCircle(at(40, 20), 22f, new Color(1f, 0.9f, 0.2f, 1f));
            commands.AddCircle(at(190, 80), 28f, new Color(0.2f, 0.9f, 0.5f, 1f));
            commands.AddBlur(at(60, 30), new Vector2(160, 80), 6f, 12f, new Color(1f, 1f, 1f, 0.15f));
            commands.AddText(at(76, 56), _fontName, "Blurred backdrop", 16f, Color.White);
        }

        private static void DrawClip(CommandList commands, Func<float, float, Matrix4x4> at, float time)
        {
            var size = new Vector2(190, 190);
            commands.AddRect(at(0, 0), size, new Color(0.2f, 0.2f, 0.26f, 1f));
            commands.PushClip(at(0, 0), size);
            commands.AddRect(Rotating(at, new Vector2(150, 140), new Vector2(240, 240), 20f + time * 10f),
                new Vector2(240, 240), new Color(1f, 0.55f, 0.1f, 1f));
            commands.AddRect(at(-30, -30), new Vector2(90, 90), new Color(0.2f, 0.8f, 0.9f, 1f), new Vector4(30f));
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
