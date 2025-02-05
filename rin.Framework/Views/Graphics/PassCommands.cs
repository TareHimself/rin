using System.Numerics;
using rin.Framework.Core.Math;
using rin.Framework.Graphics.FrameGraph;
using rin.Framework.Views.Graphics.Commands;

namespace rin.Framework.Views.Graphics;



public class ClipInfo(uint id, Mat3 transform, Vector2 size)
{
    public readonly uint Id = id;
    public Mat3 Transform = transform;
    public Vector2 Size = size;
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

public class PassCommands
{
    private readonly Stack<uint> _clipStack = [];
    private string _clipId = "";
    private int _depth = 0;
    //private readonly SortedDictionary<int, List<RawCommand>> _commands = new SortedDictionary<int, List<RawCommand>>(Comparer<int>.Create((a,b) => b.CompareTo(a)));
    private readonly List<RawCommand> _commands = [];
    public PassCommands Add(Command command)
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
    
    
    public PassCommands PushClip(Mat3 transform, Vector2 size)
    {
        var id = (uint)Clips.Count;
        var clipInfo = new ClipInfo(id, transform, size);
        Clips.Add(clipInfo);
        _clipStack.Push(clipInfo.Id);
        _clipId += id.ToString();
        return this;
    }

    public PassCommands PopClip()
    {
        var asStr = _clipStack.Peek().ToString();
        _clipStack.Pop();
        _clipId = _clipId[..^asStr.Length];
        return this;
    }


    public PassCommands IncrDepth()
    {
        _depth++;
        return this;
    }
    
    public PassCommands DecrDepth()
    {
        _depth--;
        return this;
    }

    //public IEnumerable<RawCommand> Commands => _commands.Keys.SelectMany(commandsKey => _commands[commandsKey].AsReversed());
    public IEnumerable<RawCommand> Commands => _commands;
    public List<ClipInfo> Clips { get; } = [];

    public Dictionary<string, uint[]> UniqueClipStacks { get; } = [];
    
}