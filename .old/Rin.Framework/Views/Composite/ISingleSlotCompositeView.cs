using System.Numerics;
using JetBrains.Annotations;
using Rin.Framework.Views.Enums;
using Rin.Framework.Views.Layouts;

namespace Rin.Framework.Views.Composite;

public interface ISingleSlotCompositeView : ICompositeView
{
    /// <summary>
    /// Adds the View to this container
    /// </summary>
    public IView? Child
    {
        init;
    }

    [PublicAPI]
    public void SetChild(IView? child);

    [PublicAPI]
    public IView? GetChild();

    [PublicAPI]
    public ISlot? GetSlot();
}