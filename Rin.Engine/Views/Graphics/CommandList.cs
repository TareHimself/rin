using System.Numerics;
using Rin.Engine.Views.Graphics.Commands;

namespace Rin.Engine.Views.Graphics;

public class ClipInfo(uint id, Matrix4x4 transform, Vector2 size)
{
    public readonly uint Id = id;
    public Vector2 Size = size;
    public Matrix4x4 Transform = transform;
}

public struct RawCommand(ICommand cmd, string clipId)
{
    public ICommand Cmd = cmd;
    public readonly string ClipId = clipId;
    public int AbsoluteDepth = 0;
}

public struct PendingCommand(ICommand cmd, uint clipId)
{
    public ICommand Cmd = cmd;
    public readonly uint ClipId = clipId;
}

public class CommandList
{
    private readonly Stack<uint> _clipStack = [];

    //private readonly SortedDictionary<int, List<RawCommand>> _commands = new SortedDictionary<int, List<RawCommand>>(Comparer<int>.Create((a,b) => b.CompareTo(a)));
    private readonly List<RawCommand> _commands = [];
    private string _clipId = "";
    private int _depth;

    //public IEnumerable<RawCommand> Commands => _commands.Keys.SelectMany(commandsKey => _commands[commandsKey].AsReversed());
    public IEnumerable<RawCommand> Commands => _commands;
    public List<ClipInfo> Clips { get; } = [];

    public Dictionary<string, uint[]> UniqueClipStacks { get; } = [];

    public CommandList Add(ICommand command)
    {
        var info = new RawCommand(command,_clipId){
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


    public CommandList PushClip(Matrix4x4 transform, Vector2 size)
    {
        var id = (uint)Clips.Count;
        var clipInfo = new ClipInfo(id, transform, size);
        Clips.Add(clipInfo);
        _clipStack.Push(clipInfo.Id);
        _clipId += id.ToString();
        return this;
    }

    public CommandList PopClip()
    {
        var asStr = _clipStack.Peek().ToString();
        _clipStack.Pop();
        _clipId = _clipId[..^asStr.Length];
        return this;
    }


    public CommandList IncrDepth()
    {
        _depth++;
        return this;
    }

    public CommandList DecrDepth()
    {
        _depth--;
        return this;
    }
}