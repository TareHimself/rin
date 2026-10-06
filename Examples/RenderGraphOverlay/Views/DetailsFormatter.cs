using System.Text;
using Rin.Core.Graphics;
using RenderGraphOverlay.Snapshot;

namespace RenderGraphOverlay.Views;

public static class DetailsFormatter
{
    public static string Format(PassSnapshot? pass, GraphSnapshot? snapshot)
    {
        if (snapshot is null) return "No capture yet.";
        if (pass is null) return FormatSummary(snapshot);

        var text = new StringBuilder();
        text.AppendLine(pass.Name);
        var stageTitle = snapshot.Stages[pass.Stage].Title;
        text.AppendLine(pass.IsBarrier ? stageTitle : $"{pass.Category} pass, {stageTitle}");
        AppendUses(text, "Writes", pass, snapshot, ResourceOperation.Write);
        AppendUses(text, "Reads", pass, snapshot, ResourceOperation.Read);

        if (pass.Fields.Count == 0) return text.ToString();

        text.AppendLine().AppendLine("Fields");
        foreach (var field in pass.Fields) text.AppendLine(field);
        return text.ToString();
    }

    private static string FormatSummary(GraphSnapshot snapshot)
    {
        var text = new StringBuilder();
        text.AppendLine($"Captured {snapshot.CapturedAt:T}");
        text.AppendLine($"{snapshot.PassCount} passes in {snapshot.Stages.Count(s => !s.IsBarrier)} stages, {snapshot.Stages.Count(s => s.IsBarrier)} barriers");
        text.AppendLine($"{snapshot.Resources.Count} resources, {snapshot.Links.Count} links");
        text.AppendLine($"{snapshot.PrunedPassCount} passes pruned");
        text.AppendLine().AppendLine("Click a pass for details. Scroll or drag to move through the graph.");
        return text.ToString();
    }

    private static void AppendUses(StringBuilder text, string title, PassSnapshot pass, GraphSnapshot snapshot,
        ResourceOperation operation)
    {
        var uses = pass.Uses.Where(u => u.Operation == operation).ToList();
        if (uses.Count == 0) return;

        text.AppendLine().AppendLine(title);
        foreach (var use in uses)
        {
            var label = snapshot.Resources.TryGetValue(use.ResourceId, out var resource) ? resource.Label : "?";
            text.AppendLine($"#{use.ResourceId} {label}, {use.State}");
        }
    }
}
