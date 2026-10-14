using System.Numerics;
using Examples.AudioPlayer.Playback;
using Examples.Common.Views;
using Rin.Core.Shared.Math;
using Rin.Core.Views;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;

namespace Examples.AudioPlayer.Views;

/// <summary>
///     Bars for the levels of a <see cref="SpectrumAnalyzer" />, rising from the bottom edge.
/// </summary>
public sealed class SpectrumView(SpectrumAnalyzer analyzer) : ContentView
{
    private const float Gap = 3f;
    private const float MinBarHeight = 3f;
    private const float DefaultWidth = 300f;

    public float Height { get; init; } = 64f;

    public override Vector2 ComputeDesiredContentSize()
    {
        return new Vector2(DefaultWidth, Height);
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return new Vector2(float.IsFinite(availableSpace.X) ? availableSpace.X : DefaultWidth, Height);
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        var size = GetContentSize();
        var levels = analyzer.Levels;
        var barWidth = (size.X - Gap * (levels.Length - 1)) / levels.Length;
        if (barWidth <= 0f) return;

        var radius = BorderRadius.All(barWidth / 2f);
        for (var i = 0; i < levels.Length; i++)
        {
            var level = levels[i];
            var height = float.Max(MinBarHeight, level * size.Y);
            var offset = new Vector2(i * (barWidth + Gap), size.Y - height);
            var color = Ui.Accent with { A = 0.3f + 0.7f * level };
            commands.AddRect(transform.Translate(offset), new Vector2(barWidth, height), color, radius);
        }
    }
}
