namespace rin.Framework.Views.Graphics;

public class FrameStats
{
    public int InitialCommandCount;
    public int FinalCommandCount;
    public int BatchedDrawCommandCount;
    public int NonBatchedDrawCommandCount;
    public int CustomCommandCount;
    public int StencilWriteCount;
    public int StencilClearCount;
    public ulong MemoryAllocatedBytes;
}

// {frame.BatchedDraws} Batches
// {frame.NonBatchedDraws} Draws
// {frame.StencilDraws} Stencil Draws
// {frame.NonDraws} Non Draws
// {(int)view._averageFps} FPS
// {(renderer.LastFrameTime * 1000).Round(2)}ms