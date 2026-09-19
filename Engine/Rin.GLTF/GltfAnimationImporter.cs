using Rin.World.Mesh.Skinning;
using SharpGLTF.Schema2;

namespace Rin.GLTF;

public static class GltfAnimationImporter
{
    public static IReadOnlyDictionary<string, AnimationClip> LoadAnimationClips(string filename)
    {
        var model = ModelRoot.Load(filename);
        var clips = new Dictionary<string, AnimationClip>();

        foreach (var animation in model.LogicalAnimations)
        {
            var clip = new AnimationClip { Duration = animation.Duration };

            foreach (var channel in animation.Channels)
            {
                if (channel.TargetNode is not { } node) continue;
                var curve = clip.GetOrCreate(node.Name);

                switch (channel.TargetNodePath)
                {
                    case PropertyPath.translation:
                        ImportSampler(channel.GetTranslationSampler(),
                            (t, v) => curve.AddPositionLinear(t, v), (t, v) => curve.AddPositionStep(t, v));
                        break;
                    case PropertyPath.rotation:
                        ImportSampler(channel.GetRotationSampler(),
                            (t, v) => curve.AddRotationLinear(t, v), (t, v) => curve.AddRotationStep(t, v));
                        break;
                    case PropertyPath.scale:
                        ImportSampler(channel.GetScaleSampler(),
                            (t, v) => curve.AddScaleLinear(t, v), (t, v) => curve.AddScaleStep(t, v));
                        break;
                }
            }

            clips[animation.Name ?? $"Animation{clips.Count}"] = clip;
        }

        return clips;
    }

    private static void ImportSampler<T>(IAnimationSampler<T> sampler, Action<float, T> addLinear,
        Action<float, T> addStep) where T : struct
    {
        switch (sampler.InterpolationMode)
        {
            case AnimationInterpolationMode.STEP:
                foreach (var (time, value) in sampler.GetLinearKeys()) addStep(time, value);
                break;
            case AnimationInterpolationMode.CUBICSPLINE:
                // BoneCurve's "cubic" tangent is a scalar ease shape (see AdvancedCurve.HermiteRemap), not
                // glTF's per-axis in/out tangent vectors, so a faithful conversion isn't possible - keep
                // each key's value and drop the tangents, approximating with a linear curve instead.
                foreach (var (time, key) in sampler.GetCubicKeys()) addLinear(time, key.Item2);
                break;
            default:
                foreach (var (time, value) in sampler.GetLinearKeys()) addLinear(time, value);
                break;
        }
    }
}
