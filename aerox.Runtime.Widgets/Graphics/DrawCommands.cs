using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Graphics;
using aerox.Runtime.Widgets.Graphics.Commands;

namespace aerox.Runtime.Widgets;



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

    public DrawCommands Add(Command command)
    {
        var info = new RawCommand(command, _clipId);
        
        Commands.Add(info);

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

    public List<RawCommand> Commands { get; } = [];

    public List<ClipInfo> Clips { get; } = [];

    public Dictionary<string, uint[]> UniqueClipStacks { get; } = [];
    
}