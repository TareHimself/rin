namespace rin.Core.Animation;

public class TestAnimation(double duration,string id) : IAnimation
{
    public AnimationRunner AnimationRunner { get; init; } = new AnimationRunner();
    public double Duration => duration;
    public void DoUpdate(double start, double current)
    {
        Console.WriteLine($"This is from a test animation => {id} => {Duration - (current - start)}");
    }

    public IAnimation? Next { get; set; }
}