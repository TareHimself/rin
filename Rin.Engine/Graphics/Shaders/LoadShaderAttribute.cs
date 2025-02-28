namespace Rin.Engine.Graphics.Shaders;

[AttributeUsage(AttributeTargets.Property)]
public class LoadShaderAttribute(string path) : Attribute
{
}