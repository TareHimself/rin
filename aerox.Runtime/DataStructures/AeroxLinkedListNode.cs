namespace aerox.Runtime.DataStructures;

public class AeroxLinkedListNode<T> : IAeroxLinkedListNode<T>
{
    public T Value { get; set; }

    private WeakReference<AeroxLinkedList<T>> _list;

    public AeroxLinkedListNode(T value,AeroxLinkedList<T> list)
    {
        Value = value;
        _list = new WeakReference<AeroxLinkedList<T>>(list);
    }


    public AeroxLinkedListNode<T>? ActualNext;
    public AeroxLinkedListNode<T>? ActualPrevious;
    public IAeroxLinkedListNode<T>? Next => ActualNext;

    public IAeroxLinkedListNode<T>? Previous => ActualPrevious;
    public bool HasNext => Next != null;
    public bool HasPrevious => Previous != null;
    
    

    public bool RemoveNext()
    {
        var list = TryGetLinkedList();
        if (list == null) return false;
        if (!HasNext) return false;
        var temp = ActualNext;
        ActualNext = ActualNext?.ActualNext;
        if(ActualNext != null) ActualNext.ActualPrevious = this;
        if (temp != null) list.NotifyNodeRemoved(temp);
        return true;
    }
    
    public bool RemovePrevious()
    {
        var list = TryGetLinkedList();
        if (list == null) return false;
        if (!HasPrevious) return false;
        var temp = ActualPrevious;
        ActualPrevious = ActualPrevious?.ActualPrevious;
        if(ActualPrevious != null) ActualPrevious.ActualNext = this;
        if (temp != null) list.NotifyNodeRemoved(temp);
        return true;
    }

    public bool Remove()
    {
        if (Next != null) return Next.RemovePrevious();
        if (Previous != null) return Previous.RemoveNext();
        var list = TryGetLinkedList();
        if (list == null) return false;
        list.NotifyNodeRemoved(this);
        return true;
    }

    public AeroxLinkedList<T>? TryGetLinkedList()
    {
        if (_list.TryGetTarget(out var list))
        {
            return list;
        }

        return null;
    }

    public AeroxLinkedList<T> GetLinkedList(bool createIfCollected = false)
    {
        var list = TryGetLinkedList();

        if (list != null) return list;

        if (createIfCollected)
        {
            list = AeroxLinkedList<T>.FromNode(this);
            _list.SetTarget(list);
            return list;
        }

        throw new Exception("List has been garbage collected");
    }
}