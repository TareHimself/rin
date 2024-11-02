namespace rin.Core.Animation;

public interface IAnimation
{
    public double TotalDuration => Duration + (Next?.TotalDuration ?? 0.0);
    
    public double Duration { get; }
    

    public bool Update(double start, double current)
    {
        var elapsed = current - start;
        
        if (elapsed > Duration)
        {
            DoUpdate(start,start + Duration);
            return true;
        }
        
        DoUpdate(start,current);
        return false;
    }
    
    public abstract void DoUpdate(double start, double current);
    
    public IAnimation? Next { get; set; }
}