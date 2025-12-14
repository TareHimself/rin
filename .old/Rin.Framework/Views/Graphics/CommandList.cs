using System.Numerics;
using JetBrains.Annotations;
using Rin.Framework.Views.Graphics.Commands;

namespace Rin.Framework.Views.Graphics;

public class ClipInfo(uint id, Matrix4x4 transform, Vector2 size)
{
    public readonly uint Id = id;
    public Vector2 Size = size;
    public Matrix4x4 Transform = transform;
}

public struct PendingCommand(ICommand cmd, uint clipId)
{
    public ICommand Cmd = cmd;
    public readonly uint ClipId = clipId;
}

public class CommandList
{
    private readonly Stack<uint> _clipStack = [];

    [PublicAPI] public readonly List<string> ClipIds = [];

    //private readonly SortedDictionary<int, List<RawCommand>> _commands = new SortedDictionary<int, List<RawCommand>>(Comparer<int>.Create((a,b) => b.CompareTo(a)));
    [PublicAPI] public readonly List<ICommand> Commands = [];

    private string _clipId = "";
    private int _depth;
    public List<ClipInfo> Clips { get; } = [];
    public required Vector2 SurfaceSize { get; set; }
    public Dictionary<string, uint[]> UniqueClipStacks { get; } = [];

    public CommandList Add(ICommand command)
    {
        Commands.Add(command);
        ClipIds.Add(_clipId);
        UniqueClipStacks.TryAdd(_clipId, _clipStack.ToArray());

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