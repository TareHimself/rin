using rin.Framework.Graphics.FrameGraph;
using rin.Framework.Views.Graphics.Commands;

namespace rin.Framework.Views.Graphics;

public class PassInfo
{
    public List<UtilityCommand> PreCommands = [];
    public List<FinalDrawCommand> Commands = [];
    public List<UtilityCommand> PostCommands = [];
}