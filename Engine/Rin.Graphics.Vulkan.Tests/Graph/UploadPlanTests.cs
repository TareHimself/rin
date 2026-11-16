using Rin.Core.Graphics;
using Rin.Core.Shared;
using Rin.Graphics.Vulkan.Graph;

namespace Rin.Graphics.Vulkan.Tests.Graph;

public class UploadPlanTests : UploadLanesFixture
{
    private UploadPlan Plan(IEnumerable<ResourceHandle> touched, Func<ResourceHandle, bool>? import = null)
    {
        import ??= _ => false;
        return UploadPlan.Build(Lanes, touched, (_, handle) => import(handle));
    }

    private static string Describe(UploadPlan plan)
    {
        return string.Join(" ", plan.Steps.Select(s => s.Kind switch
        {
            UploadStepKind.Copy => $"copy({s.Handle.Id}>{s.Destination.Id})",
            UploadStepKind.BufferWrites => $"w{s.Handle.Id}x{s.Count}",
            _ => $"t{s.Handle.Id}"
        }));
    }

    [Test]
    public void Build_PlansTheSegmentsBeforeACopy_ThenTheCopy_ThenTheSegmentsAfterIt()
    {
        QueueWrite(1, 0);
        QueueWrite(2, 0);
        QueueCopy(1, 2);
        QueueWrite(1, 16);
        QueueWrite(2, 16);

        var plan = Plan([Buffer(1), Buffer(2)]);

        Assert.That(Describe(plan), Is.EqualTo("w1x1 w2x1 copy(1>2) w1x1 w2x1"));
    }

