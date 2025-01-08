using rin.Framework.Core.Math;
using rin.Framework.Graphics.FrameGraph;
using rin.Framework.Views.Graphics.Commands;

namespace rin.Framework.Views.Graphics;



public class ClipInfo(uint id, Mat3 transform, Vec2<float> size)
{
    public readonly uint Id = id;
    public Mat3 Transform = transform;
    public Vec2<float> Size = size;
}

public struct RawCommand(Command drawCommand, string clipId)
{
    public Command Command = drawCommand;
    public readonly string ClipId = clipId;
    public int AbsoluteDepth = 0;
}

public struct PendingCommand(Command drawCommand, uint clipId)
{
    public Command DrawCommand = drawCommand;
    public readonly uint ClipId = clipId;
}

public class DrawCommands
{
    private readonly Stack<uint> _clipStack = [];
    private string _clipId = "";
    private int _depth = 0;
    //private readonly SortedDictionary<int, List<RawCommand>> _commands = new SortedDictionary<int, List<RawCommand>>(Comparer<int>.Create((a,b) => b.CompareTo(a)));
    private readonly List<RawCommand> _commands = [];
    private readonly List<Action<IGraphBuilder>> _builders = [];
    private readonly List<Action<IPass,IGraphConfig>> _configs = [];

    public DrawCommands AddConfigure(Action<IPass,IGraphConfig> config)
    {
        _configs.Add(config);
        return this;
    }

    public DrawCommands AddBuilder(Action<IGraphBuilder> builder)
    {
        _builders.Add(builder);
        return this;
    }
    
    public DrawCommands Add(Command command)
    {
        var info = new RawCommand(command, command is CustomCommand asCustom ? !asCustom.WillDraw ? "" : _clipId : _clipId)
        {
            AbsoluteDepth = _depth
        };

        // if (!_commands.TryGetValue(_depth, out var value))
        // {
        //     _commands.Add(_depth,[info]);
        // }
        // else
        // {
        //     value.Add(info);
        // }
        _commands.Add(info);

        UniqueClipStacks.TryAdd(info.ClipId, _clipStack.ToArray());

        return this;
    }
    
    
    public DrawCommands PushClip(Mat3 transform, Vec2<float> size)
    {
        var id = (uint)Clips.Count;
        var clipInfo = new ClipInfo(id, transform, size);
        Clips.Add(clipInfo);
        _clipStack.Push(clipInfo.Id);
        _clipId += id.ToString();
        return this;
    }

    public DrawCommands PopClip()
    {
        var asStr = _clipStack.Peek().ToString();
        _clipStack.Pop();
        _clipId = _clipId[..^asStr.Length];
        return this;
    }


    public DrawCommands IncrDepth()
    {
        _depth++;
        return this;
    }
    
    public DrawCommands DecrDepth()
    {
        _depth--;
        return this;
    }

    //public IEnumerable<RawCommand> Commands => _commands.Keys.SelectMany(commandsKey => _commands[commandsKey].AsReversed());
    public IEnumerable<RawCommand> Commands => _commands;
    public IEnumerable<Action<IPass,IGraphConfig>> Configs => _configs;
    public IEnumerable<Action<IGraphBuilder>> Builders => _builders;
    public List<ClipInfo> Clips { get; } = [];

    public Dictionary<string, uint[]> UniqueClipStacks { get; } = [];
    
}