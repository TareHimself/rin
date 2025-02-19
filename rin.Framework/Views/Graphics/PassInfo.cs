using rin.Framework.Views.Graphics.Commands;

namespace rin.Framework.Views.Graphics;

public class PassInfo
{
    public List<FinalDrawCommand> Commands = [];
    public List<UtilityCommand> PostCommands = [];
    public List<UtilityCommand> PreCommands = [];
}