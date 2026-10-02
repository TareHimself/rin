using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Views.Graphics.Commands;

namespace Rin.Core.Views.Graphics;

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

public readonly struct ClipStackKey(uint[] ids) : IEquatable<ClipStackKey>
{
    public static readonly ClipStackKey Empty = new([]);

    public readonly uint[] Ids = ids;

    public bool Equals(ClipStackKey other) => Ids.AsSpan().SequenceEqual(other.Ids);
    public override bool Equals(object? obj) => obj is ClipStackKey other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var id in Ids) hash.Add(id);
        return hash.ToHashCode();
    }

    public static bool operator ==(ClipStackKey left, ClipStackKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ClipStackKey left, ClipStackKey right)
    {
        return !(left == right);
    }
}

public class CommandList
{
    private readonly List<ClipStackKey> _clipKeyStack = [ClipStackKey.Empty];

    [PublicAPI] public readonly List<ClipStackKey> ClipIds = [];

    //private readonly SortedDictionary<int, List<RawCommand>> _commands = new SortedDictionary<int, List<RawCommand>>(Comparer<int>.Create((a,b) => b.CompareTo(a)));
    [PublicAPI] public readonly List<ICommand> Commands = [];

    private uint[]? _registeredClipIds;
    private int _depth;
    public List<ClipInfo> Clips { get; } = [];
    public required Vector2 SurfaceSize { get; set; }
    public Dictionary<ClipStackKey, uint[]> UniqueClipStacks { get; } = [];

    public CommandList Add(ICommand command)
    {
        var clipKey = _clipKeyStack[^1];
        Commands.Add(command);
        ClipIds.Add(clipKey);

        if (!ReferenceEquals(_registeredClipIds, clipKey.Ids))
        {
            UniqueClipStacks.TryAdd(clipKey, clipKey.Ids);
            _registeredClipIds = clipKey.Ids;
        }

        return this;
    }


    public CommandList PushClip(Matrix4x4 transform, Vector2 size)
    {
        var id = (uint)Clips.Count;
        var clipInfo = new ClipInfo(id, transform, size);
        Clips.Add(clipInfo);

        var parentIds = _clipKeyStack[^1].Ids;
        var ids = new uint[parentIds.Length + 1];
        parentIds.CopyTo(ids, 0);
        ids[^1] = clipInfo.Id;
        _clipKeyStack.Add(new ClipStackKey(ids));
        return this;
    }

    public CommandList PopClip()
    {
        _clipKeyStack.RemoveAt(_clipKeyStack.Count - 1);
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