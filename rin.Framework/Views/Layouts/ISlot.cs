namespace rin.Framework.Views.Layouts;

public interface ISlot
{
    public View Child { get; }
    public void SetLayout(ILayout layout);
}