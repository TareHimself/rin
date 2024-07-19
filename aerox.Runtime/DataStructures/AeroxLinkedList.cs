using System.Collections;

namespace aerox.Runtime.DataStructures;

public class AeroxLinkedList<T> : IEnumerable<IAeroxLinkedListNode<T>>
{

    public static AeroxLinkedList<T> FromNode(AeroxLinkedListNode<T> node)
    {
        var list = new AeroxLinkedList<T>();
        var current = node;
        while (node.ActualPrevious != null)
        {
            current = node.ActualPrevious;
        }

        while (current != null)
        {
            list.InsertBack(current.Value);
            current = current.ActualNext;
        }
        
        return list;
    }
    
    public AeroxLinkedList(params T[] data)
    {
        foreach (var d in data)
        {
            InsertBack(d);
        }
    }
    


    private AeroxLinkedListNode<T>? _head = null;
    private AeroxLinkedListNode<T>? _tail = null;

    public IAeroxLinkedListNode<T>? Front => _head;
    public IAeroxLinkedListNode<T>? Back => _tail;
    
    public int Count { get; private set; }

    public IAeroxLinkedListNode<T> InsertFront(T data)
    {
        var newNode = new AeroxLinkedListNode<T>(data,this);
        if (_head != null) _head.ActualPrevious = newNode;
        newNode.ActualNext = _head;
        _head = newNode.ActualNext;
        _tail ??= _head;
        Count++;

        return newNode;
    }

    public IAeroxLinkedListNode<T> InsertBack(T data)
    {
        var newNode = new AeroxLinkedListNode<T>(data,this);
        
        if (_tail != null) _tail.ActualNext = newNode;

        newNode.ActualPrevious = _tail;
        _tail = newNode;
        _head ??= _tail;
        Count++;

        return newNode;
    }
    public IEnumerator<IAeroxLinkedListNode<T>> GetEnumerator()
    {
        
        return new AeroxLinkedListEnumerator<T>(_head);
    }
    
    public IEnumerator<IAeroxLinkedListNode<T>> GetReverseEnumerator()
    {
        return new AeroxLinkedListEnumerator<T>(_tail,false);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void NotifyNodeRemoved(AeroxLinkedListNode<T> node)
    {
        
        if (_head == node)
        {
            _head = node.ActualNext;
        }
        else if(_tail == node)
        {
            _tail = node.ActualPrevious;
        }
        
        
        node.ActualNext = null;
        node.ActualPrevious = null;

        Count--;
    }

    public void Clear()
    {
        while (Front != null)
        {
            Front.Remove();
        }
    }

    ~AeroxLinkedList()
    {
        Clear();
    }
}