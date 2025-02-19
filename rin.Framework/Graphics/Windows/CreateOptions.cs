using System.Runtime.InteropServices;

namespace rin.Framework.Graphics.Windows;

public struct CreateOptions()
{
    /// <summary>
    ///     Whether the windowed mode window will be resizable by the user
    /// </summary>
    [MarshalAs(UnmanagedType.U1)] public bool Resizable = true;

    /// <summary>
    ///     Whether the windowed mode window will be initially visible
    /// </summary>
    [MarshalAs(UnmanagedType.U1)] public bool Visible = true;

    /// <summary>
    ///     Whether the windowed mode window will have window decorations such as a border, a close view, etc.
    ///     An undecorated window will not be resizable by the user but will still allow the user to generate close events on
    ///     some platforms.
    /// </summary>
    [MarshalAs(UnmanagedType.U1)] public bool Decorated = true;

    /// <summary>
    ///     Specifies whether the windowed mode window will be given input focus when created.
    /// </summary>
    [MarshalAs(UnmanagedType.U1)] public bool Focused = true;

    /// <summary>
    ///     Whether the windowed mode window will be floating above other regular windows, also called topmost or
    ///     always-on-top.
    ///     This is intended primarily for debugging purposes and cannot be used to implement proper full screen windows.
    /// </summary>
    [MarshalAs(UnmanagedType.U1)] public bool Floating = false;

    /// <summary>
    ///     Whether the windowed mode window will be maximized when created.
    /// </summary>
    [MarshalAs(UnmanagedType.U1)] public bool Maximized = false;

    /// <summary>
    ///     Whether the cursor should be centered over newly created full screen windows.
    /// </summary>
    [MarshalAs(UnmanagedType.U1)] public bool CursorCentered = false;

    /// <summary>
    /// </summary>
    [MarshalAs(UnmanagedType.U1)] public bool Transparent = false;
}