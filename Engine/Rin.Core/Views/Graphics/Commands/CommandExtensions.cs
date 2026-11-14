namespace Rin.Core.Views.Graphics.Commands;

public static class CommandExtensions
{
    /// <summary>
    ///     Copies the commands a handler is initialized with, which are only valid during the call.
    /// </summary>
    public static T[] CastToArray<T>(this ReadOnlySpan<ICommand> commands) where T : ICommand
    {
        var result = new T[commands.Length];
        for (var i = 0; i < commands.Length; i++) result[i] = (T)commands[i];
        return result;
    }
}
