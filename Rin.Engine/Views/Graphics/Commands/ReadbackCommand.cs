using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Descriptors;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Views.Graphics.Passes;
using Rin.Engine.Views.Graphics.Passes.Blur;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Views.Graphics.Commands;



public class ReadBackCommand() : TCommand<ReadBackPass>
{
}