namespace Rin.Core.Graphics.Graph;

[Flags]
public enum GraphResourceFlags
{
    None = 0,

    /// <summary>
    ///     Writes to the resource are observed outside the graph, so the passes that write it are pruning roots.
    /// </summary>
    Terminal = 1 << 0
}
