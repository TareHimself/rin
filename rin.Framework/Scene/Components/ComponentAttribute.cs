namespace rin.Framework.Scene.Components;

[AttributeUsage(AttributeTargets.Class)]
public class ComponentAttribute(params Type[] required) : Attribute
{
    public Type[] RequiredComponents = required;
}