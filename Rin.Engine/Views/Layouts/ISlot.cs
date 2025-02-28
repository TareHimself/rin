namespace Rin.Engine.Views.Layouts;

public interface ISlot
{
    public View Child { get; }
    public void OnAddedToLayout(ILayout layout);
    public void OnRemovedFromLayout(ILayout layout);
}