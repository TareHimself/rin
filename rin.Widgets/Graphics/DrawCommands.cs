using rin.Core.Extensions;
using rin.Core.Math;
using rin.Widgets.Graphics;
using rin.Widgets.Graphics.Commands;

namespace rin.Widgets;



public class ClipInfo(uint id, Matrix3 transform, Vector2<float> size)
{
    public readonly uint Id = id;
    public Matrix3 Transform = transform;
    public Vector2<float> Size = size;
}

public struct RawCommand(Command drawCommand, string clipId)
{
    public Command DrawCommand = drawCommand;
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
    public DrawCommands Add(Command command)
    {
        var info = new RawCommand(command, _clipId)
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
    
    public DrawCommands PushClip(TransformInfo info, Widget widget)
    {
        return PushClip(info.Transform, widget.GetContentSize());
    }
    
    public DrawCommands PushClip(Matrix3 transform, Vector2<float> size)
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
    public List<ClipInfo> Clips { get; } = [];

    public Dictionary<string, uint[]> UniqueClipStacks { get; } = [];
    
}