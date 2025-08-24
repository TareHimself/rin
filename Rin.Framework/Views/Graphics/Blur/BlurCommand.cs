using System.Numerics;
using Rin.Framework.Math;
using Rin.Framework.Views.Graphics.CommandHandlers;
using Rin.Framework.Graphics;
using Rin.Framework.Views.Graphics.Commands;
using Rin.Framework.Views.Graphics.PassConfigs;

namespace Rin.Framework.Views.Graphics.Blur;

internal class BlurInitCommand : TCommand<BlurInitPassConfig,BlurInitCommandHandler>
{
    public uint FirstPassImageId;
    public uint SecondPassImageId;
    // The transform in Surface Space
    public Matrix4x4 Transform;
    // The transform in the AABB Rect Space
    public Matrix4x4 LocalTransform;
    public readonly Vector2 Radius;
    public Vector2 BlurRadius;
    public readonly float Strength;
    public Vector2 Size;
    public Vector4 Tint;
    public Vector2 BoundingBoxP1;
    public Vector2 BoundingBoxP2;
    public Vector2 BlurP1;
    public Vector2 BlurP2;
    // The projection in the AABB rect space
    public Matrix4x4 LocalProjection = Matrix4x4.Identity; 

    public BlurInitCommand(in Matrix4x4 transform,float strength,float radius,in Vector2 size,in Vector2 surfaceSize, in Vector4 tint)
    {
        Radius = BlurRadius = new Vector2(radius);
        Strength = strength;
        Size = size;
        Tint = tint;
        var boundingBox = new Rect(Vector2.Zero,size,transform).Clamp(new Rect(Vector2.Zero,surfaceSize));
        
        // Need to do pixel rounding for regular bounding box
        BoundingBoxP1 = boundingBox.Offset;
        BoundingBoxP2 = BoundingBoxP1 + boundingBox.Size;
        BoundingBoxP1 = BlurP1 = BoundingBoxP1.Floor();
        BoundingBoxP2 = BlurP2 = BoundingBoxP2.Ceiling();
        Transform = transform;
        LocalTransform =  Matrix4x4.Identity.Translate(boundingBox.Offset).Inverse() * transform;
    }
}


internal class BlurFirstPassCommand(BlurInitCommand initCommand) : TCommand<BlurPassConfig,BlurFirstPassCommandHandler>
{
    public BlurInitCommand InitCommand = initCommand;
}

internal class BlurSecondPassCommand(BlurInitCommand initCommand) : TCommand<MainPassConfig,BlurSecondPassCommandHandler>{
    
    public BlurInitCommand InitCommand = initCommand;
}

public static class BlurPassExtensions
{
    public static CommandList AddBlur(this CommandList self, in Matrix4x4 transform, in Vector2 size,
        float strength = 5.0f,
        float radius = 5.0f, Vector4? tint = null)
    {
        var initCommand = new BlurInitCommand(transform,strength, radius,size,self.SurfaceSize ,tint.GetValueOrDefault(Vector4.One));
        self.Add(initCommand);
        self.Add(new BlurFirstPassCommand(initCommand));
        self.Add(new NoOpCommand());
        self.Add(new BlurSecondPassCommand(initCommand));
        return self;
    }
}