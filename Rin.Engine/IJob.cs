﻿namespace Rin.Engine;

public interface IJob : IDisposable
{
    public bool CanMergeWith(IJob other);
    public void Merge(IJob other);
    public void Run();
}