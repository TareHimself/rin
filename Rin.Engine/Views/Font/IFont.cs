namespace Rin.Engine.Views.Font;

public interface IFont
{
    public string Name { get; }

    public float GetLineHeight(float fontSize);
}