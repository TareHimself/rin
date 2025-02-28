using Rin.Engine.Views.Graphics.Commands;

namespace Rin.Engine.Views.Graphics;

public class PassInfo
{
    public List<FinalDrawCommand> Commands = [];
    public List<UtilityCommand> PostCommands = [];
    public List<UtilityCommand> PreCommands = [];
}