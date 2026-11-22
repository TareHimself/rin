using System.Numerics;

namespace Examples.Editor.Model;

/// <summary>
///     A quaternion edited as pitch, yaw and roll in degrees.
/// </summary>
/// <remarks>
///     The angles last written are kept until the quaternion changes from elsewhere, so dragging one angle never makes
///     the others jump to an equivalent rotation.
/// </remarks>
public sealed class RotationLanes(Func<Quaternion> read, Action<Quaternion> write)
{
    public static readonly string[] Labels = ["P", "Y", "R"];

    private Quaternion _known;
    private Vector3 _degrees;
    private bool _hasKnown;

    public float Get(int axis)
    {
        Refresh();
        return axis switch
        {
            0 => _degrees.X,
            1 => _degrees.Y,
            _ => _degrees.Z
        };
    }

    public void Set(int axis, float degrees)
    {
        Refresh();
        _degrees = axis switch
        {
            0 => _degrees with { X = degrees },
            1 => _degrees with { Y = degrees },
            _ => _degrees with { Z = degrees }
        };
        _known = FieldLanes.FromEuler(_degrees);
        write(_known);
    }

    private void Refresh()
    {
        var rotation = read();
        if (_hasKnown && rotation == _known) return;

        _hasKnown = true;
        _known = rotation;
        _degrees = FieldLanes.ToEuler(rotation);
    }
}
