using System.Numerics;
using Rin.Engine.Core.Math;

namespace Rin.Engine.Views.Utilities;

public class HitTestHandler
{
    private readonly List<IHitTestable> _items = [];
    
    public void Add(IHitTestable item)
    {
        _items.Add(item);
    }

    public void Remove(IHitTestable item)
    {
        _items.Remove(item);
    }

    public void TryHitTest(Vector2 position, Mat3 transform)
    {
        List<IHitTestable> itemsHit = [];
        
        foreach (var item in _items)
        {
            var finalTransform = item.GetRelativeTransform() * transform;
            if (Rect.PointWithin(item.GetRelativeSize(), finalTransform, position))
            {
                itemsHit.Add(item);
            }
        }
        
        foreach (var item in itemsHit)
        {
            item.OnHit();
        }
    }
}