namespace rin.Framework.Views.Layouts;

public interface IMultiSlotLayout : ILayout
{
    public int MaxSlotCount { get; }
    public int SlotCount { get; }
    public bool Add(View child);
    public bool Add(ISlot slot);
    public bool Remove(View view);
    public ISlot? GetSlot(int idx);
    public IEnumerable<ISlot> GetSlots();

    public ISlot? FindSlot(View view);
}