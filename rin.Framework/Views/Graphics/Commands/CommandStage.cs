namespace rin.Framework.Views.Graphics.Commands;

/// <summary>
///     Controls when a custom view draw command will be executed
/// </summary>
public enum CommandStage
{
    /// <summary>
    ///     Execute before the main pass
    /// </summary>
    Before,

    /// <summary>
    ///     Execute after the main pass
    /// </summary>
    After
}