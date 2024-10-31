// using Vulkan = TerraFX.Interop.Vulkan.Vulkan;
//
// namespace rin.Widgets.Graphics.Commands;
// using TerraFX.Interop.Vulkan;
// using static Vulkan;
//
// /// <summary>
// /// Base command for all draws that will use the main pass
// /// </summary>
// public abstract class Command : GraphicsCommand
// {
//     public override void Run(WidgetFrame frame)
//     {
//         if(!frame.IsMainPassActive) frame.Surface.BeginMainPass(frame);
//         Draw(frame);
//     }
//
//     protected abstract void Draw(WidgetFrame frame);
//     
//     public void Quad(WidgetFrame frame)
//     {
//         vkCmdDraw(frame.Raw.GetCommandBuffer(), 6, 1, 0, 0);
//     }
// }