namespace Rin.Engine.World.Components;

[AttributeUsage(AttributeTargets.Class)]
public class ComponentAttribute(params Type[] required) : Attribute
{
    public Type[] RequiredComponents = required;
}