using System.Numerics;
using JetBrains.Annotations;
using Rin.Framework.Animation;
using Rin.Framework.Graphics;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Enums;
using Rin.Framework.Views.Events;
using Rin.Framework.Views.Graphics;

namespace Rin.Framework.Views;

public interface IView : IDisposable, IAnimatable, IUpdatable
{
    /// <summary>
    ///     The offset of this view in parent space
    /// </summary>
    public Vector2 Offset { get; set; }
    
    /// <summary>
    ///     The pivot used to render this view. Affects <see cref="Angle" /> and <see cref="Scale" />.
    /// </summary>
    public Vector2 Pivot { get; set; }

    /// <summary>
    ///     The translation of this view in parent space
    /// </summary>
    public Vector2 Translate { get; set; }

    /// <summary>
    /// The scale of this view in parent space
    /// </summary>
    public Vector2 Scale { get; set; }

    /// <summary>
    ///     The Padding For This View (Left, Top, Right, Bottom)
    /// </summary>
    public Padding Padding { get; set; }

    /// <summary>
    ///     The angle this view is to be rendered at in parent space
    /// </summary>
    public float Angle { get; set; }

    /// <summary>
    ///     The visibility of this view
    /// </summary>
    public Visibility Visibility { get; set; }


    /// <summary>
    ///     Should this view be hit tested
    /// </summary>
    public bool IsSelfHitTestable => Visibility is Visibility.Visible or Visibility.VisibleNoHitTestSelf;

    /// <summary>
    ///     Should this view's children be hit tested
    /// </summary>
    public bool IsChildrenHitTestable => Visibility is Visibility.Visible or Visibility.VisibleNoHitTestChildren;

    /// <summary>
    /// Should this view or its children be hit tested
    /// </summary>
    public bool IsHitTestable => IsSelfHitTestable || IsChildrenHitTestable;

    /// <summary>
    ///     The current hovered state of this view
    /// </summary>
    public bool IsHovered { get; }

    public bool IsVisible => Visibility is not (Visibility.Hidden or Visibility.Collapsed);

    /// <summary>
    ///     The surface this view is currently on
    /// </summary>
    public ISurface? Surface { get; }

    /// <summary>
    ///     The parent of this view
    /// </summary>
    public ICompositeView? Parent { get; }
    public Matrix4x4 GetLocalPaddingTransform();
    
    // Get the local transformation applied to content (i.e. padding, scroll, etc.)
    public Matrix4x4 GetLocalContentTransform();
    /// <summary>
    ///     Check if this view is focused by its current surface
    /// </summary>
    public bool IsFocused => Surface?.FocusedView == this;

    public bool IsFocusable { get; }

    /// <summary>
    ///     Compute and set this views size based on the space available
    /// </summary>
    /// <param name="availableSpace"></param>
    /// <param name="fill">
    ///     If true will set <see cref="Size" /> to <see cref="availableSpace" /> irrespective of the space
    ///     taken by content
    /// </param>
    /// <returns></returns>
    public Vector2 ComputeSize(in Vector2 availableSpace, bool fill = false);

    /// <summary>
    ///     Computes the relative/local transformation matrix for this view
    /// </summary>
    /// <returns></returns>
    public Matrix4x4 GetLocalTransform();

    public Matrix4x4 GetLocalTransformWithPadding();

    public Matrix4x4 ComputeAbsoluteContentTransform();
    
    public Matrix4x4 ComputeAbsoluteTransform();

    public void SetParent(ICompositeView? view);

    public void SetSurface(ISurface? surface);

    public void NotifyAddedToSurface(ISurface surface);

    public void NotifyRemovedFromSurface(ISurface surface);

    public void HandleEvent(ISurfaceEvent e, in Matrix4x4 absoluteTransform);

    public void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform);
    public void OnCursorUp(CursorUpSurfaceEvent e);

    public void OnCursorMove(CursorMoveSurfaceEvent e, in Matrix4x4 transform);

    public void NotifyCursorLeave();

    public void OnCharacter(CharacterSurfaceEvent e);

    public void OnKeyboard(KeyboardSurfaceEvent e);

    public void OnFocus();

    public void OnFocusLost();

    public Vector2 GetSize();

    [PublicAPI]
    public Vector2 GetContentSize();

    [PublicAPI]
    public Vector2 GetDesiredSize();

    [PublicAPI]
    public Vector2 GetDesiredContentSize();

    public Vector2 ComputeDesiredSize();
    
    public Vector2 ComputeDesiredContentSize();

    /// <summary>
    ///     Collect draw commands from this view
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="clip"></param>
    /// <param name="commands"></param>
    public void Collect(in Matrix4x4 transform, in Rect2D clip, CommandList commands);

    public bool TryUpdateDesiredSize();

    public void Invalidate(InvalidationType type);

    // ReSharper disable once InconsistentNaming
    public Rect2D ComputeAABB(in Matrix4x4 transform);
    public bool PointWithin(in Matrix4x4 transform, in Vector2 point, bool useInverse = false);
}