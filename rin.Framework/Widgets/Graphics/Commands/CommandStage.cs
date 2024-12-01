namespace rin.Framework.Widgets.Graphics.Commands;

/// <summary>
/// Controls when a custom widget draw command will be executed
/// </summary>
public enum CommandStage
{
    /// <summary>
    /// Maintain Execution Position
    /// </summary>
    Maintain,
    /// <summary>
    /// Execute as early as possible
    /// </summary>
    Early,
    /// <summary>
    /// Execute as late as possible
    /// </summary>
    Late
}