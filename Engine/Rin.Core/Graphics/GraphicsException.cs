namespace Rin.Core.Graphics;

/// <summary>
///     The base for graphics failures, so callers can handle them without knowing the backend.
/// </summary>
public class GraphicsException(string? message) : Exception(message);
