using Rin.Engine.Views.Graphics.Commands;

namespace Rin.Engine.Views.Graphics;

public enum CommandType
{
    None,
    ClipDraw,
    ClipClear,
    BatchedDraw,
    Custom
}

public class FinalDrawCommand
{
    public IBatch? Batch;
    public StencilClip[] Clips = [];
    public uint Mask = 0x1;
    public CommandType Type = CommandType.None;
}