    [Test]
    public void Build_ChainsEachStepToThePreviousStepInEachOfItsLanes()
    {
        QueueWrite(1, 0);
        QueueWrite(2, 0);
        QueueCopy(1, 2);
        QueueWrite(1, 16);

        var plan = Plan([Buffer(1), Buffer(2)]);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Steps[0].PreviousInLane, Is.EqualTo(-1));
            Assert.That(plan.Steps[2].PreviousInLane, Is.EqualTo(0));
            Assert.That(plan.Steps[2].PreviousInDestinationLane, Is.EqualTo(1));
            Assert.That(plan.Steps[3].PreviousInLane, Is.EqualTo(2));
            Assert.That(plan.LaneSteps[Buffer(1)], Is.EqualTo(new[] { 0, 2, 3 }));
            Assert.That(plan.LaneSteps[Buffer(2)], Is.EqualTo(new[] { 1, 2 }));
        });
    }

    [Test]
    public void Build_PlansEachTextureWriteAsItsOwnStep_AndAWholeRunOfBufferWritesAsOne()
    {
        Lanes.EnqueueTexture(Texture(1), new PendingTextureWrite(PooledMemory<byte>.CopyFrom(new byte[16]),
            ImageRegion.Whole(new Extent2D(2, 2)), new ImageShape(new Extent2D(4, 4))));
        Lanes.EnqueueTexture(Texture(1), new PendingTextureWrite(PooledMemory<byte>.CopyFrom(new byte[16]),
            ImageRegion.Rect(new Offset2D(2, 2), new Extent2D(2, 2)), new ImageShape(new Extent2D(4, 4))));
        QueueWrite(2, 0);
        QueueWrite(2, 32);

        var plan = Plan([Texture(1), Buffer(2)]);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Steps.Select(s => (s.Kind, s.LaneIndex, s.Count)), Is.EquivalentTo(new[]
            {
                (UploadStepKind.TextureWrite, 0, 1), (UploadStepKind.TextureWrite, 1, 1),
                (UploadStepKind.BufferWrites, 0, 2)
            }));
        });
    }

    [Test]
    public void Build_PlansASingleLaneCopyOnce_WithOneEntryInItsLane()
    {
        QueueWrite(1, 0);
        QueueCopy(1, 1);
        QueueWrite(1, 64);

        var plan = Plan([Buffer(1)]);

        Assert.Multiple(() =>
        {
            Assert.That(Describe(plan), Is.EqualTo("w1x1 copy(1>1) w1x1"));
            Assert.That(plan.LaneSteps[Buffer(1)], Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(plan.Steps[1].PreviousInDestinationLane, Is.EqualTo(-1));
        });
    }

    [Test]
    public void Build_ImportsTheOtherSideOfACopy_AndPlansItsWritesAhead()
    {
        QueueWrite(2, 0);
        QueueCopy(1, 2);
        List<ResourceHandle> imported = [];

        var plan = Plan([Buffer(1)], handle =>
        {
            imported.Add(handle);
            return true;
        });

        Assert.Multiple(() =>
        {
            Assert.That(imported, Is.EqualTo(new[] { Buffer(2) }));
            Assert.That(Describe(plan), Is.EqualTo("w2x1 copy(1>2)"));
        });
    }

    [Test]
    public void Build_ImportsTheTextureOfATextureToBufferCopy_WhenTheGraphOnlyTouchesTheBuffer()
    {
        Lanes.EnqueueTexture(Texture(1), new PendingTextureWrite(PooledMemory<byte>.CopyFrom(new byte[16]),
            ImageRegion.Whole(new Extent2D(2, 2)), new ImageShape(new Extent2D(4, 4))));
        Lanes.EnqueueShared(new SharedWrite(new TextureToBufferCopy(Texture(1), Buffer(2), default,
            new Extent2D(2, 2), 0, 16)));
        List<ResourceHandle> imported = [];

        var plan = Plan([Buffer(2)], handle =>
        {
            imported.Add(handle);
            return true;
        });

        Assert.Multiple(() =>
        {
            Assert.That(imported, Is.EqualTo(new[] { Texture(1) }));
            Assert.That(Describe(plan), Is.EqualTo("t1 copy(1>2)"));
        });
    }

    [Test]
    public void Build_SkipsACopyWhenItsOtherSideCannotBeImported_AndStopsTheLaneAtItsFence()
    {
        QueueWrite(1, 0);
        QueueCopy(1, 2);
        QueueWrite(1, 16);

        var plan = Plan([Buffer(1)]);

        Assert.That(Describe(plan), Is.EqualTo("w1x1"));
    }

    [Test]
    public void Build_IgnoresACopyWhenTheGraphTouchesNeitherSide()
    {
        QueueWrite(3, 0);
        QueueCopy(1, 2);

        var plan = Plan([Buffer(3)]);

        Assert.That(Describe(plan), Is.EqualTo("w3x1"));
    }

    [Test]
    public void Build_DoesNotPlanACopyBehindAnEarlierCopyItSkipped()
    {
        QueueCopy(2, 3);
        QueueWrite(1, 0);
        QueueCopy(1, 2);

        var plan = Plan([Buffer(1)], _ => true);

        Assert.That(Describe(plan), Is.EqualTo("w1x1"));
    }

    [Test]
    public void Build_PlansACopyBehindAnEarlierCopyWhenTheGraphReachesIt()
    {
        QueueCopy(2, 3);
        QueueCopy(1, 2);

        var plan = Plan([Buffer(2)], _ => true);

        Assert.That(Describe(plan), Is.EqualTo("copy(2>3) copy(1>2)"));
    }

    [Test]
    public void Take_OfAPrunedSuffix_LeavesTheSuffixQueued()
    {
        QueueWrite(1, 0);
        QueueCopy(1, 2);
        QueueWrite(1, 16);
        var plan = Plan([Buffer(1), Buffer(2)]);

        TakeLeadingSteps(plan, Buffer(1), 2);
        TakeLeadingSteps(plan, Buffer(2), 1);

        Assert.Multiple(() =>
        {
            Assert.That(Lanes.Count(Buffer(1)), Is.EqualTo(1));
            Assert.That(Lanes.Count(Buffer(2)), Is.Zero);
            var remaining = new List<(ResourceHandle, PendingResourceAction)>();
            Lanes.TakeOrdered(8, remaining);
            Assert.That(remaining.Select(r => ((PendingBufferWrite)r.Item2).Offset), Is.EqualTo(new[] { 16UL }));
        });
    }

    [Test]
    public void Take_TakesExactlyWhatWasPlanned_WhenMoreIsEnqueuedAfterThePlan()
    {
        QueueWrite(1, 16);
        var plan = Plan([Buffer(1)]);

        QueueWrite(1, 0, 64);
        QueueCopy(1, 2);
        TakeLeadingSteps(plan, Buffer(1), 1);

        Assert.That(Lanes.Count(Buffer(1)), Is.EqualTo(2));
    }

    private void TakeLeadingSteps(UploadPlan plan, ResourceHandle lane, int steps)
    {
        var count = plan.LaneSteps[lane].Take(steps).Sum(index => plan.Steps[index].Count);
        List<PendingResourceAction> taken = [];
        Lanes.Take(lane, count, taken);
        foreach (var action in taken) action.Dispose();
    }
}
