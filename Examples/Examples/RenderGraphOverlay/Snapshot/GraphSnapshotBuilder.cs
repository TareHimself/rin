using System.Reflection;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Graphics.Vulkan;
using Rin.Graphics.Vulkan.Graph;

namespace Examples.RenderGraphOverlay.Snapshot;

internal static class GraphSnapshotBuilder
{
    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static GraphSnapshot Build(ICompiledGraph graph, IGraphConfig config, IGraphBuilder builder,
        uint probePassId)
    {
        var groups = ReadField<IEnumerable<ExecutionGroup>>(graph, "_nodes").ToList();
        var descriptors = ReadField<IDictionary<uint, IResourceDescriptor>>(graph, "_descriptors");
        var builderPasses = ReadField<IDictionary<uint, IPass>>(builder, "_passes");
        var resourceActions = ((GraphConfig)config).ResourceActions;

        var uses = CollectUses(resourceActions, probePassId);
        var links = CollectLinks(resourceActions, probePassId);
        var stages = CollectStages(groups, uses, probePassId);

        var usedResourceIds = uses.Values.SelectMany(u => u).Select(u => u.ResourceId).Distinct();
        var resources = usedResourceIds.ToDictionary(id => id, id => DescribeResource(id, descriptors));

        var livePassCount = groups.Where(g => !g.IsBarrier).Sum(g => g.Passes.Count);
        return new GraphSnapshot(DateTime.Now, stages, resources, links, builderPasses.Count - livePassCount);
    }

    private static T ReadField<T>(object target, string name)
    {
        var field = target.GetType().GetField(name, InstanceFields)
                    ?? throw new MissingFieldException(target.GetType().FullName, name);
        return (T)field.GetValue(target)!;
    }

    private static Dictionary<uint, List<ResourceUseSnapshot>> CollectUses(
        Dictionary<uint, List<GraphConfig.ResourceAction>> resourceActions, uint probePassId)
    {
        Dictionary<uint, List<ResourceUseSnapshot>> uses = [];
        foreach (var (resourceId, actions) in resourceActions)
        foreach (var action in actions.Where(a => a.PassId != probePassId))
        {
            if (!uses.TryGetValue(action.PassId, out var passUses)) uses[action.PassId] = passUses = [];
            passUses.Add(new ResourceUseSnapshot(resourceId, action.Operation, DescribeState(action)));
        }

        return uses;
    }

    private static string DescribeState(GraphConfig.ResourceAction action)
    {
        var state = action.Type == GraphResourceKind.Image ? action.ImageLayout.ToString() : action.BufferUsage.ToString();
        return action.Discard ? $"{state} (discard)" : state;
    }

    private static List<LinkSnapshot> CollectLinks(
        Dictionary<uint, List<GraphConfig.ResourceAction>> resourceActions, uint probePassId)
    {
        HashSet<LinkSnapshot> links = [];
        foreach (var (resourceId, actions) in resourceActions)
        {
            uint lastWriter = 0;
            foreach (var action in actions.Where(a => a.PassId != probePassId))
            {
                if (lastWriter != 0 && lastWriter != action.PassId)
                    links.Add(new LinkSnapshot(resourceId, lastWriter, action.PassId));
                if (action.Operation == ResourceOperation.Write) lastWriter = action.PassId;
            }
        }

        return links.ToList();
    }

    private static List<StageSnapshot> CollectStages(List<ExecutionGroup> groups,
        Dictionary<uint, List<ResourceUseSnapshot>> uses, uint probePassId)
    {
        List<StageSnapshot> stages = [];
        var executionStages = 0;
        foreach (var group in groups)
        {
            var passes = group.Passes.Where(p => p.Id != probePassId).ToList();
            if (passes.Count == 0) continue;

            var stageIndex = stages.Count;
            var snapshots = passes.Select(pass => new PassSnapshot(
                pass.Id,
                NameOf(pass),
                CategoryOf(pass),
                stageIndex,
                group.IsBarrier,
                uses.GetValueOrDefault(pass.Id) ?? [],
                PassInspector.DescribeFields(pass))).ToList();
            var title = group.IsBarrier ? "Barrier" : $"Stage {executionStages++}";
            stages.Add(new StageSnapshot(stageIndex, title, group.IsBarrier, snapshots));
        }

        return stages;
    }

    private static string NameOf(IPass pass)
    {
        return pass is ActionPass action ? action.Name : pass.GetType().Name;
    }

    private static PassCategory CategoryOf(IPass pass)
    {
        var ns = pass.GetType().Namespace ?? string.Empty;
        return ns switch
        {
            _ when ns.StartsWith("Rin.Core.Views") => PassCategory.Views,
            _ when ns.StartsWith("Rin.World") => PassCategory.World,
            _ when ns.StartsWith("Rin.Graphics.Vulkan") => PassCategory.Backend,
            _ when ns.StartsWith("Rin.Core") => PassCategory.Core,
            _ => PassCategory.App
        };
    }

    private static ResourceSnapshot DescribeResource(uint id, IDictionary<uint, IResourceDescriptor> descriptors)
    {
        if (!descriptors.TryGetValue(id, out var descriptor)) return new ResourceSnapshot(id, $"Resource #{id}", false);

        var type = descriptor.GetType();
        var kind = type.Name.Replace("ResourceDescriptor", string.Empty);
        var details = new[] { "Extent", "Count", "Format", "Size" }
            .Select(name => type.GetField(name)?.GetValue(descriptor))
            .OfType<object>()
            .Select(FormatDetail);

        var label = string.Join(' ', details.Prepend(kind));
        return new ResourceSnapshot(id, label, type.Name.StartsWith("External"));
    }

    private static string FormatDetail(object value)
    {
        return value switch
        {
            Extent2D extent => $"{extent.Width}x{extent.Height}",
            ulong bytes => $"{bytes / 1024.0:0.#} KiB",
            _ => value.ToString() ?? string.Empty
        };
    }
}
