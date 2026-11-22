using Examples.Editor.Model;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views.Content;
using Rin.Core.Views.Events;
using Rin.World;

namespace Examples.Editor.Views;

/// <summary>
///     The selected entity's name as text you click to edit, kept in <see cref="EntityNames" /> on Enter or when focus
///     leaves. Clearing it falls back to the entity's id and components.
/// </summary>
public sealed class NameInputView : TextInputBoxView
{
    private readonly World _world;
    private readonly EntityNames _names;
    private readonly Entity _entity;

    public NameInputView(World world, EntityNames names, Entity entity)
    {
        _world = world;
        _names = names;
        _entity = entity;
        FontSize = 16f;
        ForegroundColor = EditorTheme.Text;
        Content = names.LabelOf(world, entity);
    }

    public override bool IsFocusable => true;

    public override void OnFocus()
    {
        base.OnFocus();
        Content = _names.NameOf(_entity) ?? string.Empty;
    }

    public override void OnKeyboard(KeyboardSurfaceEvent e)
    {
        if (e is { Key: InputKey.Enter, State: InputState.Pressed }) e.Surface.ClearFocus();
        else base.OnKeyboard(e);
    }

    public override void OnCharacter(CharacterSurfaceEvent e)
    {
        if (e.Character is not ('\n' or '\r')) base.OnCharacter(e);
    }

    public override void OnFocusLost()
    {
        base.OnFocusLost();
        _names.Set(_entity, Content);
        Content = _names.LabelOf(_world, _entity);
    }
}
