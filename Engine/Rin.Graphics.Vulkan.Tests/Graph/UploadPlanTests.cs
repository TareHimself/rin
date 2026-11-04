using Rin.Core.Graphics;
using Rin.Core.Shared;
using Rin.Graphics.Vulkan.Graph;

namespace Rin.Graphics.Vulkan.Tests.Graph;

public class UploadPlanTests
{
    private UploadLanes _lanes = null!;

    [SetUp]
    public void CreateLanes()
    {
        _lanes = new UploadLanes();
    }

    [TearDown]
    public void DisposeActions()
    {
        List<PendingResourceAction> remaining = [];
        _lanes.TakeAll(remaining);
        foreach (var action in remaining) action.Dispose();
    }

    private static ResourceHandle Buffer(uint id) => new(ResourceType.Buffer, id);
    private static ResourceHandle Texture(uint id) => new(ResourceType.Texture, id);

    private PendingBufferWrite Write(uint id, ulong offset, int bytes = 16)
    {
        var write = new PendingBufferWrite(PooledMemory<byte>.CopyFrom(new byte[bytes]), offset);
        _lanes.EnqueueBuffer(Buffer(id), write);
        return write;
    }

    private SharedWrite Copy(uint source, uint destination)
    {
        var shared = new SharedWrite(new BufferCopy(Buffer(source), Buffer(destination), 0, 0, 16));
        _lanes.EnqueueShared(shared);
        return shared;
    }

    private UploadPlan Plan(IEnumerable<ResourceHandle> touched, Func<ResourceHandle, bool>? import = null)
    {
        import ??= _ => false;
        return UploadPlan.Build(_lanes, touched, (_, handle) => import(handle));
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
        Write(1, 0);
        Write(2, 0);
        Copy(1, 2);
        Write(1, 16);
        Write(2, 16);

        var plan = Plan([Buffer(1), Buffer(2)]);

        Assert.That(Describe(plan), Is.EqualTo("w1x1 w2x1 copy(1>2) w1x1 w2x1"));
    }

    [Test]
    public void Build_ChainsEachStepToThePreviousStepInEachOfItsLanes()
    {
        Write(1, 0);
        Write(2, 0);
        Copy(1, 2);
        Write(1, 16);

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
        _lanes.EnqueueTexture(Texture(1), new PendingTextureWrite(PooledMemory<byte>.CopyFrom(new byte[16]), default,
            new Extent2D(2, 2), new Extent2D(4, 4)));
        _lanes.EnqueueTexture(Texture(1), new PendingTextureWrite(PooledMemory<byte>.CopyFrom(new byte[16]),
            new Offset2D(2, 2), new Extent2D(2, 2), new Extent2D(4, 4)));
        Write(2, 0);
        Write(2, 32);

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
        Write(1, 0);
        Copy(1, 1);
        Write(1, 64);

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
        Write(2, 0);
        Copy(1, 2);
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
        _lanes.EnqueueTexture(Texture(1), new PendingTextureWrite(PooledMemory<byte>.CopyFrom(new byte[16]), default,
            new Extent2D(2, 2), new Extent2D(4, 4)));
        _lanes.EnqueueShared(new SharedWrite(new TextureToBufferCopy(Texture(1), Buffer(2), default,
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
        Write(1, 0);
        Copy(1, 2);
        Write(1, 16);

        var plan = Plan([Buffer(1)]);

        Assert.That(Describe(plan), Is.EqualTo("w1x1"));
    }

    [Test]
    public void Build_IgnoresACopyWhenTheGraphTouchesNeitherSide()
    {
        Write(3, 0);
        Copy(1, 2);

        var plan = Plan([Buffer(3)]);

        Assert.That(Describe(plan), Is.EqualTo("w3x1"));
    }

    [Test]
    public void Build_DoesNotPlanACopyBehindAnEarlierCopyItSkipped()
    {
        Copy(2, 3);
        Write(1, 0);
        Copy(1, 2);

        var plan = Plan([Buffer(1)], _ => true);

        Assert.That(Describe(plan), Is.EqualTo("w1x1"));
    }

    [Test]
    public void Build_PlansACopyBehindAnEarlierCopyWhenTheGraphReachesIt()
    {
        Copy(2, 3);
        Copy(1, 2);

        var plan = Plan([Buffer(2)], _ => true);

        Assert.That(Describe(plan), Is.EqualTo("copy(2>3) copy(1>2)"));
    }

    [Test]
    public void Take_OfAPrunedSuffix_LeavesTheSuffixQueued()
    {
        Write(1, 0);
        Copy(1, 2);
        Write(1, 16);
        var plan = Plan([Buffer(1), Buffer(2)]);

        TakeLeadingSteps(plan, Buffer(1), 2);
        TakeLeadingSteps(plan, Buffer(2), 1);

        Assert.Multiple(() =>
        {
            Assert.That(_lanes.Count(Buffer(1)), Is.EqualTo(1));
            Assert.That(_lanes.Count(Buffer(2)), Is.Zero);
            var remaining = new List<(ResourceHandle, PendingResourceAction)>();
            _lanes.TakeOrdered(8, remaining);
            Assert.That(remaining.Select(r => ((PendingBufferWrite)r.Item2).Offset), Is.EqualTo(new[] { 16UL }));
        });
    }

    [Test]
    public void Take_AfterTheWriteAfterReadCase_KeepsTheCopyBeforeTheLaterWriteOfItsSource()
    {
        Write(1, 0);
        Copy(1, 2);
        Write(1, 16);
        var plan = Plan([Buffer(1), Buffer(2)]);
        var copyStep = plan.Steps.Single(s => s.Kind == UploadStepKind.Copy);
        var laterWrite = plan.Steps.Last(s => s.Handle.Equals(Buffer(1)));

        Assert.Multiple(() =>
        {
            Assert.That(plan.Steps[laterWrite.PreviousInLane], Is.SameAs(copyStep));
            Assert.That(plan.Steps.IndexOf(copyStep), Is.LessThan(plan.Steps.IndexOf(laterWrite)));
        });
    }

    [Test]
    public void Take_TakesExactlyWhatWasPlanned_WhenMoreIsEnqueuedAfterThePlan()
    {
        Write(1, 16);
        var plan = Plan([Buffer(1)]);

        Write(1, 0, 64);
        Copy(1, 2);
        TakeLeadingSteps(plan, Buffer(1), 1);

        Assert.That(_lanes.Count(Buffer(1)), Is.EqualTo(2));
    }

    private void TakeLeadingSteps(UploadPlan plan, ResourceHandle lane, int steps)
    {
        var count = plan.LaneSteps[lane].Take(steps).Sum(index => plan.Steps[index].Count);
        List<PendingResourceAction> taken = [];
        _lanes.Take(lane, count, taken);
        foreach (var action in taken) action.Dispose();
    }
}
