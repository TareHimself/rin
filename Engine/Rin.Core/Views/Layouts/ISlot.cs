namespace Rin.Core.Views.Layouts;

public interface ISlot
{
    public IView Child { get; }
    public void OnAddedToLayout(ILayout layout);
    public void OnRemovedFromLayout(ILayout layout);
}