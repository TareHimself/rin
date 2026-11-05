namespace Rin.World.Ecs.Scheduling;

/// <summary>
///     One ordered list per phase. Systems run in registration order, nothing is sorted or reordered.
/// </summary>
internal sealed class SystemSchedule
{
    private readonly List<TickSystem> _input = [];
    private readonly List<TickSystem> _gameplay = [];
    private readonly List<TickSystem> _tasks = [];
    private readonly List<TickSystem> _physics = [];
    private readonly List<FrameSystem> _update = [];
    private readonly List<FrameSystem> _late = [];

    public void Add(TickPhase phase, TickSystem system) => TickList(phase).Add(system);

    public void Add(FramePhase phase, FrameSystem system) => FrameList(phase).Add(system);

    public void RunTick(in TickContext context)
    {
        Run(_input, context);
        Run(_gameplay, context);
        Run(_tasks, context);
        Run(_physics, context);
    }

    public void RunFrame(in FrameContext context)
    {
        Run(_update, context);
        Run(_late, context);
    }

    private static void Run(List<TickSystem> systems, in TickContext context)
    {
        for (var i = 0; i < systems.Count; i++) systems[i].Tick(context);
    }

    private static void Run(List<FrameSystem> systems, in FrameContext context)
    {
        for (var i = 0; i < systems.Count; i++) systems[i].Update(context);
    }

    private List<TickSystem> TickList(TickPhase phase) => phase switch
    {
        TickPhase.Input => _input,
        TickPhase.Gameplay => _gameplay,
        TickPhase.Tasks => _tasks,
        TickPhase.Physics => _physics,
        _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null)
    };

    private List<FrameSystem> FrameList(FramePhase phase) => phase switch
    {
        FramePhase.Update => _update,
        FramePhase.Late => _late,
        _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null)
    };
}
