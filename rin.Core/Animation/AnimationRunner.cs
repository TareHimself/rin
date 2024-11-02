namespace rin.Core.Animation;

public class AnimationRunner
{
    private readonly HashSet<ActiveAnimation> _animations = [];
    private readonly Dictionary<ActiveAnimation,double> _startTimes = [];


    public void Update()
    {
        var now = SRuntime.Get().GetTimeSeconds();

        _animations.RemoveWhere(c =>
        {
            if (!c.Update(_startTimes[c], now)) return false;
            _startTimes.Remove(c);
            return true;
        });
    }


    public IAnimation Add(IAnimation animation)
    {
        var key = new ActiveAnimation(animation);
        _animations.Add(key);
        _startTimes.Add(key,SRuntime.Get().GetTimeSeconds());
        return animation;
    }

    public void StopAll()
    {
        _animations.Clear();
        _startTimes.Clear();
    }
}