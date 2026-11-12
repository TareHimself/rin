using Rin.Core.Views.Layouts;

namespace Rin.Core.Views.Composite;

public class SimpleSlot : ISlot
{
    public required IView Child { get; set; }

    public void OnAddedToLayout(ILayout layout)
    {
    }

    public void OnRemovedFromLayout(ILayout layout)
    {
    }
}
