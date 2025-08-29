namespace Rin.Framework.Views.Layouts;

public interface IMultiSlotLayout : ILayout
{
    public int MaxSlotCount { get; }
    public int SlotCount { get; }
    public bool Add(IView child);
    public bool Add(ISlot slot);
    public bool Remove(IView view);
    public ISlot? GetSlot(int idx);
    public IEnumerable<ISlot> GetSlots();

    public ISlot? FindSlot(IView view);
}