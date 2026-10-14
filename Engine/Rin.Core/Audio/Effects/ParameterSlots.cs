using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Rin.Core.Audio.Effects;

/// <summary>
///     Triple buffer between the game thread, which sets an effect's parameters, and the audio thread, which reads
///     them once per block. The writer owns one slot, the reader owns one slot, and the third is handed over with a
///     single atomic exchange, so neither side blocks and the reader never sees a partially written struct.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ParameterSlots<T> where T : unmanaged
{
    private const int Dirty = 4;

    private int _middle;
    private int _back;
    private int _front;
    private Slots _slots;

    public ParameterSlots(in T initial)
    {
        _slots[0] = initial;
        _slots[1] = initial;
        _slots[2] = initial;
        _front = 0;
        _middle = 1;
        _back = 2;
    }

    public static unsafe void Write(void* slots, in T value)
    {
        ref var self = ref Unsafe.AsRef<ParameterSlots<T>>(slots);
        self._slots[self._back] = value;
        self._back = Interlocked.Exchange(ref self._middle, self._back | Dirty) & ~Dirty;
    }

    public static unsafe ref readonly T Read(void* slots)
    {
        ref var self = ref Unsafe.AsRef<ParameterSlots<T>>(slots);
        if ((Volatile.Read(ref self._middle) & Dirty) != 0)
            self._front = Interlocked.Exchange(ref self._middle, self._front) & ~Dirty;
        return ref self._slots[self._front];
    }

    [InlineArray(3)]
    private struct Slots
    {
        private T _element;
    }
}
