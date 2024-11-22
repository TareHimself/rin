namespace rin.Core.Animation;

public class TestAnimation(double duration,string id) : IAnimation
{
    public AnimationRunner AnimationRunner { get; init; } = new AnimationRunner();
    public double Duration => duration;
    public void Update(double elapsed)
    {
        Console.WriteLine($"This is from a test animation => {id} => {Duration - elapsed}");
    }

    public IAnimation? Next { get; set; }
    
    public void Start(double elapsed)
    {
        Update(elapsed);
    }
}