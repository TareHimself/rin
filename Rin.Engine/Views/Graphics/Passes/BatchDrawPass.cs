// using JetBrains.Annotations;
// using Rin.Engine.Graphics;
// using Rin.Engine.Graphics.FrameGraph;
// using Rin.Engine.Views.Graphics.Commands;
//
// namespace Rin.Engine.Views.Graphics.Passes;
//
// public sealed class BatchDrawPass : IViewsPass
// {
//     private readonly List<IBatch> _batches = [];
//     private readonly List<uint> _masks = [];
//     private readonly ulong[] _batchSizes;
//     private readonly Extent2D _extent;
//
//     private readonly List<Pair<ulong, ulong>> _offsets = [];
//
//     // private readonly PassInfo _passInfo;
//     private uint _bufferId;
//     private FrameStats _stats;
//
//     public BatchDrawPass(PassCreateInfo createInfo)
//     {
//         Context = createInfo.Context;
//         var commands = createInfo.Commands.Cast<IBatchedCommand>();
//         _extent = Context.Extent;
//         
//         foreach (var cmd in commands)
//         {
//             var canBeBatchedWithPrevious = _batches.LastOrDefault() is { } batch && batch.GetBatcher() == cmd.GetBatcher() &&
//                                            _masks.Last() == cmd.StencilMask;
//             if (canBeBatchedWithPrevious)
//             {
//                 _batches.Last().AddFromCommand(cmd);
//             }
//             else
//             {
//                 var newBatch = cmd.GetBatcher().NewBatch();
//                 newBatch.AddFromCommand(cmd);
//                 _batches.Add(newBatch);
//                 _masks.Add(cmd.StencilMask);
//             }
//         }
//         _batchSizes = _batches.Select(c => c.GetMemoryNeeded().Aggregate((a, b) => a + b)).ToArray();
//     }
//
//     [PublicAPI] public uint MainImageId { get; set; }
//
//     [PublicAPI] public uint CopyImageId { get; set; }
//     [PublicAPI] public uint StencilImageId { get; set; }
//
//     public string Name => "View Pass";
//
//     public SurfacePassContext Context { get; set; }
//
//     [PublicAPI] public bool IsTerminal { get; set; }
//
//     public void Configure(IGraphConfig config)
//     {
//         MainImageId = config.WriteImage(Context.MainImageId, ImageLayout.ColorAttachment);
//         StencilImageId = config.ReadImage(Context.StencilImageId, ImageLayout.StencilAttachment);
//
//         var memoryNeeded = _batchSizes.Aggregate<ulong, ulong>(0, (t, c) => t + c);
//
//         if (memoryNeeded > 0) _bufferId = config.CreateBuffer(memoryNeeded, GraphBufferUsage.HostThenGraphics);
//     }
//
//     public void Execute(ICompiledGraph graph, IExecutionContext ctx)
//     {
//         var drawImage = graph.GetImage(MainImageId);
//         var stencilImage = graph.GetImage(StencilImageId);
//         var buffer = graph.GetBufferOrNull(_bufferId);
//
//         var viewFrame = new ViewsFrame(Context, ctx);
//
//         // foreach (var command in _passInfo.PreCommands) command.Execute(viewFrame);
//
//         ctx.BeginRendering(_extent, [drawImage], stencilAttachment: stencilImage)
//             .DisableFaceCulling()
//             .StencilCompareOnly();
//
//         var compareMask = uint.MaxValue;
//
//         ulong offset = 0;
//         for (var i = 0; i < _batches.Count; i++)
//         {
//             var batch = _batches[i];
//             var mask = _masks[i];
//             if (mask != compareMask)
//             {
//                 compareMask = mask;
//                 ctx.SetStencilCompareMask(compareMask);
//             }
//
//             var batcher = batch.GetBatcher();
//             var bufferSize = _batchSizes[i];
//             batcher.Draw(viewFrame, batch, buffer.GetView(offset, bufferSize));
//             offset += bufferSize;
//         }
//
//         ctx.EndRendering();
//     }
//
//     public static IViewsPass Create(PassCreateInfo info)
//     {
//         return new BatchDrawPass(info);
//     }
//
//     public uint Id { get; set; }
// }